using Konta.Domain.Transactions;
using Konta.Infrastructure.Imports;

namespace Konta.Tests;

public sealed class SocieteGeneraleCsvParserTests
{
    [Fact]
    public void ParsesTheProvidedBankExportAndPreservesAccountAndTransactionType()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "transactions-sample.csv");
        using var stream = File.OpenRead(path);

        var result = new SocieteGeneraleCsvParser().Parse(stream);

        Assert.Empty(result.Errors);
        Assert.Equal(11, result.Transactions.Count);
        Assert.Equal("00050021832", result.Transactions[0].AccountNumber);
        Assert.Equal("Compte Commun", result.Transactions[0].AccountLabel);
        Assert.Equal(10.90m, result.Transactions[0].Amount);
        Assert.Equal(TransactionType.Expense, result.Transactions[0].Type);
        Assert.Equal(TransactionType.Income, result.Transactions[^1].Type);
        Assert.Equal(7.95m, result.Transactions[^1].Amount);
    }

    [Fact]
    public void ReportsAnInvalidAmountWithoutDroppingOtherRows()
    {
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;Achat;Achat;Alimentation;;-10,90;Non\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;Erreur;Erreur;Alimentation;;abc;Non\n";
        using var stream = new MemoryStream(System.Text.Encoding.Latin1.GetBytes(csv));

        var result = new SocieteGeneraleCsvParser().Parse(stream);

        Assert.Single(result.Transactions);
        Assert.Single(result.Errors);
        Assert.Contains("Montant invalide", result.Errors[0].Message);
    }
}