namespace Konta.Application.Transactions;

public interface ITransactionQueryService
{
    Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken);
}