using Konta.Domain.Transactions;

namespace Konta.Application.Imports;

public sealed record ImportedTransaction(
    int RowNumber,
    DateOnly TransactionDate,
    DateOnly BookingDate,
    string AccountNumber,
    string AccountLabel,
    string Label,
    string FullLabel,
    string? CategoryName,
    string? SubcategoryName,
    decimal Amount,
    TransactionType Type,
    bool IsPointed,
    Guid? AccountId = null,
    string? AccountName = null,
    bool IsDuplicate = false,
    string? Fingerprint = null);