namespace Konta.Application.Budgets;

public sealed record AnnualBudgetSummary(
    int Year,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal SavingsTotal,
    decimal AvailableTotal,
    IReadOnlyList<AnnualBudgetPostItem> Posts);
