using Konta.Domain.Transactions;

namespace Konta.Application.Transactions;

public sealed record TransactionListItem(
    Guid Id,
    DateOnly Date,
    string Label,
    decimal Amount,
    TransactionType Type,
    string? AccountNumber,
    string? AccountLabel,
    string? AccountName,
    string? CategoryName,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    bool IsTransfer = false);