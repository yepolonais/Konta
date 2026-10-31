namespace Konta.Application.Accounts;

public sealed record AccountBalanceSummary(
    Guid AccountId,
    string AccountName,
    Guid? ReferenceTransactionId,
    DateOnly? ReferenceDate,
    decimal? ReferenceBalance,
    decimal? ActualBalance);
