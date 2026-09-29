using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Konta.Tests;

public sealed class ImportPreviewApiTests
{
    [Fact]
    public async Task PreviewParsesSampleAndDoesNotPersistTransactions()
    {
        using var dataDirectory = new TemporaryDirectory();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(webHost =>
            webHost.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Konta:DatabasePath"] = Path.Combine(dataDirectory.FullName, "test.db")
                })));
        using var client = factory.CreateClient();

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

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("konta-api-test-");

        public string FullName => _directory.FullName;

        public void Dispose() => _directory.Delete(recursive: true);
    }
}