using Konta.Application.Budgets;
using Konta.Domain.Budgets;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class AnnualBudgetRepository(KontaDbContext dbContext) : IAnnualBudgetRepository
{
    public async Task<IReadOnlyList<AnnualBudgetPost>> ListPostsAsync(CancellationToken cancellationToken) =>
        await dbContext.AnnualBudgetPosts
            .AsNoTracking()
            .Include(post => post.MonthlyAmounts)
            .Include(post => post.CategoryMappings)
                .ThenInclude(mapping => mapping.Category)
            .OrderBy(post => post.Type)
            .ThenBy(post => post.ExpenseKind)
            .ThenBy(post => post.SortOrder)
            .ThenBy(post => post.Name)
            .ToArrayAsync(cancellationToken);

    public Task<AnnualBudgetPost?> GetPostAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AnnualBudgetPosts
            .Include(post => post.MonthlyAmounts)
            .Include(post => post.CategoryMappings)
                .ThenInclude(mapping => mapping.Category)
            .FirstOrDefaultAsync(post => post.Id == id, cancellationToken);

    public async Task SetPostOrderAsync(
        IReadOnlyList<Guid> postIds,
        CancellationToken cancellationToken)
    {
        var posts = await dbContext.AnnualBudgetPosts
            .Where(post => postIds.Contains(post.Id))
            .ToArrayAsync(cancellationToken);
        if (posts.Length != postIds.Count)
        {
            throw new InvalidOperationException("Un ou plusieurs postes n'existent plus.");
        }

        var postsById = posts.ToDictionary(post => post.Id);
        for (var index = 0; index < postIds.Count; index++)
        {
            postsById[postIds[index]].SetSortOrder(index);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedId,
        CancellationToken cancellationToken) =>
        dbContext.AnnualBudgetPosts.AnyAsync(
            post => post.Name.ToLower() == name.ToLower()
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

    public async Task DeletePostAsync(AnnualBudgetPost post, CancellationToken cancellationToken)
    {
        dbContext.AnnualBudgetPosts.Remove(post);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetMonthlyAmountAsync(
        Guid postId,
        int year,
        int month,
        decimal? amount,
        CancellationToken cancellationToken)
    {
        var post = await GetPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return;
        }

        var existing = post.MonthlyAmounts.SingleOrDefault(item => item.Year == year && item.Month == month);

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
            var monthlyAmount = new BudgetMonthlyAmount(postId, year, month, amount.Value);
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
        var addedCategoryIds = categoryIds.Where(id => !existingCategoryIds.Contains(id)).ToArray();
        var removedCategoryIds = mappingsToRemove.Select(mapping => mapping.CategoryId).ToArray();
        foreach (var categoryId in addedCategoryIds)
        {
            post.CategoryMappings.Add(new BudgetPostCategoryMapping(postId, categoryId));
        }

        var changedCategoryIds = addedCategoryIds.Concat(removedCategoryIds).ToArray();
        var transactions = await dbContext.Transactions
            .Where(transaction => transaction.CategoryId.HasValue
                && changedCategoryIds.Contains(transaction.CategoryId.Value))
            .ToListAsync(cancellationToken);
        foreach (var transaction in transactions)
        {
            var assignedPostId = addedCategoryIds.Contains(transaction.CategoryId!.Value) ? postId : (Guid?)null;
            transaction.SetBudgetAssignment(assignedPostId, transaction.BudgetDescription);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
