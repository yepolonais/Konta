using Konta.Domain.Budgets;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class AnnualBudgetDomainTests
{
    [Fact]
    public void Post_TrimsName_AndStartsActive()
    {
        // Act
        var post = new AnnualBudgetPost("  Courses  ", AnnualBudgetPostType.Expense);

        // Assert
        Assert.Equal("Courses", post.Name);
        Assert.Equal(AnnualBudgetPostType.Expense, post.Type);
        Assert.Equal(AnnualBudgetExpenseKind.Variable, post.ExpenseKind);
        Assert.True(post.IsActive);
    }

    [Fact]
    public void Post_ChangesExpenseKind_WithoutChangingPostType()
    {
        // Arrange
        var post = new AnnualBudgetPost("Loyer", AnnualBudgetPostType.Expense);

        // Act
        post.Update("Loyer", AnnualBudgetPostType.Expense, AnnualBudgetExpenseKind.Fixed);

        // Assert
        Assert.Equal(AnnualBudgetPostType.Expense, post.Type);
        Assert.Equal(AnnualBudgetExpenseKind.Fixed, post.ExpenseKind);
    }

    [Fact]
    public void MonthlyAmount_AllowsExplicitZero()
    {
        // Act
        var amount = new BudgetMonthlyAmount(Guid.NewGuid(), 2026, 1, 0m);

        // Assert
        Assert.Equal(0m, amount.Amount);
        Assert.Equal(2026, amount.Year);
        Assert.Equal(1, amount.Month);
    }

    [Fact]
    public void MonthlyAmount_RejectsNegativeValue()
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BudgetMonthlyAmount(Guid.NewGuid(), 2026, 1, -1m));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }
}
