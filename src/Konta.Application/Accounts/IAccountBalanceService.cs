namespace Konta.Application.Accounts;

public interface IAccountBalanceService
{
    Task<AccountBalanceSummary> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken);
    Task<AccountBalanceSummary> SetReferenceBalanceAsync(
        Guid accountId,
        Guid transactionId,
        decimal balanceAfterTransaction,
        CancellationToken cancellationToken);
}
