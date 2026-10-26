using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class AnnualBudgetsApiIntegrationTests
{
    [Fact]
    public async Task Get_ReturnsMonthlyPlanAndTotals_WhenPostsHaveAmounts()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var incomeId = await CreatePostAsync(client, 2026, "Salaires", "Income");
        var expenseId = await CreatePostAsync(client, 2026, "Loyer", "Expense");
        var savingsId = await CreatePostAsync(client, 2026, "Vacances", "Savings");
        await SetMonthlyAmountAsync(client, incomeId, 1, 3000m);
        await SetMonthlyAmountAsync(client, incomeId, 2, 3000m);
        await SetMonthlyAmountAsync(client, expenseId, 1, 900m);
        await SetMonthlyAmountAsync(client, savingsId, 1, 200m);
        await SetMonthlyAmountAsync(client, savingsId, 2, 0m);

        // Act
        using var response = await client.GetAsync("/api/annual-budgets/2026");

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal(6000m, root.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(900m, root.GetProperty("expenseTotal").GetDecimal());
        Assert.Equal(200m, root.GetProperty("savingsTotal").GetDecimal());
        Assert.Equal(4900m, root.GetProperty("availableTotal").GetDecimal());

        var savings = Assert.Single(root.GetProperty("posts").EnumerateArray()
            .Where(post => post.GetProperty("id").GetGuid() == savingsId));
        var monthlyAmounts = savings.GetProperty("monthlyAmounts");
        Assert.Equal(JsonValueKind.Null, monthlyAmounts[2].ValueKind);
        Assert.Equal(0m, monthlyAmounts[1].GetDecimal());
    }

    [Fact]
    public async Task DeletePost_PreservesHistoricalAmounts_AndExcludesPostFromTotals()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Loisirs", "Expense");
        await SetMonthlyAmountAsync(client, postId, 3, 150m);

        // Act
        using var deleteResponse = await client.DeleteAsync($"/api/annual-budgets/posts/{postId}");
        using var summaryResponse = await client.GetAsync("/api/annual-budgets/2026");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        summaryResponse.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        var post = Assert.Single(payload.RootElement.GetProperty("posts").EnumerateArray());
        Assert.False(post.GetProperty("isActive").GetBoolean());
        Assert.Equal(150m, post.GetProperty("annualTotal").GetDecimal());
        Assert.Equal(0m, payload.RootElement.GetProperty("expenseTotal").GetDecimal());
    }

    [Fact]
    public async Task SetMonthlyAmount_ReturnsBadRequest_WhenAmountIsNegative()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Courses", "Expense");

        // Act
        using var response = await client.PutAsync(
            $"/api/annual-budgets/posts/{postId}/months/1",
            JsonContent("""{"amount":-1}"""));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePost_ChangesNameAndTotals_WhenPostTypeChanges()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Loisirs", "Expense");
        await SetMonthlyAmountAsync(client, postId, 3, 80m);

        // Act
        using var updateResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{postId}",
            JsonContent("""{"name":"Vacances","type":"Savings"}"""));
        using var summaryResponse = await client.GetAsync("/api/annual-budgets/2026");

        // Assert
        updateResponse.EnsureSuccessStatusCode();
        summaryResponse.EnsureSuccessStatusCode();
        using var summary = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(0m, summary.RootElement.GetProperty("expenseTotal").GetDecimal());
        Assert.Equal(80m, summary.RootElement.GetProperty("savingsTotal").GetDecimal());
        Assert.Equal("Vacances", Assert.Single(summary.RootElement.GetProperty("posts").EnumerateArray())
            .GetProperty("name").GetString());
    }

    [Fact]
    public async Task ClearMonthlyAmount_RemovesPlannedValue_WhenMonthIsCleared()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Courses", "Expense");
        await SetMonthlyAmountAsync(client, postId, 1, 100m);

        // Act
        using var clearResponse = await client.DeleteAsync($"/api/annual-budgets/posts/{postId}/months/1");
        using var summaryResponse = await client.GetAsync("/api/annual-budgets/2026");

        // Assert
        clearResponse.EnsureSuccessStatusCode();
        summaryResponse.EnsureSuccessStatusCode();
        using var summary = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        var post = Assert.Single(summary.RootElement.GetProperty("posts").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, post.GetProperty("monthlyAmounts")[0].ValueKind);
        Assert.Equal(0m, summary.RootElement.GetProperty("expenseTotal").GetDecimal());
    }

    [Fact]
    public async Task GetMonth_AggregatesMappedCategories_AndReportsUnassignedTransactions()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "annual-month-test");
        var groceriesId = await CreateCategoryAsync(client, "Alimentation");
        var bakeryId = await CreateCategoryAsync(client, "Boulangerie");
        await CreateCategoryAsync(client, "Loisirs");
        var postId = await CreatePostAsync(client, 2026, "Courses", "Expense");
        await SetMonthlyAmountAsync(client, postId, 9, 500m);
        using var mappingResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{postId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { groceriesId, bakeryId } })));
        mappingResponse.EnsureSuccessStatusCode();
        using var importResponse = await PostCsvAsync(client,
            Csv("10/09/2026", "annual-month-test", "Supermarché", "Alimentation", "-60,00")
            + Csv("11/09/2026", "annual-month-test", "Boulangerie", "Boulangerie", "-10,00")
            + Csv("12/09/2026", "annual-month-test", "Cinéma", "Loisirs", "-20,00"));
        importResponse.EnsureSuccessStatusCode();

        // Act
        using var response = await client.GetAsync("/api/annual-budgets/2026/months/9");

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal(90m, root.GetProperty("actualExpenseTotal").GetDecimal());
        Assert.Equal(20m, root.GetProperty("unassignedExpenseTotal").GetDecimal());
        Assert.Equal(1, root.GetProperty("unassignedTransactionCount").GetInt32());
        var post = Assert.Single(root.GetProperty("posts").EnumerateArray());
        Assert.True(post.GetProperty("isMapped").GetBoolean());
        Assert.Equal(70m, post.GetProperty("actualAmount").GetDecimal());
        Assert.Equal(-430m, post.GetProperty("varianceAmount").GetDecimal());
        Assert.Equal(2, post.GetProperty("transactionCount").GetInt32());
    }

    [Fact]
    public async Task SetCategoryMappings_ReturnsConflict_WhenCategoryAlreadyBelongsToAnotherPost()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "Courses");
        var firstPostId = await CreatePostAsync(client, 2026, "Alimentation", "Expense");
        var secondPostId = await CreatePostAsync(client, 2026, "Repas", "Expense");
        using var firstMapping = await client.PutAsync(
            $"/api/annual-budgets/posts/{firstPostId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { categoryId } })));
        firstMapping.EnsureSuccessStatusCode();

        // Act
        using var response = await client.PutAsync(
            $"/api/annual-budgets/posts/{secondPostId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { categoryId } })));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_AllowsCategoryReassignment_WhenPostIsArchived()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "Courses");
        var archivedPostId = await CreatePostAsync(client, 2026, "Anciennes courses", "Expense");
        var activePostId = await CreatePostAsync(client, 2026, "Courses", "Expense");
        using var initialMapping = await client.PutAsync(
            $"/api/annual-budgets/posts/{archivedPostId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { categoryId } })));
        initialMapping.EnsureSuccessStatusCode();

        // Act
        using var archiveResponse = await client.DeleteAsync($"/api/annual-budgets/posts/{archivedPostId}");
        using var reassignmentResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{activePostId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { categoryId } })));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
        reassignmentResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetMonth_CountsMappedCategoryAsSavings_WithoutAddingItToExpenses()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "annual-savings-test");
        var savingsCategoryId = await CreateCategoryAsync(client, "Epargne vacances");
        var postId = await CreatePostAsync(client, 2026, "Vacances", "Savings");
        await SetMonthlyAmountAsync(client, postId, 9, 200m);
        using var mappingResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{postId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { savingsCategoryId } })));
        mappingResponse.EnsureSuccessStatusCode();
        using var importResponse = await PostCsvAsync(client,
            Csv("05/09/2026", "annual-savings-test", "Virement vacances", "Epargne vacances", "-150,00"));
        importResponse.EnsureSuccessStatusCode();

        // Act
        using var response = await client.GetAsync("/api/annual-budgets/2026/months/9");

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal(150m, root.GetProperty("actualSavingsTotal").GetDecimal());
        Assert.Equal(0m, root.GetProperty("actualExpenseTotal").GetDecimal());
        Assert.Equal(0, root.GetProperty("unassignedTransactionCount").GetInt32());
        var post = Assert.Single(root.GetProperty("posts").EnumerateArray());
        Assert.Equal(150m, post.GetProperty("actualAmount").GetDecimal());
        Assert.Equal(-50m, post.GetProperty("varianceAmount").GetDecimal());
    }

    [Fact]
    public async Task CopyForward_FillsFollowingMonths_WhenSourceMonthHasAmount()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Loyer", "Expense");
        await SetMonthlyAmountAsync(client, postId, 1, 700m);
        await SetMonthlyAmountAsync(client, postId, 3, 900m);

        // Act
        using var response = await client.PostAsync(
            $"/api/annual-budgets/posts/{postId}/months/3/copy-forward",
            content: null);

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var amounts = payload.RootElement.GetProperty("monthlyAmounts").EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.Null ? (decimal?)null : item.GetDecimal())
            .ToArray();
        Assert.Equal(700m, amounts[0]);
        Assert.Null(amounts[1]);
        Assert.All(amounts.Skip(2), amount => Assert.Equal(900m, amount));
        Assert.Equal(700m + 10 * 900m, payload.RootElement.GetProperty("annualTotal").GetDecimal());
    }

    [Fact]
    public async Task CopyForward_ReturnsConflict_WhenSourceMonthIsEmpty()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Loyer", "Expense");

        // Act
        using var response = await client.PostAsync(
            $"/api/annual-budgets/posts/{postId}/months/2/copy-forward",
            content: null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string name, string accountNumber)
    {
        using var response = await client.PostAsync("/api/accounts", JsonContent(
            JsonSerializer.Serialize(new { name, bankAccountNumber = accountNumber, bankLabel = name, kind = "Current" })));
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsync(
            "/api/categories",
            JsonContent(JsonSerializer.Serialize(new { name })));
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> PostCsvAsync(HttpClient client, string csv)
    {
        using var content = new MultipartFormDataContent();
        var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        return await client.PostAsync("/api/imports/commit", content);
    }

    private static string Csv(string date, string accountNumber, string label, string category, string amount) =>
        "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
        + $"{date};{date};{accountNumber};Compte courant;{label};{label};{category};;{amount};Non\n";

    private static async Task<Guid> CreatePostAsync(HttpClient client, int year, string name, string type)
    {
        using var response = await client.PostAsync(
            $"/api/annual-budgets/{year}/posts",
            JsonContent(JsonSerializer.Serialize(new { name, type })));
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task SetMonthlyAmountAsync(
        HttpClient client,
        Guid postId,
        int month,
        decimal amount)
    {
        using var response = await client.PutAsync(
            $"/api/annual-budgets/posts/{postId}/months/{month}",
            JsonContent(JsonSerializer.Serialize(new { amount })));
        response.EnsureSuccessStatusCode();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("konta-api-test-");

        public string FullName => _directory.FullName;

        public void Dispose() => _directory.Delete(recursive: true);
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

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
