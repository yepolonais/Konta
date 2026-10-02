namespace Konta.Application.Imports;

public sealed record ImportPreview(
    IReadOnlyList<ImportedTransaction> Transactions,
    IReadOnlyList<CsvRowError> Errors,
    IReadOnlyList<UnmappedAccount> UnmappedAccounts);