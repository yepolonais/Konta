namespace Konta.Application.Imports;

public sealed class ImportSettingsService(IImportSettingsRepository repository) : IImportSettingsService
{
    private const string DefaultBankUrl =
        "https://particuliers.sg.fr/icd/cbo/index-react-authsec.html#/export-operations";

    public async Task<ImportSettings> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new ImportSettings("", DefaultBankUrl);
        await repository.SaveAsync(settings, cancellationToken);
        return settings;
    }

    public async Task<ImportSettings> SaveAsync(
        string bankIdentifier,
        string bankUrl,
        CancellationToken cancellationToken)
    {
        var normalizedIdentifier = bankIdentifier.Trim();
        if (normalizedIdentifier.Length is 0 or > 100)
        {
            throw new ArgumentException("Saisissez un identifiant de 1 à 100 caractères.", nameof(bankIdentifier));
        }

        if (bankUrl.Length > 2048
            || !Uri.TryCreate(bankUrl.Trim(), UriKind.Absolute, out var destination)
            || (destination.Scheme != Uri.UriSchemeHttp && destination.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(destination.UserInfo))
        {
            throw new ArgumentException(
                "Saisissez une adresse Web http:// ou https:// valide, sans identifiant intégré.",
                nameof(bankUrl));
        }

        var settings = new ImportSettings(normalizedIdentifier, destination!.AbsoluteUri);
        await repository.SaveAsync(settings, cancellationToken);
        return settings;
    }
}
