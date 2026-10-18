namespace Konta.Application.Categories;

public sealed record CategoryListItem(
    Guid Id,
    string Name,
    bool IsActive);
