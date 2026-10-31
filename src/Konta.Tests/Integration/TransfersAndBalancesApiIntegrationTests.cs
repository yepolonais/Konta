using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class TransfersAndBalancesApiIntegrationTests
{
    [Fact]
    public async Task Transfers_AreSuggestedConfirmedAndExcludedFromHouseholdTotals()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var currentAccountId = await CreateAccountAsync(client, "Compte courant", "2001");
        var savingsAccountId = await CreateAccountAsync(client, "Livret", "2002");
        await CreateCategoryAsync(client, "Épargne");
        using var recipientImport = await PostCsvAsync(client,
            Csv("15/10/2026", "2002", "Virement reçu", "Épargne", "100,00"));
        recipientImport.EnsureSuccessStatusCode();
        var normalizedOutgoingCsv = Csv("16/10/2026", "2001", "Virement épargne", "Épargne", "-100,00");
        using var previewContent = CsvContent(normalizedOutgoingCsv);
        using var previewResponse = await client.PostAsync("/api/imports/preview", previewContent);
        previewResponse.EnsureSuccessStatusCode();
        using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var candidate = Assert.Single(preview.RootElement.GetProperty("transactions")[0]
            .GetProperty("transferMatchCandidates").EnumerateArray());
        Assert.Equal(savingsAccountId, candidate.GetProperty("accountId").GetGuid());
        var counterpartId = candidate.GetProperty("transactionId").GetGuid();

        // Act
        using var commitContent = CsvContent(normalizedOutgoingCsv);
        commitContent.Add(new StringContent(
            $$"""[{"rowNumber":2,"include":true,"confirmTransferMatch":true,"transferTransactionId":"{{counterpartId}}"}]"""),
            "review");
        using var commitResponse = await client.PostAsync("/api/imports/commit", commitContent);

        // Assert
        commitResponse.EnsureSuccessStatusCode();
        using var householdSummaryResponse = await client.GetAsync("/api/transactions/summary?year=2026&month=10");
        using var householdSummary = JsonDocument.Parse(await householdSummaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(0m, householdSummary.RootElement.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(0m, householdSummary.RootElement.GetProperty("expenseTotal").GetDecimal());
        Assert.Equal(0, householdSummary.RootElement.GetProperty("transactionCount").GetInt32());
        using var accountSummaryResponse = await client.GetAsync(
            $"/api/transactions/summary?year=2026&month=10&accountId={currentAccountId}");
        using var accountSummary = JsonDocument.Parse(await accountSummaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(100m, accountSummary.RootElement.GetProperty("expenseTotal").GetDecimal());
    }

    [Fact]
    public async Task Transfers_CanPairTwoOpposingRowsFromTheSameImport()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "3001");
        await CreateAccountAsync(client, "Livret", "3002");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "17/10/2026;17/10/2026;3001;Compte courant;Virement livret;Virement livret;;;-250,00;Non\n"
            + "17/10/2026;17/10/2026;3002;Livret;Virement reçu;Virement reçu;;;250,00;Non\n";

        // Act
        using var commitContent = CsvContent(csv);
        commitContent.Add(new StringContent(
            """[{"rowNumber":2,"include":true,"confirmTransferMatch":true,"transferRowNumber":3}]"""),
            "review");
        using var commitResponse = await client.PostAsync("/api/imports/commit", commitContent);

        // Assert
        commitResponse.EnsureSuccessStatusCode();
        using var summaryResponse = await client.GetAsync("/api/transactions/summary?year=2026&month=10");
        using var summary = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(0m, summary.RootElement.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(0m, summary.RootElement.GetProperty("expenseTotal").GetDecimal());
        Assert.Equal(0, summary.RootElement.GetProperty("transactionCount").GetInt32());
    }

    [Fact]
    public async Task Transfers_MarkMultipleCandidatesAsAmbiguousWithoutChoosingOne()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "5001");
        await CreateAccountAsync(client, "Livret 1", "5002");
        await CreateAccountAsync(client, "Livret 2", "5003");
        using var firstRecipient = await PostCsvAsync(client,
            Csv("17/10/2026", "5002", "Virement reçu 1", "", "75,00"));
        firstRecipient.EnsureSuccessStatusCode();
        using var secondRecipient = await PostCsvAsync(client,
            Csv("18/10/2026", "5003", "Virement reçu 2", "", "75,00"));
        secondRecipient.EnsureSuccessStatusCode();
        using var previewContent = CsvContent(Csv("18/10/2026", "5001", "Virement vers épargne", "", "-75,00"));

        // Act
        using var previewResponse = await client.PostAsync("/api/imports/preview", previewContent);

        // Assert
        previewResponse.EnsureSuccessStatusCode();
        using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var candidates = preview.RootElement.GetProperty("transactions")[0]
            .GetProperty("transferMatchCandidates").EnumerateArray().ToArray();
        Assert.Equal(2, candidates.Length);
        Assert.All(candidates, candidate => Assert.True(candidate.GetProperty("isAmbiguous").GetBoolean()));
        Assert.All(candidates, candidate =>
            Assert.Equal(JsonValueKind.Null, candidate.GetProperty("rowNumber").ValueKind));
    }

    [Fact]
    public async Task Transfers_AreNotPairedUntilTheUserConfirmsAMatch()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "5501");
        await CreateAccountAsync(client, "Livret", "5502");
        using var recipientImport = await PostCsvAsync(client,
            Csv("17/10/2026", "5502", "Virement reçu", "", "75,00"));
        recipientImport.EnsureSuccessStatusCode();
        using var outgoingImport = await PostCsvAsync(client,
            Csv("18/10/2026", "5501", "Virement vers épargne", "", "-75,00"));
        outgoingImport.EnsureSuccessStatusCode();

        // Act
        using var summaryResponse = await client.GetAsync("/api/transactions/summary?year=2026&month=10");
        using var transactionsResponse = await client.GetAsync("/api/transactions");

        // Assert
        summaryResponse.EnsureSuccessStatusCode();
        using var summary = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(75m, summary.RootElement.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(75m, summary.RootElement.GetProperty("expenseTotal").GetDecimal());
        transactionsResponse.EnsureSuccessStatusCode();
        using var transactions = JsonDocument.Parse(await transactionsResponse.Content.ReadAsStringAsync());
        Assert.All(transactions.RootElement.EnumerateArray(), transaction =>
            Assert.False(transaction.GetProperty("isTransfer").GetBoolean()));
    }

    [Fact]
    public async Task Balance_UsesReferenceAfterOperation_AndAddsLaterMovements()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var accountId = await CreateAccountAsync(client, "Compte courant", "1003");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "24/09/2026;24/09/2026;1003;Compte courant;Avant référence;Avant référence;;;50,00;Non\n"
            + "25/09/2026;25/09/2026;1003;Compte courant;Opération référence;Opération référence;;;-20,00;Non\n"
            + "26/09/2026;26/09/2026;1003;Compte courant;Après référence;Après référence;;;5,00;Non\n";
        using var commitResponse = await PostCsvAsync(client, csv);
        commitResponse.EnsureSuccessStatusCode();
        using var transactionsResponse = await client.GetAsync("/api/transactions");
        using var transactionsPayload = JsonDocument.Parse(await transactionsResponse.Content.ReadAsStringAsync());
        var referenceTransactionId = transactionsPayload.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("label").GetString() == "Opération référence")
            .GetProperty("id").GetGuid();
        // Act
        using var setResponse = await client.PutAsync(
            $"/api/accounts/{accountId}/balances/reference",
            JsonContent($$"""{"transactionId":"{{referenceTransactionId}}","balanceAfterTransaction":100}"""));
        setResponse.EnsureSuccessStatusCode();
        using var balanceResponse = await client.GetAsync($"/api/accounts/{accountId}/balances");

        // Assert
        balanceResponse.EnsureSuccessStatusCode();
        using var balancePayload = JsonDocument.Parse(await balanceResponse.Content.ReadAsStringAsync());
        Assert.Equal(105m, balancePayload.RootElement.GetProperty("actualBalance").GetDecimal());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("konta-planning-test-");
        public string FullName => _directory.FullName;
        public void Dispose() => _directory.Delete(recursive: true);
    }

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string name, string accountNumber)
    {
        using var response = await client.PostAsync("/api/accounts", JsonContent(
            JsonSerializer.Serialize(new { name, bankAccountNumber = accountNumber, bankLabel = name, kind = "Current" })));
        response.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return result.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsync("/api/categories", JsonContent(JsonSerializer.Serialize(new { name })));
        response.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return result.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> PostCsvAsync(HttpClient client, string csv)
    {
        using var content = CsvContent(csv);
        return await client.PostAsync("/api/imports/commit", content);
    }

    private static MultipartFormDataContent CsvContent(string csv)
    {
        var content = new MultipartFormDataContent();
        var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        return content;
    }

    private static string Csv(string date, string accountNumber, string label, string category, string amount) =>
        "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
        + $"{date};{date};{accountNumber};Compte courant;{label};{label};{category};;{amount};Non\n";

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private static WebApplicationFactory<Program> CreateFactory(TemporaryDirectory dataDirectory)
    {
        var databasePath = Path.Combine(dataDirectory.FullName, "test.db");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("Konta:DatabasePath", databasePath);
            webHost.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Konta:DatabasePath"] = databasePath
                }));
        });
    }
}
