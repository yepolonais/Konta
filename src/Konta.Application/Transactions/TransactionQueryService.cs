namespace Konta.Application.Transactions;

public sealed class TransactionQueryService(ITransactionRepository repository)
{
    public Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);
}