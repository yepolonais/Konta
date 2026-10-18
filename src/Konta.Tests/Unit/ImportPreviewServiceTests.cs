using AutoFixture;
using AutoFixture.AutoMoq;
using Konta.Application.Accounts;
using Konta.Application.Categories;
using Konta.Application.CategoryRules;
using Konta.Application.Imports;
using Konta.Application.Planning;
using Konta.Domain.Accounts;
using Konta.Domain.Categories;
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

    [Fact]
    public async Task PreviewAsync_UsesExplicitAccountMapping_WhenBankAccountIsNotInRepository()
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
        var mappings = new Dictionary<string, Guid> { [accountNumber] = account.Id };
        using var csvStream = new MemoryStream();

        parser.Setup(transactionParser => transactionParser.Parse(It.IsAny<Stream>())).Returns(parsedPreview);
        accountRepository.Setup(repository => repository.FindByBankAccountNumbersAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Account>());
        accountRepository.Setup(repository => repository.GetAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        commitRepository.Setup(repository => repository.FindDuplicateRowNumbersAsync(
                It.IsAny<IReadOnlyList<ImportedTransaction>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var service = fixture.Create<ImportPreviewService>();

        // Act
        var result = await service.PreviewAsync(csvStream, mappings, CancellationToken.None);

        // Assert
        Assert.Equal(account.Id, result.Transactions[0].AccountId);
        Assert.Equal(account.Name, result.Transactions[0].AccountName);
        Assert.Empty(result.UnmappedAccounts);
    }

    [Fact]
    public async Task PreviewAsync_AppliesMatchingCategoryRule_WhenLabelMatchesPattern()
    {
        // Arrange
        var fixture = CreateFixture();
        var parser = fixture.Freeze<Mock<ITransactionCsvParser>>();
        var accountRepository = fixture.Freeze<Mock<IAccountRepository>>();
        var commitRepository = fixture.Freeze<Mock<IImportCommitRepository>>();
        var categoryRepository = new Mock<ICategoryRepository>();
        var categoryRuleRepository = new Mock<ICategoryRuleRepository>();
        var planningService = new Mock<IPlanningService>();
        var accountNumber = fixture.Create<string>();
        var transaction = fixture.Build<ImportedTransaction>()
            .With(importedTransaction => importedTransaction.AccountNumber, accountNumber)
            .With(importedTransaction => importedTransaction.Label, "Paiement CARTE VISA")
            .With(importedTransaction => importedTransaction.CategoryName, (string?)null)
            .With(importedTransaction => importedTransaction.SubcategoryName, (string?)null)
            .Create();
        var account = fixture.Build<Account>()
            .FromFactory(() => new Account("Compte commun", accountNumber, "Compte SG", AccountKind.Current))
            .Create();
        var category = new Category("Courses");
        var parsedPreview = CreatePreview(fixture, transaction);
        using var csvStream = new MemoryStream();

        parser.Setup(transactionParser => transactionParser.Parse(It.IsAny<Stream>())).Returns(parsedPreview);
        accountRepository.Setup(repository => repository.FindByBankAccountNumbersAsync(
                It.Is<IReadOnlyCollection<string>>(numbers => numbers.Contains(accountNumber)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Account> { [accountNumber] = account });
        categoryRuleRepository.Setup(repository => repository.FindMatchingAsync(transaction.Label, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new CategoryRule("Carte", "CARTE VISA", category.Id) });
        categoryRepository.Setup(repository => repository.GetAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        planningService.Setup(service => service.FindForecastMatchesAsync(
                It.IsAny<Guid?>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<Konta.Domain.Transactions.TransactionType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        commitRepository.Setup(repository => repository.FindDuplicateRowNumbersAsync(
                It.IsAny<IReadOnlyList<ImportedTransaction>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var categoryRuleService = new CategoryRuleService(categoryRuleRepository.Object, categoryRepository.Object);
        var service = new ImportPreviewService(
            parser.Object,
            accountRepository.Object,
            commitRepository.Object,
            categoryRuleService,
            planningService.Object);

        // Act
        var result = await service.PreviewAsync(csvStream, CancellationToken.None);

        // Assert
        Assert.Equal("Courses", result.Transactions[0].CategoryName);
    }

    [Fact]
    public async Task CommitAsync_UsesReviewAccountAndCategoryOverrides_WhenProvided()
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
        var review = new[]
        {
            new ImportTransactionReview(
                transaction.RowNumber,
                true,
                AccountId: account.Id,
                AccountName: account.Name,
                CategoryName: "Courses",
                SubcategoryName: "Restaurants")
        };
        using var csvStream = new MemoryStream();

        parser.Setup(transactionParser => transactionParser.Parse(It.IsAny<Stream>())).Returns(parsedPreview);
        accountRepository.Setup(repository => repository.FindByBankAccountNumbersAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Account>());
        accountRepository.Setup(repository => repository.GetAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        commitRepository.Setup(repository => repository.SaveAsync(
                It.IsAny<IReadOnlyList<ImportedTransaction>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<ImportedTransaction>, CancellationToken>((saved, _) =>
            {
                var savedTransaction = Assert.Single(saved);
                Assert.Equal(account.Id, savedTransaction.AccountId);
                Assert.Equal(account.Name, savedTransaction.AccountName);
                Assert.Equal("Courses", savedTransaction.CategoryName);
                Assert.Equal("Restaurants", savedTransaction.SubcategoryName);
            })
            .ReturnsAsync((ImportedCount: 1, DuplicateCount: 0));

        var service = fixture.Create<ImportPreviewService>();

        // Act
        var result = await service.CommitAsync(csvStream, null, review, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);
    }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Customize<DateOnly>(composer => composer.FromFactory(() => new DateOnly(2026, 9, 25)));
        fixture.Freeze<Mock<IPlanningService>>()
            .Setup(service => service.FindForecastMatchesAsync(
                It.IsAny<Guid?>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<Konta.Domain.Transactions.TransactionType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ForecastMatchCandidate>());
        fixture.Freeze<Mock<IImportCommitRepository>>()
            .Setup(repository => repository.FindTransferMatchCandidatesAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<decimal>(),
                It.IsAny<Konta.Domain.Transactions.TransactionType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TransferMatchCandidate>());
        return fixture;
    }

    private static ImportPreview CreatePreview(IFixture fixture, ImportedTransaction transaction) =>
        fixture.Build<ImportPreview>()
            .With(preview => preview.Transactions, new[]
            {
                transaction with
                {
                    ForecastMatchConfirmation = null,
                    TransferMatchConfirmation = null
                }
            })
            .With(preview => preview.Errors, Array.Empty<CsvRowError>())
            .With(preview => preview.UnmappedAccounts, Array.Empty<UnmappedAccount>())
            .Create();
}