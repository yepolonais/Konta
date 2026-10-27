namespace Konta.Application.Budgets;

public sealed record BudgetExtraItem(Guid Id, int Year, int Month, string Label, decimal Amount);
