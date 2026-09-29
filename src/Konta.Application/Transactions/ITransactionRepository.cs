namespace Konta.Application.Transactions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken);
}