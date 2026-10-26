using Konta.Domain.Budgets;
using Konta.Application.Transactions;

namespace Konta.Application.Budgets;

public sealed record AnnualBudgetMonthSummary(
    int Year,
    int Month,
    decimal PlannedIncomeTotal,
    decimal PlannedExpenseTotal,
    decimal PlannedSavingsTotal,
    decimal ActualSavingsTotal,
    decimal ActualIncomeTotal,
    decimal ActualExpenseTotal,
    decimal UnassignedIncomeTotal,
    decimal UnassignedExpenseTotal,
    int UnassignedTransactionCount,
    IReadOnlyList<TransactionListItem> UnassignedTransactions,
    IReadOnlyList<AnnualBudgetMonthPostItem> Posts);

public sealed record AnnualBudgetMonthPostItem(
    Guid Id,
    string Name,
    AnnualBudgetPostType Type,
    decimal? PlannedAmount,
    decimal? ActualAmount,
    decimal? VarianceAmount,
    bool IsMapped,
    int TransactionCount,
    IReadOnlyList<TransactionListItem> Transactions);
