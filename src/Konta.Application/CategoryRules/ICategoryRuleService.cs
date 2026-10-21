namespace Konta.Application.CategoryRules;

public interface ICategoryRuleService
{
    Task<IReadOnlyList<CategoryRuleListItem>> ListAsync(CancellationToken cancellationToken);
    Task<CategoryRuleListItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryRuleListItem> CreateAsync(
        string name,
        string pattern,
        Guid categoryId,
        string? subcategoryName,
        CancellationToken cancellationToken);
    Task<CategoryRuleListItem?> UpdateAsync(
        Guid id,
        string name,
        string pattern,
        Guid categoryId,
        string? subcategoryName,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryRuleMatchResult?> FindBestMatchAsync(string label, CancellationToken cancellationToken);
}
