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
    public async Task DeletePost_RemovesPostAndItsAmounts()
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
        Assert.Empty(payload.RootElement.GetProperty("posts").EnumerateArray());
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
    public async Task UpdatePost_ChangesExpenseKind_WithoutChangingExpenseTotals()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Loyer", "Expense");
        await SetMonthlyAmountAsync(client, postId, 1, 800m);

        // Act
        using var updateResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{postId}",
            JsonContent("""{"name":"Loyer","type":"Expense","expenseKind":"Fixed"}"""));
        using var yearResponse = await client.GetAsync("/api/annual-budgets/2026");
        using var monthResponse = await client.GetAsync("/api/annual-budgets/2026/months/1");

        // Assert
        updateResponse.EnsureSuccessStatusCode();
        using var updated = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal("Fixed", updated.RootElement.GetProperty("expenseKind").GetString());
        Assert.Equal("Expense", updated.RootElement.GetProperty("type").GetString());
        using var year = JsonDocument.Parse(await yearResponse.Content.ReadAsStringAsync());
        Assert.Equal(800m, year.RootElement.GetProperty("expenseTotal").GetDecimal());
        using var month = JsonDocument.Parse(await monthResponse.Content.ReadAsStringAsync());
        Assert.Equal("Fixed", Assert.Single(month.RootElement.GetProperty("posts").EnumerateArray())
            .GetProperty("expenseKind").GetString());
    }

    [Fact]
    public async Task ReorderPosts_PersistsOrderWithinExpenseGroup_WithoutChangingTotals()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var rentId = await CreatePostAsync(client, 2026, "Loyer", "Expense", "Fixed");
        var internetId = await CreatePostAsync(client, 2026, "Internet", "Expense", "Fixed");
        var groceriesId = await CreatePostAsync(client, 2026, "Courses", "Expense", "Variable");
        await SetMonthlyAmountAsync(client, rentId, 1, 900m);
        await SetMonthlyAmountAsync(client, internetId, 1, 40m);
        await SetMonthlyAmountAsync(client, groceriesId, 1, 300m);

        // Act
        using var reorderResponse = await client.PutAsync(
            "/api/annual-budgets/2026/posts/order",
            JsonContent(JsonSerializer.Serialize(new
            {
                type = "Expense",
                expenseKind = "Fixed",
                postIds = new[] { internetId, rentId }
            })));
        using var yearResponse = await client.GetAsync("/api/annual-budgets/2026");
        using var monthResponse = await client.GetAsync("/api/annual-budgets/2026/months/1");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, reorderResponse.StatusCode);
        using var year = JsonDocument.Parse(await yearResponse.Content.ReadAsStringAsync());
        var expensePosts = year.RootElement.GetProperty("posts").EnumerateArray()
            .Where(post => post.GetProperty("type").GetString() == "Expense")
            .ToArray();
        Assert.Equal(
            new[] { "Internet", "Loyer", "Courses" },
            expensePosts.Select(post => post.GetProperty("name").GetString()));
        Assert.Equal(1240m, year.RootElement.GetProperty("expenseTotal").GetDecimal());
        using var month = JsonDocument.Parse(await monthResponse.Content.ReadAsStringAsync());
        var monthPosts = month.RootElement.GetProperty("posts").EnumerateArray()
            .Where(post => post.GetProperty("type").GetString() == "Expense")
            .ToArray();
        Assert.Equal(
            new[] { "Internet", "Loyer", "Courses" },
            monthPosts.Select(post => post.GetProperty("name").GetString()));
        Assert.Equal(1240m, month.RootElement.GetProperty("plannedExpenseTotal").GetDecimal());
    }

    [Fact]
    public async Task ReorderPosts_ReturnsBadRequest_WhenPostFromAnotherGroupIsIncluded()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var fixedId = await CreatePostAsync(client, 2026, "Loyer", "Expense", "Fixed");
        var variableId = await CreatePostAsync(client, 2026, "Courses", "Expense", "Variable");

        // Act
        using var response = await client.PutAsync(
            "/api/annual-budgets/2026/posts/order",
            JsonContent(JsonSerializer.Serialize(new
            {
                type = "Expense",
                expenseKind = "Fixed",
                postIds = new[] { fixedId, variableId }
            })));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task DeletePost_AllowsCategoryReassignment()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "Courses");
        var deletedPostId = await CreatePostAsync(client, 2026, "Anciennes courses", "Expense");
        var activePostId = await CreatePostAsync(client, 2026, "Courses", "Expense");
        using var initialMapping = await client.PutAsync(
            $"/api/annual-budgets/posts/{deletedPostId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { categoryId } })));
        initialMapping.EnsureSuccessStatusCode();

        // Act
        using var deleteResponse = await client.DeleteAsync($"/api/annual-budgets/posts/{deletedPostId}");
        using var reassignmentResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{activePostId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { categoryId } })));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
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

    [Fact]
    public async Task AddExtra_IncludesExtraSeparatelyInTotals_WhenExtraIsCreated()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var incomeId = await CreatePostAsync(client, 2026, "Salaires", "Income");
        await SetMonthlyAmountAsync(client, incomeId, 3, 3000m);

        // Act
        using var createResponse = await client.PostAsync(
            "/api/annual-budgets/2026/extras",
            JsonContent("""{"month":3,"label":"Apport","amount":500}"""));
        using var yearResponse = await client.GetAsync("/api/annual-budgets/2026");
        using var monthResponse = await client.GetAsync("/api/annual-budgets/2026/months/3");

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var year = JsonDocument.Parse(await yearResponse.Content.ReadAsStringAsync());
        Assert.Equal(3000m, year.RootElement.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(500m, year.RootElement.GetProperty("extraTotal").GetDecimal());
        Assert.Equal(3500m, year.RootElement.GetProperty("availableTotal").GetDecimal());
        Assert.Single(year.RootElement.GetProperty("extras").EnumerateArray());
        using var month = JsonDocument.Parse(await monthResponse.Content.ReadAsStringAsync());
        Assert.Equal(500m, month.RootElement.GetProperty("plannedExtraTotal").GetDecimal());
        Assert.Equal(3000m, month.RootElement.GetProperty("plannedIncomeTotal").GetDecimal());
    }

    [Fact]
    public async Task AddExtra_ReturnsBadRequest_WhenAmountIsNotPositive()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsync(
            "/api/annual-budgets/2026/extras",
            JsonContent("""{"month":3,"label":"Apport","amount":0}"""));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExtra_RemovesExtraFromTotals_WhenExtraExists()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        using var createResponse = await client.PostAsync(
            "/api/annual-budgets/2026/extras",
            JsonContent("""{"month":6,"label":"Prime","amount":800}"""));
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var extraId = created.RootElement.GetProperty("id").GetGuid();

        // Act
        using var deleteResponse = await client.DeleteAsync($"/api/annual-budgets/extras/{extraId}");
        using var yearResponse = await client.GetAsync("/api/annual-budgets/2026");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var year = JsonDocument.Parse(await yearResponse.Content.ReadAsStringAsync());
        Assert.Equal(0m, year.RootElement.GetProperty("extraTotal").GetDecimal());
    }

    [Fact]
    public async Task GetMonth_ComputesPlannedAndActualAvailable_WhenMonthHasPlanAndOperations()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "annual-available-test");
        var savingsCategoryId = await CreateCategoryAsync(client, "Epargne vacances");
        await CreateCategoryAsync(client, "Salaire");
        await CreateCategoryAsync(client, "Alimentation");
        var incomeId = await CreatePostAsync(client, 2026, "Salaires", "Income");
        var expenseId = await CreatePostAsync(client, 2026, "Courses", "Expense", "Variable");
        var savingsId = await CreatePostAsync(client, 2026, "Vacances", "Savings");
        await SetMonthlyAmountAsync(client, incomeId, 9, 2000m);
        await SetMonthlyAmountAsync(client, expenseId, 9, 500m);
        await SetMonthlyAmountAsync(client, savingsId, 9, 200m);
        using var extraResponse = await client.PostAsync(
            "/api/annual-budgets/2026/extras",
            JsonContent("""{"month":9,"label":"Apport","amount":100}"""));
        extraResponse.EnsureSuccessStatusCode();
        using var mappingResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{savingsId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { savingsCategoryId } })));
        mappingResponse.EnsureSuccessStatusCode();
        using var importResponse = await PostCsvAsync(client,
            Csv("01/09/2026", "annual-available-test", "Salaire", "Salaire", "2100,00")
            + Csv("10/09/2026", "annual-available-test", "Supermarché", "Alimentation", "-300,00")
            + Csv("05/09/2026", "annual-available-test", "Virement vacances", "Epargne vacances", "-150,00"));
        importResponse.EnsureSuccessStatusCode();

        // Act
        using var response = await client.GetAsync("/api/annual-budgets/2026/months/9");

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1400m, payload.RootElement.GetProperty("plannedAvailableTotal").GetDecimal());
        Assert.Equal(1650m, payload.RootElement.GetProperty("actualAvailableTotal").GetDecimal());
    }

    [Fact]
    public async Task GetMonth_CarriesForecastAcrossMonthsAndYears_WhenPriorMonthHasAvailableAmount()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var priorIncomeId = await CreatePostAsync(client, 2025, "Salaires", "Income");
        var priorExpenseId = await CreatePostAsync(client, 2025, "Dépenses", "Expense");
        var incomeId = await CreatePostAsync(client, 2026, "Salaires", "Income");
        var expenseId = await CreatePostAsync(client, 2026, "Dépenses", "Expense");
        await SetMonthlyAmountAsync(client, priorIncomeId, 12, 1000m);
        await SetMonthlyAmountAsync(client, priorExpenseId, 12, 1200m);
        await SetMonthlyAmountAsync(client, incomeId, 1, 1000m);
        await SetMonthlyAmountAsync(client, expenseId, 1, 700m);
        await SetMonthlyAmountAsync(client, incomeId, 2, 100m);
        await SetMonthlyAmountAsync(client, expenseId, 2, 200m);

        // Act
        using var januaryResponse = await client.GetAsync("/api/annual-budgets/2026/months/1");
        using var februaryResponse = await client.GetAsync("/api/annual-budgets/2026/months/2");
        using var yearResponse = await client.GetAsync("/api/annual-budgets/2026");

        // Assert
        using var january = JsonDocument.Parse(await januaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(-200m, january.RootElement.GetProperty("plannedCarryForwardAmount").GetDecimal());
        Assert.Equal(300m, january.RootElement.GetProperty("plannedAvailableTotal").GetDecimal());
        using var february = JsonDocument.Parse(await februaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(100m, february.RootElement.GetProperty("plannedCarryForwardAmount").GetDecimal());
        Assert.Equal(-100m, february.RootElement.GetProperty("plannedAvailableTotal").GetDecimal());
        using var year = JsonDocument.Parse(await yearResponse.Content.ReadAsStringAsync());
        Assert.Equal(-200m, year.RootElement.GetProperty("plannedCarryForwardAmounts")[0].GetDecimal());
        Assert.Equal(100m, year.RootElement.GetProperty("plannedCarryForwardAmounts")[1].GetDecimal());
        Assert.Equal(200m, year.RootElement.GetProperty("availableTotal").GetDecimal());
    }

    [Fact]
    public async Task GetMonth_ReturnsZeroTotals_WhenMonthHasNoPlanAndNoOperations()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/annual-budgets/2026/months/4");

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal(0m, root.GetProperty("plannedAvailableTotal").GetDecimal());
        Assert.Equal(0m, root.GetProperty("actualAvailableTotal").GetDecimal());
        Assert.Equal(0, root.GetProperty("unassignedTransactionCount").GetInt32());
        Assert.Empty(root.GetProperty("posts").EnumerateArray());
    }

    [Fact]
    public async Task GetMonth_ReturnsNegativeAvailableAndPositiveVariance_WhenExpensesExceedPlan()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "annual-overrun-test");
        var groceriesId = await CreateCategoryAsync(client, "Alimentation");
        var postId = await CreatePostAsync(client, 2026, "Courses", "Expense", "Variable");
        await SetMonthlyAmountAsync(client, postId, 9, 100m);
        using var mappingResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{postId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { groceriesId } })));
        mappingResponse.EnsureSuccessStatusCode();
        using var importResponse = await PostCsvAsync(client,
            Csv("10/09/2026", "annual-overrun-test", "Supermarché", "Alimentation", "-130,00"));
        importResponse.EnsureSuccessStatusCode();

        // Act
        using var response = await client.GetAsync("/api/annual-budgets/2026/months/9");

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal(-100m, root.GetProperty("plannedAvailableTotal").GetDecimal());
        Assert.Equal(-130m, root.GetProperty("actualAvailableTotal").GetDecimal());
        var post = Assert.Single(root.GetProperty("posts").EnumerateArray());
        Assert.Equal(30m, post.GetProperty("varianceAmount").GetDecimal());
    }

    [Fact]
    public async Task GetMonth_ReportsNoActualAndNoVariance_WhenPostHasNoMappedCategory()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Courses", "Expense", "Variable");
        await SetMonthlyAmountAsync(client, postId, 9, 100m);

        // Act
        using var response = await client.GetAsync("/api/annual-budgets/2026/months/9");

        // Assert
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var post = Assert.Single(payload.RootElement.GetProperty("posts").EnumerateArray());
        Assert.False(post.GetProperty("isMapped").GetBoolean());
        Assert.Equal(JsonValueKind.Null, post.GetProperty("actualAmount").ValueKind);
        Assert.Equal(JsonValueKind.Null, post.GetProperty("varianceAmount").ValueKind);
    }

    [Fact]
    public async Task FutureRevisions_KeepPastMonthsAndUpdateTotals_WhenCopiedForward()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        var postId = await CreatePostAsync(client, 2026, "Courses", "Expense", "Variable");
        for (var month = 1; month <= 12; month++)
        {
            await SetMonthlyAmountAsync(client, postId, month, 400m);
        }

        // Act
        await SetMonthlyAmountAsync(client, postId, 6, 450m);
        using var copyResponse = await client.PostAsync(
            $"/api/annual-budgets/posts/{postId}/months/6/copy-forward",
            content: null);
        using var yearResponse = await client.GetAsync("/api/annual-budgets/2026");

        // Assert
        copyResponse.EnsureSuccessStatusCode();
        using var year = JsonDocument.Parse(await yearResponse.Content.ReadAsStringAsync());
        var post = Assert.Single(year.RootElement.GetProperty("posts").EnumerateArray());
        var amounts = post.GetProperty("monthlyAmounts").EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.Null ? (decimal?)null : item.GetDecimal())
            .ToArray();
        Assert.All(amounts.Take(5), amount => Assert.Equal(400m, amount));
        Assert.All(amounts.Skip(5), amount => Assert.Equal(450m, amount));
        var expectedTotal = 5 * 400m + 7 * 450m;
        Assert.Equal(expectedTotal, post.GetProperty("annualTotal").GetDecimal());
        Assert.Equal(expectedTotal, year.RootElement.GetProperty("expenseTotal").GetDecimal());
    }

    [Fact]
    public async Task BudgetJourney_CompareThenAdjustRemainingMonths_KeepsPastAndFutureCoherent()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        await CreateAccountAsync(client, "Compte courant", "annual-journey-test");
        var groceriesId = await CreateCategoryAsync(client, "Alimentation");
        await CreateCategoryAsync(client, "Salaire");
        var salaryId = await CreatePostAsync(client, 2026, "Salaires", "Income");
        var groceriesPostId = await CreatePostAsync(client, 2026, "Courses", "Expense", "Variable");
        for (var month = 1; month <= 12; month++)
        {
            await SetMonthlyAmountAsync(client, salaryId, month, 2000m);
            await SetMonthlyAmountAsync(client, groceriesPostId, month, 400m);
        }

        using var mappingResponse = await client.PutAsync(
            $"/api/annual-budgets/posts/{groceriesPostId}/categories",
            JsonContent(JsonSerializer.Serialize(new { categoryIds = new[] { groceriesId } })));
        mappingResponse.EnsureSuccessStatusCode();
        using var importResponse = await PostCsvAsync(client,
            Csv("03/03/2026", "annual-journey-test", "Salaire", "Salaire", "2000,00")
            + Csv("12/03/2026", "annual-journey-test", "Supermarché", "Alimentation", "-480,00"));
        importResponse.EnsureSuccessStatusCode();

        // Act
        using var marchResponse = await client.GetAsync("/api/annual-budgets/2026/months/3");
        await SetMonthlyAmountAsync(client, groceriesPostId, 4, 320m);
        using var copyResponse = await client.PostAsync(
            $"/api/annual-budgets/posts/{groceriesPostId}/months/4/copy-forward",
            content: null);
        using var extraResponse = await client.PostAsync(
            "/api/annual-budgets/2026/extras",
            JsonContent("""{"month":4,"label":"Apport","amount":80}"""));
        using var yearResponse = await client.GetAsync("/api/annual-budgets/2026");
        using var marchAfterResponse = await client.GetAsync("/api/annual-budgets/2026/months/3");

        // Assert
        copyResponse.EnsureSuccessStatusCode();
        extraResponse.EnsureSuccessStatusCode();
        using var march = JsonDocument.Parse(await marchResponse.Content.ReadAsStringAsync());
        var marchPost = march.RootElement.GetProperty("posts").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "Courses");
        Assert.Equal(480m, marchPost.GetProperty("actualAmount").GetDecimal());
        Assert.Equal(80m, marchPost.GetProperty("varianceAmount").GetDecimal());
        Assert.Equal(1520m, march.RootElement.GetProperty("actualAvailableTotal").GetDecimal());
        using var year = JsonDocument.Parse(await yearResponse.Content.ReadAsStringAsync());
        var expectedExpenses = 3 * 400m + 9 * 320m;
        Assert.Equal(expectedExpenses, year.RootElement.GetProperty("expenseTotal").GetDecimal());
        Assert.Equal(80m, year.RootElement.GetProperty("extraTotal").GetDecimal());
        Assert.Equal(24000m + 80m - expectedExpenses, year.RootElement.GetProperty("availableTotal").GetDecimal());
        using var marchAfter = JsonDocument.Parse(await marchAfterResponse.Content.ReadAsStringAsync());
        var marchAfterPost = marchAfter.RootElement.GetProperty("posts").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "Courses");
        Assert.Equal(400m, marchAfterPost.GetProperty("plannedAmount").GetDecimal());
        Assert.Equal(480m, marchAfterPost.GetProperty("actualAmount").GetDecimal());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetMonth_ReturnsBadRequest_WhenMonthIsOutOfRange(int month)
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/annual-budgets/2026/months/{month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static async Task<Guid> CreatePostAsync(
        HttpClient client,
        int year,
        string name,
        string type,
        string? expenseKind = null)
    {
        using var response = await client.PostAsync(
            $"/api/annual-budgets/{year}/posts",
            JsonContent(JsonSerializer.Serialize(new { name, type, expenseKind })));
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
