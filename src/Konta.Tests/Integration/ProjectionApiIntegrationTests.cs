using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konta.Domain.Accounts;
using Konta.Domain.Budgets;
using Konta.Domain.Categories;
using Konta.Domain.Transactions;
using Konta.Domain.Transfers;
using Konta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konta.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ProjectionApiIntegrationTests
{
    [Fact]
    public async Task GetMonth_RequiresCommonAccount_WhenNoneDesignated()
    {
        using var directory = new TemporaryDirectory();
        using var factory = CreateFactory(directory);
        using var client = factory.CreateClient();

        using var monthly = await client.GetAsync("/api/annual-budgets/2026/months/11");
        using var projection = await client.GetAsync("/api/projection");

        Assert.Equal(HttpStatusCode.Conflict, monthly.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, projection.StatusCode);
    }

    [Fact]
    public async Task Projection_UsesCommonAccountAndRemainingBudget_WithoutFutureActuals()
    {
        using var directory = new TemporaryDirectory();
        using var factory = CreateFactory(directory);
        using var client = factory.CreateClient();
        var common = new Account("Commun", "100", "Banque", AccountKind.Current);
        var personal = new Account("Personnel", "200", "Banque", AccountKind.Current);
        var income = new AnnualBudgetPost("Alimentation du compte", AnnualBudgetPostType.Income);
        var groceries = new AnnualBudgetPost("Courses", AnnualBudgetPostType.Expense);
        var overrun = new AnnualBudgetPost("Loisirs", AnnualBudgetPostType.Expense);
        income.MonthlyAmounts.Add(new BudgetMonthlyAmount(income.Id, 2026, 11, 1000m));
        groceries.MonthlyAmounts.Add(new BudgetMonthlyAmount(groceries.Id, 2026, 11, 1000m));
        overrun.MonthlyAmounts.Add(new BudgetMonthlyAmount(overrun.Id, 2026, 11, 50m));
        var expense = new Transaction(new DateOnly(2026, 11, 2), "Courses", 800m, TransactionType.Expense, accountId: common.Id);
        expense.SetBudgetAssignment(groceries.Id, null);
        var outgoing = new Transaction(new DateOnly(2026, 11, 10), "Virement", 400m, TransactionType.Expense, accountId: personal.Id);
        var incoming = new Transaction(new DateOnly(2026, 11, 10), "Virement", 400m, TransactionType.Income, accountId: common.Id);
        incoming.SetBudgetAssignment(income.Id, null);
        var extra = new Transaction(new DateOnly(2026, 11, 12), "Loisirs", 70m, TransactionType.Expense, accountId: common.Id);
        extra.SetBudgetAssignment(overrun.Id, null);
        var future = new Transaction(new DateOnly(2026, 11, 20), "Courses futures", 500m, TransactionType.Expense, accountId: common.Id);
        future.SetBudgetAssignment(groceries.Id, null);
        var unrelated = new Transaction(new DateOnly(2026, 11, 5), "Personnel", 300m, TransactionType.Expense, accountId: personal.Id);
        unrelated.SetBudgetAssignment(groceries.Id, null);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KontaDbContext>();
            db.Accounts.AddRange(common, personal);
            db.AnnualBudgetPosts.AddRange(income, groceries, overrun);
            db.Transactions.AddRange(expense, outgoing, incoming, extra, future, unrelated);
            db.AccountTransfers.Add(new AccountTransfer(outgoing.Id, incoming.Id, 400m));
            db.AccountBalanceReferences.Add(new AccountBalanceReference(common.Id, incoming.Id, 1000m));
            await db.SaveChangesAsync();
        }

        using var designated = await client.PutAsync($"/api/accounts/{common.Id}/common", null);
        designated.EnsureSuccessStatusCode();
        using var response = await client.GetAsync("/api/projection");
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal(930m, root.GetProperty("currentBalance").GetDecimal());
        Assert.Equal(1330m, root.GetProperty("projectedBalance").GetDecimal());
        Assert.Equal(15, root.GetProperty("history").GetArrayLength());
        Assert.Equal(1400m, root.GetProperty("history")[0].GetProperty("balance").GetDecimal());
        Assert.Equal(600m, root.GetProperty("history")[1].GetProperty("balance").GetDecimal());
        var bands = root.GetProperty("bands").EnumerateArray().ToArray();
        Assert.Equal(2, bands.Length);
        Assert.Equal(600m, bands.Single(band => band.GetProperty("name").GetString() == income.Name).GetProperty("amount").GetDecimal());
        Assert.Equal(200m, bands.Single(band => band.GetProperty("name").GetString() == groceries.Name).GetProperty("amount").GetDecimal());

        using var monthly = JsonDocument.Parse(await client.GetStringAsync("/api/annual-budgets/2026/months/11"));
        Assert.Equal(400m, monthly.RootElement.GetProperty("actualIncomeTotal").GetDecimal());
        Assert.Equal(1370m, monthly.RootElement.GetProperty("actualExpenseTotal").GetDecimal());
    }

    [Fact]
    public async Task SetCommon_ReplacesPreviousSelection_AndProjectionRequiresReference()
    {
        using var directory = new TemporaryDirectory();
        using var factory = CreateFactory(directory);
        using var client = factory.CreateClient();
        var first = new Account("Commun", "100", "Banque", AccountKind.Current);
        var second = new Account("Autre", "200", "Banque", AccountKind.Current);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KontaDbContext>();
            db.Accounts.AddRange(first, second);
            var unmapped = new AnnualBudgetPost("Non rapproché", AnnualBudgetPostType.Expense);
            unmapped.MonthlyAmounts.Add(new BudgetMonthlyAmount(unmapped.Id, 2026, 11, 100m));
            db.AnnualBudgetPosts.AddRange(unmapped, new AnnualBudgetPost("Sans prévision", AnnualBudgetPostType.Income));
            await db.SaveChangesAsync();
        }

        using var firstResponse = await client.PutAsync($"/api/accounts/{first.Id}/common", null);
        using var secondResponse = await client.PutAsync($"/api/accounts/{second.Id}/common", null);
        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();
        using var accounts = JsonDocument.Parse(await client.GetStringAsync("/api/accounts"));
        Assert.Equal(second.Id, Assert.Single(accounts.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("isCommon").GetBoolean())).GetProperty("id").GetGuid());
        using var projection = JsonDocument.Parse(await client.GetStringAsync("/api/projection"));
        Assert.Equal(JsonValueKind.Null, projection.RootElement.GetProperty("currentBalance").ValueKind);
        Assert.Equal(JsonValueKind.Null, projection.RootElement.GetProperty("projectedBalance").ValueKind);
        Assert.Equal(1, projection.RootElement.GetProperty("unmappedPostCount").GetInt32());
        Assert.Equal(1, projection.RootElement.GetProperty("unplannedPostCount").GetInt32());
    }

    [Fact]
    public async Task Projection_RewindsReference_WhenReferenceIsAfterToday()
    {
        using var directory = new TemporaryDirectory();
        using var factory = CreateFactory(directory);
        using var client = factory.CreateClient();
        var account = new Account("Commun", "100", "Banque", AccountKind.Current);
        var operation = new Transaction(new DateOnly(2026, 11, 20), "Paiement futur", 100m, TransactionType.Expense, accountId: account.Id);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KontaDbContext>();
            db.Accounts.Add(account);
            db.Transactions.Add(operation);
            db.AccountBalanceReferences.Add(new AccountBalanceReference(account.Id, operation.Id, 900m));
            await db.SaveChangesAsync();
        }
        using var designated = await client.PutAsync($"/api/accounts/{account.Id}/common", null);
        designated.EnsureSuccessStatusCode();

        using var projection = JsonDocument.Parse(await client.GetStringAsync("/api/projection"));
        using var balance = JsonDocument.Parse(await client.GetStringAsync($"/api/accounts/{account.Id}/balances"));

        Assert.Equal(1000m, projection.RootElement.GetProperty("currentBalance").GetDecimal());
        Assert.Equal(1000m, balance.RootElement.GetProperty("actualBalance").GetDecimal());
    }

    [Fact]
    public async Task SetBudgetAssignment_AllowsCommonAccountTransfer_ToIncomePost()
    {
        using var directory = new TemporaryDirectory();
        using var factory = CreateFactory(directory);
        using var client = factory.CreateClient();
        var common = new Account("Commun", "100", "Banque", AccountKind.Current);
        var personal = new Account("Personnel", "200", "Banque", AccountKind.Current);
        var category = new Category("Virements");
        category.EnableMultiPost();
        var post = new AnnualBudgetPost("Apport", AnnualBudgetPostType.Income);
        var outgoing = new Transaction(new DateOnly(2026, 11, 10), "Virement", 100m, TransactionType.Expense, accountId: personal.Id);
        var incoming = new Transaction(new DateOnly(2026, 11, 10), "Virement", 100m, TransactionType.Income, categoryId: category.Id, accountId: common.Id);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KontaDbContext>();
            db.Accounts.AddRange(common, personal);
            db.Categories.Add(category);
            db.AnnualBudgetPosts.Add(post);
            db.Transactions.AddRange(outgoing, incoming);
            db.AccountTransfers.Add(new AccountTransfer(outgoing.Id, incoming.Id, 100m));
            await db.SaveChangesAsync();
        }
        using var designated = await client.PutAsync($"/api/accounts/{common.Id}/common", null);
        designated.EnsureSuccessStatusCode();

        using var response = await client.PutAsJsonAsync($"/api/transactions/{incoming.Id}/budget-assignment",
            new { budgetPostId = post.Id });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var month = JsonDocument.Parse(await client.GetStringAsync("/api/annual-budgets/2026/months/11"));
        Assert.Equal(100m, Assert.Single(month.RootElement.GetProperty("posts").EnumerateArray())
            .GetProperty("actualAmount").GetDecimal());
    }

    private static WebApplicationFactory<Program> CreateFactory(TemporaryDirectory directory) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            var path = Path.Combine(directory.Path, "test.db");
            builder.UseSetting("Konta:DatabasePath", path);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Konta:DatabasePath"] = path }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider());
            });
        });

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("konta-projection-test-");
        public string Path => directory.FullName;
        public void Dispose() => directory.Delete(recursive: true);
    }
}
