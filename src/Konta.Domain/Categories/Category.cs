namespace Konta.Domain.Categories;

public sealed class Category
{
    private Category()
    {
    }

    public Category(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A category name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
}