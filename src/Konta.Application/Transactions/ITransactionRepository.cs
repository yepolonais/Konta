using Konta.Domain.Transactions;

namespace Konta.Application.Transactions;

public interface ITransactionRepository
{
    Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionListItem>> ListForPeriodAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId,
        Guid? categoryId,
        CancellationToken cancellationToken);
}