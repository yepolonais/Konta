using Konta.Domain.Budgets;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class AnnualBudgetDomainTests
{
    [Fact]
    public void Post_TrimsName_AndStartsActive()
    {
        // Act
        var post = new AnnualBudgetPost(2026, "  Courses  ", AnnualBudgetPostType.Expense);

        // Assert
        Assert.Equal("Courses", post.Name);
        Assert.Equal(2026, post.Year);
        Assert.Equal(AnnualBudgetPostType.Expense, post.Type);
        Assert.True(post.IsActive);
    }

    [Fact]
    public void MonthlyAmount_AllowsExplicitZero()
    {
        // Act
        var amount = new BudgetMonthlyAmount(Guid.NewGuid(), 1, 0m);

        // Assert
        Assert.Equal(0m, amount.Amount);
        Assert.Equal(1, amount.Month);
    }

    [Fact]
    public void MonthlyAmount_RejectsNegativeValue()
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BudgetMonthlyAmount(Guid.NewGuid(), 1, -1m));

        // Assert
        Assert.Equal("amount", exception.ParamName);
    }
}
