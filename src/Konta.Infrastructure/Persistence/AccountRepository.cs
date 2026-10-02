using Konta.Application.Accounts;
using Konta.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class AccountRepository(KontaDbContext dbContext) : IAccountRepository
{
    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);

    public Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<string, Account>> FindByBankAccountNumbersAsync(
        IReadOnlyCollection<string> accountNumbers,
        CancellationToken cancellationToken)
    {
        if (accountNumbers.Count == 0)
        {
            return new Dictionary<string, Account>(StringComparer.Ordinal);
        }

        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accountNumbers.Contains(account.BankAccountNumber))
            .ToListAsync(cancellationToken);
        return accounts.ToDictionary(account => account.BankAccountNumber, StringComparer.Ordinal);
    }

    public Task<bool> ExistsByBankAccountNumberAsync(string accountNumber, CancellationToken cancellationToken) =>
        dbContext.Accounts.AnyAsync(account => account.BankAccountNumber == accountNumber, cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}