using Konta.Domain.Categories;

namespace Konta.Domain.Budgets;

public sealed class BudgetPostCategoryMapping
{
    private BudgetPostCategoryMapping()
    {
    }

    public BudgetPostCategoryMapping(Guid annualBudgetPostId, int year, Guid categoryId)
    {
        AnnualBudgetPostId = annualBudgetPostId;
        Year = year;
        CategoryId = categoryId;
    }

    public Guid Id { get; private set; }
    public Guid AnnualBudgetPostId { get; private set; }
    public int Year { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
}
