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

    public async Task<IResult> CreateOrUpdateAsync(StorefrontSettings settings)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.StorefrontSettings
            .FirstOrDefaultAsync(s => s.TenantId == settings.TenantId);

        if (existing is null)
        {
            await dbContext.StorefrontSettings.AddAsync(settings);
        }
        else
        {
            existing.StoreName = settings.StoreName;
            existing.StoreSlogan = settings.StoreSlogan;
            existing.LogoUrl = settings.LogoUrl;
            existing.FaviconUrl = settings.FaviconUrl;
            existing.PrimaryColor = settings.PrimaryColor;
            existing.SecondaryColor = settings.SecondaryColor;
            existing.AccentColor = settings.AccentColor;
            existing.CustomCss = settings.CustomCss;
            existing.CompanyName = settings.CompanyName;
            existing.CompanyTaxOffice = settings.CompanyTaxOffice;
            existing.CompanyTaxNumber = settings.CompanyTaxNumber;
            existing.MersisNumber = settings.MersisNumber;
            existing.KepAddress = settings.KepAddress;
            existing.ContactPhone = settings.ContactPhone;
            existing.WhatsAppNumber = settings.WhatsAppNumber;
            existing.ContactEmail = settings.ContactEmail;
            existing.Address = settings.Address;
            existing.City = settings.City;
            existing.District = settings.District;
            existing.InstagramUrl = settings.InstagramUrl;
            existing.FacebookUrl = settings.FacebookUrl;
            existing.TwitterUrl = settings.TwitterUrl;
            existing.YouTubeUrl = settings.YouTubeUrl;
            existing.TikTokUrl = settings.TikTokUrl;
            existing.GoogleAnalyticsId = settings.GoogleAnalyticsId;
            existing.GoogleTagManagerId = settings.GoogleTagManagerId;
            existing.FacebookPixelId = settings.FacebookPixelId;
            existing.AnnouncementBarText = settings.AnnouncementBarText;
            existing.AnnouncementBarActive = settings.AnnouncementBarActive;
            existing.AnnouncementBarColor = settings.AnnouncementBarColor;
            existing.DefaultSeoTitle = settings.DefaultSeoTitle;
            existing.DefaultSeoDescription = settings.DefaultSeoDescription;
            existing.DefaultSeoKeywords = settings.DefaultSeoKeywords;
            existing.FreeShippingThreshold = settings.FreeShippingThreshold;
            existing.FlatShippingRate = settings.FlatShippingRate;
            existing.EstimatedDeliveryDays = settings.EstimatedDeliveryDays;
            existing.IsMaintenanceMode = settings.IsMaintenanceMode;
            existing.MaintenanceMessage = settings.MaintenanceMessage;
            existing.CookieConsentActive = settings.CookieConsentActive;
            existing.IsWhatsAppWidgetActive = settings.IsWhatsAppWidgetActive;
            existing.NewsletterEnabled = settings.NewsletterEnabled;

            dbContext.StorefrontSettings.Update(existing);
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Ayarlar kaydedildi.");
    }

    public async Task<IResult> UpdateLegalTextAsync(int tenantId, string fieldName, string htmlContent)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var settings = await dbContext.StorefrontSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings is null)
            return new ErrorResult("Storefront ayarlari bulunamadi.");

        var property = typeof(StorefrontSettings).GetProperty(fieldName);
        if (property is null || property.PropertyType != typeof(string))
            return new ErrorResult($"Gecersiz alan adi: {fieldName}");

        property.SetValue(settings, htmlContent);

        dbContext.StorefrontSettings.Update(settings);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Yasal metin guncellendi.");
    }
}
