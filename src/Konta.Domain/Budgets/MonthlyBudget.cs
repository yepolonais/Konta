namespace Konta.Domain.Budgets;

public sealed class MonthlyBudget
{
    private MonthlyBudget()
    {
    }

    public MonthlyBudget(Guid categoryId, decimal amount)
    {
        ValidateAmount(amount);
        Id = Guid.NewGuid();
        CategoryId = categoryId;
        Amount = amount;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(decimal amount)
    {
        ValidateAmount(amount);
        Amount = amount;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Le budget doit être supérieur à zéro.");
        }
    }
}
