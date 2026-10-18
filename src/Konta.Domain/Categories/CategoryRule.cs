namespace Konta.Domain.Categories;

public sealed class CategoryRule
{
    private CategoryRule()
    {
    }

    public CategoryRule(string name, string pattern, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A rule name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("A rule pattern is required.", nameof(pattern));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Pattern = pattern.Trim();
        CategoryId = categoryId;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Pattern { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string pattern, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A rule name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("A rule pattern is required.", nameof(pattern));
        }

        Name = name.Trim();
        Pattern = pattern.Trim();
        CategoryId = categoryId;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
