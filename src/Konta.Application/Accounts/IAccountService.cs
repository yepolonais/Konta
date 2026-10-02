using Konta.Domain.Accounts;

namespace Konta.Application.Accounts;

public interface IAccountService
{
    Task<IReadOnlyList<AccountListItem>> ListAsync(CancellationToken cancellationToken);
    Task<AccountListItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AccountListItem> CreateAsync(
        string name,
        string bankAccountNumber,
        string bankLabel,
        AccountKind kind,
        CancellationToken cancellationToken);
}