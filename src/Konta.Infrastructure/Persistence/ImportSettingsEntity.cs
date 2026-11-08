namespace Konta.Infrastructure.Persistence;

internal sealed class ImportSettingsEntity
{
    public int Id { get; set; }
    public string BankIdentifier { get; set; } = "";
    public string BankUrl { get; set; } = "";
}
