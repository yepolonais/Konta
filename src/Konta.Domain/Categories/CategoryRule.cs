namespace Konta.Domain.Categories;

public sealed class CategoryRule
{
    private CategoryRule()
    {
    }

    public CategoryRule(string name, string pattern, Guid categoryId, string? subcategoryName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A rule name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("A rule pattern is required.", nameof(pattern));
        }

        var normalizedSubcategory = NormalizeSubcategory(subcategoryName);
        Id = Guid.NewGuid();
        Name = name.Trim();
        Pattern = pattern.Trim();
        CategoryId = categoryId;
        SubcategoryName = normalizedSubcategory;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Pattern { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public string? SubcategoryName { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string pattern, Guid categoryId, string? subcategoryName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A rule name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("A rule pattern is required.", nameof(pattern));
        }

        var normalizedSubcategory = NormalizeSubcategory(subcategoryName);
        Name = name.Trim();
        Pattern = pattern.Trim();
        CategoryId = categoryId;
        SubcategoryName = normalizedSubcategory;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string? NormalizeSubcategory(string? subcategoryName)
    {
        if (string.IsNullOrWhiteSpace(subcategoryName))
        {
            return null;
        }

        var normalized = subcategoryName.Trim();
        if (normalized.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(subcategoryName), "The subcategory cannot exceed 200 characters.");
        }

        return normalized;
    }
}
