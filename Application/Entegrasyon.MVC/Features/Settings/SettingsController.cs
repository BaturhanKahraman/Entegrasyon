using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.Entity.Labels;
using Entegrasyon.MVC.Features.Settings.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Settings;

[Authorize]
public class SettingsController(
    IApplicationSettingManager settingManager,
    ILabelTemplateService labelTemplateService,
    IVatRateManager vatRateManager,
    ICargoCompaniesManager cargoCompaniesManager,
    IApiKeyManager apiKeyManager,
    IWebhookManager webhookManager,
    IPaymentMethodManager paymentMethodManager,
    ITenantContext tenantContext) : Controller
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    [HttpGet("/settings")]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(General));
    }

    [HttpGet("/settings/general")]
    public async Task<IActionResult> General()
    {
        ViewData.SetPageTitle("Genel Ayarlar");
        ViewData.SetActiveNav("settings");

        var settings = await settingManager.GetSettingsByGroupAsync("General");
        return View(settings);
    }

    [HttpPost("/settings/general")]
    public async Task<IActionResult> General([FromForm] Dictionary<int, string> settings)
    {
        var updates = settings.Select(s => new UpdateApplicationSettingDto
        {
            Id = s.Key,
            Value = s.Value
        }).ToList();

        var success = await settingManager.UpdateSettingsAsync(updates);

        if (success)
            TempData.SetSuccess("Genel ayarlar basariyla guncellendi.");
        else
            TempData.SetError("Ayarlar guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(General));
    }

    [HttpGet("/settings/integrations")]
    public async Task<IActionResult> Integrations()
    {
        ViewData.SetPageTitle("Entegrasyon Ayarları");
        ViewData.SetActiveNav("settings-integrations");

        var marketplaces = await settingManager.GetMarketplacesAsync();
        return View(marketplaces);
    }

    [HttpPost("/settings/integrations/{id:int}")]
    public async Task<IActionResult> UpdateMarketplace(
        int id,
        [FromForm] string? apiKey,
        [FromForm] string? apiSecret,
        [FromForm] string? sellerId,
        [FromForm] string? baseUrl,
        [FromForm] string? tokenUrl,
        [FromForm] string? refreshToken)
    {
        var success = await settingManager.UpdateMarketplaceAsync(id, apiKey, apiSecret, sellerId, baseUrl, tokenUrl, refreshToken);

        if (success)
            TempData.SetSuccess("Pazaryeri ayarlari basariyla guncellendi.");
        else
            TempData.SetError("Ayarlar guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(Integrations));
    }

    [HttpGet("/settings/notifications")]
    public async Task<IActionResult> Notifications()
    {
        ViewData.SetPageTitle("Bildirim Ayarları");
        ViewData.SetActiveNav("settings-notifications");

        var settings = await settingManager.GetSettingsByGroupAsync("Notification");
        return View(settings);
    }

    [HttpPost("/settings/notifications")]
    public async Task<IActionResult> Notifications([FromForm] Dictionary<int, string> settings)
    {
        var updates = settings.Select(s => new UpdateApplicationSettingDto
        {
            Id = s.Key,
            Value = s.Value
        }).ToList();

        var success = await settingManager.UpdateSettingsAsync(updates);

        if (success)
            TempData.SetSuccess("Bildirim ayarlari basariyla guncellendi.");
        else
            TempData.SetError("Ayarlar guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(Notifications));
    }

    [HttpGet("/settings/printing")]
    public async Task<IActionResult> Printing()
    {
        ViewData.SetPageTitle("Yazıcı Ayarları");
        ViewData.SetActiveNav("settings-printing");

        var settings = await settingManager.GetSettingsByGroupAsync("Printer");
        var templatesResult = await labelTemplateService.GetAllAsync();

        var vm = new PrintingSettingsVm
        {
            Settings = settings,
            Templates = templatesResult.Success ? templatesResult.Data ?? [] : []
        };

        return View(vm);
    }

    [HttpPost("/settings/printing/save-printer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePrinterSettings([FromForm] Dictionary<int, string> settings)
    {
        var updates = settings.Select(s => new UpdateApplicationSettingDto
        {
            Id = s.Key,
            Value = s.Value
        }).ToList();

        var success = await settingManager.UpdateSettingsAsync(updates);

        if (success)
            TempData.SetSuccess("Yazici ayarlari basariyla guncellendi.");
        else
            TempData.SetError("Ayarlar guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(Printing));
    }

    [HttpPost("/settings/printing/save-template")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate(
        [FromForm] Guid? templateId,
        [FromForm] string name,
        [FromForm] int type,
        [FromForm] int widthMm,
        [FromForm] int heightMm,
        [FromForm] int dpi,
        [FromForm] bool isDefault)
    {
        var dto = new SaveLabelTemplateDto(
            Id: templateId,
            Name: name,
            Type: (LabelType)type,
            WidthMm: widthMm,
            HeightMm: heightMm,
            Dpi: dpi,
            Elements: [],
            IsDefault: isDefault);

        var result = await labelTemplateService.SaveAsync(dto);

        if (result.Success)
            TempData.SetSuccess("Etiket sablonu basariyla kaydedildi.");
        else
            TempData.SetError(result.Message ?? "Sablon kaydedilemedi.");

        return RedirectToAction(nameof(Printing));
    }

    [HttpPost("/settings/printing/delete-template")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate([FromForm] Guid templateId)
    {
        var result = await labelTemplateService.DeleteAsync(templateId);

        if (result.Success)
            TempData.SetSuccess("Etiket sablonu silindi.");
        else
            TempData.SetError(result.Message ?? "Sablon silinemedi.");

        return RedirectToAction(nameof(Printing));
    }

    [HttpPost("/settings/printing/set-default")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultTemplate([FromForm] Guid templateId)
    {
        var result = await labelTemplateService.SetAsDefaultAsync(templateId);

        if (result.Success)
            TempData.SetSuccess("Varsayilan sablon ayarlandi.");
        else
            TempData.SetError(result.Message ?? "Varsayilan sablon ayarlanamadi.");

        return RedirectToAction(nameof(Printing));
    }

    [HttpGet("/settings/tax")]
    public async Task<IActionResult> Tax()
    {
        ViewData.SetPageTitle("Vergi Ayarları");
        ViewData.SetActiveNav("settings-tax");
        var rates = await vatRateManager.GetAllAsync();
        return View(rates);
    }

    [HttpPost("/settings/tax/create")]
    public async Task<IActionResult> CreateVatRate([FromForm] string name, [FromForm] decimal rate, [FromForm] string? description)
    {
        var result = await vatRateManager.CreateAsync(name, rate, description);
        if (result.Success)
            TempData.SetSuccess($"KDV orani olusturuldu: {name} (%{rate})");
        else
            TempData.SetError(result.Message ?? "KDV orani olusturulamadi.");
        return RedirectToAction(nameof(Tax));
    }

    [HttpPost("/settings/tax/{id:int}/update")]
    public async Task<IActionResult> UpdateVatRate(int id, [FromForm] string name, [FromForm] decimal rate, [FromForm] string? description)
    {
        var result = await vatRateManager.UpdateAsync(id, name, rate, description);
        if (result.Success)
            TempData.SetSuccess("KDV orani guncellendi.");
        else
            TempData.SetError(result.Message ?? "KDV orani guncellenemedi.");
        return RedirectToAction(nameof(Tax));
    }

    [HttpPost("/settings/tax/{id:int}/delete")]
    public async Task<IActionResult> DeleteVatRate(int id)
    {
        var result = await vatRateManager.DeleteAsync(id);
        if (result.Success)
            TempData.SetSuccess("KDV orani silindi.");
        else
            TempData.SetError(result.Message ?? "KDV orani silinemedi.");
        return RedirectToAction(nameof(Tax));
    }

    [HttpPost("/settings/tax/{id:int}/set-default")]
    public async Task<IActionResult> SetDefaultVatRate(int id)
    {
        var result = await vatRateManager.SetDefaultAsync(id);
        if (result.Success)
            TempData.SetSuccess(result.Message!);
        else
            TempData.SetError(result.Message ?? "Varsayilan oran ayarlanamadi.");
        return RedirectToAction(nameof(Tax));
    }

    [HttpGet("/settings/shipping")]
    public async Task<IActionResult> ShippingSettings()
    {
        ViewData.SetPageTitle("Kargo Ayarları");
        ViewData.SetActiveNav("settings-shipping");
        var settings = await settingManager.GetSettingsByGroupAsync("Shipping");
        var companies = await cargoCompaniesManager.GetCargoCompanies();
        ViewBag.CargoCompanies = companies.Data ?? [];
        return View(settings);
    }

    [HttpPost("/settings/shipping/default-company")]
    public async Task<IActionResult> SetDefaultCargoCompany(
        [FromForm] int companyId, [FromForm] string? customerCode, [FromForm] string? apiKey, [FromForm] string? secretKey)
    {
        var result = await cargoCompaniesManager.SetDefaultCargoCompany(companyId, customerCode, apiKey, secretKey);
        if (result.Success)
            TempData.SetSuccess("Varsayilan kargo firmasi ayarlandi.");
        else
            TempData.SetError(result.Message ?? "Kargo firmasi ayarlanamadi.");
        return RedirectToAction(nameof(ShippingSettings));
    }

    [HttpPost("/settings/shipping/free-shipping")]
    public async Task<IActionResult> SaveFreeShipping([FromForm] decimal threshold, [FromForm] decimal fee)
    {
        var updates = new List<UpdateApplicationSettingDto>();

        var thresholdSetting = await settingManager.GetSettingAsync("FreeShippingThreshold");
        var feeSetting = await settingManager.GetSettingAsync("DefaultShippingFee");

        if (thresholdSetting is not null)
            updates.Add(new UpdateApplicationSettingDto { Id = thresholdSetting.Id, Value = threshold.ToString("F2") });
        if (feeSetting is not null)
            updates.Add(new UpdateApplicationSettingDto { Id = feeSetting.Id, Value = fee.ToString("F2") });

        if (updates.Count > 0)
            await settingManager.UpdateSettingsAsync(updates);

        TempData.SetSuccess("Ucretsiz kargo ayarlari kaydedildi.");
        return RedirectToAction(nameof(ShippingSettings));
    }

    [HttpPost("/settings/shipping")]
    public async Task<IActionResult> ShippingSettings([FromForm] Dictionary<int, string> settings)
    {
        var updates = settings.Select(s => new UpdateApplicationSettingDto
        {
            Id = s.Key,
            Value = s.Value
        }).ToList();

        var success = await settingManager.UpdateSettingsAsync(updates);

        if (success)
            TempData.SetSuccess("Kargo ayarlari basariyla guncellendi.");
        else
            TempData.SetError("Ayarlar guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(ShippingSettings));
    }

    [HttpGet("/settings/webhooks")]
    public async Task<IActionResult> Webhooks()
    {
        ViewData.SetPageTitle("Webhook Yönetimi");
        ViewData.SetActiveNav("settings-webhooks");
        var webhooks = await webhookManager.GetAllAsync(TenantId);
        return View(webhooks);
    }

    [HttpPost("/settings/webhooks/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWebhook([FromForm] string url, [FromForm] string? secret, [FromForm] string[] eventTypes)
    {
        var result = await webhookManager.CreateAsync(TenantId, url, secret, eventTypes);
        if (result.Success)
            TempData.SetSuccess("Webhook olusturuldu.");
        else
            TempData.SetError(result.Message ?? "Webhook olusturulamadi.");
        return RedirectToAction(nameof(Webhooks));
    }

    [HttpPost("/settings/webhooks/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWebhook(int id)
    {
        var result = await webhookManager.ToggleAsync(id);
        if (result.Success)
            TempData.SetSuccess(result.Message!);
        else
            TempData.SetError(result.Message ?? "Webhook guncellenemedi.");
        return RedirectToAction(nameof(Webhooks));
    }

    [HttpPost("/settings/webhooks/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWebhook(int id)
    {
        var result = await webhookManager.DeleteAsync(id);
        if (result.Success)
            TempData.SetSuccess("Webhook silindi.");
        else
            TempData.SetError(result.Message ?? "Webhook silinemedi.");
        return RedirectToAction(nameof(Webhooks));
    }

    [HttpGet("/settings/webhooks/{id:int}/logs")]
    public async Task<IActionResult> WebhookLogs(int id)
    {
        var logs = await webhookManager.GetDeliveryLogsAsync(id);
        return PartialView("Partials/_WebhookLogs", logs);
    }

    [HttpGet("/settings/api-keys")]
    public async Task<IActionResult> ApiKeys()
    {
        ViewData.SetPageTitle("API Anahtar Yönetimi");
        ViewData.SetActiveNav("settings-api-keys");
        var keys = await apiKeyManager.GetAllAsync(TenantId);
        return View(keys);
    }

    [HttpPost("/settings/api-keys/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateApiKey([FromForm] string name, [FromForm] string[] scopes, [FromForm] string? expiresAt)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            TempData.SetError("Kullanici kimlik bilgisi alinamadi.");
            return RedirectToAction(nameof(ApiKeys));
        }

        DateTimeOffset? expiry = null;
        if (DateTimeOffset.TryParse(expiresAt, out var parsed))
            expiry = parsed;

        var result = await apiKeyManager.CreateAsync(TenantId, name, scopes, expiry, userId);

        if (result.Success)
        {
            TempData.SetSuccess(result.Message!);
            TempData["NewApiKey"] = result.Data.PlainKey;
        }
        else
        {
            TempData.SetError(result.Message ?? "API anahtari olusturulamadi.");
        }

        return RedirectToAction(nameof(ApiKeys));
    }

    [HttpPost("/settings/api-keys/{id:int}/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeApiKey(int id)
    {
        var result = await apiKeyManager.RevokeAsync(id);
        if (result.Success)
            TempData.SetSuccess("API anahtari iptal edildi.");
        else
            TempData.SetError(result.Message ?? "API anahtari iptal edilemedi.");
        return RedirectToAction(nameof(ApiKeys));
    }

    [HttpGet("/settings/payment-methods")]
    public async Task<IActionResult> PaymentMethods()
    {
        ViewData.SetPageTitle("Ödeme Yöntemleri");
        ViewData.SetActiveNav("settings");

        var result = await paymentMethodManager.GetAllPaymentMethodsAsync(TenantId);
        return View(result.Data ?? []);
    }

    [HttpPost("/settings/payment-methods/{id:int}/toggle")]
    public async Task<IActionResult> TogglePaymentMethod(int id)
    {
        var result = await paymentMethodManager.TogglePaymentMethodAsync(id);

        if (Request.IsHtmx())
        {
            var methods = await paymentMethodManager.GetAllPaymentMethodsAsync(TenantId);
            return PartialView("Partials/_PaymentMethodsTable", methods.Data ?? []);
        }

        if (result.Success)
            TempData.SetSuccess(result.Message ?? "Ödeme yöntemi güncellendi.");
        else
            TempData.SetError(result.Message ?? "Ödeme yöntemi güncellenemedi.");
        return RedirectToAction(nameof(PaymentMethods));
    }

    [HttpPost("/settings/payment-methods/{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePaymentMethod(int id, [FromForm] string name, [FromForm] string icon, [FromForm] decimal? commissionRate)
    {
        var result = await paymentMethodManager.UpdatePaymentMethodAsync(id, name, icon, commissionRate);
        if (result.Success)
            TempData.SetSuccess("Ödeme yöntemi güncellendi.");
        else
            TempData.SetError(result.Message ?? "Ödeme yöntemi güncellenemedi.");
        return RedirectToAction(nameof(PaymentMethods));
    }

    [HttpPost("/settings/payment-methods/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePaymentMethod(
        [FromForm] string name,
        [FromForm] string systemCode,
        [FromForm] string icon,
        [FromForm] bool requiresAuthCode,
        [FromForm] bool requiresCashInput)
    {
        var result = await paymentMethodManager.CreatePaymentMethodAsync(
            name, systemCode, icon, requiresAuthCode, requiresCashInput, TenantId);
        if (result.Success)
            TempData.SetSuccess("Ödeme yöntemi oluşturuldu.");
        else
            TempData.SetError(result.Message ?? "Ödeme yöntemi oluşturulamadı.");
        return RedirectToAction(nameof(PaymentMethods));
    }

    [HttpPost("/settings/payment-methods/reorder")]
    public async Task<IActionResult> ReorderPaymentMethods([FromBody] List<int> orderedIds)
    {
        var result = await paymentMethodManager.ReorderPaymentMethodsAsync(orderedIds);
        return result.Success ? Ok() : BadRequest(result.Message);
    }
}
