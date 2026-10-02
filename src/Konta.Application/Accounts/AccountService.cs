using Konta.Domain.Accounts;

namespace Konta.Application.Accounts;

public sealed class AccountService(IAccountRepository repository) : IAccountService
{
    public async Task<IReadOnlyList<AccountListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var accounts = await repository.ListAsync(cancellationToken);
        return accounts.Select(ToListItem).ToArray();
    }

    public async Task<AccountListItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await repository.GetAsync(id, cancellationToken);
        return account is null ? null : ToListItem(account);
    }

    public async Task<AccountListItem> CreateAsync(
        string name,
        string bankAccountNumber,
        string bankLabel,
        AccountKind kind,
        CancellationToken cancellationToken)
    {
        var normalizedNumber = bankAccountNumber.Trim();
        if (await repository.ExistsByBankAccountNumberAsync(normalizedNumber, cancellationToken))
        {
            throw new DuplicateAccountException(normalizedNumber);
        }

        var account = new Account(name, normalizedNumber, bankLabel, kind);
        await repository.AddAsync(account, cancellationToken);
        return ToListItem(account);
    }

    private static AccountListItem ToListItem(Account account) => new(
        account.Id,
        account.Name,
        account.BankAccountNumber,
        account.BankLabel,
        account.Kind,
        account.IsActive);
}