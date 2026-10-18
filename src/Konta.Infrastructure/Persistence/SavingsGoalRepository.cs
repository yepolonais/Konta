using Konta.Application.Savings;
using Konta.Domain.Savings;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class SavingsGoalRepository(KontaDbContext dbContext) : ISavingsGoalRepository
{
    public async Task<IReadOnlyList<SavingsGoal>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.SavingsGoals
            .AsNoTracking()
            .Where(goal => goal.IsActive)
            .OrderBy(goal => goal.Name)
            .ToListAsync(cancellationToken);

    public Task<SavingsGoal?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SavingsGoals.FirstOrDefaultAsync(
            goal => goal.Id == id && goal.IsActive,
            cancellationToken);

    public Task<bool> HasActiveForAccountAsync(
        Guid accountId,
        Guid? exceptGoalId,
        CancellationToken cancellationToken) =>
        dbContext.SavingsGoals.AnyAsync(
            goal => goal.AccountId == accountId
                && goal.IsActive
                && (!exceptGoalId.HasValue || goal.Id != exceptGoalId.Value),
            cancellationToken);

    public async Task AddAsync(SavingsGoal goal, CancellationToken cancellationToken)
    {
        dbContext.SavingsGoals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SavingsGoal goal, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SavingsGoalContribution>> ListContributionsAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.AccountTransfers
            .AsNoTracking()
            .Where(transfer => transfer.DestinationTransaction.AccountId == accountId)
            .Select(transfer => new SavingsGoalContribution(
                transfer.DestinationTransaction.Date,
                transfer.Amount))
            .ToListAsync(cancellationToken);
}
