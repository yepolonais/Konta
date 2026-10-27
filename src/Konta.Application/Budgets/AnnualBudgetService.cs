using Konta.Domain.Budgets;
using Konta.Application.Categories;
using Konta.Application.Transactions;
using Konta.Domain.Transactions;

namespace Konta.Application.Budgets;

public sealed class AnnualBudgetService(
    IAnnualBudgetRepository repository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository) : IAnnualBudgetService
{
    public async Task<AnnualBudgetSummary> GetAsync(int year, CancellationToken cancellationToken)
    {
        ValidateYear(year);
        var posts = (await repository.ListPostsAsync(year, cancellationToken))
            .Select(ToItem)
            .ToArray();
        var activePosts = posts.Where(post => post.IsActive).ToArray();
        var incomeTotal = activePosts
            .Where(post => post.Type == AnnualBudgetPostType.Income)
            .Sum(post => post.AnnualTotal);
        var expenseTotal = activePosts
            .Where(post => post.Type == AnnualBudgetPostType.Expense)
            .Sum(post => post.AnnualTotal);
        var savingsTotal = activePosts
            .Where(post => post.Type == AnnualBudgetPostType.Savings)
            .Sum(post => post.AnnualTotal);

        var extras = (await repository.ListExtrasAsync(year, cancellationToken)).Select(ToExtraItem).ToArray();
        var extraTotal = extras.Sum(extra => extra.Amount);

        return new AnnualBudgetSummary(
            year,
            incomeTotal,
            expenseTotal,
            savingsTotal,
            extraTotal,
            incomeTotal + extraTotal - expenseTotal - savingsTotal,
            posts,
            extras);
    }

    public async Task<AnnualBudgetMonthSummary> GetMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidateYear(year);
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Le mois doit être compris entre 1 et 12.");
        }

        var posts = await repository.ListPostsForYearAsync(year, cancellationToken);
        var startDate = new DateOnly(year, month, 1);
        var allTransactions = await transactionRepository.ListForPeriodAsync(
            startDate,
            startDate.AddMonths(1),
            null,
            null,
            cancellationToken);
        var activePosts = posts.Where(post => post.IsActive).ToArray();
        var savingsCategoryIds = activePosts
            .Where(post => post.Type == AnnualBudgetPostType.Savings)
            .SelectMany(post => post.CategoryMappings.Select(mapping => mapping.CategoryId))
            .ToHashSet();
        // Les transferts sont écartés du réel, sauf pour l'épargne où ils constituent le versement.
        var transactions = allTransactions
            .Where(transaction => !transaction.IsTransfer
                && !(transaction.CategoryId.HasValue && savingsCategoryIds.Contains(transaction.CategoryId.Value)))
            .ToArray();
        var categoryPostMappings = activePosts
            .SelectMany(post => post.CategoryMappings.Select(mapping => (mapping.CategoryId, Post: post)))
            .ToDictionary(mapping => mapping.CategoryId, mapping => mapping.Post);
        var postItems = activePosts.Select(post =>
        {
            var plannedAmount = post.MonthlyAmounts
                .Where(amount => amount.Month == month)
                .Select(amount => (decimal?)amount.Amount)
                .SingleOrDefault();
            var categoryIds = post.CategoryMappings.Select(mapping => mapping.CategoryId).ToHashSet();
            var isComparable = categoryIds.Count > 0;
            var source = post.Type == AnnualBudgetPostType.Savings ? allTransactions : transactions;
            var matchedTransactions = isComparable
                ? source
                    .Where(transaction => transaction.CategoryId.HasValue
                        && categoryIds.Contains(transaction.CategoryId.Value)
                        && TransactionTypeMatches(post.Type, transaction.Type))
                    .ToArray()
                : [];
            var actualAmount = isComparable ? matchedTransactions.Sum(transaction => transaction.Amount) : (decimal?)null;
            decimal? varianceAmount = actualAmount.HasValue && plannedAmount.HasValue
                ? actualAmount.Value - plannedAmount.Value
                : null;

            return new AnnualBudgetMonthPostItem(
                post.Id,
                post.Name,
                post.Type,
                post.ExpenseKind,
                post.SortOrder,
                plannedAmount,
                actualAmount,
                varianceAmount,
                isComparable,
                matchedTransactions.Length,
                matchedTransactions);
        }).ToArray();

        var unassignedTransactions = transactions.Where(transaction =>
        {
            if (!transaction.CategoryId.HasValue
                || !categoryPostMappings.TryGetValue(transaction.CategoryId.Value, out var post))
            {
                return true;
            }

            return !TransactionTypeMatches(post.Type, transaction.Type);
        }).ToArray();
        var actualSavingsTotal = postItems
            .Where(post => post.Type == AnnualBudgetPostType.Savings)
            .Sum(post => post.ActualAmount ?? 0m);
        var plannedIncomeTotal = SumPlanned(activePosts, AnnualBudgetPostType.Income, month);
        var plannedExpenseTotal = SumPlanned(activePosts, AnnualBudgetPostType.Expense, month);
        var plannedSavingsTotal = SumPlanned(activePosts, AnnualBudgetPostType.Savings, month);
        var monthExtras = (await repository.ListExtrasAsync(year, cancellationToken))
            .Where(extra => extra.Month == month)
            .Select(ToExtraItem)
            .ToArray();

        return new AnnualBudgetMonthSummary(
            year,
            month,
            plannedIncomeTotal,
            plannedExpenseTotal,
            plannedSavingsTotal,
            monthExtras.Sum(extra => extra.Amount),
            actualSavingsTotal,
            transactions.Where(transaction => transaction.Type == TransactionType.Income).Sum(transaction => transaction.Amount),
            transactions.Where(transaction => transaction.Type == TransactionType.Expense).Sum(transaction => transaction.Amount),
            unassignedTransactions.Where(transaction => transaction.Type == TransactionType.Income).Sum(transaction => transaction.Amount),
            unassignedTransactions.Where(transaction => transaction.Type == TransactionType.Expense).Sum(transaction => transaction.Amount),
            unassignedTransactions.Length,
            unassignedTransactions,
            postItems,
            monthExtras);
    }

    public async Task<AnnualBudgetPostItem> CreatePostAsync(
        int year,
        string name,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind,
        CancellationToken cancellationToken)
    {
        ValidateYear(year);
        var normalizedName = NormalizeName(name);
        if (await repository.ExistsByNameAsync(year, normalizedName, null, cancellationToken))
        {
            throw new DuplicateAnnualBudgetPostException(normalizedName);
        }

        var targetExpenseKind = ResolveExpenseKind(type, expenseKind);
        var existingPosts = await repository.ListPostsAsync(year, cancellationToken);
        var sortOrder = NextSortOrder(existingPosts, null, type, targetExpenseKind);
        var post = new AnnualBudgetPost(year, normalizedName, type, targetExpenseKind, sortOrder);
        await repository.AddPostAsync(post, cancellationToken);
        return ToItem(post);
    }

    public async Task<AnnualBudgetPostItem?> UpdatePostAsync(
        Guid id,
        string name,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind,
        CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var normalizedName = NormalizeName(name);
        if (!string.Equals(post.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            && await repository.ExistsByNameAsync(post.Year, normalizedName, id, cancellationToken))
        {
            throw new DuplicateAnnualBudgetPostException(normalizedName);
        }

        var targetExpenseKind = ResolveExpenseKind(type, expenseKind);
        if (!IsInGroup(post, type, targetExpenseKind))
        {
            var existingPosts = await repository.ListPostsAsync(post.Year, cancellationToken);
            post.SetSortOrder(NextSortOrder(existingPosts, post.Id, type, targetExpenseKind));
        }

        post.Update(normalizedName, type, targetExpenseKind);
        await repository.UpdatePostAsync(post, cancellationToken);
        return ToItem(post);
    }

    public async Task ReorderPostsAsync(
        int year,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind,
        IReadOnlyList<Guid> postIds,
        CancellationToken cancellationToken)
    {
        ValidateYear(year);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Le type du poste budgétaire est invalide.");
        }

        if (expenseKind.HasValue && !Enum.IsDefined(expenseKind.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(expenseKind), "La catégorie de dépense est invalide.");
        }

        if ((type == AnnualBudgetPostType.Expense) != expenseKind.HasValue)
        {
            throw new ArgumentException("Indiquez une catégorie uniquement pour les dépenses.", nameof(expenseKind));
        }

        if (postIds.Count == 0 || postIds.Distinct().Count() != postIds.Count)
        {
            throw new ArgumentException("La liste des postes à ordonner est vide ou contient des doublons.", nameof(postIds));
        }

        var posts = (await repository.ListPostsAsync(year, cancellationToken))
            .Where(post => post.IsActive && IsInGroup(post, type, expenseKind))
            .ToArray();
        if (posts.Length != postIds.Count || !posts.Select(post => post.Id).ToHashSet().SetEquals(postIds))
        {
            throw new ArgumentException("La liste doit contenir exactement les postes actifs de cette section.", nameof(postIds));
        }

        await repository.SetPostOrderAsync(postIds, cancellationToken);
    }

    public async Task<bool> DeletePostAsync(Guid id, CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(id, cancellationToken);
        if (post is null)
        {
            return false;
        }

        await repository.DeletePostAsync(post, cancellationToken);
        return true;
    }

    public async Task<AnnualBudgetPostItem?> SetMonthlyAmountAsync(
        Guid id,
        int month,
        decimal? amount,
        CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Le mois doit être compris entre 1 et 12.");
        }

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Le montant prévisionnel ne peut pas être négatif.");
        }

        var post = await repository.GetPostAsync(id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        if (!post.IsActive)
        {
            throw new InvalidOperationException("Un poste désactivé ne peut pas être modifié.");
        }

        await repository.SetMonthlyAmountAsync(id, month, amount, cancellationToken);
        return ToItem(await repository.GetPostAsync(id, cancellationToken) ?? post);
    }

    public async Task<AnnualBudgetPostItem?> CopyMonthlyAmountForwardAsync(
        Guid id,
        int month,
        CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Le mois doit être compris entre 1 et 12.");
        }

        var post = await repository.GetPostAsync(id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        if (!post.IsActive)
        {
            throw new InvalidOperationException("Un poste désactivé ne peut pas être modifié.");
        }

        var source = post.MonthlyAmounts.SingleOrDefault(item => item.Month == month)
            ?? throw new InvalidOperationException("Aucun montant n'est prévu pour le mois à recopier.");

        for (var target = month + 1; target <= 12; target++)
        {
            await repository.SetMonthlyAmountAsync(id, target, source.Amount, cancellationToken);
        }

        return ToItem(await repository.GetPostAsync(id, cancellationToken) ?? post);
    }

    public async Task<AnnualBudgetPostItem?> ClearMonthlyAmountsFromAsync(
        Guid id,
        int month,
        CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Le mois doit être compris entre 1 et 12.");
        }

        var post = await repository.GetPostAsync(id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        if (!post.IsActive)
        {
            throw new InvalidOperationException("Un poste désactivé ne peut pas être modifié.");
        }

        for (var target = month; target <= 12; target++)
        {
            await repository.SetMonthlyAmountAsync(id, target, null, cancellationToken);
        }

        return ToItem(await repository.GetPostAsync(id, cancellationToken) ?? post);
    }

    public async Task<BudgetExtraItem> AddExtraAsync(
        int year,
        int month,
        string label,
        decimal amount,
        CancellationToken cancellationToken)
    {
        ValidateYear(year);
        var extra = new BudgetExtra(year, month, label, amount);
        await repository.AddExtraAsync(extra, cancellationToken);
        return ToExtraItem(extra);
    }

    public Task<bool> DeleteExtraAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteExtraAsync(id, cancellationToken);

    public async Task<AnnualBudgetPostItem?> SetCategoryMappingsAsync(
        Guid id,
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        if (!post.IsActive)
        {
            throw new InvalidOperationException("Un poste désactivé ne peut pas être modifié.");
        }

        if (categoryIds.Count != categoryIds.Distinct().Count())
        {
            throw new ArgumentException("Une catégorie ne peut être sélectionnée qu'une seule fois.", nameof(categoryIds));
        }

        foreach (var categoryId in categoryIds)
        {
            if (await categoryRepository.GetAsync(categoryId, cancellationToken) is null)
            {
                throw new ArgumentException("Une catégorie sélectionnée n'existe pas ou n'est plus active.", nameof(categoryIds));
            }
        }

        var conflictingPost = (await repository.ListPostsForYearAsync(post.Year, cancellationToken))
            .Where(candidate => candidate.Id != post.Id && candidate.IsActive)
            .FirstOrDefault(candidate => candidate.CategoryMappings.Any(mapping => categoryIds.Contains(mapping.CategoryId)));
        if (conflictingPost is not null)
        {
            var conflictingCategory = conflictingPost.CategoryMappings
                .First(mapping => categoryIds.Contains(mapping.CategoryId))
                .Category?.Name ?? "Catégorie";
            throw new DuplicateBudgetCategoryMappingException(conflictingCategory, conflictingPost.Name);
        }

        await repository.SetCategoryMappingsAsync(id, categoryIds, cancellationToken);
        return ToItem(await repository.GetPostAsync(id, cancellationToken) ?? post);
    }

    private static AnnualBudgetPostItem ToItem(AnnualBudgetPost post)
    {
        var amounts = new decimal?[12];
        foreach (var monthlyAmount in post.MonthlyAmounts)
        {
            amounts[monthlyAmount.Month - 1] = monthlyAmount.Amount;
        }

        return new AnnualBudgetPostItem(
            post.Id,
            post.Year,
            post.Name,
            post.Type,
            post.ExpenseKind,
            post.SortOrder,
            post.IsActive,
            amounts,
            amounts.Where(amount => amount.HasValue).Sum(amount => amount!.Value),
            post.CategoryMappings
                .Select(mapping => new AnnualBudgetCategoryItem(
                    mapping.CategoryId,
                    mapping.Category?.Name ?? "Catégorie désactivée"))
                .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static decimal SumPlanned(
        IReadOnlyCollection<AnnualBudgetPost> posts,
        AnnualBudgetPostType type,
        int month) =>
        posts.Where(post => post.Type == type)
            .SelectMany(post => post.MonthlyAmounts)
            .Where(amount => amount.Month == month)
            .Sum(amount => amount.Amount);

    private static bool TransactionTypeMatches(AnnualBudgetPostType postType, TransactionType transactionType) =>
        (postType == AnnualBudgetPostType.Income && transactionType == TransactionType.Income)
        || (postType != AnnualBudgetPostType.Income && transactionType == TransactionType.Expense);

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new ArgumentException("Le nom du poste doit contenir entre 1 et 100 caractères.", nameof(name));
        }

        return name.Trim();
    }

    private static BudgetExtraItem ToExtraItem(BudgetExtra extra) =>
        new(extra.Id, extra.Year, extra.Month, extra.Label, extra.Amount);

    private static AnnualBudgetExpenseKind? ResolveExpenseKind(
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind) =>
        type == AnnualBudgetPostType.Expense ? expenseKind ?? AnnualBudgetExpenseKind.Variable : null;

    private static int NextSortOrder(
        IEnumerable<AnnualBudgetPost> posts,
        Guid? excludedPostId,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind) =>
        posts
            .Where(item => item.Id != excludedPostId && item.IsActive && IsInGroup(item, type, expenseKind))
            .Select(item => item.SortOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;

    private static bool IsInGroup(
        AnnualBudgetPost post,
        AnnualBudgetPostType type,
        AnnualBudgetExpenseKind? expenseKind) =>
        post.Type == type
        && (type != AnnualBudgetPostType.Expense
            || (post.ExpenseKind ?? AnnualBudgetExpenseKind.Variable) == expenseKind);

    private static void ValidateYear(int year)
    {
        if (year is < 1 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "L'année doit être comprise entre 1 et 9998.");
        }
    }
}
