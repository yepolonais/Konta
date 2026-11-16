using Konta.Application.Budgets;
using Konta.Domain.Budgets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class MonthlyDisplaySectionRepository(KontaDbContext dbContext) : IMonthlyDisplaySectionRepository
{
    public async Task<IReadOnlyList<MonthlyDisplaySection>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.MonthlyDisplaySections.AsNoTracking().Include(section => section.Posts)
            .OrderBy(section => section.Name).ToArrayAsync(cancellationToken);

    public Task<MonthlyDisplaySection?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.MonthlyDisplaySections.Include(section => section.Posts)
            .FirstOrDefaultAsync(section => section.Id == id, cancellationToken);

    public async Task SaveAsync(
        MonthlyDisplaySection section, IReadOnlyCollection<Guid> postIds, bool isNew, CancellationToken cancellationToken)
    {
        if (isNew) dbContext.MonthlyDisplaySections.Add(section);
        var removed = section.Posts.Where(post => !postIds.Contains(post.PostId)).ToArray();
        foreach (var post in removed)
        {
            section.Posts.Remove(post);
            dbContext.MonthlyDisplaySectionPosts.Remove(post);
        }

        var current = section.Posts.Select(post => post.PostId).ToHashSet();
        foreach (var postId in postIds.Where(id => !current.Contains(id)))
            section.Posts.Add(new MonthlyDisplaySectionPost(section.Id, postId));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            throw new InvalidOperationException("La section ou ses postes ont changé. Actualisez puis réessayez.", exception);
        }
    }

    public async Task DeleteAsync(MonthlyDisplaySection section, CancellationToken cancellationToken)
    {
        dbContext.MonthlyDisplaySections.Remove(section);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
