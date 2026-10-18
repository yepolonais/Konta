using Konta.Domain.Savings;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class SavingsGoalTests
{
    [Fact]
    public void Constructor_TrimsName_AndStoresGoalAmounts()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var goal = new SavingsGoal("  Fonds d'urgence  ", accountId, 5000m, 250m);

        // Assert
        Assert.Equal("Fonds d'urgence", goal.Name);
        Assert.Equal(accountId, goal.AccountId);
        Assert.Equal(5000m, goal.TargetAmount);
        Assert.Equal(250m, goal.PlannedMonthlyContribution);
        Assert.True(goal.IsActive);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveAmounts()
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SavingsGoal("Fonds", Guid.NewGuid(), 0m, 100m));

        // Assert
        Assert.Equal("targetAmount", exception.ParamName);
    }

    [Fact]
    public void Deactivate_PreventsLaterUpdates()
    {
        // Arrange
        var goal = new SavingsGoal("Fonds", Guid.NewGuid(), 5000m, 250m);
        goal.Deactivate();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => goal.Update("Voyage", Guid.NewGuid(), 1000m, 100m));

        // Assert
        Assert.False(goal.IsActive);
        Assert.Contains("inactive", exception.Message, StringComparison.Ordinal);
    }
}
