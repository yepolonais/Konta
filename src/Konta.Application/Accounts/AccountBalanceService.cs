using Konta.Domain.Transactions;

namespace Konta.Application.Accounts;

public sealed class AccountBalanceService(IAccountRepository repository, TimeProvider clock) : IAccountBalanceService
{
    public async Task<AccountBalanceSummary> SetReferenceBalanceAsync(
        Guid accountId,
        Guid transactionId,
        decimal balanceAfterTransaction,
        CancellationToken cancellationToken)
    {
        _ = await repository.GetAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException("Le compte n'existe pas.");
        var transactions = await repository.ListTransactionsAsync(accountId, cancellationToken);
        if (!transactions.Any(transaction => transaction.Id == transactionId))
        {
            throw new ArgumentException("L'opération de référence doit appartenir au compte sélectionné.", nameof(transactionId));
        }

        await repository.SetBalanceReferenceAsync(accountId, transactionId, balanceAfterTransaction, cancellationToken);
        return await GetBalanceAsync(accountId, cancellationToken);
    }

    public async Task<AccountBalanceSummary> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await repository.GetAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException("Le compte n'existe pas.");
        var reference = await repository.GetBalanceReferenceAsync(accountId, cancellationToken);
        if (reference is null)
        {
            return new AccountBalanceSummary(account.Id, account.Name, null, null, null, null);
        }

        var transactions = await repository.ListTransactionsAsync(accountId, cancellationToken);
        var referenceTransaction = transactions.Single(transaction => transaction.Id == reference.TransactionId);
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var actual = CalculateBalance(reference.BalanceAfterTransaction, referenceTransaction, transactions, today);

        return new AccountBalanceSummary(
            account.Id,
            account.Name,
            reference.TransactionId,
            referenceTransaction.Date,
            reference.BalanceAfterTransaction,
            actual);
    }

    internal static decimal CalculateBalance(
        decimal referenceBalance, Transaction reference, IReadOnlyList<Transaction> transactions, DateOnly date) =>
        referenceBalance
        + transactions.Where(transaction => IsAfterReference(transaction, reference) && transaction.Date <= date).Sum(SignedAmount)
        - transactions.Where(transaction => !IsAfterReference(transaction, reference) && transaction.Date > date).Sum(SignedAmount);

    private static bool IsAfterReference(Transaction transaction, Transaction reference) =>
        transaction.Date > reference.Date
        || (transaction.Date == reference.Date
            && (transaction.CreatedAt > reference.CreatedAt
                || (transaction.CreatedAt == reference.CreatedAt && transaction.Id.CompareTo(reference.Id) > 0)));

    private static decimal SignedAmount(Transaction transaction) =>
        transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;
}
