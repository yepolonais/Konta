namespace Konta.Application.Planning;

public sealed record AccountBalanceSummary(
    Guid AccountId,
    string AccountName,
    Guid? ReferenceTransactionId,
    DateOnly? ReferenceDate,
    decimal? ReferenceBalance,
    decimal? ActualBalance,
    decimal? ForecastBalance,
    DateOnly ThroughDate);
