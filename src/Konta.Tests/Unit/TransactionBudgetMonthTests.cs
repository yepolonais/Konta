using Konta.Domain.Transactions;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class TransactionBudgetMonthTests
{
    [Fact]
    public void SetBudgetMonth_ChangesBudgetPeriod_WithoutChangingBankDate()
    {
        // Arrange
        var transaction = new Transaction(new DateOnly(2026, 12, 29), "Virement", 100m, TransactionType.Income);
        Assert.Equal(new DateOnly(2026, 12, 1), transaction.BudgetMonth);

        // Act
        transaction.SetBudgetMonth(2027, 1);

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 1), transaction.BudgetMonth);
        Assert.Equal(new DateOnly(2026, 12, 29), transaction.Date);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(9999, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public void SetBudgetMonth_RejectsInvalidPeriod_WithoutChangingExistingPeriod(int year, int month)
    {
        // Arrange
        var transaction = new Transaction(new DateOnly(2026, 10, 29), "Achat", 10m, TransactionType.Expense);

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => transaction.SetBudgetMonth(year, month));

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 1), transaction.BudgetMonth);
    }
}
