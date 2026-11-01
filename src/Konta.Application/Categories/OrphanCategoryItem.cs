namespace Konta.Application.Categories;

public sealed record OrphanCategoryItem(
    Guid Id,
    string Name,
    int TransactionCount,
    decimal TotalAmount);
