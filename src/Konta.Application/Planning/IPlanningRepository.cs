using Konta.Domain.Accounts;
using Konta.Domain.Budgets;
using Konta.Domain.Forecasts;

namespace Konta.Application.Planning;

public interface IPlanningRepository
{
    Task<IReadOnlyList<MonthlyBudget>> ListBudgetsAsync(CancellationToken cancellationToken);
    Task<MonthlyBudget?> GetBudgetAsync(Guid id, CancellationToken cancellationToken);
    Task<MonthlyBudget?> GetBudgetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken);
    Task AddBudgetAsync(MonthlyBudget budget, CancellationToken cancellationToken);
    Task UpdateBudgetAsync(MonthlyBudget budget, CancellationToken cancellationToken);
    Task<BudgetOverride?> GetBudgetOverrideAsync(Guid budgetId, int year, int month, CancellationToken cancellationToken);
    Task AddBudgetOverrideAsync(BudgetOverride budgetOverride, CancellationToken cancellationToken);
    Task UpdateBudgetOverrideAsync(BudgetOverride budgetOverride, CancellationToken cancellationToken);
    Task DeleteBudgetOverrideAsync(BudgetOverride budgetOverride, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecurringForecast>> ListForecastsAsync(CancellationToken cancellationToken);
    Task<RecurringForecast?> GetForecastAsync(Guid id, CancellationToken cancellationToken);
    Task AddForecastAsync(RecurringForecast forecast, CancellationToken cancellationToken);
    Task UpdateForecastAsync(RecurringForecast forecast, CancellationToken cancellationToken);
    Task<IReadOnlyList<ForecastOccurrence>> ListOccurrencesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
    Task AddOccurrenceAsync(ForecastOccurrence occurrence, CancellationToken cancellationToken);
    Task<AccountBalanceReference?> GetBalanceReferenceAsync(Guid accountId, CancellationToken cancellationToken);
    Task SetBalanceReferenceAsync(Guid accountId, Guid transactionId, decimal balanceAfterTransaction, CancellationToken cancellationToken);
    Task<IReadOnlyList<Konta.Domain.Transactions.Transaction>> ListAccountTransactionsAsync(Guid accountId, CancellationToken cancellationToken);
    Task<Account?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);
}
