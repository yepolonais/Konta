using Konta.Domain.Budgets;

namespace Konta.Application.Budgets;

public interface IAnnualBudgetService
{
    Task<AnnualBudgetSummary> GetAsync(int year, CancellationToken cancellationToken);
    Task<AnnualBudgetMonthSummary> GetMonthAsync(int year, int month, CancellationToken cancellationToken);
    Task<AnnualBudgetPostItem> CreatePostAsync(
        int year,
        string name,
        AnnualBudgetPostType type,
        CancellationToken cancellationToken);
    Task<AnnualBudgetPostItem?> UpdatePostAsync(
        Guid id,
        string name,
        AnnualBudgetPostType type,
        CancellationToken cancellationToken);
    Task<bool> DeletePostAsync(Guid id, CancellationToken cancellationToken);
    Task<AnnualBudgetPostItem?> SetMonthlyAmountAsync(
        Guid id,
        int month,
        decimal? amount,
        CancellationToken cancellationToken);
    Task<AnnualBudgetPostItem?> CopyMonthlyAmountForwardAsync(
        Guid id,
        int month,
        CancellationToken cancellationToken);
    Task<AnnualBudgetPostItem?> SetCategoryMappingsAsync(
        Guid id,
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);
}
