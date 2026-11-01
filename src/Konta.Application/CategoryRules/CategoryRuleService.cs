using Konta.Application.Categories;
using Konta.Application.Imports;
using Konta.Domain.Categories;

namespace Konta.Application.CategoryRules;

public sealed class CategoryRuleService(
    ICategoryRuleRepository repository,
    ICategoryRepository categoryRepository) : ICategoryRuleService
{
    public async Task<IReadOnlyList<CategoryRuleListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var rules = await repository.ListAsync(cancellationToken);
        return rules.Select(ToListItem).ToArray();
    }

    public async Task<CategoryRuleListItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await repository.GetAsync(id, cancellationToken);
        return rule is null ? null : ToListItem(rule);
    }

    public async Task<CategoryRuleListItem> CreateAsync(
        string name,
        string pattern,
        Guid categoryId,
        OperationType? operationType,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(name);
        var normalizedPattern = NormalizePattern(pattern);
        if (await repository.ExistsByNameAsync(normalizedName, cancellationToken))
        {
            throw new DuplicateCategoryRuleException(normalizedName);
        }

        if (await repository.ExistsByPatternAsync(normalizedPattern, operationType, null, cancellationToken))
        {
            throw new DuplicateCategoryRuleException(normalizedPattern);
        }

        await EnsureCategoryExistsAsync(categoryId, cancellationToken);
        var rule = new CategoryRule(normalizedName, normalizedPattern, categoryId, operationType);
        await repository.AddAsync(rule, cancellationToken);
        return ToListItem(rule);
    }

    public async Task<CategoryRuleListItem?> UpdateAsync(
        Guid id,
        string name,
        string pattern,
        Guid categoryId,
        OperationType? operationType,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var normalizedName = NormalizeName(name);
        var normalizedPattern = NormalizePattern(pattern);
        if (!string.Equals(rule.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            && await repository.ExistsByNameAsync(normalizedName, cancellationToken))
        {
            throw new DuplicateCategoryRuleException(normalizedName);
        }

        if (await repository.ExistsByPatternAsync(normalizedPattern, operationType, rule.Id, cancellationToken))
        {
            throw new DuplicateCategoryRuleException(normalizedPattern);
        }

        await EnsureCategoryExistsAsync(categoryId, cancellationToken);
        rule.Update(normalizedName, normalizedPattern, categoryId, operationType);
        rule.SetActive(true);
        await repository.UpdateAsync(rule, cancellationToken);
        return ToListItem(rule);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await repository.GetAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        rule.SetActive(false);
        await repository.UpdateAsync(rule, cancellationToken);
        return true;
    }

    public async Task<CategoryRuleMatchResult?> FindBestMatchAsync(
        OperationKey? operation,
        CancellationToken cancellationToken)
    {
        if (operation is null)
        {
            return null;
        }

        var normalizedKey = OperationKeyExtractor.Normalize(operation.Key);
        var rules = await repository.ListAsync(cancellationToken);
        var bestMatch = rules
            .Where(rule => (rule.OperationType is null || rule.OperationType == operation.Type)
                && normalizedKey.Contains(OperationKeyExtractor.Normalize(rule.Pattern), StringComparison.Ordinal))
            .OrderByDescending(rule => rule.OperationType.HasValue)
            .ThenByDescending(rule => rule.Pattern.Length)
            .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (bestMatch is null)
        {
            return null;
        }

        var category = await categoryRepository.GetAsync(bestMatch.CategoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        return new CategoryRuleMatchResult(
            bestMatch.Id,
            bestMatch.CategoryId,
            category.Name,
            bestMatch.Name,
            bestMatch.Pattern,
            bestMatch.OperationType);
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new ArgumentException("La catégorie sélectionnée n'existe pas ou n'est plus active.", nameof(categoryId));
        }
    }

    private static string NormalizeName(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Le nom de la règle est requis.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizePattern(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Le motif de la règle est requis.", nameof(value));
        }

        return normalized;
    }

    private static CategoryRuleListItem ToListItem(CategoryRule rule) => new(
        rule.Id,
        rule.Name,
        rule.Pattern,
        rule.CategoryId,
        rule.OperationType,
        rule.IsActive);
}
