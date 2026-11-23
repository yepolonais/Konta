namespace Konta.Application.Accounts;

public interface IMonthlyProjectionService
{
    Task<MonthlyProjection?> GetAsync(CancellationToken cancellationToken);
}
