using Konta.Application.Budgets;
using Konta.Domain.Budgets;

namespace Konta.Application.Accounts;

public sealed class MonthlyProjectionService(
    IAccountRepository accounts,
    IAnnualBudgetService budgets,
    TimeProvider clock) : IMonthlyProjectionService
{
    public async Task<MonthlyProjection?> GetAsync(CancellationToken cancellationToken)
    {
        var account = await accounts.GetCommonAsync(cancellationToken);
        if (account is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var month = await budgets.GetMonthAsOfAsync(today.Year, today.Month, today, cancellationToken);
        var bands = month.Posts
            .Where(post => post.PlannedAmount.HasValue)
            .Select(post => new ProjectionBand(
                post.Id, post.Name, post.Type, Math.Max(0m, post.PlannedAmount!.Value - (post.ActualAmount ?? 0m))))
            .Where(band => band.Amount > 0m)
            .ToArray();
        var reference = await accounts.GetBalanceReferenceAsync(account.Id, cancellationToken);
        if (reference is null)
        {
            return new MonthlyProjection(account.Id, account.Name, today, null, null, null, [], bands,
                month.UnassignedTransactionCount, month.Posts.Count(post => post.PlannedAmount > 0 && !post.IsMapped),
                month.Posts.Count(post => !post.PlannedAmount.HasValue));
        }

        var transactions = await accounts.ListTransactionsAsync(account.Id, cancellationToken);
        var referenceTransaction = transactions.Single(transaction => transaction.Id == reference.TransactionId);
        var history = Enumerable.Range(1, today.Day)
            .Select(day => new DateOnly(today.Year, today.Month, day))
            .Select(date => new ProjectionBalancePoint(date,
                AccountBalanceService.CalculateBalance(reference.BalanceAfterTransaction, referenceTransaction, transactions, date)))
            .ToArray();
        var current = history[^1].Balance;
        var projected = current + bands.Sum(band => band.Type == AnnualBudgetPostType.Income ? band.Amount : -band.Amount);
        return new MonthlyProjection(account.Id, account.Name, today, referenceTransaction.Date, current, projected,
            history, bands, month.UnassignedTransactionCount,
            month.Posts.Count(post => post.PlannedAmount > 0 && !post.IsMapped),
            month.Posts.Count(post => !post.PlannedAmount.HasValue));
    }
}
