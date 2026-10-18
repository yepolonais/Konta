using Konta.Application.Categories;
using Konta.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class CategoryRepository(KontaDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id && category.IsActive, cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        return await dbContext.Categories
            .AnyAsync(category => category.IsActive
                && category.Name.ToLower() == normalizedName.ToLower(), cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        dbContext.Categories.Update(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
