using Konta.Domain.Categories;

namespace Konta.Application.Categories;

public sealed class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var categories = await repository.ListAsync(cancellationToken);
        return categories.Select(ToListItem).ToArray();
    }

    public Task<IReadOnlyList<OrphanCategoryItem>> ListOrphansAsync(CancellationToken cancellationToken) =>
        repository.ListOrphansAsync(cancellationToken);

    public async Task<CategoryListItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await repository.GetAsync(id, cancellationToken);
        return category is null ? null : ToListItem(category);
    }

    public async Task<CategoryListItem> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(name);
        if (await repository.ExistsByNameAsync(normalizedName, cancellationToken))
        {
            throw new DuplicateCategoryException(normalizedName);
        }

        var category = new Category(normalizedName);
        await repository.AddAsync(category, cancellationToken);
        return ToListItem(category);
    }

    public async Task<CategoryListItem?> UpdateAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        var category = await repository.GetAsync(id, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var normalizedName = NormalizeName(name);
        if (!string.Equals(category.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            && await repository.ExistsByNameAsync(normalizedName, cancellationToken))
        {
            throw new DuplicateCategoryException(normalizedName);
        }

        category.Rename(normalizedName);
        await repository.UpdateAsync(category, cancellationToken);
        return ToListItem(category);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await repository.GetAsync(id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        var transactionCount = await repository.CountTransactionsAsync(id, cancellationToken);
        if (transactionCount > 0)
        {
            throw new CategoryInUseException(category.Name, transactionCount);
        }

        await repository.DeactivateWithDependenciesAsync(id, cancellationToken);
        return true;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Le nom de la catégorie est requis.", nameof(name));
        }

        return normalized;
    }

    private static CategoryListItem ToListItem(Category category) => new(
        category.Id,
        category.Name,
        category.IsActive);
}
