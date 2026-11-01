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

    public async Task<IReadOnlyList<OrphanCategoryItem>> ListOrphansAsync(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive
                && !dbContext.BudgetPostCategoryMappings.Any(mapping => mapping.CategoryId == category.Id))
            .Select(category => new { category.Id, category.Name })
            .ToListAsync(cancellationToken);
        var ids = categories.Select(category => category.Id).ToArray();
        var amounts = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.CategoryId != null && ids.Contains(transaction.CategoryId.Value))
            .Select(transaction => new { CategoryId = transaction.CategoryId!.Value, transaction.Amount })
            .ToListAsync(cancellationToken);
        var totals = amounts
            .GroupBy(item => item.CategoryId)
            .ToDictionary(group => group.Key, group => (Count: group.Count(), Total: group.Sum(item => item.Amount)));

        return categories
            .Select(category => totals.TryGetValue(category.Id, out var total)
                ? new OrphanCategoryItem(category.Id, category.Name, total.Count, total.Total)
                : new OrphanCategoryItem(category.Id, category.Name, 0, 0m))
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public Task<int> CountTransactionsAsync(Guid categoryId, CancellationToken cancellationToken) =>
        dbContext.Transactions.CountAsync(transaction => transaction.CategoryId == categoryId, cancellationToken);

    public async Task DeactivateWithDependenciesAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstAsync(item => item.Id == categoryId, cancellationToken);
        category.SetActive(false);
        var rules = await dbContext.CategoryRules
            .Where(rule => rule.CategoryId == categoryId && rule.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var rule in rules)
        {
            rule.SetActive(false);
        }

        var mappings = await dbContext.BudgetPostCategoryMappings
            .Where(mapping => mapping.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        dbContext.BudgetPostCategoryMappings.RemoveRange(mappings);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        dbContext.Categories.Update(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
