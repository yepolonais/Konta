namespace Konta.Application.Imports;

public interface IImportCommitRepository
{
    Task<IReadOnlySet<int>> FindDuplicateRowNumbersAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<TransferMatchCandidate>> FindTransferMatchCandidatesAsync(
        Guid accountId,
        DateOnly date,
        decimal amount,
        Konta.Domain.Transactions.TransactionType type,
        CancellationToken cancellationToken);
    Task<(int ImportedCount, int DuplicateCount)> SaveAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken);
}