using Konta.Domain.Budgets;

namespace Konta.Application.Budgets;

public sealed record AnnualBudgetPostItem(
    Guid Id,
    int Year,
    string Name,
    AnnualBudgetPostType Type,
    bool IsActive,
    IReadOnlyList<decimal?> MonthlyAmounts,
    decimal AnnualTotal,
    IReadOnlyList<AnnualBudgetCategoryItem> Categories);

public sealed record AnnualBudgetCategoryItem(Guid Id, string Name);
