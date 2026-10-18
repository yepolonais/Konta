using Konta.Domain.Transactions;

namespace Konta.Application.Planning;

public sealed record ForecastOccurrenceItem(
    Guid ForecastId,
    string Name,
    string LabelPattern,
    Guid AccountId,
    string AccountName,
    DateOnly ScheduledDate,
    decimal ForecastAmount,
    decimal? ActualAmount,
    TransactionType Type,
    Guid? CategoryId,
    bool IsCompleted);
