using Konta.Domain.Budgets;
using Konta.Application.Transactions;

namespace Konta.Application.Budgets;

public sealed record AnnualBudgetMonthSummary(
    int Year,
    int Month,
    decimal PlannedIncomeTotal,
    decimal PlannedExpenseTotal,
    decimal PlannedSavingsTotal,
    decimal PlannedExtraTotal,
    decimal PlannedCarryForwardAmount,
    decimal ActualSavingsTotal,
    decimal PlannedAvailableTotal,
    decimal ActualAvailableTotal,
    decimal ActualIncomeTotal,
    decimal ActualExpenseTotal,
    decimal UnassignedIncomeTotal,
    decimal UnassignedExpenseTotal,
    int UnassignedTransactionCount,
    IReadOnlyList<TransactionListItem> UnassignedTransactions,
    IReadOnlyList<AnnualBudgetMonthPostItem> Posts,
    IReadOnlyList<BudgetExtraItem> Extras);

public sealed record AnnualBudgetMonthPostItem(
    Guid Id,
    string Name,
    AnnualBudgetPostType Type,
    AnnualBudgetExpenseKind? ExpenseKind,
    int SortOrder,
    decimal? PlannedAmount,
    decimal? ActualAmount,
    decimal? VarianceAmount,
    bool IsMapped,
    int TransactionCount,
    IReadOnlyList<TransactionListItem> Transactions);
