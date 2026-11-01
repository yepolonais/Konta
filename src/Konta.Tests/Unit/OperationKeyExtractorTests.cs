using Konta.Application.Imports;
using Konta.Domain.Categories;

namespace Konta.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class OperationKeyExtractorTests
{
    [Theory]
    [InlineData("CARTE X4468 29/10 MARIE BLACHERE 110630301772261IOPD", OperationType.Card, "MARIE BLACHERE")]
    [InlineData("CARTE X6608 29/10 IKEA BORDEAUX   4112360 230630302276721IOPD", OperationType.Card, "IKEA BORDEAUX 4112360")]
    [InlineData("CARTE X6608 28/10 Temu 45,95 EUR IRLANDE COMMERCE ELECTRONIQUE 230630201098618IOPD", OperationType.Card, "Temu IRLANDE COMMERCE ELECTRONIQUE")]
    [InlineData("VIR RECU 9630289826922 DE: MME JENNIFER CRONIER-FLURA MOTIF: Novembre", OperationType.TransferReceived, "MME JENNIFER CRONIER-FLURA")]
    [InlineData("VIR RECU    2987467181S DE: MHP PRESTATIONS SANTE PLEIADE MOTIF: CRONIER-FLURA JENNIFER(M2630104614) REF: 630113007821 PRESTATIONS", OperationType.TransferReceived, "MHP PRESTATIONS SANTE PLEIADE")]
    [InlineData("000001 VIR EUROPEEN EMIS   LOGITEL POUR: MME JENNIFER CRONIER-FLURA 27 10 SG 00392 CPT 00050021790 REF: 9630087686682", OperationType.TransferSent, "MME JENNIFER CRONIER-FLURA SG 00392 CPT 00050021790")]
    [InlineData("PRELEVEMENT EUROPEEN 2009920651 POUR CPTE DE:DGFIP IMPOT 033036 ID: FR46ZZZ005002 MOTIF: SOLDE IMPOT REVENUS 2025", OperationType.DirectDebit, "DGFIP IMPOT 033036")]
    [InlineData("PRELEVEMENT EUROPEEN 1602354562 DE: ENGIE ID: FR03SYM002381 MOTIF: Mandat 00S011473511", OperationType.DirectDebit, "ENGIE")]
    [InlineData("ECHEANCE PRET N°825038424047 CAPITAL AMORTI : 1 045,20 INTERETS : 1 100,16 CAPITAL RESTANT : 439 019,89", OperationType.Loan, "ECHEANCE PRET N°825038424047")]
    [InlineData("COMMISSION D'INTERVENTION Calculée au 25/09/2026 pour : PRELEVEMENT EUROPEEN 8016567364 Montant :            -439,00", OperationType.BankFee, "COMMISSION D'INTERVENTION")]
    [InlineData("PRLV EUROP PONCTUEL 1514595150 POUR CPTE DE:DGFIP IMPOT 33036 ID: FR46ZZZ005002 MOTIF: 600232378925TLR2633410933474 TLRSEP", OperationType.DirectDebit, "DGFIP IMPOT 33036")]
    [InlineData("CARTE X6608 REMBT 14/10 Vinted COMMERCE ELECTRONIQUE 110628902359191ILIC", OperationType.Card, "Vinted COMMERCE ELECTRONIQUE")]
    [InlineData("INTERETS DEBITEURS 01.07/30.09 CALCULES AU TAUX DE 21,13% TEG/AN=23,520% TEG/JOUR=0,0579% NOMBRE JOURS DEBITEURS : 38", OperationType.BankFee, "INTERETS DEBITEURS")]
    public void Extract_ReturnsTypeAndKey_ForKnownFormats(string fullLabel, OperationType type, string key)
    {
        // Act
        var result = OperationKeyExtractor.Extract(fullLabel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(type, result!.Type);
        Assert.Equal(key, result.Key);
    }

    [Theory]
    [InlineData("CHEQUE      21")]
    [InlineData("CARTE X9734 RETRAIT DAB 02/05 12H29 CASH SERVICES 20279A00 510612501154676IOPD")]
    [InlineData("COTISATION MENSUELLE SOBRIO")]
    [InlineData("VIR RECU 123")]
    [InlineData("")]
    [InlineData(null)]
    public void Extract_ReturnsNull_WhenFormatHasNoKey(string? fullLabel)
    {
        // Act
        var result = OperationKeyExtractor.Extract(fullLabel);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Normalize_IgnoresCaseAccentsAndRepeatedSpaces()
    {
        // Act & Assert
        Assert.Equal("MARIE BLACHERE", OperationKeyExtractor.Normalize("  Marie   Blachère "));
    }
}
