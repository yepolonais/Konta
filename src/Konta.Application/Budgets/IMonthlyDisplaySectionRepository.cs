using Konta.Domain.Budgets;

namespace Konta.Application.Budgets;

public interface IMonthlyDisplaySectionRepository
{
    Task<IReadOnlyList<MonthlyDisplaySection>> ListAsync(CancellationToken cancellationToken);
    Task<MonthlyDisplaySection?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task SaveAsync(MonthlyDisplaySection section, IReadOnlyCollection<Guid> postIds, bool isNew, CancellationToken cancellationToken);
    Task DeleteAsync(MonthlyDisplaySection section, CancellationToken cancellationToken);
}
