using Konta.Domain.Budgets;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class MonthlyDisplaySectionTests
{
    [Fact]
    public void Rename_RejectsBlankName_WithoutChangingSection()
    {
        // Arrange
        var section = new MonthlyDisplaySection("  Impôts  ");

        // Act
        Assert.Throws<ArgumentException>(() => section.Rename("  "));

        // Assert
        Assert.Equal("Impôts", section.Name);
    }
}
