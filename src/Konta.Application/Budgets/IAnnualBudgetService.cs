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
        AnnualBudgetExpenseKind? expenseKind,
        CancellationToken cancellationToken);
    Task<AnnualBudgetPostItem?> UpdatePostAsync(
        Guid id,
        string name,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind,
        CancellationToken cancellationToken);
    Task ReorderPostsAsync(
        int year,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind,
        IReadOnlyList<Guid> postIds,
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
    Task<AnnualBudgetPostItem?> ClearMonthlyAmountsFromAsync(
        Guid id,
        int month,
        CancellationToken cancellationToken);
    Task<BudgetExtraItem> AddExtraAsync(
        int year,
        int month,
        string label,
        decimal amount,
        CancellationToken cancellationToken);
    Task<bool> DeleteExtraAsync(Guid id, CancellationToken cancellationToken);
    Task<AnnualBudgetPostItem?> SetCategoryMappingsAsync(
        Guid id,
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);
}
