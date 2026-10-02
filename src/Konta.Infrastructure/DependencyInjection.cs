using Konta.Application.Imports;
using Konta.Application.Accounts;
using Konta.Application.Transactions;
using Konta.Infrastructure.Imports;
using Konta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Konta.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKontaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databasePath = SqliteDatabasePath.Get(configuration);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        services.AddDbContext<KontaDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IImportCommitRepository, ImportCommitRepository>();
        services.AddSingleton<ITransactionCsvParser, SocieteGeneraleCsvParser>();
        return services;
    }
}