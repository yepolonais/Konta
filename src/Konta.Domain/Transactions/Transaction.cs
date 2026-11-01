using Konta.Domain.Categories;
using Konta.Domain.Accounts;

namespace Konta.Domain.Transactions;

public sealed class Transaction
{
    private Transaction()
    {
    }

    public Transaction(
        DateOnly date,
        string label,
        decimal amount,
        TransactionType type,
        string? accountNumber = null,
        string? accountLabel = null,
        Guid? categoryId = null,
        TransactionSource source = TransactionSource.Manual,
        Guid? accountId = null,
        string? importFingerprint = null,
        int? importOccurrence = null)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("A transaction label is required.", nameof(label));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The amount must be positive.");
        }

        Id = Guid.NewGuid();
        Date = date;
        Label = label.Trim();
        Amount = amount;
        Type = type;
        AccountNumber = accountNumber;
        AccountLabel = accountLabel;
        AccountId = accountId;
        ImportFingerprint = importFingerprint;
        ImportOccurrence = importOccurrence;
        CategoryId = categoryId;
        Source = source;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string? AccountNumber { get; private set; }
    public string? AccountLabel { get; private set; }
    public Guid? AccountId { get; private set; }
    public Account? Account { get; private set; }
    public string? ImportFingerprint { get; private set; }
    public int? ImportOccurrence { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public TransactionSource Source { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}