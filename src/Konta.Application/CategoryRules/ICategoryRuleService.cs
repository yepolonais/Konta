using Konta.Application.Imports;
using Konta.Domain.Categories;

namespace Konta.Application.CategoryRules;

public interface ICategoryRuleService
{
    Task<IReadOnlyList<CategoryRuleListItem>> ListAsync(CancellationToken cancellationToken);
    Task<CategoryRuleListItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryRuleListItem> CreateAsync(
        string name,
        string pattern,
        Guid categoryId,
        OperationType? operationType,
        CancellationToken cancellationToken);
    Task<CategoryRuleListItem?> UpdateAsync(
        Guid id,
        string name,
        string pattern,
        Guid categoryId,
        OperationType? operationType,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryRuleMatchResult?> FindBestMatchAsync(OperationKey? operation, CancellationToken cancellationToken);
}
