using Konta.Domain.Budgets;

namespace Konta.Application.Accounts;

public sealed record MonthlyProjection(
    Guid AccountId,
    string AccountName,
    DateOnly Today,
    DateOnly? ReferenceDate,
    decimal? CurrentBalance,
    decimal? ProjectedBalance,
    IReadOnlyList<ProjectionBalancePoint> History,
    IReadOnlyList<ProjectionBand> Bands,
    int UnassignedTransactionCount,
    int UnmappedPostCount,
    int UnplannedPostCount);

public sealed record ProjectionBalancePoint(DateOnly Date, decimal Balance);

public sealed record ProjectionBand(Guid PostId, string Name, AnnualBudgetPostType Type, decimal Amount);
