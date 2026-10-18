using Konta.Domain.Transactions;

namespace Konta.Domain.Forecasts;

public sealed class RecurringForecast
{
    private RecurringForecast()
    {
    }

    public RecurringForecast(
        string name,
        string labelPattern,
        Guid accountId,
        decimal amount,
        TransactionType type,
        int dayOfMonth,
        ForecastFrequency frequency,
        DateOnly startDate,
        DateOnly? endDate,
        Guid? categoryId)
    {
        Validate(name, labelPattern, amount, type, dayOfMonth, frequency, startDate, endDate);
        Id = Guid.NewGuid();
        Name = name.Trim();
        LabelPattern = labelPattern.Trim();
        AccountId = accountId;
        Amount = amount;
        Type = type;
        DayOfMonth = dayOfMonth;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        CategoryId = categoryId;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string LabelPattern { get; private set; } = string.Empty;
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public int DayOfMonth { get; private set; }
    public ForecastFrequency Frequency { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public Guid? CategoryId { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string name,
        string labelPattern,
        decimal amount,
        TransactionType type,
        int dayOfMonth,
        ForecastFrequency frequency,
        DateOnly startDate,
        DateOnly? endDate,
        Guid? categoryId)
    {
        Validate(name, labelPattern, amount, type, dayOfMonth, frequency, startDate, endDate);
        Name = name.Trim();
        LabelPattern = labelPattern.Trim();
        Amount = amount;
        Type = type;
        DayOfMonth = dayOfMonth;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        CategoryId = categoryId;
        IsActive = true;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static void Validate(
        string name,
        string labelPattern,
        decimal amount,
        TransactionType type,
        int dayOfMonth,
        ForecastFrequency frequency,
        DateOnly startDate,
        DateOnly? endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom de l'échéance est requis.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(labelPattern))
        {
            throw new ArgumentException("Le motif de libellé est requis.", nameof(labelPattern));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Le montant prévu doit être supérieur à zéro.");
        }

        if (dayOfMonth is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), "Le jour doit être compris entre 1 et 31.");
        }

        if (!Enum.IsDefined(frequency) || !Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(frequency));
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new ArgumentException("La date de fin ne peut pas précéder la date de début.", nameof(endDate));
        }
    }
}
