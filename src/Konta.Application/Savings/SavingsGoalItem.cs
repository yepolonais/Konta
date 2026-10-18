namespace Konta.Application.Savings;

public sealed record SavingsGoalItem(
    Guid Id,
    string Name,
    Guid AccountId,
    string AccountName,
    decimal TargetAmount,
    decimal PlannedMonthlyContribution,
    decimal RealizedMonthlyContribution,
    decimal TotalContributions,
    decimal ProgressPercent);
