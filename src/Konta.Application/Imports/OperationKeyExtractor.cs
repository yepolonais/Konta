using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Konta.Domain.Categories;

namespace Konta.Application.Imports;

public static partial class OperationKeyExtractor
{
    private const string BankFeeKey = "COMMISSION D'INTERVENTION";
    private const string DebitInterestKey = "INTERETS DEBITEURS";

    // L'ordre compte : les types les plus spécifiques sont testés avant les plus génériques.
    public static OperationKey? Extract(string? fullLabel)
    {
        var text = CollapseWhitespace(fullLabel);
        if (text.Length == 0)
        {
            return null;
        }

        if (text.StartsWith(BankFeeKey, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationKey(OperationType.BankFee, BankFeeKey);
        }

        if (text.StartsWith(DebitInterestKey, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationKey(OperationType.BankFee, DebitInterestKey);
        }

        var loan = LoanRegex().Match(text);
        if (loan.Success)
        {
            return new OperationKey(OperationType.Loan, $"ECHEANCE PRET N°{loan.Groups[1].Value}");
        }

        var card = CardRegex().Match(text);
        if (card.Success)
        {
            return Build(OperationType.Card, CleanMerchant(card.Groups[1].Value));
        }

        if (ReceivedTransferRegex().IsMatch(text))
        {
            return Build(OperationType.TransferReceived, Field(text, "DE:", ["MOTIF:", "REF:"]));
        }

        if (SentTransferRegex().IsMatch(text))
        {
            var beneficiary = Field(text, "POUR:", ["REF:"]);
            return Build(OperationType.TransferSent, EmbeddedDateRegex().Replace(beneficiary, string.Empty));
        }

        if (DirectDebitRegex().IsMatch(text))
        {
            return Build(OperationType.DirectDebit, Field(text, "DE:", ["ID:", "MOTIF:", "REF:"]));
        }

        return null;
    }

    // Insensible à la casse, aux accents et aux espaces multiples.
    public static string Normalize(string? value)
    {
        var decomposed = CollapseWhitespace(value).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static OperationKey? Build(OperationType type, string key) =>
        key.Length == 0 ? null : new OperationKey(type, key);

    private static string CleanMerchant(string merchant)
    {
        var withoutReference = TrailingReferenceRegex().Replace(merchant, string.Empty);
        return CollapseWhitespace(ForeignAmountRegex().Replace(withoutReference, string.Empty));
    }

    private static string Field(string text, string marker, string[] terminators)
    {
        var start = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return string.Empty;
        }

        var value = text[(start + marker.Length)..];
        var end = terminators
            .Select(terminator => value.IndexOf(terminator, StringComparison.OrdinalIgnoreCase))
            .Where(index => index >= 0)
            .DefaultIfEmpty(value.Length)
            .Min();
        return value[..end].Trim();
    }

    private static string CollapseWhitespace(string? value) =>
        WhitespaceRegex().Replace(value ?? string.Empty, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^ECHEANCE PRET N°\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex LoanRegex();

    [GeneratedRegex(@"^CARTE X\d+ (?:REMBT )?(?!RETRAIT)\d{2}/\d{2} (.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CardRegex();

    [GeneratedRegex(@"^VIR RECU\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReceivedTransferRegex();

    [GeneratedRegex(@"^(\d+ )?VIR\b.*\bEMIS\b", RegexOptions.IgnoreCase)]
    private static partial Regex SentTransferRegex();

    [GeneratedRegex(@"^(?:PRELEVEMENT EUROPEEN|PRLV EUROP PONCTUEL)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DirectDebitRegex();

    [GeneratedRegex(@"\s\d{2} \d{2}(?=\s+[A-Z]{2}\s+\d)", RegexOptions.IgnoreCase)]
    private static partial Regex EmbeddedDateRegex();

    [GeneratedRegex(@"\s*\b\d{9,}[A-Z]*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingReferenceRegex();

    [GeneratedRegex(@"\s\d+[.,]\d{2} [A-Z]{3}\b")]
    private static partial Regex ForeignAmountRegex();
}
