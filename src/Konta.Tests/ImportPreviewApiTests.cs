using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests;

public sealed class ImportPreviewApiTests
{
    [Fact]
    public async Task CommitRoutesTransactionsByAccountAndDoesNotDuplicateReimports()
    {
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

        var commonAccountId = await CreateAccountAsync(client, "Compte commun", "0001", "Compte Commun", "Current");
        var savingsAccountId = await CreateAccountAsync(client, "Livret", "0002", "Livret A", "Savings");
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;VIREMENT;VIREMENT;Virements;;-25,00;Non\n"
            + "25/09/2026;25/09/2026;0001;Compte Commun;VIREMENT;VIREMENT;Virements;;-25,00;Non\n"
            + "25/09/2026;25/09/2026;0002;Livret A;VIREMENT;VIREMENT;Virements;;-25,00;Non\n";

        using var previewResponse = await PostCsvAsync(client, "/api/imports/preview", csv);
        previewResponse.EnsureSuccessStatusCode();
        using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        Assert.Equal(commonAccountId.ToString(), preview.RootElement.GetProperty("transactions")[0].GetProperty("accountId").GetString());
        Assert.Equal("Compte commun", preview.RootElement.GetProperty("transactions")[0].GetProperty("accountName").GetString());
        Assert.Equal(savingsAccountId.ToString(), preview.RootElement.GetProperty("transactions")[2].GetProperty("accountId").GetString());

        using var firstCommit = await PostCsvAsync(client, "/api/imports/commit", csv);
        firstCommit.EnsureSuccessStatusCode();
        using var firstResult = JsonDocument.Parse(await firstCommit.Content.ReadAsStringAsync());
        Assert.Equal(3, firstResult.RootElement.GetProperty("importedCount").GetInt32());
        Assert.Equal(0, firstResult.RootElement.GetProperty("duplicateCount").GetInt32());

        using var secondCommit = await PostCsvAsync(client, "/api/imports/commit", csv);
        secondCommit.EnsureSuccessStatusCode();
        using var secondResult = JsonDocument.Parse(await secondCommit.Content.ReadAsStringAsync());
        Assert.Equal(0, secondResult.RootElement.GetProperty("importedCount").GetInt32());
        Assert.Equal(3, secondResult.RootElement.GetProperty("duplicateCount").GetInt32());

        using var listResponse = await client.GetAsync("/api/transactions");
        listResponse.EnsureSuccessStatusCode();
        using var savedTransactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, savedTransactions.RootElement.GetArrayLength());
        var accountNames = savedTransactions.RootElement.EnumerateArray()
            .Select(transaction => transaction.GetProperty("accountName").GetString())
            .ToArray();
        Assert.Equal(2, accountNames.Count(name => name == "Compte commun"));
        Assert.Contains("Livret", accountNames);
    }

    [Fact]
    public async Task CommitDoesNotSaveAnythingWhenAnAccountIsUnmapped()
    {
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        const string csv = "Date transaction;Date comptabilisation;Num Compte;Libellé Compte;Libellé opération;Libellé complet;Catégorie;Sous-Catégorie;Montant;Pointée;\n"
            + "25/09/2026;25/09/2026;0099;Compte inconnu;ACHAT;ACHAT;;;-12,00;Non\n";

        using var response = await PostCsvAsync(client, "/api/imports/commit", csv);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        using var listResponse = await client.GetAsync("/api/transactions");
        using var savedTransactions = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Empty(savedTransactions.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task PreviewParsesSampleAndDoesNotPersistTransactions()
    {
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();

            using var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
            swaggerResponse.EnsureSuccessStatusCode();
            using var swaggerDocument = JsonDocument.Parse(await swaggerResponse.Content.ReadAsStringAsync());
            Assert.Equal("3.1.1", swaggerDocument.RootElement.GetProperty("openapi").GetString());

        var samplePath = Path.Combine(AppContext.BaseDirectory, "TestData", "transactions-sample.csv");
        using var content = new MultipartFormDataContent();
        using var csvContent = new ByteArrayContent(await File.ReadAllBytesAsync(samplePath));
        csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csvContent, "file", "transactions-sample.csv");

        var response = await client.PostAsync("/api/imports/preview", content);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var payload = await JsonDocument.ParseAsync(responseStream);
        Assert.Equal(11, payload.RootElement.GetProperty("transactions").GetArrayLength());
        Assert.Equal(0, payload.RootElement.GetProperty("errors").GetArrayLength());
        Assert.Equal(
            "00050021832",
            payload.RootElement.GetProperty("transactions")[0].GetProperty("accountNumber").GetString());

        using var listResponse = await client.GetAsync("/api/transactions");
        listResponse.EnsureSuccessStatusCode();
        await using var listStream = await listResponse.Content.ReadAsStreamAsync();
        using var persistedTransactions = await JsonDocument.ParseAsync(listStream);
        Assert.Equal(0, persistedTransactions.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task PreviewRejectsFilesThatAreNotCsvWithProblemDetails()
    {
        using var dataDirectory = new TemporaryDirectory();
        using var factory = CreateFactory(dataDirectory);
        using var client = factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StringContent("not a csv");
        content.Add(fileContent, "file", "invalid.txt");

        var response = await client.PostAsync("/api/imports/preview", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Format non pris en charge", problem.RootElement.GetProperty("title").GetString());
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