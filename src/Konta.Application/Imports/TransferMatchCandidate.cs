using Konta.Domain.Transactions;

namespace Konta.Application.Imports;

public sealed record TransferMatchCandidate(
    Guid? TransactionId,
    int? RowNumber,
    string Label,
    Guid AccountId,
    string AccountName,
    DateOnly Date,
    decimal Amount,
    TransactionType Type,
    int DateDifferenceDays,
    bool IsAmbiguous);
