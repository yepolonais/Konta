using Konta.Domain.Budgets;

namespace Konta.Application.Budgets;

public interface IAnnualBudgetRepository
{
    Task<IReadOnlyList<AnnualBudgetPost>> ListPostsAsync(CancellationToken cancellationToken);
    Task<AnnualBudgetPost?> GetPostAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, Guid? excludedId, CancellationToken cancellationToken);
    Task AddPostAsync(AnnualBudgetPost post, CancellationToken cancellationToken);
    Task UpdatePostAsync(AnnualBudgetPost post, CancellationToken cancellationToken);
    Task DeletePostAsync(AnnualBudgetPost post, CancellationToken cancellationToken);
    Task SetPostOrderAsync(IReadOnlyList<Guid> postIds, CancellationToken cancellationToken);
    Task SetMonthlyAmountAsync(Guid postId, int year, int month, decimal? amount, CancellationToken cancellationToken);
    Task SetCategoryMappingsAsync(
        Guid postId,
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);
}
