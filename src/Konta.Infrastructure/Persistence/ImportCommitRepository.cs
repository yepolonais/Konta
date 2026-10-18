using Konta.Application.Imports;
using Konta.Domain.Categories;
using Konta.Domain.Forecasts;
using Konta.Domain.Transfers;
using Konta.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class ImportCommitRepository(KontaDbContext dbContext) : IImportCommitRepository
{
    public async Task<IReadOnlySet<int>> FindDuplicateRowNumbersAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var importRows = transactions
            .Where(transaction => transaction.AccountId.HasValue && transaction.Fingerprint is not null)
            .ToArray();
        var existing = await GetExistingOccurrencesAsync(importRows, cancellationToken);
        var duplicateRows = new HashSet<int>();

        foreach (var group in importRows.GroupBy(transaction =>
                     (transaction.AccountId!.Value, transaction.Fingerprint!)))
        {
            var existingCount = existing.Count(item =>
                item.AccountId == group.Key.Value && item.Fingerprint == group.Key.Item2);
            foreach (var transaction in group.OrderBy(transaction => transaction.RowNumber).Take(existingCount))
            {
                duplicateRows.Add(transaction.RowNumber);
            }
        }

        return duplicateRows;
    }

    public async Task<IReadOnlyList<TransferMatchCandidate>> FindTransferMatchCandidatesAsync(
        Guid accountId,
        DateOnly date,
        decimal amount,
        TransactionType type,
        CancellationToken cancellationToken)
    {
        var startDate = date.AddDays(-3);
        var endDate = date.AddDays(3);
        var candidates = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId != accountId
                && transaction.AccountId.HasValue
                && transaction.Type != type
                && transaction.Amount == amount
                && transaction.Date >= startDate
                && transaction.Date <= endDate
                && !dbContext.AccountTransfers.Any(transfer =>
                    transfer.SourceTransactionId == transaction.Id
                    || transfer.DestinationTransactionId == transaction.Id))
            .Select(transaction => new
            {
                transaction.Id,
                transaction.Label,
                AccountId = transaction.AccountId!.Value,
                AccountName = transaction.Account == null ? transaction.AccountLabel ?? "Compte inconnu" : transaction.Account.Name,
                transaction.Date,
                transaction.Amount,
                transaction.Type
            })
            .ToListAsync(cancellationToken);

        return candidates
            .Select(candidate => new TransferMatchCandidate(
                candidate.Id,
                null,
                candidate.Label,
                candidate.AccountId,
                candidate.AccountName,
                candidate.Date,
                candidate.Amount,
                candidate.Type,
                Math.Abs(candidate.Date.DayNumber - date.DayNumber),
                candidates.Count > 1))
            .OrderBy(candidate => candidate.DateDifferenceDays)
            .ThenBy(candidate => candidate.AccountName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<(int ImportedCount, int DuplicateCount)> SaveAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var importRows = transactions
            .Where(transaction => transaction.AccountId.HasValue && transaction.Fingerprint is not null)
            .ToArray();
        await using var databaseTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await GetExistingOccurrencesAsync(importRows, cancellationToken);
        var categoryLookup = await ResolveCategoryIdsAsync(importRows, cancellationToken);
        var forecastIds = importRows
            .Where(transaction => transaction.ForecastMatchConfirmation is not null)
            .Select(transaction => transaction.ForecastMatchConfirmation!.ForecastId)
            .Distinct()
            .ToArray();
        var forecasts = await dbContext.RecurringForecasts
            .Where(forecast => forecastIds.Contains(forecast.Id) && forecast.IsActive)
            .ToDictionaryAsync(forecast => forecast.Id, cancellationToken);
        var forecastOccurrences = new List<ForecastOccurrence>();
        var matchedOccurrenceKeys = new HashSet<(Guid ForecastId, DateOnly ScheduledDate)>();
        var newTransactions = new List<Transaction>();
        var importedTransactions = new Dictionary<int, (ImportedTransaction Import, Transaction Entity)>();

        foreach (var group in importRows.GroupBy(transaction =>
                     (transaction.AccountId!.Value, transaction.Fingerprint!)))
        {
            var matchingOccurrences = existing
                .Where(item => item.AccountId == group.Key.Value && item.Fingerprint == group.Key.Item2)
                .ToArray();
            var existingCount = matchingOccurrences.Length;
            var nextOccurrence = matchingOccurrences
                .Select(item => item.Occurrence ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            foreach (var transaction in group.OrderBy(transaction => transaction.RowNumber).Skip(existingCount))
            {
                nextOccurrence++;
                var categoryId = GetCategoryId(transaction, categoryLookup);
                var newEntity = new Transaction(
                    transaction.TransactionDate,
                    transaction.Label,
                    transaction.Amount,
                    transaction.Type,
                    transaction.AccountNumber,
                    transaction.AccountLabel,
                    categoryId: categoryId,
                    source: TransactionSource.Import,
                    accountId: transaction.AccountId,
                    importFingerprint: transaction.Fingerprint,
                    importOccurrence: nextOccurrence);
                newTransactions.Add(newEntity);
                importedTransactions.Add(transaction.RowNumber, (transaction, newEntity));

                if (transaction.ForecastMatchConfirmation is { } confirmation)
                {
                    if (!forecasts.TryGetValue(confirmation.ForecastId, out var forecast)
                        || forecast.AccountId != transaction.AccountId
                        || forecast.Type != transaction.Type
                        || !transaction.Label.Contains(forecast.LabelPattern, StringComparison.OrdinalIgnoreCase)
                        || Math.Abs(confirmation.ScheduledDate.DayNumber - transaction.TransactionDate.DayNumber) > 3
                        || !IsScheduledDate(forecast, confirmation.ScheduledDate))
                    {
                        throw new InvalidOperationException("L'échéance prévisionnelle confirmée n'est plus valide.");
                    }

                    var occurrenceKey = (forecast.Id, confirmation.ScheduledDate);
                    if (!matchedOccurrenceKeys.Add(occurrenceKey)
                        || await dbContext.ForecastOccurrences.AnyAsync(
                            occurrence => occurrence.ForecastId == forecast.Id
                                && occurrence.ScheduledDate == confirmation.ScheduledDate,
                            cancellationToken))
                    {
                        throw new InvalidOperationException("Cette échéance a déjà été rapprochée d'une opération.");
                    }

                    forecastOccurrences.Add(new ForecastOccurrence(
                        forecast.Id,
                        confirmation.ScheduledDate,
                        newEntity.Id,
                        transaction.Amount));
                }
            }
        }

        var accountTransfers = new List<AccountTransfer>();
        var linkedTransactionIds = new HashSet<Guid>();
        var transferPairKeys = new HashSet<(Guid FirstId, Guid SecondId)>();
        foreach (var imported in importedTransactions.Values)
        {
            if (imported.Import.TransferMatchConfirmation is not { } confirmation)
            {
                continue;
            }

            Transaction? counterpart = null;
            if (confirmation.TransactionId.HasValue)
            {
                counterpart = await dbContext.Transactions
                    .FirstOrDefaultAsync(item => item.Id == confirmation.TransactionId.Value, cancellationToken);
            }
            else if (confirmation.RowNumber.HasValue
                && importedTransactions.TryGetValue(confirmation.RowNumber.Value, out var importedCounterpart))
            {
                counterpart = importedCounterpart.Entity;
            }

            if (counterpart is null)
            {
                throw new InvalidTransferMatchException("L'opération sélectionnée pour le transfert n'est plus disponible.");
            }

            var current = imported.Entity;
            var pairKey = current.Id.CompareTo(counterpart.Id) < 0
                ? (current.Id, counterpart.Id)
                : (counterpart.Id, current.Id);
            if (transferPairKeys.Contains(pairKey))
            {
                continue;
            }

            if (!AreTransferCounterparts(current, counterpart)
                || linkedTransactionIds.Contains(current.Id)
                || linkedTransactionIds.Contains(counterpart.Id)
                || await dbContext.AccountTransfers.AnyAsync(transfer =>
                    transfer.SourceTransactionId == counterpart.Id
                    || transfer.DestinationTransactionId == counterpart.Id,
                    cancellationToken))
            {
                throw new InvalidTransferMatchException(
                    "Le transfert sélectionné n'est plus valide ou une opération est déjà liée.");
            }

            linkedTransactionIds.Add(current.Id);
            linkedTransactionIds.Add(counterpart.Id);
            transferPairKeys.Add(pairKey);
            var source = current.Type == TransactionType.Expense ? current : counterpart;
            var destination = current.Type == TransactionType.Income ? current : counterpart;
            accountTransfers.Add(new AccountTransfer(source.Id, destination.Id, current.Amount));
        }

        dbContext.Transactions.AddRange(newTransactions);
        dbContext.AccountTransfers.AddRange(accountTransfers);
        dbContext.ForecastOccurrences.AddRange(forecastOccurrences);
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        return (newTransactions.Count, importRows.Length - newTransactions.Count);
    }

    private static bool AreTransferCounterparts(Transaction first, Transaction second) =>
        first.Id != second.Id
        && first.AccountId.HasValue
        && second.AccountId.HasValue
        && first.AccountId != second.AccountId
        && first.Type != second.Type
        && first.Amount == second.Amount
        && Math.Abs(first.Date.DayNumber - second.Date.DayNumber) <= 3;

    private static bool IsScheduledDate(RecurringForecast forecast, DateOnly date)
    {
        if (date < forecast.StartDate || (forecast.EndDate.HasValue && date > forecast.EndDate.Value))
        {
            return false;
        }

        if (forecast.Frequency == ForecastFrequency.Annual && date.Month != forecast.StartDate.Month)
        {
            return false;
        }

        return date.Day == Math.Min(forecast.DayOfMonth, DateTime.DaysInMonth(date.Year, date.Month));
    }

    private async Task<Dictionary<string, Guid>> ResolveCategoryIdsAsync(
        IReadOnlyCollection<ImportedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var categoryNames = transactions
            .Select(transaction => transaction.CategoryName ?? transaction.SubcategoryName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (categoryNames.Length == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var existing = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive && categoryNames.Contains(category.Name!))
            .ToDictionaryAsync(category => category.Name!, category => category.Id, cancellationToken);

        var toCreate = categoryNames
            .Where(name => !existing.ContainsKey(name))
            .Select(name => new Category(name.Trim()))
            .ToArray();

        if (toCreate.Length > 0)
        {
            dbContext.Categories.AddRange(toCreate);
            await dbContext.SaveChangesAsync(cancellationToken);
            foreach (var category in toCreate)
            {
                existing[category.Name] = category.Id;
            }
        }

        return existing;
    }

    private static Guid? GetCategoryId(
        ImportedTransaction transaction,
        IReadOnlyDictionary<string, Guid> categoryLookup)
    {
        var categoryName = transaction.CategoryName ?? transaction.SubcategoryName;
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        return categoryLookup.TryGetValue(categoryName.Trim(), out var categoryId)
            ? categoryId
            : null;
    }

    private async Task<List<ExistingImportOccurrence>> GetExistingOccurrencesAsync(
        IReadOnlyCollection<ImportedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var accountIds = transactions
            .Where(transaction => transaction.AccountId.HasValue)
            .Select(transaction => transaction.AccountId!.Value)
            .Distinct()
            .ToArray();
        var fingerprints = transactions
            .Where(transaction => transaction.Fingerprint is not null)
            .Select(transaction => transaction.Fingerprint!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (accountIds.Length == 0 || fingerprints.Length == 0)
        {
            return [];
        }

        return await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId.HasValue
                && accountIds.Contains(transaction.AccountId.Value)
                && transaction.ImportFingerprint != null
                && fingerprints.Contains(transaction.ImportFingerprint))
            .Select(transaction => new ExistingImportOccurrence(
                transaction.AccountId!.Value,
                transaction.ImportFingerprint!,
                transaction.ImportOccurrence))
            .ToListAsync(cancellationToken);
    }

    private sealed record ExistingImportOccurrence(Guid AccountId, string Fingerprint, int? Occurrence);
}