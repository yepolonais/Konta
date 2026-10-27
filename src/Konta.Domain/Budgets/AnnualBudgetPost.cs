namespace Konta.Domain.Budgets;

public sealed class AnnualBudgetPost
{
    private AnnualBudgetPost()
    {
    }

    public AnnualBudgetPost(
        int year,
        string name,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind = null,
        int sortOrder = 0)
    {
        ValidateYear(year);
        ValidateName(name);
        ValidateType(type);
        ValidateExpenseKind(type, expenseKind);

        Id = Guid.NewGuid();
        Year = year;
        Name = name.Trim();
        Type = type;
        ExpenseKind = type == AnnualBudgetPostType.Expense
            ? expenseKind ?? AnnualBudgetExpenseKind.Variable
            : null;
        SortOrder = ValidateSortOrder(sortOrder);
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AnnualBudgetPostType Type { get; private set; }
    public AnnualBudgetExpenseKind? ExpenseKind { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<BudgetMonthlyAmount> MonthlyAmounts { get; private set; } = new List<BudgetMonthlyAmount>();
    public ICollection<BudgetPostCategoryMapping> CategoryMappings { get; private set; } =
        new List<BudgetPostCategoryMapping>();

    public void Update(string name, AnnualBudgetPostType type, AnnualBudgetExpenseKind? expenseKind = null)
    {
        ValidateName(name);
        ValidateType(type);
        ValidateExpenseKind(type, expenseKind);
        Name = name.Trim();
        if (type != AnnualBudgetPostType.Expense)
        {
            ExpenseKind = null;
        }
        else if (expenseKind.HasValue)
        {
            ExpenseKind = expenseKind;
        }
        else if (Type != AnnualBudgetPostType.Expense)
        {
            ExpenseKind = AnnualBudgetExpenseKind.Variable;
        }

        Type = type;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetSortOrder(int sortOrder) => SortOrder = ValidateSortOrder(sortOrder);

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

    private static void ValidateExpenseKind(AnnualBudgetPostType type, AnnualBudgetExpenseKind? expenseKind)
    {
        if (expenseKind.HasValue && !Enum.IsDefined(expenseKind.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(expenseKind), "La catégorie de dépense est invalide.");
        }

        if (type != AnnualBudgetPostType.Expense && expenseKind.HasValue)
        {
            throw new ArgumentException("La catégorie fixe ou variable ne s'applique qu'aux dépenses.", nameof(expenseKind));
        }

    }

    private static int ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "L'ordre du poste ne peut pas être négatif.");
        }

        return sortOrder;
    }
}
