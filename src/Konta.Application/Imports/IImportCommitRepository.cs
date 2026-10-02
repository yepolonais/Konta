namespace Konta.Application.Imports;

public interface IImportCommitRepository
{
    Task<IReadOnlySet<int>> FindDuplicateRowNumbersAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken);
    Task<(int ImportedCount, int DuplicateCount)> SaveAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken);
}