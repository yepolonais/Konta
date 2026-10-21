using Konta.Application.Accounts;
using Konta.Application.CategoryRules;
using Konta.Application.Planning;
using Konta.Domain.Accounts;

namespace Konta.Application.Imports;

public sealed class ImportPreviewService(
    ITransactionCsvParser parser,
    IAccountRepository accountRepository,
    IImportCommitRepository importCommitRepository,
    ICategoryRuleService categoryRuleService,
    IPlanningService planningService) : IImportPreviewService
{
    public Task<ImportPreview> PreviewAsync(Stream csvStream, CancellationToken cancellationToken) =>
        PreviewAsync(csvStream, null, cancellationToken);

    public async Task<ImportPreview> PreviewAsync(
        Stream csvStream,
        IReadOnlyDictionary<string, Guid>? accountMappings,
        CancellationToken cancellationToken)
    {
        var preview = await ResolveAccountsAsync(parser.Parse(csvStream), accountMappings, null, cancellationToken);
        var categorizedPreview = await ApplyCategoryRulesAsync(preview, cancellationToken);
        var matchedPreview = await ApplyForecastMatchesAsync(categorizedPreview, cancellationToken);
        var duplicateRows = await importCommitRepository.FindDuplicateRowNumbersAsync(
            matchedPreview.Transactions,
            cancellationToken);
        var duplicatePreview = matchedPreview with
        {
            Transactions = matchedPreview.Transactions
                .Select(transaction => transaction with { IsDuplicate = duplicateRows.Contains(transaction.RowNumber) })
                .ToArray()
        };
        var transferPreview = await ApplyTransferMatchesAsync(duplicatePreview, cancellationToken);
        return transferPreview;
    }

    public Task<ImportCommitResult> CommitAsync(Stream csvStream, CancellationToken cancellationToken) =>
        CommitAsync(csvStream, null, cancellationToken);

    public async Task<ImportCommitResult> CommitAsync(
        Stream csvStream,
        IReadOnlyDictionary<string, Guid>? accountMappings,
        CancellationToken cancellationToken)
    {
        return await CommitAsync(csvStream, accountMappings, null, cancellationToken);
    }

    public async Task<ImportCommitResult> CommitAsync(
        Stream csvStream,
        IReadOnlyDictionary<string, Guid>? accountMappings,
        IReadOnlyList<ImportTransactionReview>? review,
        CancellationToken cancellationToken)
    {
        var parsed = parser.Parse(csvStream);
        var reviewed = ApplyReview(parsed, review);
        var preview = await ResolveAccountsAsync(reviewed.Preview, accountMappings, reviewed.AccountOverrides, cancellationToken);
        var categorizedPreview = await ApplyCategoryRulesAsync(preview, cancellationToken);
        var matchedPreview = await ApplyForecastMatchesAsync(categorizedPreview, cancellationToken);
        var duplicateRows = await importCommitRepository.FindDuplicateRowNumbersAsync(matchedPreview.Transactions, cancellationToken);
        var duplicatePreview = matchedPreview with
        {
            Transactions = matchedPreview.Transactions
                .Select(transaction => transaction with { IsDuplicate = duplicateRows.Contains(transaction.RowNumber) })
                .ToArray()
        };
        var transferPreview = await ApplyTransferMatchesAsync(duplicatePreview, cancellationToken);
        if (transferPreview.UnmappedAccounts.Count > 0)
        {
            throw new UnmappedAccountsException(transferPreview.UnmappedAccounts);
        }

        var saveResult = await importCommitRepository.SaveAsync(transferPreview.Transactions, cancellationToken);
        return new ImportCommitResult(
            saveResult.ImportedCount,
            saveResult.DuplicateCount,
            preview.Errors,
            reviewed.ExcludedCount);
    }

    private async Task<ImportPreview> ApplyForecastMatchesAsync(ImportPreview preview, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var transactions = new List<ImportedTransaction>(preview.Transactions.Count);
        foreach (var transaction in preview.Transactions)
        {
            var candidates = await planningService.FindForecastMatchesAsync(
                transaction.AccountId,
                transaction.TransactionDate,
                transaction.Label,
                transaction.Amount,
                transaction.Type,
                cancellationToken);
            if (transaction.ForecastMatchConfirmation is { } confirmation
                && !candidates.Any(candidate => candidate.ForecastId == confirmation.ForecastId
                    && candidate.ScheduledDate == confirmation.ScheduledDate))
            {
                errors.Add($"Le rapprochement prévisionnel de la ligne {transaction.RowNumber} n'est plus disponible ou ne correspond pas à l'opération.");
            }

            transactions.Add(transaction with { ForecastMatchCandidates = candidates });
        }

        if (errors.Count > 0)
        {
            throw new InvalidImportReviewException(errors);
        }

        return preview with { Transactions = transactions.ToArray() };
    }

    private async Task<ImportPreview> ApplyTransferMatchesAsync(ImportPreview preview, CancellationToken cancellationToken)
    {
        var transactions = new List<ImportedTransaction>(preview.Transactions.Count);
        var linkedReferences = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new List<string>();
        foreach (var transaction in preview.Transactions)
        {
            var candidates = transaction.AccountId.HasValue && !transaction.IsDuplicate
                ? (await importCommitRepository.FindTransferMatchCandidatesAsync(
                    transaction.AccountId.Value,
                    transaction.TransactionDate,
                    transaction.Amount,
                    transaction.Type,
                    cancellationToken)).ToList()
                : [];
            candidates.AddRange(preview.Transactions
                .Where(candidate => candidate.RowNumber != transaction.RowNumber
                    && !candidate.IsDuplicate
                    && candidate.AccountId.HasValue
                    && candidate.AccountId != transaction.AccountId
                    && candidate.Type != transaction.Type
                    && candidate.Amount == transaction.Amount
                    && Math.Abs(candidate.TransactionDate.DayNumber - transaction.TransactionDate.DayNumber) <= 3)
                .Select(candidate => new TransferMatchCandidate(
                    null,
                    candidate.RowNumber,
                    candidate.Label,
                    candidate.AccountId!.Value,
                    candidate.AccountName ?? candidate.AccountLabel ?? "Compte inconnu",
                    candidate.TransactionDate,
                    candidate.Amount,
                    candidate.Type,
                    Math.Abs(candidate.TransactionDate.DayNumber - transaction.TransactionDate.DayNumber),
                    false)));

            var normalizedCandidates = candidates
                .Select(candidate => candidate with { IsAmbiguous = candidates.Count > 1 })
                .ToArray();
            if (transaction.TransferMatchConfirmation is { } confirmation
                && !normalizedCandidates.Any(candidate =>
                    candidate.TransactionId == confirmation.TransactionId
                    && candidate.RowNumber == confirmation.RowNumber))
            {
                errors.Add($"Le rapprochement de transfert de la ligne {transaction.RowNumber} n'est plus disponible ou ne correspond pas à l'opération.");
            }
            else if (transaction.TransferMatchConfirmation is { } selected)
            {
                var currentReference = $"row:{transaction.RowNumber}";
                var otherReference = selected.TransactionId.HasValue
                    ? $"transaction:{selected.TransactionId.Value}"
                    : $"row:{selected.RowNumber}";
                if (linkedReferences.TryGetValue(currentReference, out var currentMatch)
                    && currentMatch != otherReference
                    || linkedReferences.TryGetValue(otherReference, out var otherMatch)
                    && otherMatch != currentReference)
                {
                    errors.Add($"Une opération ne peut être associée qu'à un seul transfert (ligne {transaction.RowNumber}).");
                }
                else
                {
                    linkedReferences[currentReference] = otherReference;
                    linkedReferences[otherReference] = currentReference;
                }
            }

            transactions.Add(transaction with { TransferMatchCandidates = normalizedCandidates });
        }

        if (errors.Count > 0)
        {
            throw new InvalidImportReviewException(errors);
        }

        return preview with { Transactions = transactions.ToArray() };
    }

    private async Task<ImportPreview> ResolveAccountsAsync(
        ImportPreview parsed,
        IReadOnlyDictionary<string, Guid>? accountMappings,
        IReadOnlyDictionary<int, Guid>? accountOverrides,
        CancellationToken cancellationToken)
    {
        var accountNumbers = parsed.Transactions
            .Select(transaction => transaction.AccountNumber)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var knownAccounts = await accountRepository.FindByBankAccountNumbersAsync(accountNumbers, cancellationToken);
        var mappedAccounts = await ResolveMappedAccountsAsync(accountNumbers, accountMappings, cancellationToken);
        var resolvedAccounts = new Dictionary<string, Account>(StringComparer.Ordinal);

        foreach (var account in knownAccounts)
        {
            resolvedAccounts[account.Key] = account.Value;
        }

        foreach (var account in mappedAccounts)
        {
            resolvedAccounts[account.Key] = account.Value;
        }

        var transactions = new List<ImportedTransaction>(parsed.Transactions.Count);
        var resolvedRowNumbers = new HashSet<int>();
        foreach (var transaction in parsed.Transactions)
        {
            var explicitAccountId = accountOverrides is not null && accountOverrides.TryGetValue(transaction.RowNumber, out var overrideId)
                ? overrideId
                : (Guid?)null;
            Account? account = null;
            if (explicitAccountId.HasValue)
            {
                account = await accountRepository.GetAsync(explicitAccountId.Value, cancellationToken);
            }

            if (account is null)
            {
                account = resolvedAccounts.GetValueOrDefault(transaction.AccountNumber);
            }

            if (account is not null)
            {
                resolvedRowNumbers.Add(transaction.RowNumber);
            }

            transactions.Add(transaction with
            {
                AccountId = account?.Id,
                AccountName = account?.Name,
                Fingerprint = ImportFingerprint.Create(transaction)
            });
        }

        var unmappedAccounts = parsed.Transactions
            .Where(transaction => !resolvedRowNumbers.Contains(transaction.RowNumber))
            .Select(transaction => new UnmappedAccount(transaction.AccountNumber, transaction.AccountLabel))
            .DistinctBy(account => account.AccountNumber, StringComparer.Ordinal)
            .ToArray();

        return parsed with
        {
            Transactions = transactions.ToArray(),
            UnmappedAccounts = unmappedAccounts
        };
    }

    private async Task<ImportPreview> ApplyCategoryRulesAsync(ImportPreview preview, CancellationToken cancellationToken)
    {
        var transactions = new List<ImportedTransaction>(preview.Transactions.Count);
        foreach (var transaction in preview.Transactions)
        {
            var categoryName = transaction.CategoryName;
            var match = await categoryRuleService.FindBestMatchAsync(transaction.Label, cancellationToken);
            var subcategoryName = string.IsNullOrWhiteSpace(transaction.SubcategoryName)
                ? match?.SubcategoryName
                : transaction.SubcategoryName;
            if (match is not null
                && (string.IsNullOrWhiteSpace(categoryName) || !string.IsNullOrWhiteSpace(match.SubcategoryName)))
            {
                categoryName = match.CategoryName;
            }

            transactions.Add(transaction with
            {
                CategoryName = categoryName,
                SubcategoryName = subcategoryName,
                CategorySuggestion = match is null
                    ? transaction.CategorySuggestion
                    : new CategoryRuleSuggestion(match.RuleId, match.RuleName, match.CategoryName, match.Pattern)
            });
        }

        return preview with { Transactions = transactions.ToArray() };
    }

    private async Task<IReadOnlyDictionary<string, Account>> ResolveMappedAccountsAsync(
        IReadOnlyCollection<string> accountNumbers,
        IReadOnlyDictionary<string, Guid>? accountMappings,
        CancellationToken cancellationToken)
    {
        if (accountMappings is null || accountMappings.Count == 0)
        {
            return new Dictionary<string, Account>(StringComparer.Ordinal);
        }

        var mappedByNumber = new Dictionary<string, Account>(StringComparer.Ordinal);
        foreach (var bankAccountNumber in accountNumbers)
        {
            if (!accountMappings.TryGetValue(bankAccountNumber, out var accountId))
            {
                continue;
            }

            var account = await accountRepository.GetAsync(accountId, cancellationToken);
            if (account is not null)
            {
                mappedByNumber[bankAccountNumber] = account;
            }
        }

        return mappedByNumber;
    }

    private static (ImportPreview Preview, int ExcludedCount, IReadOnlyDictionary<int, Guid> AccountOverrides) ApplyReview(
        ImportPreview parsed,
        IReadOnlyList<ImportTransactionReview>? review)
    {
        if (review is null || review.Count == 0)
        {
            return (parsed, 0, new Dictionary<int, Guid>());
        }

        var errors = new List<string>();
        var decisions = new Dictionary<int, ImportTransactionReview>();
        foreach (var decision in review)
        {
            if (decision.RowNumber <= 0)
            {
                errors.Add($"Numéro de ligne invalide : {decision.RowNumber}.");
            }
            else if (!decisions.TryAdd(decision.RowNumber, decision))
            {
                errors.Add($"La ligne {decision.RowNumber} est présente plusieurs fois.");
            }
        }

        var parsedRows = parsed.Transactions.Select(transaction => transaction.RowNumber).ToHashSet();
        foreach (var decision in decisions.Values)
        {
            if (!parsedRows.Contains(decision.RowNumber))
            {
                errors.Add($"La ligne {decision.RowNumber} n'est pas une opération valide de l'aperçu.");
            }
        }

        var transactions = new List<ImportedTransaction>(parsed.Transactions.Count);
        var excludedCount = 0;
        var accountOverrides = new Dictionary<int, Guid>();
        foreach (var transaction in parsed.Transactions)
        {
            if (!decisions.TryGetValue(transaction.RowNumber, out var decision))
            {
                transactions.Add(transaction);
                continue;
            }

            if (!decision.Include)
            {
                excludedCount++;
                continue;
            }

            if (decision.Amount is <= 0)
            {
                errors.Add($"La ligne {transaction.RowNumber} doit avoir un montant supérieur à zéro.");
            }

            if (decision.Label is not null && string.IsNullOrWhiteSpace(decision.Label))
            {
                errors.Add($"Le libellé de la ligne {transaction.RowNumber} ne peut pas être vide.");
            }

            if (decision.Label?.Length > 500)
            {
                errors.Add($"Le libellé de la ligne {transaction.RowNumber} dépasse 500 caractères.");
            }

            if (decision.CategoryName?.Length > 100)
            {
                errors.Add($"La catégorie de la ligne {transaction.RowNumber} dépasse 100 caractères.");
            }

            if (decision.CreateSubcategoryRule)
            {
                if (string.IsNullOrWhiteSpace(decision.CategoryName))
                {
                    errors.Add($"La ligne {transaction.RowNumber} doit avoir une catégorie pour mémoriser sa sous-catégorie.");
                }

                if (string.IsNullOrWhiteSpace(decision.SubcategoryName))
                {
                    errors.Add($"La ligne {transaction.RowNumber} doit avoir une sous-catégorie pour créer une règle.");
                }

                if (decision.Label?.Length > 200)
                {
                    errors.Add($"Le libellé de la ligne {transaction.RowNumber} dépasse 200 caractères et ne peut pas servir de motif.");
                }
            }

            if (decision.SubcategoryName?.Length > 200)
            {
                errors.Add($"La sous-catégorie de la ligne {transaction.RowNumber} dépasse 200 caractères.");
            }

            if (decision.Type.HasValue && !Enum.IsDefined(decision.Type.Value))
            {
                errors.Add($"Le type de la ligne {transaction.RowNumber} est invalide.");
            }

            if (decision.AccountId.HasValue)
            {
                accountOverrides[transaction.RowNumber] = decision.AccountId.Value;
            }

            if (decision.ConfirmForecastMatch
                && (!decision.ForecastId.HasValue || !decision.ForecastDate.HasValue))
            {
                errors.Add($"Le rapprochement de la ligne {transaction.RowNumber} doit désigner une échéance.");
            }

            if (decision.ConfirmTransferMatch
                && (decision.TransferTransactionId.HasValue == decision.TransferRowNumber.HasValue))
            {
                errors.Add($"Le rapprochement de transfert de la ligne {transaction.RowNumber} doit désigner une seule opération.");
            }

            transactions.Add(transaction with
            {
                TransactionDate = decision.TransactionDate ?? transaction.TransactionDate,
                Label = decision.Label?.Trim() ?? transaction.Label,
                Amount = decision.Amount ?? transaction.Amount,
                Type = decision.Type ?? transaction.Type,
                AccountName = decision.AccountName?.Trim() ?? transaction.AccountName,
                CategoryName = decision.CategoryName is null
                    ? transaction.CategoryName
                    : string.IsNullOrWhiteSpace(decision.CategoryName) ? null : decision.CategoryName.Trim(),
                SubcategoryName = decision.SubcategoryName is null
                    ? transaction.SubcategoryName
                    : string.IsNullOrWhiteSpace(decision.SubcategoryName) ? null : decision.SubcategoryName.Trim(),
                CreateSubcategoryRule = decision.CreateSubcategoryRule,
                ForecastMatchConfirmation = decision.ConfirmForecastMatch && decision.ForecastId.HasValue && decision.ForecastDate.HasValue
                    ? new ForecastMatchConfirmation(decision.ForecastId.Value, decision.ForecastDate.Value)
                    : null,
                TransferMatchConfirmation = decision.ConfirmTransferMatch
                    ? new TransferMatchConfirmation(decision.TransferTransactionId, decision.TransferRowNumber)
                    : null
            });
        }

        if (errors.Count > 0)
        {
            throw new InvalidImportReviewException(errors);
        }

        return (parsed with { Transactions = transactions }, excludedCount, accountOverrides);
    }
}