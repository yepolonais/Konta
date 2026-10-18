namespace Konta.Application.Planning;

public sealed record MonthlyBudgetItem(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal RecurringAmount,
    decimal EffectiveAmount,
    decimal ExpenseTotal,
    decimal RemainingAmount,
    bool HasMonthlyOverride);
