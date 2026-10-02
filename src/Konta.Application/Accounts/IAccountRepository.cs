using Konta.Domain.Accounts;

namespace Konta.Application.Accounts;

public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);
    Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, Account>> FindByBankAccountNumbersAsync(
        IReadOnlyCollection<string> accountNumbers,
        CancellationToken cancellationToken);
    Task<bool> ExistsByBankAccountNumberAsync(string accountNumber, CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
}