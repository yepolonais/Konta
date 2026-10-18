namespace Konta.Domain.Savings;

public sealed class SavingsGoal
{
    private SavingsGoal()
    {
    }

    public SavingsGoal(string name, Guid accountId, decimal targetAmount, decimal plannedMonthlyContribution)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A savings goal name is required.", nameof(name));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("A savings account is required.", nameof(accountId));
        }

        ValidateAmount(targetAmount, nameof(targetAmount));
        ValidateAmount(plannedMonthlyContribution, nameof(plannedMonthlyContribution));

        Id = Guid.NewGuid();
        Name = name.Trim();
        AccountId = accountId;
        TargetAmount = targetAmount;
        PlannedMonthlyContribution = plannedMonthlyContribution;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid AccountId { get; private set; }
    public decimal TargetAmount { get; private set; }
    public decimal PlannedMonthlyContribution { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, Guid accountId, decimal targetAmount, decimal plannedMonthlyContribution)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("An inactive savings goal cannot be updated.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A savings goal name is required.", nameof(name));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("A savings account is required.", nameof(accountId));
        }

        ValidateAmount(targetAmount, nameof(targetAmount));
        ValidateAmount(plannedMonthlyContribution, nameof(plannedMonthlyContribution));
        Name = name.Trim();
        AccountId = accountId;
        TargetAmount = targetAmount;
        PlannedMonthlyContribution = plannedMonthlyContribution;
    }

    public void Deactivate() => IsActive = false;

    private static void ValidateAmount(decimal amount, string parameterName)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Amounts must be positive.");
        }
    }
}
