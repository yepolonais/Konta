using Konta.Domain.Categories;

namespace Konta.Application.CategoryRules;

public interface ICategoryRuleRepository
{
    Task<IReadOnlyList<CategoryRule>> ListAsync(CancellationToken cancellationToken);
    Task<CategoryRule?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task<bool> ExistsByPatternAsync(
        string pattern,
        OperationType? operationType,
        Guid? excludingRuleId,
        CancellationToken cancellationToken);
    Task AddAsync(CategoryRule rule, CancellationToken cancellationToken);
    Task UpdateAsync(CategoryRule rule, CancellationToken cancellationToken);
}
