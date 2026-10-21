using Konta.Domain.Transactions;
using Konta.Infrastructure.Imports;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class SocieteGeneraleCsvParserTests
{
    [Fact]
    public void Parse_ReturnsAllRows_WhenGivenSampleBankExport()
    {
        // Arrange
        using var stream = OpenSample();

        // Act
        var result = new SocieteGeneraleCsvParser().Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(11, result.Transactions.Count);
    }

    [Fact]
    public void Parse_PreservesAccountDetails_WhenGivenSampleBankExport()
    {
        // Arrange
        using var stream = OpenSample();

        // Act
        var result = new SocieteGeneraleCsvParser().Parse(stream);

        // Assert
        Assert.Equal("00050021832", result.Transactions[0].AccountNumber);
        Assert.Equal("Compte Commun", result.Transactions[0].AccountLabel);
    }

    [Fact]
    public void Parse_DeterminesTypeFromAmountSign_WhenGivenSampleBankExport()
    {
        // Arrange
        using var stream = OpenSample();

        // Act
        var result = new SocieteGeneraleCsvParser().Parse(stream);

        // Assert
        Assert.Equal(TransactionType.Expense, result.Transactions[0].Type);
        Assert.Equal(10.90m, result.Transactions[0].Amount);
        Assert.Equal(TransactionType.Income, result.Transactions[^1].Type);
        Assert.Equal(7.95m, result.Transactions[^1].Amount);
    }

    [Fact]
    public void Parse_ExtractsMotifUntilReference_WhenFullLabelContainsMotif()
    {
        // Arrange
        const string fullLabel = "VIR RECU    8386125727S DE: MHP PRESTATIONS SANTE PLEIADE MOTIF: CRONIER-FLURA JENNIFER(N262611D68B) REF: 626104308255 PRESTATIONS";
        var csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + $"25/09/2026;25/09/2026;0001;Compte Commun;Virement reçu;{fullLabel};;;7,95;Non\n";
        using var stream = new MemoryStream(System.Text.Encoding.Latin1.GetBytes(csv));

        // Act
        var result = new SocieteGeneraleCsvParser().Parse(stream);

        // Assert
        var transaction = Assert.Single(result.Transactions);
        Assert.Equal("Virement reçu", transaction.Label);
        Assert.Equal("CRONIER-FLURA JENNIFER(N262611D68B)", transaction.Motif);
    }

    [Fact]
    public void Parse_LeavesMotifEmpty_WhenFullLabelDoesNotContainMotif()
    {
        // Arrange
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;Achat;PAIEMENT CARTE;;;-10,00;Non\n";
        using var stream = new MemoryStream(System.Text.Encoding.Latin1.GetBytes(csv));

        // Act
        var result = new SocieteGeneraleCsvParser().Parse(stream);

        // Assert
        Assert.Null(Assert.Single(result.Transactions).Motif);
    }

    [Fact]
    public void Parse_ReportsInvalidAmount_WithoutDiscardingOtherRows()
    {
        // Arrange
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;Achat;Achat;Alimentation;;-10,90;Non\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;Erreur;Erreur;Alimentation;;abc;Non\n";
        using var stream = new MemoryStream(System.Text.Encoding.Latin1.GetBytes(csv));

        // Act
        var result = new SocieteGeneraleCsvParser().Parse(stream);

        // Assert
        Assert.Single(result.Transactions);
        var error = Assert.Single(result.Errors);
        Assert.Contains("Montant invalide", error.Message);
    }

    private static Stream OpenSample()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "transactions-sample.csv");
        return File.OpenRead(path);
    }
}