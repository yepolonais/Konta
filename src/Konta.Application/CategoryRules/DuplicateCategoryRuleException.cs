namespace Konta.Application.CategoryRules;

public sealed class DuplicateCategoryRuleException(string ruleName)
    : Exception($"La règle de catégorie \"{ruleName}\" existe déjà.")
{
    public string RuleName { get; } = ruleName;
}
