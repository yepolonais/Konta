namespace Konta.Application.Transactions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionListItem>> ListForPeriodAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId,
        Guid? categoryId,
        CancellationToken cancellationToken);
}