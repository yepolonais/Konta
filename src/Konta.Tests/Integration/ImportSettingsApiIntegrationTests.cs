using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ImportSettingsApiIntegrationTests
{
    [Fact]
    public async Task Get_ReturnsAndPersistsDefaultSettings_WhenNoSettingsExist()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync("/api/import-settings");
        using var first = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        using var second = JsonDocument.Parse(await client.GetStringAsync("/api/import-settings"));

        // Assert
        firstResponse.EnsureSuccessStatusCode();
        Assert.Equal("", first.RootElement.GetProperty("bankIdentifier").GetString());
        Assert.Equal(
            "https://particuliers.sg.fr/icd/cbo/index-react-authsec.html#/export-operations",
            first.RootElement.GetProperty("bankUrl").GetString());
        Assert.Equal(first.RootElement.GetProperty("bankUrl").GetString(), second.RootElement.GetProperty("bankUrl").GetString());
    }

    [Fact]
    public async Task Save_PersistsImportSettings_WhenValuesAreValid()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var saveResponse = await client.PutAsJsonAsync("/api/import-settings", new
        {
            bankIdentifier = "test-user-123",
            bankUrl = "https://bank.example.test/export"
        });
        using var saved = JsonDocument.Parse(await saveResponse.Content.ReadAsStringAsync());
        using var persisted = JsonDocument.Parse(await client.GetStringAsync("/api/import-settings"));

        // Assert
        saveResponse.EnsureSuccessStatusCode();
        Assert.Equal("test-user-123", saved.RootElement.GetProperty("bankIdentifier").GetString());
        Assert.Equal("https://bank.example.test/export", saved.RootElement.GetProperty("bankUrl").GetString());
        Assert.Equal("test-user-123", persisted.RootElement.GetProperty("bankIdentifier").GetString());
        Assert.Equal("https://bank.example.test/export", persisted.RootElement.GetProperty("bankUrl").GetString());
    }

    [Fact]
    public async Task Save_ReturnsBadRequest_WhenBankUrlUsesUnsupportedScheme()
    {
        // Arrange
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PutAsJsonAsync("/api/import-settings", new
        {
            bankIdentifier = "test-user-123",
            bankUrl = "ftp://bank.example.test/export"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("konta-settings-test-");

        public string FullName => _directory.FullName;

        public void Dispose() => _directory.Delete(recursive: true);
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
