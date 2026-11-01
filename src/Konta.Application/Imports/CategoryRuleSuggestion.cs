using Konta.Domain.Categories;

namespace Konta.Application.Imports;

public sealed record CategoryRuleSuggestion(
    Guid RuleId,
    string RuleName,
    string CategoryName,
    string Pattern,
    OperationType? OperationType);
