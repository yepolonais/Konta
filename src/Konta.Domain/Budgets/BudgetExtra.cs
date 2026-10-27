namespace Konta.Domain.Budgets;

public sealed class BudgetExtra
{
    private BudgetExtra()
    {
        Label = string.Empty;
    }

    public BudgetExtra(int year, int month, string label, decimal amount)
    {
        if (year is < 1 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "L'année doit être comprise entre 1 et 9998.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Le mois doit être compris entre 1 et 12.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Le libellé de l'extra est obligatoire.", nameof(label));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Le montant de l'extra doit être positif.");
        }

        Id = Guid.NewGuid();
        Year = year;
        Month = month;
        Label = label.Trim();
        Amount = amount;
    }

    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Label { get; private set; }
    public decimal Amount { get; private set; }
}
