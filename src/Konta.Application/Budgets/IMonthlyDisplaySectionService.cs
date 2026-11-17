namespace Konta.Application.Budgets;

public interface IMonthlyDisplaySectionService
{
    Task<IReadOnlyList<MonthlyDisplaySectionItem>> ListAsync(CancellationToken cancellationToken);
    Task<MonthlyDisplaySectionItem> CreateAsync(string name, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);
    Task<MonthlyDisplaySectionItem?> UpdateAsync(Guid id, string name, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>A custom grouping of budget posts shared by the forecast and monthly review.</summary>
public sealed record MonthlyDisplaySectionItem(Guid Id, string Name, IReadOnlyList<Guid> PostIds);
