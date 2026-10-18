namespace Konta.Application.Savings;

public interface ISavingsGoalService
{
    Task<IReadOnlyList<SavingsGoalItem>> ListAsync(int year, int month, CancellationToken cancellationToken);
    Task<SavingsGoalItem> CreateAsync(
        string name,
        Guid accountId,
        decimal targetAmount,
        decimal plannedMonthlyContribution,
        int year,
        int month,
        CancellationToken cancellationToken);
    Task<SavingsGoalItem?> UpdateAsync(
        Guid id,
        string name,
        Guid accountId,
        decimal targetAmount,
        decimal plannedMonthlyContribution,
        int year,
        int month,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
