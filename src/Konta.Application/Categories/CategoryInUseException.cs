namespace Konta.Application.Categories;

public sealed class CategoryInUseException(string categoryName, int transactionCount)
    : Exception($"La catégorie \"{categoryName}\" ne peut pas être supprimée : {transactionCount} opération(s) y sont rattachées.")
{
    public int TransactionCount { get; } = transactionCount;
}
