namespace Konta.Application.Imports;

public sealed class UnmappedAccountsException(IReadOnlyList<UnmappedAccount> accounts)
    : Exception("Un ou plusieurs comptes du relevé ne sont pas encore associés.")
{
    public IReadOnlyList<UnmappedAccount> Accounts { get; } = accounts;
}

public sealed record UnmappedAccount(string AccountNumber, string BankLabel);