using Konta.Domain.Categories;

namespace Konta.Domain.Budgets;

public sealed class BudgetPostCategoryMapping
{
    private BudgetPostCategoryMapping()
    {
    }

    public BudgetPostCategoryMapping(Guid annualBudgetPostId, Guid categoryId)
    {
        AnnualBudgetPostId = annualBudgetPostId;
        CategoryId = categoryId;
    }

    public Guid Id { get; private set; }
    public Guid AnnualBudgetPostId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
}
