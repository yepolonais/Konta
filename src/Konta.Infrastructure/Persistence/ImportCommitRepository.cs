using Konta.Application.Imports;
using Konta.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class ImportCommitRepository(KontaDbContext dbContext) : IImportCommitRepository
{
    public async Task<IReadOnlySet<int>> FindDuplicateRowNumbersAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var importRows = transactions
            .Where(transaction => transaction.AccountId.HasValue && transaction.Fingerprint is not null)
            .ToArray();
        var existing = await GetExistingOccurrencesAsync(importRows, cancellationToken);
        var duplicateRows = new HashSet<int>();

        foreach (var group in importRows.GroupBy(transaction =>
                     (transaction.AccountId!.Value, transaction.Fingerprint!)))
        {
            var existingCount = existing.Count(item =>
                item.AccountId == group.Key.Value && item.Fingerprint == group.Key.Item2);
            foreach (var transaction in group.OrderBy(transaction => transaction.RowNumber).Take(existingCount))
            {
                duplicateRows.Add(transaction.RowNumber);
            }
        }

        return duplicateRows;
    }

    public async Task<(int ImportedCount, int DuplicateCount)> SaveAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var importRows = transactions
            .Where(transaction => transaction.AccountId.HasValue && transaction.Fingerprint is not null)
            .ToArray();
        await using var databaseTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await GetExistingOccurrencesAsync(importRows, cancellationToken);
        var newTransactions = new List<Transaction>();

        foreach (var group in importRows.GroupBy(transaction =>
                     (transaction.AccountId!.Value, transaction.Fingerprint!)))
        {
            var matchingOccurrences = existing
                .Where(item => item.AccountId == group.Key.Value && item.Fingerprint == group.Key.Item2)
                .ToArray();
            var existingCount = matchingOccurrences.Length;
            var nextOccurrence = matchingOccurrences
                .Select(item => item.Occurrence ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            foreach (var transaction in group.OrderBy(transaction => transaction.RowNumber).Skip(existingCount))
            {
                nextOccurrence++;
                newTransactions.Add(new Transaction(
                    transaction.TransactionDate,
                    transaction.Label,
                    transaction.Amount,
                    transaction.Type,
                    transaction.AccountNumber,
                    transaction.AccountLabel,
                    source: TransactionSource.Import,
                    accountId: transaction.AccountId,
                    importFingerprint: transaction.Fingerprint,
                    importOccurrence: nextOccurrence));
            }
        }

        dbContext.Transactions.AddRange(newTransactions);
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        return (newTransactions.Count, importRows.Length - newTransactions.Count);
    }

    private async Task<List<ExistingImportOccurrence>> GetExistingOccurrencesAsync(
        IReadOnlyCollection<ImportedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var accountIds = transactions
            .Where(transaction => transaction.AccountId.HasValue)
            .Select(transaction => transaction.AccountId!.Value)
            .Distinct()
            .ToArray();
        var fingerprints = transactions
            .Where(transaction => transaction.Fingerprint is not null)
            .Select(transaction => transaction.Fingerprint!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (accountIds.Length == 0 || fingerprints.Length == 0)
        {
            return [];
        }

        return await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId.HasValue
                && accountIds.Contains(transaction.AccountId.Value)
                && transaction.ImportFingerprint != null
                && fingerprints.Contains(transaction.ImportFingerprint))
            .Select(transaction => new ExistingImportOccurrence(
                transaction.AccountId!.Value,
                transaction.ImportFingerprint!,
                transaction.ImportOccurrence))
            .ToListAsync(cancellationToken);
    }

    private sealed record ExistingImportOccurrence(Guid AccountId, string Fingerprint, int? Occurrence);
}