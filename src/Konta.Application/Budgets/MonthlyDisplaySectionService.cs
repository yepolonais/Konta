using Konta.Domain.Budgets;

namespace Konta.Application.Budgets;

public sealed class MonthlyDisplaySectionService(
    IMonthlyDisplaySectionRepository repository,
    IAnnualBudgetRepository budgetRepository) : IMonthlyDisplaySectionService
{
    public async Task<IReadOnlyList<MonthlyDisplaySectionItem>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(ToItem).ToArray();

    public async Task<MonthlyDisplaySectionItem> CreateAsync(
        string name, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken)
    {
        var section = new MonthlyDisplaySection(name);
        await ValidateAsync(section, postIds, cancellationToken);
        await repository.SaveAsync(section, postIds, true, cancellationToken);
        return ToItem(section);
    }

    public async Task<MonthlyDisplaySectionItem?> UpdateAsync(
        Guid id, string name, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken)
    {
        var section = await repository.GetAsync(id, cancellationToken);
        if (section is null) return null;
        section.Rename(name);
        await ValidateAsync(section, postIds, cancellationToken);
        await repository.SaveAsync(section, postIds, false, cancellationToken);
        return ToItem(section);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var section = await repository.GetAsync(id, cancellationToken);
        if (section is null) return false;
        await repository.DeleteAsync(section, cancellationToken);
        return true;
    }

    private async Task ValidateAsync(MonthlyDisplaySection section, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken)
    {
        if (postIds.Distinct().Count() != postIds.Count || postIds.Contains(Guid.Empty))
            throw new ArgumentException("La liste des postes contient des identifiants invalides ou en double.", nameof(postIds));

        var sections = await repository.ListAsync(cancellationToken);
        if (sections.Any(item => item.Id != section.Id && string.Equals(item.Name, section.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Une section porte déjà ce nom.");
        if (sections.Any(item => item.Id != section.Id && item.Posts.Any(post => postIds.Contains(post.PostId))))
            throw new InvalidOperationException("Un poste sélectionné appartient déjà à une autre section.");

        var posts = (await budgetRepository.ListPostsAsync(cancellationToken)).ToDictionary(post => post.Id);
        var existing = section.Posts.Select(post => post.PostId).ToHashSet();
        foreach (var postId in postIds)
        {
            if (!posts.TryGetValue(postId, out var post)
                || (!existing.Contains(postId) && (!post.IsActive || post.Type == AnnualBudgetPostType.Savings)))
                throw new ArgumentException("Un poste sélectionné n'existe pas, n'est pas actif ou n'est ni un revenu ni une dépense.", nameof(postIds));
        }
    }

    private static MonthlyDisplaySectionItem ToItem(MonthlyDisplaySection section) =>
        new(section.Id, section.Name, section.Posts.Select(post => post.PostId).ToArray());
}
