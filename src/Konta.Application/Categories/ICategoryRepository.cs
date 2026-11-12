using Konta.Domain.Categories;

namespace Konta.Application.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken);
    Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task UpdateAsync(Category category, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrphanCategoryItem>> ListOrphansAsync(CancellationToken cancellationToken);
    Task<int> CountTransactionsAsync(Guid categoryId, CancellationToken cancellationToken);
    Task DeactivateWithDependenciesAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<bool> EnableMultiPostAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<bool> DisableMultiPostAsync(Guid categoryId, CancellationToken cancellationToken);
}
