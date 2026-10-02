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
        ["souscategorie"] = "Sous-Catégorie",
        ["montant"] = "Montant",
        ["pointee"] = "Pointée"
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

        var headerIndexes = ReadHeaderIndexes(csv);
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
                    EmptyAsNull(GetField(csv, headerIndexes, "souscategorie")),
                    Math.Abs(amount),
                    amount < 0 ? TransactionType.Expense : TransactionType.Income,
                    string.Equals(GetField(csv, headerIndexes, "pointee"), "oui", StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception exception) when (exception is FormatException or CsvHelperException or IndexOutOfRangeException)
            {
                errors.Add(new CsvRowError(csv.Context.Parser?.Row ?? 0, exception.Message));
            }
        }

        return new ImportPreview(transactions, errors, []);
    }

    private static Dictionary<string, int>? ReadHeaderIndexes(CsvReader csv)
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

        if (RequiredHeaders.Keys.Any(header => !normalizedHeaders.ContainsKey(header)))
        {
            return null;
        }

        return RequiredHeaders.Keys.ToDictionary(header => header, header => normalizedHeaders[header]);
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
}