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
                transaction.Category == null ? null : transaction.Category.Name))
            .ToListAsync(cancellationToken);
    }
}