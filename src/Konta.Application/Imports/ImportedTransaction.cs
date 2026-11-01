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
    decimal Amount,
    TransactionType Type,
    bool IsPointed,
    Guid? AccountId = null,
    string? AccountName = null,
    bool IsDuplicate = false,
    string? Fingerprint = null,
    CategoryRuleSuggestion? CategorySuggestion = null,
    IReadOnlyList<TransferMatchCandidate>? TransferMatchCandidates = null,
    TransferMatchConfirmation? TransferMatchConfirmation = null,
    bool CreateCategoryRule = false,
    string? Motif = null,
    OperationKey? Operation = null);