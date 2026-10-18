using Konta.Application.Accounts;
using Konta.Domain.Savings;

namespace Konta.Application.Savings;

public sealed class SavingsGoalService(
    ISavingsGoalRepository repository,
    IAccountRepository accountRepository) : ISavingsGoalService
{
    public async Task<IReadOnlyList<SavingsGoalItem>> ListAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var goals = await repository.ListAsync(cancellationToken);
        var items = new List<SavingsGoalItem>(goals.Count);
        foreach (var goal in goals)
        {
            var account = await accountRepository.GetAsync(goal.AccountId, cancellationToken);
            if (account is null)
            {
                continue;
            }

            var contributions = await repository.ListContributionsAsync(goal.AccountId, cancellationToken);
            var monthly = contributions
                .Where(item => item.Date >= monthStart && item.Date < monthEnd)
                .Sum(item => item.Amount);
            var total = contributions.Sum(item => item.Amount);
            items.Add(new SavingsGoalItem(
                goal.Id,
                goal.Name,
                goal.AccountId,
                account.Name,
                goal.TargetAmount,
                goal.PlannedMonthlyContribution,
                monthly,
                total,
                Math.Min(100m, total / goal.TargetAmount * 100m)));
        }

        return items;
    }

    public async Task<SavingsGoalItem> CreateAsync(
        string name,
        Guid accountId,
        decimal targetAmount,
        decimal plannedMonthlyContribution,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(year, month);
        if (await accountRepository.GetAsync(accountId, cancellationToken) is null)
        {
            throw new ArgumentException("Le compte d'épargne n'existe pas.", nameof(accountId));
        }

        if (await repository.HasActiveForAccountAsync(accountId, null, cancellationToken))
        {
            throw new InvalidOperationException("Un objectif d'épargne actif existe déjà pour ce compte.");
        }

        var goal = new SavingsGoal(name, accountId, targetAmount, plannedMonthlyContribution);
        await repository.AddAsync(goal, cancellationToken);
        return (await ListAsync(year, month, cancellationToken)).Single(item => item.Id == goal.Id);
    }

    public async Task<SavingsGoalItem?> UpdateAsync(
        Guid id,
        string name,
        Guid accountId,
        decimal targetAmount,
        decimal plannedMonthlyContribution,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(year, month);
        if (await accountRepository.GetAsync(accountId, cancellationToken) is null)
        {
            throw new ArgumentException("Le compte d'épargne n'existe pas.", nameof(accountId));
        }

        var goal = await repository.GetAsync(id, cancellationToken);
        if (goal is null)
        {
            return null;
        }

        if (await repository.HasActiveForAccountAsync(accountId, id, cancellationToken))
        {
            throw new InvalidOperationException("Un objectif d'épargne actif existe déjà pour ce compte.");
        }

        goal.Update(name, accountId, targetAmount, plannedMonthlyContribution);
        await repository.UpdateAsync(goal, cancellationToken);
        return (await ListAsync(year, month, cancellationToken)).Single(item => item.Id == id);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var goal = await repository.GetAsync(id, cancellationToken);
        if (goal is null)
        {
            return false;
        }

        goal.Deactivate();
        await repository.UpdateAsync(goal, cancellationToken);
        return true;
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 1 or > 9998) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }
}
