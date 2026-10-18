namespace Konta.Application.Categories;

public sealed class DuplicateCategoryException(string categoryName)
    : Exception($"La catégorie \"{categoryName}\" existe déjà.")
{
    public string CategoryName { get; } = categoryName;
}
