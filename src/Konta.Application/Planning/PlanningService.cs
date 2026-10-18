using Konta.Application.Categories;
using Konta.Domain.Accounts;
using Konta.Domain.Budgets;
using Konta.Domain.Forecasts;
using Konta.Domain.Transactions;
using Konta.Application.Transactions;

namespace Konta.Application.Planning;

public sealed class PlanningService(
    IPlanningRepository repository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository) : IPlanningService
{
    public async Task<IReadOnlyList<MonthlyBudgetItem>> ListBudgetsAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(year, month);
        var budgets = await repository.ListBudgetsAsync(cancellationToken);
        var overrides = await Task.WhenAll(budgets.Select(budget =>
            repository.GetBudgetOverrideAsync(budget.Id, year, month, cancellationToken)));
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        var transactions = await transactionRepository.ListForPeriodAsync(start, end, null, null, cancellationToken);
        var categories = await categoryRepository.ListAsync(cancellationToken);
        var categoryNames = categories.ToDictionary(category => category.Id, category => category.Name);

        return budgets
            .Where(budget => budget.IsActive && categoryNames.ContainsKey(budget.CategoryId))
            .Select((budget, index) =>
            {
                var effectiveAmount = overrides[index]?.Amount ?? budget.Amount;
                var spent = transactions
                    .Where(transaction => transaction.CategoryId == budget.CategoryId
                        && transaction.Type == TransactionType.Expense)
                    .Sum(transaction => transaction.Amount);
                return new MonthlyBudgetItem(
                    budget.Id,
                    budget.CategoryId,
                    categoryNames[budget.CategoryId],
                    budget.Amount,
                    effectiveAmount,
                    spent,
                    effectiveAmount - spent,
                    overrides[index] is not null);
            })
            .OrderBy(item => item.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<MonthlyBudgetItem> CreateBudgetAsync(
        Guid categoryId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        if (await categoryRepository.GetAsync(categoryId, cancellationToken) is null)
        {
            throw new ArgumentException("La catégorie n'existe pas ou n'est plus active.", nameof(categoryId));
        }

        if (await repository.GetBudgetByCategoryAsync(categoryId, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Un budget récurrent existe déjà pour cette catégorie.");
        }

        var budget = new MonthlyBudget(categoryId, amount);
        await repository.AddBudgetAsync(budget, cancellationToken);
        var current = DateOnly.FromDateTime(DateTime.UtcNow);
        return (await ListBudgetsAsync(current.Year, current.Month, cancellationToken))
            .Single(item => item.Id == budget.Id);
    }

    public async Task<MonthlyBudgetItem?> UpdateBudgetAsync(Guid id, decimal amount, CancellationToken cancellationToken)
    {
        var budget = await repository.GetBudgetAsync(id, cancellationToken);
        if (budget is null)
        {
            return null;
        }

        budget.Update(amount);
        await repository.UpdateBudgetAsync(budget, cancellationToken);
        var current = DateOnly.FromDateTime(DateTime.UtcNow);
        return (await ListBudgetsAsync(current.Year, current.Month, cancellationToken))
            .Single(item => item.Id == id);
    }

    public async Task<MonthlyBudgetItem?> SetBudgetOverrideAsync(
        Guid id,
        int year,
        int month,
        decimal amount,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(year, month);
        var budget = await repository.GetBudgetAsync(id, cancellationToken);
        if (budget is null)
        {
            return null;
        }

        var budgetOverride = await repository.GetBudgetOverrideAsync(id, year, month, cancellationToken);
        if (budgetOverride is null)
        {
            await repository.AddBudgetOverrideAsync(new BudgetOverride(id, year, month, amount), cancellationToken);
        }
        else
        {
            budgetOverride.Update(amount);
            await repository.UpdateBudgetOverrideAsync(budgetOverride, cancellationToken);
        }

        return (await ListBudgetsAsync(year, month, cancellationToken)).Single(item => item.Id == id);
    }

    public async Task<MonthlyBudgetItem?> DeleteBudgetOverrideAsync(
        Guid id,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidatePeriod(year, month);
        if (await repository.GetBudgetAsync(id, cancellationToken) is null)
        {
            return null;
        }

        var budgetOverride = await repository.GetBudgetOverrideAsync(id, year, month, cancellationToken);
        if (budgetOverride is not null)
        {
            await repository.DeleteBudgetOverrideAsync(budgetOverride, cancellationToken);
        }

        return (await ListBudgetsAsync(year, month, cancellationToken)).Single(item => item.Id == id);
    }

    public async Task<IReadOnlyList<RecurringForecastItem>> ListForecastsAsync(CancellationToken cancellationToken)
    {
        var forecasts = await repository.ListForecastsAsync(cancellationToken);
        var items = new List<RecurringForecastItem>(forecasts.Count);
        foreach (var forecast in forecasts)
        {
            var account = await repository.GetAccountAsync(forecast.AccountId, cancellationToken);
            if (account is not null)
            {
                items.Add(ToForecastItem(forecast, account));
            }
        }

        return items;
    }

    public async Task<RecurringForecastItem> CreateForecastAsync(
        string name,
        string labelPattern,
        Guid accountId,
        decimal amount,
        TransactionType type,
        int dayOfMonth,
        ForecastFrequency frequency,
        DateOnly startDate,
        DateOnly? endDate,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetAccountAsync(accountId, cancellationToken)
            ?? throw new ArgumentException("Le compte n'existe pas.", nameof(accountId));
        if (categoryId.HasValue && await categoryRepository.GetAsync(categoryId.Value, cancellationToken) is null)
        {
            throw new ArgumentException("La catégorie n'existe pas ou n'est plus active.", nameof(categoryId));
        }

        var forecast = new RecurringForecast(
            name, labelPattern, accountId, amount, type, dayOfMonth, frequency, startDate, endDate, categoryId);
        await repository.AddForecastAsync(forecast, cancellationToken);
        return ToForecastItem(forecast, account);
    }

    public async Task<bool> DeleteForecastAsync(Guid id, CancellationToken cancellationToken)
    {
        var forecast = await repository.GetForecastAsync(id, cancellationToken);
        if (forecast is null)
        {
            return false;
        }

        forecast.SetActive(false);
        await repository.UpdateForecastAsync(forecast, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ForecastOccurrenceItem>> ListForecastOccurrencesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (endDate < startDate || endDate.DayNumber - startDate.DayNumber > 3660)
        {
            throw new ArgumentException("La période des échéances est invalide ou dépasse dix ans.");
        }

        var forecasts = await repository.ListForecastsAsync(cancellationToken);
        var matchedOccurrences = await repository.ListOccurrencesAsync(startDate, endDate, cancellationToken);
        var matches = matchedOccurrences.ToDictionary(item => (item.ForecastId, item.ScheduledDate));
        var items = new List<ForecastOccurrenceItem>();

        foreach (var forecast in forecasts.Where(item => item.IsActive))
        {
            var account = await repository.GetAccountAsync(forecast.AccountId, cancellationToken);
            if (account is null) continue;
            foreach (var date in EnumerateDates(forecast, startDate, endDate))
            {
                matches.TryGetValue((forecast.Id, date), out var match);
                items.Add(new ForecastOccurrenceItem(
                    forecast.Id,
                    forecast.Name,
                    forecast.LabelPattern,
                    forecast.AccountId,
                    account.Name,
                    date,
                    forecast.Amount,
                    match?.ActualAmount,
                    forecast.Type,
                    forecast.CategoryId,
                    match is not null));
            }
        }

        return items.OrderBy(item => item.ScheduledDate).ThenBy(item => item.Name).ToArray();
    }

    public async Task<IReadOnlyList<ForecastMatchCandidate>> FindForecastMatchesAsync(
        Guid? accountId,
        DateOnly transactionDate,
        string label,
        decimal amount,
        TransactionType type,
        CancellationToken cancellationToken)
    {
        if (!accountId.HasValue || string.IsNullOrWhiteSpace(label))
        {
            return [];
        }

        var startDate = transactionDate.AddDays(-3);
        var endDate = transactionDate.AddDays(3);
        var candidates = await ListForecastOccurrencesAsync(startDate, endDate, cancellationToken);
        var matching = candidates
            .Where(item => !item.IsCompleted
                && item.AccountId == accountId.Value
                && item.Type == type
                && label.Contains(item.LabelPattern, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matching
            .Select(item => new ForecastMatchCandidate(
                item.ForecastId,
                item.Name,
                item.ScheduledDate,
                item.ForecastAmount,
                Math.Abs(item.ForecastAmount - amount),
                Math.Abs(item.ScheduledDate.DayNumber - transactionDate.DayNumber),
                matching.Length > 1))
            .OrderBy(candidate => candidate.DateDifferenceDays)
            .ThenBy(candidate => candidate.AmountDifference)
            .ToArray();
    }

    public async Task<AccountBalanceSummary> SetReferenceBalanceAsync(
        Guid accountId,
        Guid transactionId,
        decimal balanceAfterTransaction,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetAccountAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException("Le compte n'existe pas.");
        var transactions = await repository.ListAccountTransactionsAsync(accountId, cancellationToken);
        if (!transactions.Any(transaction => transaction.Id == transactionId))
        {
            throw new ArgumentException("L'opération de référence doit appartenir au compte sélectionné.", nameof(transactionId));
        }

        await repository.SetBalanceReferenceAsync(accountId, transactionId, balanceAfterTransaction, cancellationToken);
        return await GetBalancesAsync(accountId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
    }

    public async Task<AccountBalanceSummary> GetBalancesAsync(
        Guid accountId,
        DateOnly throughDate,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetAccountAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException("Le compte n'existe pas.");
        var reference = await repository.GetBalanceReferenceAsync(accountId, cancellationToken);
        if (reference is null)
        {
            return new AccountBalanceSummary(account.Id, account.Name, null, null, null, null, null, throughDate);
        }

        var transactions = await repository.ListAccountTransactionsAsync(accountId, cancellationToken);
        var referenceTransaction = transactions.Single(transaction => transaction.Id == reference.TransactionId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var actual = reference.BalanceAfterTransaction + transactions
            .Where(transaction => IsAfterReference(transaction, referenceTransaction) && transaction.Date <= today)
            .Sum(SignedAmount);
        var forecastBalance = actual;
        if (throughDate > today)
        {
            var occurrences = await ListForecastOccurrencesAsync(today.AddDays(1), throughDate, cancellationToken);
            forecastBalance += occurrences
                .Where(item => item.AccountId == accountId && !item.IsCompleted)
                .Sum(item => item.Type == TransactionType.Income ? item.ForecastAmount : -item.ForecastAmount);
        }

        return new AccountBalanceSummary(
            account.Id,
            account.Name,
            reference.TransactionId,
            referenceTransaction.Date,
            reference.BalanceAfterTransaction,
            actual,
            forecastBalance,
            throughDate);
    }

    private static IEnumerable<DateOnly> EnumerateDates(RecurringForecast forecast, DateOnly start, DateOnly end)
    {
        var month = new DateOnly(start.Year, start.Month, 1);
        var lastMonth = new DateOnly(end.Year, end.Month, 1);
        while (month <= lastMonth)
        {
            var inDateRange = forecast.Frequency == ForecastFrequency.Monthly
                || month.Month == forecast.StartDate.Month;
            if (inDateRange)
            {
                var date = new DateOnly(month.Year, month.Month, Math.Min(forecast.DayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)));
                if (date >= start && date <= end && date >= forecast.StartDate
                    && (!forecast.EndDate.HasValue || date <= forecast.EndDate.Value))
                {
                    yield return date;
                }
            }

            month = month.AddMonths(1);
        }
    }

    private static bool IsAfterReference(Transaction transaction, Transaction reference) =>
        transaction.Date > reference.Date
        || (transaction.Date == reference.Date
            && (transaction.CreatedAt > reference.CreatedAt
                || (transaction.CreatedAt == reference.CreatedAt && transaction.Id.CompareTo(reference.Id) > 0)));

    private static decimal SignedAmount(Transaction transaction) =>
        transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;

    private static RecurringForecastItem ToForecastItem(RecurringForecast forecast, Account account) => new(
        forecast.Id,
        forecast.Name,
        forecast.LabelPattern,
        account.Id,
        account.Name,
        forecast.Amount,
        forecast.Type,
        forecast.DayOfMonth,
        forecast.Frequency,
        forecast.StartDate,
        forecast.EndDate,
        forecast.CategoryId,
        forecast.IsActive);

    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 1 or > 9998) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }
}
