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
