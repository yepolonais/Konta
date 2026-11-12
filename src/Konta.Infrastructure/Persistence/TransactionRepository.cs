using Konta.Application.Transactions;
using Konta.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class TransactionRepository(KontaDbContext dbContext) : ITransactionRepository
{
    public Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Transactions.FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        dbContext.Transactions.Update(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Guid?> GetCategoryBudgetPostIdAsync(Guid categoryId, CancellationToken cancellationToken) =>
        dbContext.BudgetPostCategoryMappings
            .Where(mapping => mapping.CategoryId == categoryId)
            .Select(mapping => (Guid?)mapping.AnnualBudgetPostId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> IsTransferAsync(Guid transactionId, CancellationToken cancellationToken) =>
        dbContext.AccountTransfers.AnyAsync(transfer =>
            transfer.SourceTransactionId == transactionId || transfer.DestinationTransactionId == transactionId,
            cancellationToken);

    public async Task<IReadOnlySet<Guid>> ListAssignedBudgetPostIdsAsync(CancellationToken cancellationToken) =>
        (await dbContext.Transactions.AsNoTracking()
            .Where(transaction => transaction.BudgetPostId.HasValue)
            .Select(transaction => transaction.BudgetPostId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

    public async Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Label)
            .Select(transaction => new TransactionListItem(
                transaction.Id,
                transaction.Date,
                transaction.Label,
                transaction.Amount,
                transaction.Type,
                transaction.AccountNumber,
                transaction.AccountLabel,
                transaction.Account == null ? null : transaction.Account.Name,
                transaction.Category == null ? null : transaction.Category.Name,
                transaction.AccountId,
                transaction.CategoryId,
                dbContext.AccountTransfers.Any(transfer =>
                    transfer.SourceTransactionId == transaction.Id
                    || transfer.DestinationTransactionId == transaction.Id),
                transaction.Details,
                transaction.BudgetPostId,
                transaction.BudgetDescription))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionListItem>> ListForPeriodAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Date >= startDate && transaction.Date < endDate);
        if (accountId.HasValue)
        {
            query = query.Where(transaction => transaction.AccountId == accountId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == categoryId.Value);
        }

        return await query
            .OrderByDescending(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Label)
            .Select(transaction => new TransactionListItem(
                transaction.Id,
                transaction.Date,
                transaction.Label,
                transaction.Amount,
                transaction.Type,
                transaction.AccountNumber,
                transaction.AccountLabel,
                transaction.Account == null ? null : transaction.Account.Name,
                transaction.Category == null ? null : transaction.Category.Name,
                transaction.AccountId,
                transaction.CategoryId,
                dbContext.AccountTransfers.Any(transfer =>
                    transfer.SourceTransactionId == transaction.Id
                    || transfer.DestinationTransactionId == transaction.Id),
                transaction.Details,
                transaction.BudgetPostId,
                transaction.BudgetDescription))
            .ToListAsync(cancellationToken);
    }
}