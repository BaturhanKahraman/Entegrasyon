using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Settings;

[Authorize]
public class SettingsController(IApplicationSettingManager settingManager) : Controller
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
        ViewData.SetActiveNav("settings");

        var settings = await settingManager.GetSettingsByGroupAsync("Integration");
        return View(settings);
    }

    [HttpPost("/settings/integrations")]
    public async Task<IActionResult> Integrations([FromForm] Dictionary<int, string> settings)
    {
        var updates = settings.Select(s => new UpdateApplicationSettingDto
        {
            Id = s.Key,
            Value = s.Value
        }).ToList();

        var success = await settingManager.UpdateSettingsAsync(updates);

        if (success)
            TempData.SetSuccess("Entegrasyon ayarlari basariyla guncellendi.");
        else
            TempData.SetError("Ayarlar guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(Integrations));
    }

    [HttpGet("/settings/notifications")]
    public async Task<IActionResult> Notifications()
    {
        ViewData.SetPageTitle("Bildirim Ayarlari");
        ViewData.SetActiveNav("settings");

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
    public IActionResult Printing()
    {
        ViewData.SetPageTitle("Yazici Ayarlari");
        ViewData.SetActiveNav("settings");
        return View();
    }

    [HttpGet("/settings/desktop")]
    public IActionResult Desktop()
    {
        ViewData.SetPageTitle("Masaustu Uygulama");
        ViewData.SetActiveNav("settings");
        return View();
    }
}
