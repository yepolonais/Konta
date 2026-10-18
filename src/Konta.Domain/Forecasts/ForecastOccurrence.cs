namespace Konta.Domain.Forecasts;

public sealed class ForecastOccurrence
{
    private ForecastOccurrence()
    {
    }

    public ForecastOccurrence(Guid forecastId, DateOnly scheduledDate, Guid transactionId, decimal actualAmount)
    {
        if (actualAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualAmount));
        }

        Id = Guid.NewGuid();
        ForecastId = forecastId;
        ScheduledDate = scheduledDate;
        TransactionId = transactionId;
        ActualAmount = actualAmount;
    }

    public Guid Id { get; private set; }
    public Guid ForecastId { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal ActualAmount { get; private set; }
}
