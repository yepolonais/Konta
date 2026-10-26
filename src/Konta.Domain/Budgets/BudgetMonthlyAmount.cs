namespace Konta.Domain.Budgets;

public sealed class BudgetMonthlyAmount
{
    private BudgetMonthlyAmount()
    {
    }

    public BudgetMonthlyAmount(Guid annualBudgetPostId, int month, decimal amount)
    {
        Validate(month, amount);
        Id = Guid.NewGuid();
        AnnualBudgetPostId = annualBudgetPostId;
        Month = month;
        Amount = amount;
    }

    public Guid Id { get; private set; }
    public Guid AnnualBudgetPostId { get; private set; }
    public int Month { get; private set; }
    public decimal Amount { get; private set; }

    public void Update(decimal amount) => Amount = ValidateAmount(amount);

    private static void Validate(int month, decimal amount)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Le mois doit être compris entre 1 et 12.");
        }

        ValidateAmount(amount);
    }

    private static decimal ValidateAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Le montant prévisionnel ne peut pas être négatif.");
        }

        return amount;
    }
}
