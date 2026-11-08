using Konta.Application.Imports;
using Microsoft.EntityFrameworkCore;

namespace Konta.Infrastructure.Persistence;

public sealed class ImportSettingsRepository(KontaDbContext dbContext) : IImportSettingsRepository
{
    public async Task<ImportSettings?> GetAsync(CancellationToken cancellationToken)
    {
        var entity = await dbContext.ImportSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.Id == 1, cancellationToken);
        return entity is null
            ? null
            : new ImportSettings(entity.BankIdentifier, entity.BankUrl);
    }

    public async Task SaveAsync(ImportSettings settings, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ImportSettings
            .SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);
        if (entity is null)
        {
            entity = new ImportSettingsEntity { Id = 1 };
            dbContext.ImportSettings.Add(entity);
        }

        entity.BankIdentifier = settings.BankIdentifier;
        entity.BankUrl = settings.BankUrl;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
