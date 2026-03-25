using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontSettingsManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontSettingsManager
{
    public async Task<IDataResult<StorefrontSettings>> GetByTenantIdAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var settings = await dbContext.StorefrontSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings is null)
            return new ErrorDataResult<StorefrontSettings>(null!, "Storefront ayarlari bulunamadi.");

        return new SuccessDataResult<StorefrontSettings>(settings);
    }

    public async Task<IResult> ToggleMaintenanceModeAsync(int tenantId, bool enabled, string? message)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var settings = await dbContext.StorefrontSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings is null)
            return new ErrorResult("Storefront ayarlari bulunamadi.");

        settings.IsMaintenanceMode = enabled;
        settings.MaintenanceMessage = message;

        dbContext.StorefrontSettings.Update(settings);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Bakim modu guncellendi.");
    }
}
