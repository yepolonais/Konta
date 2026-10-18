using Konta.Domain.Forecasts;
using Konta.Domain.Transactions;

namespace Konta.Application.Planning;

public sealed record RecurringForecastItem(
    Guid Id,
    string Name,
    string LabelPattern,
    Guid AccountId,
    string AccountName,
    decimal Amount,
    TransactionType Type,
    int DayOfMonth,
    ForecastFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    Guid? CategoryId,
    bool IsActive);
