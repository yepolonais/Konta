namespace Konta.Application.Budgets;

public sealed class DuplicateAnnualBudgetPostException(string name)
    : Exception($"Un poste nommé « {name} » existe déjà pour cette année.");
