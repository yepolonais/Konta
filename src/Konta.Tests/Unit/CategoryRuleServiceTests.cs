using Konta.Application.Imports;
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
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newCategory.Id, result!.CategoryId);
        Assert.Equal(newCategory.Id, rule.CategoryId);
        repository.Verify(item => item.UpdateAsync(rule, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindBestMatchAsync_ReturnsLongestPattern_IgnoringCaseAndAccents()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var categoryRepository = fixture.Freeze<Mock<ICategoryRepository>>();
        var repository = fixture.Freeze<Mock<ICategoryRuleRepository>>();
        var bakery = new Category("Boulangerie");
        var generic = new Category("Courses");
        repository.Setup(service => service.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CategoryRule("Générique", "BLAC", generic.Id),
                new CategoryRule("Boulangerie", "blachère", bakery.Id)
            });
        categoryRepository.Setup(service => service.GetAsync(bakery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bakery);
        var service = fixture.Create<CategoryRuleService>();

        // Act
        var result = await service.FindBestMatchAsync(
            new OperationKey(OperationType.Card, "MARIE  BLACHERE"),
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Boulangerie", result!.CategoryName);
    }

    [Fact]
    public async Task FindBestMatchAsync_PrefersTypedRule_AndIgnoresOtherOperationTypes()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var categoryRepository = fixture.Freeze<Mock<ICategoryRepository>>();
        var repository = fixture.Freeze<Mock<ICategoryRuleRepository>>();
        var income = new Category("Revenus");
        var savings = new Category("Épargne");
        var any = new Category("Divers");
        repository.Setup(service => service.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CategoryRule("Tous", "CRONIER-FLURA JENNIFER", any.Id),
                new CategoryRule("Reçu", "CRONIER", income.Id, OperationType.TransferReceived),
                new CategoryRule("Émis", "CRONIER", savings.Id, OperationType.TransferSent)
            });
        categoryRepository.Setup(service => service.GetAsync(income.Id, It.IsAny<CancellationToken>())).ReturnsAsync(income);
        categoryRepository.Setup(service => service.GetAsync(savings.Id, It.IsAny<CancellationToken>())).ReturnsAsync(savings);
        var service = fixture.Create<CategoryRuleService>();

        // Act
        var received = await service.FindBestMatchAsync(
            new OperationKey(OperationType.TransferReceived, "MME JENNIFER CRONIER-FLURA"),
            CancellationToken.None);
        var sent = await service.FindBestMatchAsync(
            new OperationKey(OperationType.TransferSent, "MME JENNIFER CRONIER-FLURA SG 00392 CPT 1"),
            CancellationToken.None);
        var card = await service.FindBestMatchAsync(
            new OperationKey(OperationType.Card, "CRONIER"),
            CancellationToken.None);

        // Assert
        Assert.Equal("Revenus", received!.CategoryName);
        Assert.Equal("Épargne", sent!.CategoryName);
        Assert.Null(card);
    }

    [Fact]
    public async Task FindBestMatchAsync_ReturnsNull_WhenOperationHasNoKey()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var service = fixture.Create<CategoryRuleService>();

        // Act
        var result = await service.FindBestMatchAsync(null, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
