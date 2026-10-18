using Konta.Domain.Transactions;

namespace Konta.Domain.Transfers;

public sealed class AccountTransfer
{
    private AccountTransfer()
    {
    }

    public AccountTransfer(Guid sourceTransactionId, Guid destinationTransactionId, decimal amount)
    {
        if (sourceTransactionId == Guid.Empty || destinationTransactionId == Guid.Empty
            || sourceTransactionId == destinationTransactionId)
        {
            throw new ArgumentException("A transfer must link two different transactions.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "A transfer amount must be positive.");
        }

        Id = Guid.NewGuid();
        SourceTransactionId = sourceTransactionId;
        DestinationTransactionId = destinationTransactionId;
        Amount = amount;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SourceTransactionId { get; private set; }
    public Guid DestinationTransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Transaction SourceTransaction { get; private set; } = null!;
    public Transaction DestinationTransaction { get; private set; } = null!;
}
