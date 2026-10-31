using Konta.Domain.Accounts;
using Konta.Domain.Transactions;

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
    Task<AccountBalanceReference?> GetBalanceReferenceAsync(Guid accountId, CancellationToken cancellationToken);
    Task SetBalanceReferenceAsync(
        Guid accountId,
        Guid transactionId,
        decimal balanceAfterTransaction,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Transaction>> ListTransactionsAsync(Guid accountId, CancellationToken cancellationToken);
}