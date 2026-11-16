namespace Konta.Domain.Budgets;

public sealed class MonthlyDisplaySection
{
    private MonthlyDisplaySection() { }

    public MonthlyDisplaySection(string name)
    {
        Id = Guid.NewGuid();
        Rename(name);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<MonthlyDisplaySectionPost> Posts { get; private set; } = new List<MonthlyDisplaySectionPost>();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new ArgumentException("Le nom de la section doit contenir entre 1 et 100 caractères.", nameof(name));
        }

        Name = name.Trim();
    }
}
