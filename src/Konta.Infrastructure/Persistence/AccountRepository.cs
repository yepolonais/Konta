using Konta.Application.Accounts;
using Konta.Domain.Accounts;
using Konta.Domain.Transactions;
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

    public Task<AccountBalanceReference?> GetBalanceReferenceAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.AccountBalanceReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(reference => reference.AccountId == accountId, cancellationToken);

    public async Task SetBalanceReferenceAsync(
        Guid accountId,
        Guid transactionId,
        decimal balanceAfterTransaction,
        CancellationToken cancellationToken)
    {
        var reference = await dbContext.AccountBalanceReferences
            .FirstOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (reference is null)
        {
            dbContext.AccountBalanceReferences.Add(new AccountBalanceReference(
                accountId,
                transactionId,
                balanceAfterTransaction));
        }
        else
        {
            reference.Update(transactionId, balanceAfterTransaction);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .ToListAsync(cancellationToken);
}
