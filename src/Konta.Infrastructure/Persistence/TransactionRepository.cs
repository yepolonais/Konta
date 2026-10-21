using Konta.Application.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class TransactionRepository(KontaDbContext dbContext) : ITransactionRepository
{
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
                transaction.SubcategoryName,
                transaction.AccountId,
                transaction.CategoryId,
                dbContext.AccountTransfers.Any(transfer =>
                    transfer.SourceTransactionId == transaction.Id
                    || transfer.DestinationTransactionId == transaction.Id)))
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
                transaction.SubcategoryName,
                transaction.AccountId,
                transaction.CategoryId,
                dbContext.AccountTransfers.Any(transfer =>
                    transfer.SourceTransactionId == transaction.Id
                    || transfer.DestinationTransactionId == transaction.Id)))
            .ToListAsync(cancellationToken);
    }
}