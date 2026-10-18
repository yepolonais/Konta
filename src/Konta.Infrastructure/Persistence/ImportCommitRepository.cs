using Konta.Application.Imports;
using Konta.Domain.Categories;
using Konta.Domain.Forecasts;
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

        dbContext.Transactions.AddRange(newTransactions);
        dbContext.ForecastOccurrences.AddRange(forecastOccurrences);
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        return (newTransactions.Count, importRows.Length - newTransactions.Count);
    }

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