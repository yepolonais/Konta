namespace Konta.Application.Planning;

public interface IPlanningService
{
    Task<IReadOnlyList<MonthlyBudgetItem>> ListBudgetsAsync(int year, int month, CancellationToken cancellationToken);
    Task<MonthlyBudgetItem> CreateBudgetAsync(Guid categoryId, decimal amount, CancellationToken cancellationToken);
    Task<MonthlyBudgetItem?> UpdateBudgetAsync(Guid id, decimal amount, CancellationToken cancellationToken);
    Task<MonthlyBudgetItem?> SetBudgetOverrideAsync(Guid id, int year, int month, decimal amount, CancellationToken cancellationToken);
    Task<MonthlyBudgetItem?> DeleteBudgetOverrideAsync(Guid id, int year, int month, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecurringForecastItem>> ListForecastsAsync(CancellationToken cancellationToken);
    Task<RecurringForecastItem> CreateForecastAsync(
        string name, string labelPattern, Guid accountId, decimal amount,
        Konta.Domain.Transactions.TransactionType type, int dayOfMonth,
        Konta.Domain.Forecasts.ForecastFrequency frequency,
        DateOnly startDate, DateOnly? endDate, Guid? categoryId,
        CancellationToken cancellationToken);
    Task<bool> DeleteForecastAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ForecastOccurrenceItem>> ListForecastOccurrencesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
    Task<IReadOnlyList<ForecastMatchCandidate>> FindForecastMatchesAsync(
        Guid? accountId,
        DateOnly transactionDate,
        string label,
        decimal amount,
        Konta.Domain.Transactions.TransactionType type,
        CancellationToken cancellationToken);
    Task<AccountBalanceSummary> SetReferenceBalanceAsync(Guid accountId, Guid transactionId, decimal balanceAfterTransaction, CancellationToken cancellationToken);
    Task<AccountBalanceSummary> GetBalancesAsync(Guid accountId, DateOnly throughDate, CancellationToken cancellationToken);
}
