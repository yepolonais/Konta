namespace Konta.Application.Planning;

public sealed record ForecastMatchCandidate(
    Guid ForecastId,
    string ForecastName,
    DateOnly ScheduledDate,
    decimal ForecastAmount,
    decimal AmountDifference,
    int DateDifferenceDays,
    bool IsAmbiguous);
