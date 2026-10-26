using Konta.Application.Budgets;
using Konta.Domain.Budgets;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class AnnualBudgetRepository(KontaDbContext dbContext) : IAnnualBudgetRepository
{
    public async Task<IReadOnlyList<AnnualBudgetPost>> ListPostsAsync(
        int year,
        CancellationToken cancellationToken) =>
        await dbContext.AnnualBudgetPosts
            .Include(post => post.MonthlyAmounts)
            .Include(post => post.CategoryMappings)
                .ThenInclude(mapping => mapping.Category)
            .Where(post => post.Year == year)
            .OrderBy(post => post.Type)
            .ThenBy(post => post.Name)
            .ToArrayAsync(cancellationToken);

    public Task<AnnualBudgetPost?> GetPostAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AnnualBudgetPosts
            .Include(post => post.MonthlyAmounts)
            .Include(post => post.CategoryMappings)
                .ThenInclude(mapping => mapping.Category)
            .FirstOrDefaultAsync(post => post.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AnnualBudgetPost>> ListPostsForYearAsync(
        int year,
        CancellationToken cancellationToken) =>
        await dbContext.AnnualBudgetPosts
            .AsNoTracking()
            .Include(post => post.MonthlyAmounts)
            .Include(post => post.CategoryMappings)
                .ThenInclude(mapping => mapping.Category)
            .Where(post => post.Year == year)
            .OrderBy(post => post.Type)
            .ThenBy(post => post.Name)
            .ToArrayAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(
        int year,
        string name,
        Guid? excludedId,
        CancellationToken cancellationToken) =>
        dbContext.AnnualBudgetPosts.AnyAsync(
            post => post.Year == year
                && post.Name.ToLower() == name.ToLower()
                && (!excludedId.HasValue || post.Id != excludedId.Value),
            cancellationToken);

    public async Task AddPostAsync(AnnualBudgetPost post, CancellationToken cancellationToken)
    {
        dbContext.AnnualBudgetPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePostAsync(AnnualBudgetPost post, CancellationToken cancellationToken)
    {
        dbContext.AnnualBudgetPosts.Update(post);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetMonthlyAmountAsync(
        Guid postId,
        int month,
        decimal? amount,
        CancellationToken cancellationToken)
    {
        var post = await GetPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return;
        }

        var existing = post.MonthlyAmounts.SingleOrDefault(item => item.Month == month);

        if (amount is null)
        {
            if (existing is not null)
            {
                post.MonthlyAmounts.Remove(existing);
                dbContext.BudgetMonthlyAmounts.Remove(existing);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (existing is null)
        {
            var monthlyAmount = new BudgetMonthlyAmount(postId, month, amount.Value);
            post.MonthlyAmounts.Add(monthlyAmount);
            dbContext.BudgetMonthlyAmounts.Add(monthlyAmount);
        }
        else
        {
            existing.Update(amount.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetCategoryMappingsAsync(
        Guid postId,
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        var post = await dbContext.AnnualBudgetPosts
            .Include(item => item.CategoryMappings)
            .FirstOrDefaultAsync(item => item.Id == postId, cancellationToken);
        if (post is null)
        {
            return;
        }

        var mappingsToRemove = post.CategoryMappings
            .Where(mapping => !categoryIds.Contains(mapping.CategoryId))
            .ToArray();
        dbContext.BudgetPostCategoryMappings.RemoveRange(mappingsToRemove);

        var existingCategoryIds = post.CategoryMappings
            .Where(mapping => categoryIds.Contains(mapping.CategoryId))
            .Select(mapping => mapping.CategoryId)
            .ToHashSet();
        foreach (var categoryId in categoryIds.Where(id => !existingCategoryIds.Contains(id)))
        {
            post.CategoryMappings.Add(new BudgetPostCategoryMapping(postId, post.Year, categoryId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
