using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class VatRateManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<VatRateManager> logger) : IVatRateManager
{
    public async Task<List<VatRate>> GetAllAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.VatRates
            .AsNoTracking()
            .OrderBy(v => v.Rate)
            .ToListAsync();
    }

    public async Task<IDataResult<VatRate>> CreateAsync(string name, decimal rate, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ErrorDataResult<VatRate>(null!, "KDV orani adi bos olamaz.");
        if (rate < 0 || rate > 100)
            return new ErrorDataResult<VatRate>(null!, "KDV orani 0-100 arasinda olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        if (await dbContext.VatRates.AnyAsync(v => v.Rate == rate))
            return new ErrorDataResult<VatRate>(null!, $"%{rate} oraninda bir KDV zaten mevcut.");

        var vatRate = new VatRate
        {
            Name = name,
            Rate = rate,
            Description = description,
            IsActive = true,
            IsDefault = false
        };

        dbContext.VatRates.Add(vatRate);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"KDV orani olusturuldu: {name} (%{rate})", LogType.Settings, LogAction.Add);
        logger.LogInformation("VatRate created: {Id} {Name} {Rate}%", vatRate.Id, name, rate);

        return new SuccessDataResult<VatRate>(vatRate, "KDV orani olusturuldu.");
    }

    public async Task<IResult> UpdateAsync(int id, string name, decimal rate, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ErrorResult("KDV orani adi bos olamaz.");
        if (rate < 0 || rate > 100)
            return new ErrorResult("KDV orani 0-100 arasinda olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var vatRate = await dbContext.VatRates.FindAsync(id);
        if (vatRate is null)
            return new ErrorResult("KDV orani bulunamadı.");

        if (await dbContext.VatRates.AnyAsync(v => v.Rate == rate && v.Id != id))
            return new ErrorResult($"%{rate} oraninda baska bir KDV zaten mevcut.");

        vatRate.Name = name;
        vatRate.Rate = rate;
        vatRate.Description = description;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"KDV orani guncellendi: {name} (%{rate})", LogType.Settings, LogAction.Update);
        return new SuccessResult("KDV orani guncellendi.");
    }

    public async Task<IResult> DeleteAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var vatRate = await dbContext.VatRates.FindAsync(id);
        if (vatRate is null)
            return new ErrorResult("KDV orani bulunamadı.");

        if (vatRate.IsDefault)
            return new ErrorResult("Varsayilan KDV orani silinemez. Once baska bir orani varsayilan yapin.");

        // Kullanımda mı kontrol et
        var rateValue = vatRate.Rate;
        var usedInVariants = await dbContext.ProductVariants
            .AnyAsync(v => v.VatRate == rateValue);

        if (usedInVariants)
            return new ErrorResult($"Bu oran urunlerde kullanilmakta. Silmeden once urun KDV oranlarini degistirin.");

        vatRate.IsDeleted = true;
        vatRate.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"KDV orani silindi: {vatRate.Name}", LogType.Settings, LogAction.Delete);
        return new SuccessResult("KDV orani silindi.");
    }

    public async Task<IResult> SetDefaultAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // Global no-tracking varsayilani altinda guncellenecek entity izlenmeli;
        // aksi halde SaveChangesAsync IsDefault degisikligini kalici hale getirmez.
        var vatRate = await dbContext.VatRates
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
        if (vatRate is null)
            return new ErrorResult("KDV orani bulunamadı.");

        // Tek-varsayilan invariant'i: hedef disindaki mevcut varsayilanlari temizle.
        await dbContext.VatRates
            .Where(v => v.IsDefault && v.Id != id)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsDefault, false));

        vatRate.IsDefault = true;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"Varsayilan KDV orani degistirildi: {vatRate.Name} (%{vatRate.Rate})", LogType.Settings, LogAction.Update);
        logger.LogInformation("VatRate default set: {Id} {Name} {Rate}%", vatRate.Id, vatRate.Name, vatRate.Rate);
        return new SuccessResult($"%{vatRate.Rate} varsayilan KDV orani olarak ayarlandi.");
    }

    public async Task<decimal> GetDefaultRateAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var defaultRate = await dbContext.VatRates
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.IsDefault);
        return defaultRate?.Rate ?? 10;
    }
}
