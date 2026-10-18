using AutoFixture;
using AutoFixture.AutoMoq;
using Konta.Application.Categories;
using Konta.Domain.Categories;
using Moq;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_ThrowsDuplicate_WhenCategoryAlreadyExists()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var repository = fixture.Freeze<Mock<ICategoryRepository>>();
        repository.Setup(service => service.ExistsByNameAsync("Courses", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = fixture.Create<CategoryService>();

        // Act
        var action = () => service.CreateAsync("Courses", CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DuplicateCategoryException>(action);
    }

    [Fact]
    public async Task UpdateAsync_ChangesName_WhenCategoryExists()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var repository = fixture.Freeze<Mock<ICategoryRepository>>();
        var category = new Category("Courses");
        repository.Setup(service => service.GetAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repository.Setup(service => service.ExistsByNameAsync("Restaurants", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = fixture.Create<CategoryService>();

        // Act
        var result = await service.UpdateAsync(category.Id, "Restaurants", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Restaurants", result!.Name);
        repository.Verify(service => service.UpdateAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }
}
