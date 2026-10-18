namespace Konta.Domain.Budgets;

public sealed class BudgetOverride
{
    private BudgetOverride()
    {
    }

    public BudgetOverride(Guid budgetId, int year, int month, decimal amount)
    {
        Validate(year, month, amount);
        Id = Guid.NewGuid();
        BudgetId = budgetId;
        Year = year;
        Month = month;
        Amount = amount;
    }

    public Guid Id { get; private set; }
    public Guid BudgetId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal Amount { get; private set; }

    public void Update(decimal amount) => Amount = ValidateAmount(amount);

    private static void Validate(int year, int month, decimal amount)
    {
        if (year is < 1 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        ValidateAmount(amount);
    }

    private static decimal ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Le budget exceptionnel doit être supérieur à zéro.");
        }

        return amount;
    }
}
