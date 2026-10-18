using Konta.Domain.Transactions;

namespace Konta.Application.Transactions;

public sealed class TransactionQueryService(ITransactionRepository repository) : ITransactionQueryService
{
    public Task<IReadOnlyList<TransactionListItem>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public async Task<TransactionPeriodSummary> GetPeriodSummaryAsync(
        int year,
        int? month,
        Guid? accountId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (year is < 1 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "L'année doit être comprise entre 1 et 9998.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Le mois doit être compris entre 1 et 12.");
        }

        var startDate = new DateOnly(year, month ?? 1, 1);
        var endDate = month.HasValue
            ? startDate.AddMonths(1)
            : new DateOnly(year + 1, 1, 1);
        var transactions = await repository.ListForPeriodAsync(
            startDate,
            endDate,
            accountId,
            categoryId,
            cancellationToken);

        var householdTransactions = accountId.HasValue
            ? transactions
            : transactions.Where(transaction => !transaction.IsTransfer).ToArray();
        var categories = householdTransactions
            .GroupBy(transaction => (transaction.CategoryId, transaction.CategoryName))
            .Select(group =>
            {
                var items = group.ToArray();
                var income = items.Where(item => item.Type == TransactionType.Income)
                    .Sum(item => item.Amount);
                var expense = items.Where(item => item.Type == TransactionType.Expense)
                    .Sum(item => item.Amount);
                return new TransactionCategorySummary(
                    group.Key.CategoryId,
                    group.Key.CategoryName ?? "Non catégorisé",
                    income,
                    expense,
                    income - expense,
                    items);
            })
            .OrderByDescending(category => category.ExpenseTotal)
            .ThenBy(category => category.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var totalIncome = householdTransactions
            .Where(item => item.Type == TransactionType.Income)
            .Sum(item => item.Amount);
        var totalExpense = householdTransactions
            .Where(item => item.Type == TransactionType.Expense)
            .Sum(item => item.Amount);

        return new TransactionPeriodSummary(
            year,
            month,
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            householdTransactions.Count,
            categories);
    }
}