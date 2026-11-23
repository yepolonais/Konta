using Konta.Domain.Transactions;

namespace Konta.Application.Transactions;

public interface ITransactionRepository
{
    Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken);
    Task<Guid?> GetCategoryBudgetPostIdAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<bool> IsTransferAsync(Guid transactionId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> ListAssignedBudgetPostIdsAsync(Guid accountId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionListItem>> ListForBudgetMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionListItem>> ListForPeriodAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId,
        Guid? categoryId,
        CancellationToken cancellationToken);
}