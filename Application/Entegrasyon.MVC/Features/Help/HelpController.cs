using System.Security.Claims;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Help;
using Entegrasyon.MVC.Features.Help.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Help;

[Authorize]
public class HelpController(IHelpRequestManager helpRequestManager) : Controller
{
    // ── Kullanıcı: yardım talebi formu ───────────────────────────────────

    [HttpGet("/help/new")]
    public IActionResult New()
    {
        ViewData.SetPageTitle("Yardım");
        ViewData.SetActiveNav("help");
        ViewData.SetBreadcrumb(("Yardım", null));
        return View(new CreateHelpRequestVm());
    }

    [HttpPost("/help/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(CreateHelpRequestVm vm)
    {
        // AutoValidationFilter ModelState invalid'se buraya gelmeden PRG yapar.
        var dto = new CreateHelpRequestDto(vm.Subject, vm.Message, vm.Category);
        var result = await helpRequestManager.CreateAsync(dto, GetUserId());

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Yardım talebi gönderilemedi.");
            return RedirectToAction(nameof(New));
        }

        TempData.SetSuccess(result.Message ?? "Yardım talebiniz alındı.");
        return RedirectToAction(nameof(New));
    }

    // ── Admin: gelen talepler ────────────────────────────────────────────

    [HttpGet("/help")]
    [Authorize(Policy = AppPermissions.Settings.View)]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Yardım Talepleri");
        ViewData.SetActiveNav("help-admin");
        ViewData.SetBreadcrumb(("Yardım Talepleri", null));

        var requests = await helpRequestManager.GetAllAsync();
        return View(requests);
    }

    [HttpGet("/help/{id:int}")]
    [Authorize(Policy = AppPermissions.Settings.View)]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await helpRequestManager.GetDetailAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Yardım talebi bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Yardım Talebi Detayı");
        ViewData.SetActiveNav("help-admin");
        ViewData.SetBreadcrumb(("Yardım Talepleri", "/help"), ("Detay", null));
        return View(result.Data);
    }

    [HttpPost("/help/{id:int}/resolve")]
    [Authorize(Policy = AppPermissions.Settings.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id)
    {
        var result = await helpRequestManager.MarkResolvedAsync(id);
        if (!result.Success)
            TempData.SetError(result.Message ?? "Talep güncellenemedi.");
        else
            TempData.SetSuccess(result.Message ?? "Talep çözüldü olarak işaretlendi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
