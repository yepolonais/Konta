namespace Konta.Application.Imports;

public interface IImportSettingsRepository
{
    Task<ImportSettings?> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(ImportSettings settings, CancellationToken cancellationToken);
}
