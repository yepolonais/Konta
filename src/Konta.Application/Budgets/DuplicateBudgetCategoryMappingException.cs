namespace Konta.Application.Budgets;

public sealed class DuplicateBudgetCategoryMappingException(string categoryName, string postName)
    : Exception($"La catégorie « {categoryName} » est déjà rattachée au poste « {postName} » pour cette année.");
