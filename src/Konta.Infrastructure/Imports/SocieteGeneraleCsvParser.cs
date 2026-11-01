using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Konta.Application.Imports;
using Konta.Domain.Transactions;

namespace Konta.Infrastructure.Imports;

public sealed class SocieteGeneraleCsvParser : ITransactionCsvParser
{
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

    private static readonly IReadOnlyDictionary<string, string> RequiredHeaders = new Dictionary<string, string>
    {
        ["datetransaction"] = "Date transaction",
        ["datecomptabilisation"] = "Date comptabilisation",
        ["numcompte"] = "Num Compte",
        ["libellecompte"] = "Libellé Compte",
        ["libelleoperation"] = "Libellé opération",
        ["libellecomplet"] = "Libellé complet",
        ["categorie"] = "Catégorie",
        ["montant"] = "Montant",
        ["pointee"] = "Pointée"
    };

    private static readonly IReadOnlyDictionary<string, string> SummaryFormatHeaders = new Dictionary<string, string>
    {
        ["date"] = "Date de l'opération",
        ["libelle"] = "Libellé",
        ["detail"] = "Détail de l'écriture",
        ["montant"] = "Montant de l'opération"
    };

    public ImportPreview Parse(Stream csvStream)
    {
        var transactions = new List<ImportedTransaction>();
        var errors = new List<CsvRowError>();

        using var reader = new StreamReader(csvStream, Encoding.Latin1, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(FrenchCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = false,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim
        });

        if (!csv.Read())
        {
            return new ImportPreview(transactions, [new CsvRowError(1, "Le fichier est vide.")], []);
        }

        var statement = ReadStatementSummary(csv);
        if (statement is not null && !csv.Read())
        {
            return new ImportPreview(transactions, [new CsvRowError(2, "En-têtes CSV manquants après la ligne de résumé du compte.")], [], statement);
        }

        if (statement is not null)
        {
            return ParseSummaryFormat(csv, statement);
        }

        var headerIndexes = ReadHeaderIndexes(csv, RequiredHeaders);
        if (headerIndexes is null)
        {
            return new ImportPreview(transactions, [new CsvRowError(1, "En-têtes CSV manquants ou non reconnus. Vérifiez le format Société Générale exporté.")], []);
        }

        while (true)
        {
            try
            {
                if (!csv.Read())
                {
                    break;
                }

                var rowNumber = csv.Context.Parser?.Row ?? 0;
                var transactionDate = ParseDate(GetField(csv, headerIndexes, "datetransaction"));
                var bookingDate = ParseDate(GetField(csv, headerIndexes, "datecomptabilisation"));
                var accountNumber = GetField(csv, headerIndexes, "numcompte");
                var accountLabel = GetField(csv, headerIndexes, "libellecompte");
                var operationLabel = GetField(csv, headerIndexes, "libelleoperation");
                var fullLabel = GetField(csv, headerIndexes, "libellecomplet");
                var amountText = GetField(csv, headerIndexes, "montant");

                if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(accountLabel))
                {
                    throw new FormatException("Le numéro ou le libellé du compte est manquant.");
                }

                var label = string.IsNullOrWhiteSpace(operationLabel) ? fullLabel : operationLabel;
                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new FormatException("Le libellé de l'opération est manquant.");
                }

                var amount = ParseAmount(amountText);
                transactions.Add(new ImportedTransaction(
                    rowNumber,
                    transactionDate,
                    bookingDate,
                    accountNumber,
                    accountLabel,
                    label,
                    fullLabel,
                    EmptyAsNull(GetField(csv, headerIndexes, "categorie")),
                    Math.Abs(amount),
                    amount < 0 ? TransactionType.Expense : TransactionType.Income,
                    string.Equals(GetField(csv, headerIndexes, "pointee"), "oui", StringComparison.OrdinalIgnoreCase),
                    Motif: ExtractMotif(fullLabel)));
            }
            catch (Exception exception) when (exception is FormatException or CsvHelperException or IndexOutOfRangeException or ArgumentException)
            {
                errors.Add(new CsvRowError(csv.Context.Parser?.Row ?? 0, exception.Message));
            }
        }

        return new ImportPreview(transactions, errors, []);
    }

    private static ImportPreview ParseSummaryFormat(CsvReader csv, StatementBalance statement)
    {
        var transactions = new List<ImportedTransaction>();
        var errors = new List<CsvRowError>();
        var headerIndexes = ReadHeaderIndexes(csv, SummaryFormatHeaders);
        if (headerIndexes is null)
        {
            return new ImportPreview(transactions, [new CsvRowError(csv.Context.Parser?.Row ?? 2, "En-têtes CSV manquants ou non reconnus. Vérifiez le format Société Générale exporté.")], [], statement);
        }

        var accountLabel = $"Compte {statement.AccountNumber}";
        while (csv.Read())
        {
            var rowNumber = csv.Context.Parser?.Row ?? 0;
            try
            {
                var date = ParseDate(GetField(csv, headerIndexes, "date"));
                var label = GetField(csv, headerIndexes, "libelle");
                var detail = GetField(csv, headerIndexes, "detail");
                if (string.IsNullOrWhiteSpace(label))
                {
                    label = detail;
                }

                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new FormatException("Le libellé de l'opération est manquant.");
                }

                var amount = ParseAmount(GetField(csv, headerIndexes, "montant"));
                transactions.Add(new ImportedTransaction(
                    rowNumber,
                    date,
                    date,
                    statement.AccountNumber,
                    accountLabel,
                    label,
                    string.IsNullOrWhiteSpace(detail) ? label : detail,
                    null,
                    Math.Abs(amount),
                    amount < 0 ? TransactionType.Expense : TransactionType.Income,
                    false,
                    Motif: ExtractMotif(detail)));
            }
            catch (Exception exception) when (exception is FormatException or CsvHelperException or IndexOutOfRangeException or ArgumentException)
            {
                errors.Add(new CsvRowError(rowNumber, exception.Message));
            }
        }

        return new ImportPreview(transactions, errors, [], statement);
    }

    // Les exports récents commencent par : numéro de compte;début;fin;nombre d'opérations;date du solde;solde EUR
    private static StatementBalance? ReadStatementSummary(CsvReader csv)
    {
        var parser = csv.Context.Parser;
        if (parser is null || parser.Count < 6)
        {
            return null;
        }

        var accountNumber = (csv.GetField(0) ?? string.Empty).Trim().TrimStart('\uFEFF');
        var balanceToken = (csv.GetField(5) ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        if (accountNumber.Length == 0
            || !accountNumber.All(char.IsDigit)
            || !DateOnly.TryParseExact(csv.GetField(4)?.Trim(), "dd/MM/yyyy", FrenchCulture, DateTimeStyles.None, out var balanceDate)
            || !decimal.TryParse(balanceToken.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var balance))
        {
            return null;
        }

        return new StatementBalance(accountNumber, balanceDate, balance);
    }

    private static Dictionary<string, int>? ReadHeaderIndexes(CsvReader csv, IReadOnlyDictionary<string, string> requiredHeaders)
    {
        var normalizedHeaders = new Dictionary<string, int>(StringComparer.Ordinal);
        var parser = csv.Context.Parser;
        if (parser is null)
        {
            return null;
        }

        for (var index = 0; index < parser.Count; index++)
        {
            var header = NormalizeHeader(csv.GetField(index) ?? string.Empty);
            if (header.Length > 0)
            {
                normalizedHeaders.TryAdd(header, index);
            }
        }

        var requiredKeys = requiredHeaders.ToDictionary(header => NormalizeHeader(header.Value), header => header.Key);
        if (requiredKeys.Keys.Any(header => !normalizedHeaders.ContainsKey(header)))
        {
            return null;
        }

        return requiredKeys.ToDictionary(header => header.Value, header => normalizedHeaders[header.Key]);
    }

    private static string GetField(CsvReader csv, IReadOnlyDictionary<string, int> indexes, string header)
    {
        return csv.GetField(indexes[header])?.Trim() ?? string.Empty;
    }

    private static DateOnly ParseDate(string value)
    {
        if (!DateOnly.TryParseExact(value, "dd/MM/yyyy", FrenchCulture, DateTimeStyles.None, out var date))
        {
            throw new FormatException($"Date invalide « {value} » (format attendu : jj/MM/aaaa).");
        }

        return date;
    }

    private static decimal ParseAmount(string value)
    {
        var normalized = value.Replace("\u00a0", string.Empty, StringComparison.Ordinal)
            .Replace("\u202f", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (!decimal.TryParse(normalized, NumberStyles.Number, FrenchCulture, out var amount) || amount == 0)
        {
            throw new FormatException($"Montant invalide « {value} ».");
        }

        return amount;
    }

    private static string NormalizeHeader(string value)
    {
        var decomposed = value.Trim().TrimStart('\uFEFF').Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }

    private static string? EmptyAsNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? ExtractMotif(string fullLabel)
    {
        var motifStart = fullLabel.IndexOf("MOTIF:", StringComparison.OrdinalIgnoreCase);
        if (motifStart < 0)
        {
            return null;
        }

        motifStart += "MOTIF:".Length;
        var referenceStart = fullLabel.IndexOf("REF:", motifStart, StringComparison.OrdinalIgnoreCase);
        var motif = (referenceStart < 0
            ? fullLabel[motifStart..]
            : fullLabel[motifStart..referenceStart]).Trim();
        return string.IsNullOrEmpty(motif) ? null : motif;
    }
}