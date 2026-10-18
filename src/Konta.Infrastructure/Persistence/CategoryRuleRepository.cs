using Konta.Application.CategoryRules;
using Konta.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class CategoryRuleRepository(KontaDbContext dbContext) : ICategoryRuleRepository
{
    public async Task<IReadOnlyList<CategoryRule>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.CategoryRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.Name)
            .ToListAsync(cancellationToken);

    public Task<CategoryRule?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.CategoryRules
            .AsNoTracking()
            .FirstOrDefaultAsync(rule => rule.Id == id && rule.IsActive, cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var names = await dbContext.CategoryRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .Select(rule => rule.Name)
            .ToListAsync(cancellationToken);
        return names.Any(existingName => string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ExistsByPatternAsync(string pattern, Guid? excludingRuleId, CancellationToken cancellationToken)
    {
        var patterns = await dbContext.CategoryRules
            .AsNoTracking()
            .Where(rule => rule.IsActive
                && (!excludingRuleId.HasValue || rule.Id != excludingRuleId.Value))
            .Select(rule => rule.Pattern)
            .ToListAsync(cancellationToken);
        return patterns.Any(existingPattern => string.Equals(existingPattern, pattern, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(CategoryRule rule, CancellationToken cancellationToken)
    {
        dbContext.CategoryRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CategoryRule rule, CancellationToken cancellationToken)
    {
        dbContext.CategoryRules.Update(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryRule>> FindMatchingAsync(string label, CancellationToken cancellationToken)
    {
        var normalized = label.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var rules = await dbContext.CategoryRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .ToListAsync(cancellationToken);

        return rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Pattern)
                && normalized.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(rule => rule.Pattern.Length)
            .ToArray();
    }
}
