using Konta.Application.Accounts;

namespace Konta.Application.Imports;

public sealed class ImportPreviewService(
    ITransactionCsvParser parser,
    IAccountRepository accountRepository,
    IImportCommitRepository importCommitRepository) : IImportPreviewService
{
    public async Task<ImportPreview> PreviewAsync(Stream csvStream, CancellationToken cancellationToken)
    {
        var preview = await ResolveAccountsAsync(parser.Parse(csvStream), cancellationToken);
        var duplicateRows = await importCommitRepository.FindDuplicateRowNumbersAsync(
            preview.Transactions,
            cancellationToken);
        return preview with
        {
            Transactions = preview.Transactions
                .Select(transaction => transaction with { IsDuplicate = duplicateRows.Contains(transaction.RowNumber) })
                .ToArray()
        };
    }

    public async Task<ImportCommitResult> CommitAsync(Stream csvStream, CancellationToken cancellationToken)
    {
        var preview = await ResolveAccountsAsync(parser.Parse(csvStream), cancellationToken);
        if (preview.UnmappedAccounts.Count > 0)
        {
            throw new UnmappedAccountsException(preview.UnmappedAccounts);
        }

        var saveResult = await importCommitRepository.SaveAsync(preview.Transactions, cancellationToken);
        return new ImportCommitResult(saveResult.ImportedCount, saveResult.DuplicateCount, preview.Errors);
    }

    private async Task<ImportPreview> ResolveAccountsAsync(
        ImportPreview parsed,
        CancellationToken cancellationToken)
    {
        var accountNumbers = parsed.Transactions
            .Select(transaction => transaction.AccountNumber)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var accounts = await accountRepository.FindByBankAccountNumbersAsync(accountNumbers, cancellationToken);
        var transactions = parsed.Transactions.Select(transaction =>
        {
            var account = accounts.GetValueOrDefault(transaction.AccountNumber);
            return transaction with
            {
                AccountId = account?.Id,
                AccountName = account?.Name,
                Fingerprint = ImportFingerprint.Create(transaction)
            };
        }).ToArray();
        var unmappedAccounts = parsed.Transactions
            .Where(transaction => !accounts.ContainsKey(transaction.AccountNumber))
            .Select(transaction => new UnmappedAccount(transaction.AccountNumber, transaction.AccountLabel))
            .DistinctBy(account => account.AccountNumber, StringComparer.Ordinal)
            .ToArray();

        return parsed with
        {
            Transactions = transactions,
            UnmappedAccounts = unmappedAccounts
        };
    }
}