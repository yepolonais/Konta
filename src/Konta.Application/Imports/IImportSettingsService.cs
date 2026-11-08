namespace Konta.Application.Imports;

public interface IImportSettingsService
{
    Task<ImportSettings> GetAsync(CancellationToken cancellationToken);
    Task<ImportSettings> SaveAsync(
        string bankIdentifier,
        string bankUrl,
        CancellationToken cancellationToken);
}
