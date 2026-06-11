using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Validation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class ReceiptTemplateManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IMinioFileStorage fileStorage,
    ILogger<ReceiptTemplateManager> logger) : IReceiptTemplateManager
{
    private const int SingletonId = 1;
    private const long MaxLogoBytes = 500_000;
    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/webp"];

    public async Task<IDataResult<ReceiptTemplateDto>> GetAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null)
            return new ErrorDataResult<ReceiptTemplateDto>(null!, "Şablon bulunamadı.");

        return new SuccessDataResult<ReceiptTemplateDto>(new ReceiptTemplateDto
        {
            ThermalJson = row.ThermalJson,
            A4Json = row.A4Json,
            LogoUrl = row.LogoUrl,
            LogoWidthPx = row.LogoWidthPx,
            StoreName = row.StoreName,
            StoreAddress = row.StoreAddress,
            StorePhone = row.StorePhone
        });
    }

    public async Task<Entegrasyon.Entity.Results.IResult> UpdateAsync(UpdateReceiptTemplateDto dto)
    {
        var tv = ReceiptTemplateJsonValidator.ValidateThermal(dto.ThermalJson);
        if (!tv.IsValid) return new ErrorResult($"Thermal JSON hatalı: {tv.Error}");

        var av = ReceiptTemplateJsonValidator.ValidateA4(dto.A4Json);
        if (!av.IsValid) return new ErrorResult($"A4 JSON hatalı: {av.Error}");

        if (dto.LogoWidthPx < 80 || dto.LogoWidthPx > 200)
            return new ErrorResult("Logo genişliği 80-200px aralığında olmalı.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.AsTracking().FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null) return new ErrorResult("Şablon bulunamadı.");

        row.ThermalJson = dto.ThermalJson;
        row.A4Json = dto.A4Json;
        row.LogoWidthPx = dto.LogoWidthPx;
        row.StoreName = dto.StoreName?.Trim() ?? "";
        row.StoreAddress = dto.StoreAddress?.Trim() ?? "";
        row.StorePhone = dto.StorePhone?.Trim() ?? "";

        await db.SaveChangesAsync();
        await applicationLogManager.AddLog("Fiş şablonu güncellendi.", LogType.Settings, LogAction.Update);
        logger.LogInformation("ReceiptTemplate updated.");
        return new SuccessResult("Şablon kaydedildi.");
    }

    public async Task<IDataResult<string>> UploadLogoAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return new ErrorDataResult<string>("", "Dosya boş.");
        if (file.Length > MaxLogoBytes)
            return new ErrorDataResult<string>("", "Dosya 500KB'dan büyük.");
        if (!AllowedContentTypes.Contains(file.ContentType))
            return new ErrorDataResult<string>("", "Sadece PNG, JPEG, WEBP kabul edilir.");

        var ext = file.ContentType switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/webp" => "webp",
            _ => "bin"
        };
        var objectName = $"receipt-logos/logo-{Guid.NewGuid():N}.{ext}";

        using var stream = file.OpenReadStream();
        var url = await fileStorage.UploadAsync(stream, objectName, file.ContentType);

        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.AsTracking().FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null) return new ErrorDataResult<string>("", "Şablon bulunamadı.");

        var oldObjectName = ExtractObjectNameFromUrl(row.LogoUrl);
        row.LogoUrl = url;
        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldObjectName))
        {
            try { await fileStorage.DeleteAsync(oldObjectName); }
            catch (Exception ex) { logger.LogWarning(ex, "Eski logo silinemedi."); }
        }

        return new SuccessDataResult<string>(url, "Logo yüklendi.");
    }

    public async Task<Entegrasyon.Entity.Results.IResult> DeleteLogoAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var row = await db.ReceiptTemplates.AsTracking().FirstOrDefaultAsync(x => x.Id == SingletonId);
        if (row is null || string.IsNullOrEmpty(row.LogoUrl))
            return new SuccessResult("Silinecek logo yok.");

        var objectName = ExtractObjectNameFromUrl(row.LogoUrl);
        row.LogoUrl = null;
        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(objectName))
        {
            try { await fileStorage.DeleteAsync(objectName); }
            catch (Exception ex) { logger.LogWarning(ex, "Logo silinirken hata."); }
        }

        return new SuccessResult("Logo silindi.");
    }

    private static string? ExtractObjectNameFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var idx = url.IndexOf("receipt-logos/", StringComparison.Ordinal);
        return idx >= 0 ? url[idx..] : null;
    }
}
