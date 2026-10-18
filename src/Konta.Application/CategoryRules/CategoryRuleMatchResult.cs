namespace Konta.Application.CategoryRules;

public sealed record CategoryRuleMatchResult(
    Guid RuleId,
    Guid CategoryId,
    string CategoryName,
    string RuleName,
    string Pattern);