using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class CategoriesApiIntegrationTests
{
    [Fact]
    public async Task Get_AndCreate_Category_RoundTrip()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var createResponse = await client.PostAsync(
            "/api/categories",
            JsonContent("{ \"name\": \"Courses\" }"));

        // Assert
        createResponse.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var categoryId = created.RootElement.GetProperty("id").GetGuid();

        using var listResponse = await client.GetAsync("/api/categories");
        listResponse.EnsureSuccessStatusCode();
        using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var categories = list.RootElement.EnumerateArray().ToArray();
        Assert.Contains(categories, item => item.GetProperty("id").GetGuid() == categoryId);
        Assert.Contains(categories, item => item.GetProperty("name").GetString() == "Courses");
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenCategoryAlreadyExists()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.PostAsync(
            "/api/categories",
            JsonContent("{ \"name\": \"Courses\" }"));
        using var secondResponse = await client.PostAsync(
            "/api/categories",
            JsonContent("{ \"name\": \"courses\" }"));

        // Assert
        firstResponse.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsConflict_WhenTransactionsUseTheCategory()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        using var accountResponse = await client.PostAsync(
            "/api/accounts",
            JsonContent("{ \"name\": \"Compte commun\", \"bankAccountNumber\": \"0001\", \"bankLabel\": \"Compte Commun\", \"kind\": \"Current\" }"));
        accountResponse.EnsureSuccessStatusCode();
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;BOULANGERIE;BOULANGERIE;;;-3,00;Non\n";
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        content.Add(new StringContent("[{ \"rowNumber\": 2, \"include\": true, \"categoryName\": \"Boulangerie\", \"createCategoryRule\": true }]"), "review");
        using var commitResponse = await client.PostAsync("/api/imports/commit", content);
        commitResponse.EnsureSuccessStatusCode();
        using var categories = JsonDocument.Parse(await client.GetStringAsync("/api/categories"));
        var categoryId = categories.RootElement.EnumerateArray().Single().GetProperty("id").GetGuid();

        // Act
        using var response = await client.DeleteAsync($"/api/categories/{categoryId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        using var after = JsonDocument.Parse(await client.GetStringAsync("/api/categories"));
        Assert.Single(after.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task Delete_RemovesUnusedCategory_AndItsRules()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        using var createResponse = await client.PostAsync("/api/categories", JsonContent("{ \"name\": \"Boulangerie\" }"));
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var categoryId = created.RootElement.GetProperty("id").GetGuid();
        using var ruleResponse = await client.PostAsync(
            "/api/category-rules",
            JsonContent($"{{ \"name\": \"Pain\", \"pattern\": \"BOULANGERIE\", \"categoryId\": \"{categoryId}\" }}"));
        ruleResponse.EnsureSuccessStatusCode();

        // Act
        using var response = await client.DeleteAsync($"/api/categories/{categoryId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        using var rules = JsonDocument.Parse(await client.GetStringAsync("/api/category-rules"));
        Assert.Empty(rules.RootElement.EnumerateArray());
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
