namespace Konta.Domain.Accounts;

public sealed class Account
{
    private Account()
    {
    }

    public Account(string name, string bankAccountNumber, string bankLabel, AccountKind kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An account name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(bankAccountNumber))
        {
            throw new ArgumentException("A bank account number is required.", nameof(bankAccountNumber));
        }

        if (string.IsNullOrWhiteSpace(bankLabel))
        {
            throw new ArgumentException("A bank account label is required.", nameof(bankLabel));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        BankAccountNumber = bankAccountNumber.Trim();
        BankLabel = bankLabel.Trim();
        Kind = kind;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string BankAccountNumber { get; private set; } = string.Empty;
    public string BankLabel { get; private set; } = string.Empty;
    public AccountKind Kind { get; private set; }
    public bool IsActive { get; private set; }
}