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
    ILabelTemplateService labelTemplateService) : Controller
{
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
        ViewData.SetPageTitle("Entegrasyon Ayarlari");
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
        ViewData.SetPageTitle("Bildirim Ayarlari");
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
        ViewData.SetPageTitle("Yazici Ayarlari");
        ViewData.SetActiveNav("settings");

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

    [HttpGet("/settings/desktop")]
    public IActionResult Desktop()
    {
        ViewData.SetPageTitle("Masaustu Uygulama");
        ViewData.SetActiveNav("settings");
        return View();
    }

    [HttpGet("/settings/tax")]
    public IActionResult Tax()
    {
        ViewData.SetPageTitle("Vergi Ayarlari");
        ViewData.SetActiveNav("settings-tax");
        return View();
    }

    [HttpGet("/settings/shipping")]
    public async Task<IActionResult> ShippingSettings()
    {
        ViewData.SetPageTitle("Kargo Ayarlari");
        ViewData.SetActiveNav("settings-shipping");
        var settings = await settingManager.GetSettingsByGroupAsync("Shipping");
        return View(settings);
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
    public IActionResult Webhooks()
    {
        ViewData.SetPageTitle("Webhook Yonetimi");
        ViewData.SetActiveNav("settings-webhooks");
        return View();
    }

    [HttpGet("/settings/api-keys")]
    public IActionResult ApiKeys()
    {
        ViewData.SetPageTitle("API Anahtar Yonetimi");
        ViewData.SetActiveNav("settings-api-keys");
        return View();
    }
}
