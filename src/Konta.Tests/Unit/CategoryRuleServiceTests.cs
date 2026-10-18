using AutoFixture;
using AutoFixture.AutoMoq;
using Konta.Application.Categories;
using Konta.Application.CategoryRules;
using Konta.Domain.Categories;
using Moq;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CategoryRuleServiceTests
{
    [Fact]
    public async Task UpdateAsync_ChangesCategory_WhenRuleExists()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var categoryRepository = fixture.Freeze<Mock<ICategoryRepository>>();
        var repository = fixture.Freeze<Mock<ICategoryRuleRepository>>();
        var oldCategory = new Category("Courses");
        var newCategory = new Category("Restaurants");
        var rule = new CategoryRule("Carte", "CARTE", oldCategory.Id);
        repository.Setup(service => service.GetAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        categoryRepository.Setup(service => service.GetAsync(newCategory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCategory);
        var service = fixture.Create<CategoryRuleService>();

        // Act
        var result = await service.UpdateAsync(
            rule.Id,
            rule.Name,
            rule.Pattern,
            newCategory.Id,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newCategory.Id, result!.CategoryId);
        Assert.Equal(newCategory.Id, rule.CategoryId);
        repository.Verify(item => item.UpdateAsync(rule, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindBestMatchAsync_ReturnsMostSpecificRule()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var categoryRepository = fixture.Freeze<Mock<ICategoryRepository>>();
        var repository = fixture.Freeze<Mock<ICategoryRuleRepository>>();
        var restaurantCategory = new Category("Restaurants");
        var foodCategory = new Category("Courses");
        var matchingRules = new[]
        {
            new CategoryRule("Générique", "CARTE", foodCategory.Id),
            new CategoryRule("Restaurant", "CARTE VISA", restaurantCategory.Id)
        };
        repository.Setup(service => service.FindMatchingAsync("Paiement CARTE VISA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchingRules);
        categoryRepository.Setup(service => service.GetAsync(restaurantCategory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurantCategory);
        var service = fixture.Create<CategoryRuleService>();

        // Act
        var result = await service.FindBestMatchAsync("Paiement CARTE VISA", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Restaurants", result!.CategoryName);
        Assert.Equal("CARTE VISA", result.Pattern);
    }
}