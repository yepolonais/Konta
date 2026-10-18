namespace Konta.Domain.Accounts;

public sealed class AccountBalanceReference
{
    private AccountBalanceReference()
    {
    }

    public AccountBalanceReference(Guid accountId, Guid transactionId, decimal balanceAfterTransaction)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        TransactionId = transactionId;
        BalanceAfterTransaction = balanceAfterTransaction;
    }

    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal BalanceAfterTransaction { get; private set; }

    public void Update(Guid transactionId, decimal balanceAfterTransaction)
    {
        TransactionId = transactionId;
        BalanceAfterTransaction = balanceAfterTransaction;
    }
}
