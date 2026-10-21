using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ImportApiIntegrationTests
{
    [Fact]
    public async Task Preview_UsesExplicitAccountMappings_WhenProvided()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;ACHAT;ACHAT;Virements;;-25,00;Non\n";

        await CreateAccountAsync(client, "Compte source", "0001", "Compte Commun", "Current");
        var accountId = await CreateAccountAsync(client, "Compte cible", "0002", "Compte cible", "Savings");
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        content.Add(new StringContent(JsonSerializer.Serialize(new Dictionary<string, Guid>
        {
            ["0001"] = accountId
        })), "accountMappings");

        // Act
        using var response = await client.PostAsync("/api/imports/preview", content);

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Compte cible", payload.RootElement.GetProperty("transactions")[0].GetProperty("accountName").GetString());
    }

    [Fact]
    public async Task Commit_UsesReviewAccountOverride_WhenProvided()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        var targetAccountId = await CreateAccountAsync(client, "Compte cible", "0002", "Compte cible", "Savings");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;ACHAT;ACHAT;;;-12,00;Non\n";
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        content.Add(new StringContent($$"""
            [
              { "rowNumber": 2, "include": true, "accountId": "{{targetAccountId}}", "categoryName": "Courses", "subcategoryName": "Restaurants" }
            ]
            """), "review");

        // Act
        using var response = await client.PostAsync("/api/imports/commit", content);

        // Assert
        response.EnsureSuccessStatusCode();
        using var listResponse = await client.GetAsync("/api/transactions");
        using var transactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var transaction = Assert.Single(transactions.RootElement.EnumerateArray());
        Assert.Equal("Compte cible", transaction.GetProperty("accountName").GetString());
        Assert.Equal("Courses", transaction.GetProperty("categoryName").GetString());
        Assert.Equal("Restaurants", transaction.GetProperty("subcategoryName").GetString());

        using var categoriesResponse = await client.GetAsync("/api/categories");
        using var categories = JsonDocument.Parse(await categoriesResponse.Content.ReadAsStringAsync());
        Assert.Contains(categories.RootElement.EnumerateArray(), category =>
            category.GetProperty("name").GetString() == "Courses");
    }

    [Fact]
    public async Task Commit_ReturnsConflict_WhenAccountIsUnmapped()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0099;Compte inconnu;ACHAT;ACHAT;;;-12,00;Non\n";

        using var response = await PostCsvAsync(client, "/api/imports/commit", csv);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);

        // Act
        using var listResponse = await client.GetAsync("/api/transactions");
        using var savedTransactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());

        // Assert
        Assert.Empty(savedTransactions.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Commit_AppliesReviewedCorrections_AndExcludesUncheckedRows()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;ACHAT;ACHAT;;;-12,00;Non\n"
            + "26/09/2026;26/09/2026;0001;Compte Commun;ABONNEMENT;ABONNEMENT;;;-5,00;Non\n";
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        content.Add(new StringContent("""
            [
              { "rowNumber": 2, "include": true, "transactionDate": "2026-09-24", "label": "Achat corrigé", "amount": 15.5, "type": "Income" },
              { "rowNumber": 3, "include": false }
            ]
            """), "review");

        // Act
        using var response = await client.PostAsync("/api/imports/commit", content);

        // Assert
        response.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, result.RootElement.GetProperty("importedCount").GetInt32());
        Assert.Equal(1, result.RootElement.GetProperty("excludedCount").GetInt32());

        using var listResponse = await client.GetAsync("/api/transactions");
        using var transactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var transaction = Assert.Single(transactions.RootElement.EnumerateArray());
        Assert.Equal("2026-09-24", transaction.GetProperty("date").GetString());
        Assert.Equal("Achat corrigé", transaction.GetProperty("label").GetString());
        Assert.Equal(15.5m, transaction.GetProperty("amount").GetDecimal());
        Assert.Equal("Income", transaction.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Commit_CreatesSubcategoryRule_AndAppliesItToFutureMatchingImports()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;CARTE X9734 MACHINE;CARTE X9734 MACHINE;;;-0,35;Non\n";
        using var content = CsvContent(csv);
        content.Add(new StringContent("""
            [{
              "rowNumber": 2,
              "include": true,
              "categoryName": "Vie quotidienne",
              "subcategoryName": "Machine à café",
              "createSubcategoryRule": true
            }]
            """), "review");

        // Act
        using var commitResponse = await client.PostAsync("/api/imports/commit", content);

        // Assert
        commitResponse.EnsureSuccessStatusCode();
        using var rulesResponse = await client.GetAsync("/api/category-rules");
        using var rules = JsonDocument.Parse(await rulesResponse.Content.ReadAsStringAsync());
        var rule = Assert.Single(rules.RootElement.EnumerateArray());
        Assert.Equal("CARTE X9734 MACHINE", rule.GetProperty("pattern").GetString());
        Assert.Equal("Machine à café", rule.GetProperty("subcategoryName").GetString());

        using var previewResponse = await PostCsvAsync(client, "/api/imports/preview", csv);
        previewResponse.EnsureSuccessStatusCode();
        using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var transaction = preview.RootElement.GetProperty("transactions")[0];
        Assert.Equal("Vie quotidienne", transaction.GetProperty("categoryName").GetString());
        Assert.Equal("Machine à café", transaction.GetProperty("subcategoryName").GetString());
    }

    [Fact]
    public async Task Commit_ReturnsBadRequest_WhenReviewedAmountIsNotPositive()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;ACHAT;ACHAT;;;-12,00;Non\n";
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        content.Add(new StringContent("""
            [{ "rowNumber": 2, "include": true, "amount": 0 }]
            """), "review");

        // Act
        using var response = await client.PostAsync("/api/imports/commit", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Corrections d'import invalides", problem.RootElement.GetProperty("title").GetString());

        using var listResponse = await client.GetAsync("/api/transactions");
        using var transactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Empty(transactions.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Commit_SavesTransactionsForTheirAccounts()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        await CreateAccountAsync(client, "Livret", "0002", "Livret A", "Savings");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;ACHAT;ACHAT;Virements;;-25,00;Non\n"
            + "25/09/2026;25/09/2026;0002;Livret A;VERSEMENT;VERSEMENT;Virements;;25,00;Non\n";

        // Act
        using var commitResponse = await PostCsvAsync(client, "/api/imports/commit", csv);

        // Assert
        commitResponse.EnsureSuccessStatusCode();

        // Act
        using var listResponse = await client.GetAsync("/api/transactions");
        using var savedTransactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var accountNames = savedTransactions.RootElement.EnumerateArray()
            .Select(transaction => transaction.GetProperty("accountName").GetString())
            .ToArray();

        // Assert
        Assert.Equal(2, accountNames.Length);
        Assert.Contains("Compte commun", accountNames);
        Assert.Contains("Livret", accountNames);
    }

    [Fact]
    public async Task Commit_UsesCategoryReview_WhenProvided()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;ACHAT;ACHAT;Virements;;-25,00;Non\n";
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        content.Add(new StringContent("""
            [
              { "rowNumber": 2, "include": true, "categoryName": "Courses" }
            ]
            """), "review");

        // Act
        using var response = await client.PostAsync("/api/imports/commit", content);

        // Assert
        response.EnsureSuccessStatusCode();
        using var listResponse = await client.GetAsync("/api/transactions");
        using var transactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var transaction = Assert.Single(transactions.RootElement.EnumerateArray());
        Assert.Equal("Courses", transaction.GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task Commit_ReportsDuplicates_WhenCsvIsImportedTwice()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;ACHAT;ACHAT;Virements;;-25,00;Non\n";

        // Act
        using var firstResponse = await PostCsvAsync(client, "/api/imports/commit", csv);

        // Assert
        firstResponse.EnsureSuccessStatusCode();

        // Act
        using var secondResponse = await PostCsvAsync(client, "/api/imports/commit", csv);

        // Assert
        secondResponse.EnsureSuccessStatusCode();
        using var secondResult = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());

        Assert.Equal(0, secondResult.RootElement.GetProperty("importedCount").GetInt32());
        Assert.Equal(1, secondResult.RootElement.GetProperty("duplicateCount").GetInt32());
    }

    [Fact]
    public async Task Preview_SuggestsRuleCategory_AndCommitAppliesIt()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        using var categoryResponse = await client.PostAsync(
            "/api/categories",
            new StringContent("{ \"name\": \"Achats en ligne\" }", Encoding.UTF8, "application/json"));
        categoryResponse.EnsureSuccessStatusCode();
        using var categoryPayload = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryPayload.RootElement.GetProperty("id").GetGuid();
        using var ruleResponse = await client.PostAsync(
            "/api/category-rules",
            new StringContent(
                JsonSerializer.Serialize(new { name = "Amazon", pattern = "AMAZON", categoryId }),
                Encoding.UTF8,
                "application/json"));
        ruleResponse.EnsureSuccessStatusCode();
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;PAIEMENT AMAZON;PAIEMENT AMAZON;;;-25,00;Non\n";

        // Act
        using var previewContent = CsvContent(csv);
        using var previewResponse = await client.PostAsync("/api/imports/preview", previewContent);

        // Assert
        previewResponse.EnsureSuccessStatusCode();
        using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var suggestion = preview.RootElement.GetProperty("transactions")[0].GetProperty("categorySuggestion");
        Assert.Equal("Achats en ligne", suggestion.GetProperty("categoryName").GetString());
        Assert.Equal("Amazon", suggestion.GetProperty("ruleName").GetString());

        // Act
        using var commitResponse = await PostCsvAsync(client, "/api/imports/commit", csv);

        // Assert
        commitResponse.EnsureSuccessStatusCode();
        using var transactionsResponse = await client.GetAsync("/api/transactions");
        using var transactions = JsonDocument.Parse(await transactionsResponse.Content.ReadAsStringAsync());
        Assert.Equal("Achats en ligne", Assert.Single(transactions.RootElement.EnumerateArray())
            .GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task Summary_FiltersByPeriodAndAccount_AndReturnsCategoryTransactions()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var firstAccount = await CreateAccountAsync(client, "Compte courant", "0001", "Compte courant", "Current");
        await CreateAccountAsync(client, "Épargne", "0002", "Compte épargne", "Savings");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte courant;Courses;Courses;Alimentation;;-10,00;Non\n"
            + "26/09/2026;26/09/2026;0001;Compte courant;Salaire;Salaire;Revenus;;20,00;Non\n"
            + "27/09/2026;27/09/2026;0002;Compte épargne;Courses;Courses;Alimentation;;-7,00;Non\n";
        using var commitResponse = await PostCsvAsync(client, "/api/imports/commit", csv);
        commitResponse.EnsureSuccessStatusCode();

        // Act
        using var response = await client.GetAsync($"/api/transactions/summary?year=2026&month=9&accountId={firstAccount}");

        // Assert
        response.EnsureSuccessStatusCode();
        using var summary = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = summary.RootElement;
        Assert.Equal(2, result.GetProperty("transactionCount").GetInt32());
        Assert.Equal(20m, result.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(10m, result.GetProperty("expenseTotal").GetDecimal());
        Assert.Equal(10m, result.GetProperty("netTotal").GetDecimal());
        var foodCategory = result.GetProperty("categories").EnumerateArray()
            .Single(category => category.GetProperty("categoryName").GetString() == "Alimentation");
        Assert.Equal(1, foodCategory.GetProperty("transactions").GetArrayLength());
        Assert.Equal("Courses", foodCategory.GetProperty("transactions")[0].GetProperty("label").GetString());

        // Act
        using var annualResponse = await client.GetAsync("/api/transactions/summary?year=2026");

        // Assert
        annualResponse.EnsureSuccessStatusCode();
        using var annualSummary = JsonDocument.Parse(await annualResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, annualSummary.RootElement.GetProperty("transactionCount").GetInt32());
        Assert.Equal(20m, annualSummary.RootElement.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(17m, annualSummary.RootElement.GetProperty("expenseTotal").GetDecimal());
    }

    [Fact]
    public async Task Summary_ReturnsBadRequest_WhenMonthIsOutsideRange()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/transactions/summary?year=2026&month=13");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Preview_ReturnsValidTransactions_WithoutPersistingThem()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var samplePath = Path.Combine(AppContext.BaseDirectory, "TestData", "transactions-sample.csv");
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(await File.ReadAllBytesAsync(samplePath));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions-sample.csv");

        // Act
        using var response = await client.PostAsync("/api/imports/preview", content);

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(11, payload.RootElement.GetProperty("transactions").GetArrayLength());
        Assert.Empty(payload.RootElement.GetProperty("errors").EnumerateArray());

        // Act
        using var listResponse = await client.GetAsync("/api/transactions");
        using var persistedTransactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());

        // Assert
        Assert.Empty(persistedTransactions.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Preview_ReturnsProblemDetails_WhenFileIsNotCsv()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StringContent("not a csv");
        content.Add(fileContent, "file", "invalid.txt");

        using var response = await client.PostAsync("/api/imports/preview", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Format non pris en charge", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Swagger_UsesOpenApi31_WhenApplicationRunsInDevelopment()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Equal("3.1.1", document.RootElement.GetProperty("openapi").GetString());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("konta-api-test-");

        public string FullName => _directory.FullName;

        public void Dispose() => _directory.Delete(recursive: true);
    }

    private static async Task<Guid> CreateAccountAsync(
        HttpClient client,
        string name,
        string accountNumber,
        string bankLabel,
        string kind)
    {
        var body = JsonSerializer.Serialize(new
        {
            name,
            bankAccountNumber = accountNumber,
            bankLabel,
            kind
        });
        using var response = await client.PostAsync(
            "/api/accounts",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> PostCsvAsync(HttpClient client, string path, string csv)
    {
        using var content = CsvContent(csv);
        return await client.PostAsync(path, content);
    }

    private static MultipartFormDataContent CsvContent(string csv)
    {
        var content = new MultipartFormDataContent();
        var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        return content;
    }

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