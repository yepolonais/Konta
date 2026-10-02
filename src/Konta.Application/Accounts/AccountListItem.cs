using Konta.Domain.Accounts;

namespace Konta.Application.Accounts;

public sealed record AccountListItem(
    Guid Id,
    string Name,
    string BankAccountNumber,
    string BankLabel,
    AccountKind Kind,
    bool IsActive);