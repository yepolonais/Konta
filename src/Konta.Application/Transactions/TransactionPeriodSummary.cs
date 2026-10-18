namespace Konta.Application.Transactions;

public sealed record TransactionPeriodSummary(
    int Year,
    int? Month,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal NetTotal,
    int TransactionCount,
    IReadOnlyList<TransactionCategorySummary> Categories);

public sealed record TransactionCategorySummary(
    Guid? CategoryId,
    string CategoryName,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal NetTotal,
    IReadOnlyList<TransactionListItem> Transactions);
