using Konta.Application.Planning;
using Konta.Domain.Accounts;
using Konta.Domain.Budgets;
using Konta.Domain.Forecasts;
using Konta.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class PlanningRepository(KontaDbContext dbContext) : IPlanningRepository
{
    public async Task<IReadOnlyList<MonthlyBudget>> ListBudgetsAsync(CancellationToken cancellationToken) =>
        await dbContext.MonthlyBudgets
            .AsNoTracking()
            .Where(budget => budget.IsActive)
            .OrderBy(budget => budget.CategoryId)
            .ToListAsync(cancellationToken);

    public Task<MonthlyBudget?> GetBudgetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.MonthlyBudgets.FirstOrDefaultAsync(budget => budget.Id == id && budget.IsActive, cancellationToken);

    public Task<MonthlyBudget?> GetBudgetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        dbContext.MonthlyBudgets.FirstOrDefaultAsync(
            budget => budget.CategoryId == categoryId && budget.IsActive,
            cancellationToken);

    public async Task AddBudgetAsync(MonthlyBudget budget, CancellationToken cancellationToken)
    {
        dbContext.MonthlyBudgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBudgetAsync(MonthlyBudget budget, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<BudgetOverride?> GetBudgetOverrideAsync(
        Guid budgetId,
        int year,
        int month,
        CancellationToken cancellationToken) =>
        dbContext.BudgetOverrides.FirstOrDefaultAsync(
            item => item.BudgetId == budgetId && item.Year == year && item.Month == month,
            cancellationToken);

    public async Task AddBudgetOverrideAsync(BudgetOverride budgetOverride, CancellationToken cancellationToken)
    {
        dbContext.BudgetOverrides.Add(budgetOverride);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBudgetOverrideAsync(BudgetOverride budgetOverride, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteBudgetOverrideAsync(BudgetOverride budgetOverride, CancellationToken cancellationToken)
    {
        dbContext.BudgetOverrides.Remove(budgetOverride);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringForecast>> ListForecastsAsync(CancellationToken cancellationToken) =>
        await dbContext.RecurringForecasts
            .AsNoTracking()
            .Where(forecast => forecast.IsActive)
            .OrderBy(forecast => forecast.Name)
            .ToListAsync(cancellationToken);

    public Task<RecurringForecast?> GetForecastAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.RecurringForecasts.FirstOrDefaultAsync(forecast => forecast.Id == id && forecast.IsActive, cancellationToken);

    public async Task AddForecastAsync(RecurringForecast forecast, CancellationToken cancellationToken)
    {
        dbContext.RecurringForecasts.Add(forecast);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateForecastAsync(RecurringForecast forecast, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForecastOccurrence>> ListOccurrencesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken) =>
        await dbContext.ForecastOccurrences
            .AsNoTracking()
            .Where(item => item.ScheduledDate >= startDate && item.ScheduledDate <= endDate)
            .ToListAsync(cancellationToken);

    public async Task AddOccurrenceAsync(ForecastOccurrence occurrence, CancellationToken cancellationToken)
    {
        dbContext.ForecastOccurrences.Add(occurrence);
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

    public async Task<IReadOnlyList<Transaction>> ListAccountTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .ToListAsync(cancellationToken);

    public Task<Account?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);
}
