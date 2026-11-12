using Konta.Application.Categories;
using Konta.Application.Budgets;
using Konta.Domain.Budgets;
using Konta.Domain.Transactions;

namespace Konta.Application.Transactions;

public sealed class TransactionQueryService(
    ITransactionRepository repository,
    ICategoryRepository categoryRepository,
    IAnnualBudgetRepository budgetRepository) : ITransactionQueryService
{
    public async Task<bool> ChangeCategoryAsync(
        Guid transactionId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        var category = await categoryRepository.GetAsync(categoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            throw new ArgumentException("La catégorie choisie n'existe pas ou est désactivée.", nameof(categoryId));
        }

        transaction.ChangeCategory(categoryId);
        var postId = category.IsMultiPost
            ? null
            : await repository.GetCategoryBudgetPostIdAsync(categoryId, cancellationToken);
        transaction.SetBudgetAssignment(postId, transaction.BudgetDescription);
        await repository.UpdateAsync(transaction, cancellationToken);
        return true;
    }

    public async Task<bool> SetBudgetAssignmentAsync(
        Guid transactionId,
        Guid? postId,
        string? description,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        var category = transaction.CategoryId.HasValue
            ? await categoryRepository.GetAsync(transaction.CategoryId.Value, cancellationToken)
            : null;
        if (category is null || !category.IsMultiPost)
        {
            throw new InvalidOperationException("L'opération doit appartenir à une catégorie multi-postes.");
        }

        if (postId.HasValue)
        {
            var post = await budgetRepository.GetPostAsync(postId.Value, cancellationToken);
            if (post is null || !post.IsActive
                || (post.Type == AnnualBudgetPostType.Income) != (transaction.Type == TransactionType.Income))
            {
                throw new ArgumentException("Le poste choisi n'existe pas, n'est pas actif ou ne correspond pas au type de l'opération.", nameof(postId));
            }
            if (post.Type != AnnualBudgetPostType.Savings
                && await repository.IsTransferAsync(transactionId, cancellationToken))
            {
                throw new ArgumentException("Un transfert interne ne peut être rattaché qu'à un poste d'épargne.", nameof(postId));
            }
        }

        transaction.SetBudgetAssignment(postId, description);
        await repository.UpdateAsync(transaction, cancellationToken);
        return true;
    }

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