using Konta.Domain.Savings;

namespace Konta.Application.Savings;

public interface ISavingsGoalRepository
{
    Task<IReadOnlyList<SavingsGoal>> ListAsync(CancellationToken cancellationToken);
    Task<SavingsGoal?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasActiveForAccountAsync(Guid accountId, Guid? exceptGoalId, CancellationToken cancellationToken);
    Task AddAsync(SavingsGoal goal, CancellationToken cancellationToken);
    Task UpdateAsync(SavingsGoal goal, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavingsGoalContribution>> ListContributionsAsync(
        Guid accountId,
        CancellationToken cancellationToken);
}
