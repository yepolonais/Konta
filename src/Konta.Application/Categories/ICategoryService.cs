namespace Konta.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken);
    Task<CategoryListItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryListItem> CreateAsync(string name, CancellationToken cancellationToken);
    Task<CategoryListItem?> UpdateAsync(Guid id, string name, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
