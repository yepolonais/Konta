using Microsoft.Extensions.Configuration;

namespace Konta.Infrastructure.Persistence;

public static class SqliteDatabasePath
{
    public static string Get(IConfiguration configuration)
    {
        var configuredPath = configuration["Konta:DatabasePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Konta");
        Directory.CreateDirectory(dataDirectory);
        return Path.Combine(dataDirectory, "konta.db");
    }
}