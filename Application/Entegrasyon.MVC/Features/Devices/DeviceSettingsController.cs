using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Devices;

[Authorize]
public class DeviceSettingsController(
    IDeviceManager deviceManager,
    IDeviceInviteCodeManager inviteCodeManager,
    ITenantContext tenantContext) : Controller
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    [HttpGet("/settings/desktop")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Masaüstü Cihazlar");
        ViewData.SetActiveNav("settings-desktop");

        var vm = new DesktopSettingsVm
        {
            Devices = await deviceManager.GetAllAsync(TenantId),
            ActiveInviteCodes = await inviteCodeManager.GetActiveAsync(TenantId),
            NewlyIssuedInviteCode = TempData["NewInviteCode"] as string
        };

        return View("~/Features/Settings/Views/Desktop.cshtml", vm);
    }

    [HttpPost("/settings/desktop/invite-codes/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInviteCode()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            TempData.SetError("Kullanıcı kimlik bilgisi alınamadı.");
            return RedirectToAction(nameof(Index));
        }

        var result = await inviteCodeManager.IssueAsync(TenantId, userId);

        if (result.Success)
        {
            TempData.SetSuccess("Davet kodu oluşturuldu. Kodu kurulum sırasında kullanın.");
            TempData["NewInviteCode"] = result.Data.Code;
        }
        else
        {
            TempData.SetError(result.Message ?? "Davet kodu oluşturulamadı.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/settings/desktop/devices/{id:int}/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeDevice(int id)
    {
        var result = await deviceManager.RevokeAsync(id);
        if (result.Success)
            TempData.SetSuccess("Cihaz iptal edildi. Yeniden kullanmak için tekrar kaydetmeniz gerekir.");
        else
            TempData.SetError(result.Message ?? "Cihaz iptal edilemedi.");

        return RedirectToAction(nameof(Index));
    }
}
