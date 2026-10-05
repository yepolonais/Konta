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

    private static async Task CreateAccountAsync(
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
    }

    private static async Task<HttpResponseMessage> PostCsvAsync(HttpClient client, string path, string csv)
    {
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(Encoding.Latin1.GetBytes(csv));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions.csv");
        return await client.PostAsync(path, content);
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