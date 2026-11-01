namespace Konta.Application.CategoryRules;

public sealed record CategoryRuleListItem(
    Guid Id,
    string Name,
    string Pattern,
    Guid CategoryId,
    bool IsActive);
