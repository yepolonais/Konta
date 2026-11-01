using Konta.Application.CategoryRules;
using Konta.Application.Imports;
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

    public async Task<bool> ExistsByPatternAsync(
        string pattern,
        OperationType? operationType,
        Guid? excludingRuleId,
        CancellationToken cancellationToken)
    {
        var rules = await dbContext.CategoryRules
            .AsNoTracking()
            .Where(rule => rule.IsActive
                && rule.OperationType == operationType
                && (!excludingRuleId.HasValue || rule.Id != excludingRuleId.Value))
            .Select(rule => rule.Pattern)
            .ToListAsync(cancellationToken);
        var normalized = OperationKeyExtractor.Normalize(pattern);
        return rules.Any(existing => OperationKeyExtractor.Normalize(existing) == normalized);
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
}
