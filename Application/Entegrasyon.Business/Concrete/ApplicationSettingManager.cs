using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Settings;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class ApplicationSettingManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IApplicationSettingManager
{
    public async Task<List<ApplicationSettingDto>> GetAllSettingsAsync()
    {
        using var dbContext = contextFactory.CreateDbContext();
        var settings = await dbContext.ApplicationSettings
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Id)
            .ToListAsync();
        return settings.Adapt<List<ApplicationSettingDto>>();
    }

    public async Task<List<ApplicationSettingDto>> GetSettingsByGroupAsync(string group)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var settings = await dbContext.ApplicationSettings
            .Where(s => s.Group == group)
            .OrderBy(s => s.Id)
            .ToListAsync();
        return settings.Adapt<List<ApplicationSettingDto>>();
    }

    public async Task<ApplicationSettingDto?> GetSettingAsync(string key)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var setting = await dbContext.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Adapt<ApplicationSettingDto>();
    }

    public async Task<bool> UpdateSettingsAsync(List<UpdateApplicationSettingDto> settings)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var ids = settings.Select(s => s.Id).ToList();
        var entities = await dbContext.ApplicationSettings
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        foreach (var entity in entities)
        {
            var update = settings.First(s => s.Id == entity.Id);
            entity.Value = update.Value;
        }

        dbContext.ApplicationSettings.UpdateRange(entities);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
