using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PlanningApiIntegrationTests
{
    [Fact]
    public async Task Budgets_ApplyMonthlyOverride_AndIncludeActualExpenses()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "1001");
        var categoryId = await CreateCategoryAsync(client, "Alimentation");
        using var createResponse = await client.PostAsync("/api/planning/budgets", JsonContent(
            $$"""{"categoryId":"{{categoryId}}","amount":200}"""));
        createResponse.EnsureSuccessStatusCode();
        using var commitResponse = await PostCsvAsync(client, Csv(
            "26/09/2026", "1001", "Courses", "Alimentation", "-25,00"));
        commitResponse.EnsureSuccessStatusCode();

        // Act
        using var createdBudget = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var budgetId = createdBudget.RootElement.GetProperty("id").GetGuid();
        using var overrideResponse = await client.PutAsync(
            $"/api/planning/budgets/{budgetId}/overrides/2026/9",
            JsonContent("""{"amount":300}"""));

        // Assert
        overrideResponse.EnsureSuccessStatusCode();
        using var budgetResponse = await client.GetAsync("/api/planning/budgets?year=2026&month=9");
        budgetResponse.EnsureSuccessStatusCode();
        using var budgets = JsonDocument.Parse(await budgetResponse.Content.ReadAsStringAsync());
        var budget = Assert.Single(budgets.RootElement.EnumerateArray());
        Assert.Equal(categoryId, budget.GetProperty("categoryId").GetGuid());
        Assert.Equal(200m, budget.GetProperty("recurringAmount").GetDecimal());
        Assert.Equal(300m, budget.GetProperty("effectiveAmount").GetDecimal());
        Assert.Equal(25m, budget.GetProperty("expenseTotal").GetDecimal());
        Assert.Equal(275m, budget.GetProperty("remainingAmount").GetDecimal());
        Assert.True(budget.GetProperty("hasMonthlyOverride").GetBoolean());
    }

    [Fact]
    public async Task Forecast_IsSeparateUntilImportMatchIsConfirmed()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var accountId = await CreateAccountAsync(client, "Compte courant", "1002");
        using var createResponse = await client.PostAsync("/api/planning/forecasts", JsonContent(
            $$"""{"name":"Abonnement vidéo","labelPattern":"STREAMING","accountId":"{{accountId}}","amount":12,"type":"Expense","dayOfMonth":25,"frequency":"Monthly","startDate":"2026-09-01"}"""));
        createResponse.EnsureSuccessStatusCode();
        using var beforeCommit = await client.GetAsync("/api/transactions");
        using var emptyTransactions = JsonDocument.Parse(await beforeCommit.Content.ReadAsStringAsync());
        Assert.Empty(emptyTransactions.RootElement.EnumerateArray());
        using var occurrenceResponse = await client.GetAsync("/api/planning/occurrences?startDate=2026-09-01&endDate=2026-09-30");
        occurrenceResponse.EnsureSuccessStatusCode();
        using var occurrences = JsonDocument.Parse(await occurrenceResponse.Content.ReadAsStringAsync());
        var scheduled = Assert.Single(occurrences.RootElement.EnumerateArray());
        Assert.False(scheduled.GetProperty("isCompleted").GetBoolean());

        using var forecast = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var forecastId = forecast.RootElement.GetProperty("id").GetGuid();
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;1002;Compte courant;PAIEMENT STREAMING;PAIEMENT STREAMING;;;-13,50;Non\n";
        using var previewContent = CsvContent(csv);
        using var previewResponse = await client.PostAsync("/api/imports/preview", previewContent);
        previewResponse.EnsureSuccessStatusCode();
        using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var match = Assert.Single(preview.RootElement.GetProperty("transactions")[0]
            .GetProperty("forecastMatchCandidates").EnumerateArray());
        Assert.Equal(forecastId, match.GetProperty("forecastId").GetGuid());
        Assert.Equal(1.5m, match.GetProperty("amountDifference").GetDecimal());

        // Act
        using var commitContent = CsvContent(csv);
        commitContent.Add(new StringContent(
            $$"""[{"rowNumber":2,"include":true,"confirmForecastMatch":true,"forecastId":"{{forecastId}}","forecastDate":"2026-09-25"}]"""),
            "review");
        using var commitResponse = await client.PostAsync("/api/imports/commit", commitContent);

        // Assert
        commitResponse.EnsureSuccessStatusCode();
        using var completedResponse = await client.GetAsync("/api/planning/occurrences?startDate=2026-09-01&endDate=2026-09-30");
        using var completedPayload = JsonDocument.Parse(await completedResponse.Content.ReadAsStringAsync());
        var completed = Assert.Single(completedPayload.RootElement.EnumerateArray());
        Assert.True(completed.GetProperty("isCompleted").GetBoolean());
        Assert.Equal(13.5m, completed.GetProperty("actualAmount").GetDecimal());
    }

    [Fact]
    public async Task Forecast_DoesNotMatchUnexpectedTransaction_AndLeavesOccurrenceUncompleted()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var accountId = await CreateAccountAsync(client, "Compte courant", "1004");
        using var createResponse = await client.PostAsync("/api/planning/forecasts", JsonContent(
            $$"""{"name":"Échéance attendue","labelPattern":"EXPECTED","accountId":"{{accountId}}","amount":40,"type":"Expense","dayOfMonth":25,"frequency":"Monthly","startDate":"2026-09-01"}"""));
        createResponse.EnsureSuccessStatusCode();

        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "30/09/2026;30/09/2026;1004;Compte courant;PAIEMENT EXPECTED;PAIEMENT EXPECTED;;;-40,00;Non\n";
        using var commitResponse = await PostCsvAsync(client, csv);
        commitResponse.EnsureSuccessStatusCode();

        // Act
        using var occurrenceResponse = await client.GetAsync("/api/planning/occurrences?startDate=2026-09-01&endDate=2026-09-30");

        // Assert
        occurrenceResponse.EnsureSuccessStatusCode();
        using var occurrences = JsonDocument.Parse(await occurrenceResponse.Content.ReadAsStringAsync());
        var occurrence = Assert.Single(occurrences.RootElement.EnumerateArray());
        Assert.False(occurrence.GetProperty("isCompleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, occurrence.GetProperty("actualAmount").ValueKind);
    }

    [Fact]
    public async Task Forecasts_GenerateMonthlyAndAnnualOccurrences_ClampingToMonthEnd()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var accountId = await CreateAccountAsync(client, "Compte courant", "1005");
        foreach (var (name, frequency) in new[] { ("Mensuelle", "Monthly"), ("Annuelle", "Annual") })
        {
            using var createResponse = await client.PostAsync("/api/planning/forecasts", JsonContent(
                $$"""{"name":"{{name}}","labelPattern":"{{name}}","accountId":"{{accountId}}","amount":10,"type":"Expense","dayOfMonth":31,"frequency":"{{frequency}}","startDate":"2026-02-01"}"""));
            createResponse.EnsureSuccessStatusCode();
        }

        // Act
        using var occurrenceResponse = await client.GetAsync(
            "/api/planning/occurrences?startDate=2026-02-01&endDate=2027-02-28");

        // Assert
        occurrenceResponse.EnsureSuccessStatusCode();
        using var occurrences = JsonDocument.Parse(await occurrenceResponse.Content.ReadAsStringAsync());
        var monthlyOccurrences = occurrences.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("name").GetString() == "Mensuelle")
            .ToArray();
        var annualOccurrences = occurrences.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("name").GetString() == "Annuelle")
            .ToArray();
        Assert.Equal(13, monthlyOccurrences.Length);
        Assert.All(monthlyOccurrences, item =>
        {
            var scheduledDate = DateOnly.Parse(item.GetProperty("scheduledDate").GetString()!);
            Assert.Equal(DateTime.DaysInMonth(scheduledDate.Year, scheduledDate.Month), scheduledDate.Day);
        });
        Assert.Equal(new[] { "2026-02-28", "2027-02-28" }, annualOccurrences
            .Select(item => item.GetProperty("scheduledDate").GetString()));
    }

    [Fact]
    public async Task Balance_UsesReferenceAfterOperation_AndAddsFutureMovementsAndForecasts()
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
        var forecastResponse = await client.PostAsync("/api/planning/forecasts", JsonContent(
            $$"""{"name":"Loyer","labelPattern":"LOYER","accountId":"{{accountId}}","amount":30,"type":"Expense","dayOfMonth":25,"frequency":"Monthly","startDate":"2026-11-01"}"""));
        forecastResponse.EnsureSuccessStatusCode();

        // Act
        using var setResponse = await client.PutAsync(
            $"/api/accounts/{accountId}/balances/reference",
            JsonContent($$"""{"transactionId":"{{referenceTransactionId}}","balanceAfterTransaction":100}"""));
        setResponse.EnsureSuccessStatusCode();
        using var balanceResponse = await client.GetAsync($"/api/accounts/{accountId}/balances?throughDate=2026-12-01");

        // Assert
        balanceResponse.EnsureSuccessStatusCode();
        using var balancePayload = JsonDocument.Parse(await balanceResponse.Content.ReadAsStringAsync());
        Assert.Equal(105m, balancePayload.RootElement.GetProperty("actualBalance").GetDecimal());
        Assert.Equal(75m, balancePayload.RootElement.GetProperty("forecastBalance").GetDecimal());
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
