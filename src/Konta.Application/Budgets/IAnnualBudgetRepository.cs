using Konta.Domain.Budgets;

namespace Konta.Application.Budgets;

public interface IAnnualBudgetRepository
{
    Task<IReadOnlyList<AnnualBudgetPost>> ListPostsAsync(int year, CancellationToken cancellationToken);
    Task<AnnualBudgetPost?> GetPostAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int year, string name, Guid? excludedId, CancellationToken cancellationToken);
    Task AddPostAsync(AnnualBudgetPost post, CancellationToken cancellationToken);
    Task UpdatePostAsync(AnnualBudgetPost post, CancellationToken cancellationToken);
    Task DeletePostAsync(AnnualBudgetPost post, CancellationToken cancellationToken);
    Task SetMonthlyAmountAsync(Guid postId, int month, decimal? amount, CancellationToken cancellationToken);
    Task SetCategoryMappingsAsync(
        Guid postId,
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AnnualBudgetPost>> ListPostsForYearAsync(
        int year,
        CancellationToken cancellationToken);
}
