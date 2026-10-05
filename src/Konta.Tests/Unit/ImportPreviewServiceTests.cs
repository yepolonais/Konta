using AutoFixture;
using AutoFixture.AutoMoq;
using Konta.Application.Accounts;
using Konta.Application.Imports;
using Konta.Domain.Accounts;
using Moq;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class ImportPreviewServiceTests
{
    [Fact]
    public async Task PreviewAsync_AssociatesAccount_WhenAccountNumberIsMapped()
    {
        // Arrange
        var fixture = CreateFixture();
        var parser = fixture.Freeze<Mock<ITransactionCsvParser>>();
        var accountRepository = fixture.Freeze<Mock<IAccountRepository>>();
        var commitRepository = fixture.Freeze<Mock<IImportCommitRepository>>();
        var accountNumber = fixture.Create<string>();
        var transaction = fixture.Build<ImportedTransaction>()
            .With(importedTransaction => importedTransaction.AccountNumber, accountNumber)
            .Create();
        var account = fixture.Build<Account>()
            .FromFactory(() => new Account("Compte commun", accountNumber, "Compte SG", AccountKind.Current))
            .Create();
        var parsedPreview = CreatePreview(fixture, transaction);
        using var csvStream = new MemoryStream();

        parser.Setup(transactionParser => transactionParser.Parse(It.IsAny<Stream>())).Returns(parsedPreview);
        accountRepository.Setup(repository => repository.FindByBankAccountNumbersAsync(
                It.Is<IReadOnlyCollection<string>>(numbers => numbers.Contains(accountNumber)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Account> { [accountNumber] = account });
        commitRepository.Setup(repository => repository.FindDuplicateRowNumbersAsync(
                It.IsAny<IReadOnlyList<ImportedTransaction>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var service = fixture.Create<ImportPreviewService>();

        // Act
        var result = await service.PreviewAsync(csvStream, CancellationToken.None);

        // Assert
        Assert.Equal(account.Id, result.Transactions[0].AccountId);
        Assert.Equal(account.Name, result.Transactions[0].AccountName);
    }

    [Fact]
    public async Task CommitAsync_Throws_WhenAccountNumberIsUnmapped()
    {
        // Arrange
        var fixture = CreateFixture();
        var parser = fixture.Freeze<Mock<ITransactionCsvParser>>();
        var accountRepository = fixture.Freeze<Mock<IAccountRepository>>();
        var commitRepository = fixture.Freeze<Mock<IImportCommitRepository>>();
        var transaction = fixture.Create<ImportedTransaction>();
        var parsedPreview = CreatePreview(fixture, transaction);
        using var csvStream = new MemoryStream();

        parser.Setup(transactionParser => transactionParser.Parse(It.IsAny<Stream>())).Returns(parsedPreview);
        accountRepository.Setup(repository => repository.FindByBankAccountNumbersAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Account>());
        var service = fixture.Create<ImportPreviewService>();

        var commit = () => service.CommitAsync(csvStream, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnmappedAccountsException>(commit);
        Assert.Contains(exception.Accounts, account => account.AccountNumber == transaction.AccountNumber);
        commitRepository.Verify(repository => repository.SaveAsync(
                It.IsAny<IReadOnlyList<ImportedTransaction>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Customize<DateOnly>(composer => composer.FromFactory(() => new DateOnly(2026, 9, 25)));
        return fixture;
    }

    private static ImportPreview CreatePreview(IFixture fixture, ImportedTransaction transaction) =>
        fixture.Build<ImportPreview>()
            .With(preview => preview.Transactions, new[] { transaction })
            .With(preview => preview.Errors, Array.Empty<CsvRowError>())
            .With(preview => preview.UnmappedAccounts, Array.Empty<UnmappedAccount>())
            .Create();
}