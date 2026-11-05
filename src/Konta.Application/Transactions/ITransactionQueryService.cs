namespace Konta.Application.Transactions;

public interface ITransactionQueryService
{
    Task<bool> ChangeCategoryAsync(Guid transactionId, Guid categoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken);
    Task<TransactionPeriodSummary> GetPeriodSummaryAsync(
        int year,
        int? month,
        Guid? accountId,
        Guid? categoryId,
        CancellationToken cancellationToken);
}