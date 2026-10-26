namespace Konta.Domain.Budgets;

public sealed class AnnualBudgetPost
{
    private AnnualBudgetPost()
    {
    }

    public AnnualBudgetPost(int year, string name, AnnualBudgetPostType type)
    {
        ValidateYear(year);
        ValidateName(name);
        ValidateType(type);

        Id = Guid.NewGuid();
        Year = year;
        Name = name.Trim();
        Type = type;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AnnualBudgetPostType Type { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<BudgetMonthlyAmount> MonthlyAmounts { get; private set; } = new List<BudgetMonthlyAmount>();
    public ICollection<BudgetPostCategoryMapping> CategoryMappings { get; private set; } =
        new List<BudgetPostCategoryMapping>();

    public void Update(string name, AnnualBudgetPostType type)
    {
        ValidateName(name);
        ValidateType(type);
        Name = name.Trim();
        Type = type;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static void ValidateYear(int year)
    {
        if (year is < 1 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "L'année doit être comprise entre 1 et 9998.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new ArgumentException("Le nom du poste doit contenir entre 1 et 100 caractères.", nameof(name));
        }
    }

    private static void ValidateType(AnnualBudgetPostType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Le type du poste budgétaire est invalide.");
        }
    }
}
