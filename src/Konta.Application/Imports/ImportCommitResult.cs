namespace Konta.Application.Imports;

public sealed record ImportCommitResult(
    int ImportedCount,
    int DuplicateCount,
    IReadOnlyList<CsvRowError> Errors,
    int ExcludedCount = 0,
    bool BalanceApplied = false);