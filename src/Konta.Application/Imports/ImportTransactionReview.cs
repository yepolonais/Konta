using Konta.Domain.Transactions;

namespace Konta.Application.Imports;

public sealed record ImportTransactionReview(
    int RowNumber,
    bool Include = true,
    DateOnly? TransactionDate = null,
    string? Label = null,
    decimal? Amount = null,
    TransactionType? Type = null,
    Guid? AccountId = null,
    string? AccountName = null,
    string? CategoryName = null,
    string? SubcategoryName = null);
