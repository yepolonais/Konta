using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Konta.Application.Imports;

public static class ImportFingerprint
{
    public static string Create(ImportedTransaction transaction)
    {
        var normalizedLabel = string.Join(' ', transaction.FullLabel
            .Normalize(NormalizationForm.FormKC)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
        var canonicalValue = string.Join('\n',
            transaction.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            transaction.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            normalizedLabel,
            transaction.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            transaction.Type.ToString());

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalValue)));
    }
}