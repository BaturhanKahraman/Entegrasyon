using System.Security.Claims;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.BranchOffices.Approvals;

/// <summary>
/// Şube ofisi silme talep iş akışı controller'ı.
/// Endpoint yetki dağıtımı:
///   POST /branch-offices/{id}/request-delete → BranchOffices.Delete (herkese açık silme talebi oluşturma)
///   GET  /branch-office-deletion-requests → BranchOffices.View (liste/detay)
///   POST /branch-office-deletion-requests/{id}/approve → StockOffice.DeleteApprove
///   POST /branch-office-deletion-requests/{id}/reject → StockOffice.DeleteApprove
/// </summary>
[Authorize]
public class BranchOfficeDeletionRequestController(
    IBranchOfficeDeletionRequestManager manager) : Controller
{
    [HttpGet("/branch-office-deletion-requests")]
    [Authorize(Policy = AppPermissions.BranchOffices.View)]
    public async Task<IActionResult> Index(BranchOfficeDeletionRequestStatus? status = null, int page = 0)
    {
        ViewData.SetPageTitle("Depo Silme Talepleri");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Depolar", "/branch-offices"), ("Silme Talepleri", null));

        var result = await manager.GetPagedAsync(page, 50, status);
        ViewBag.Status = status;
        ViewBag.PendingCount = await manager.GetPendingCountAsync();

        return View("~/Features/BranchOffices/Approvals/Views/Index.cshtml", result.Data);
    }

    [HttpGet("/branch-office-deletion-requests/{id:int}")]
    [Authorize(Policy = AppPermissions.BranchOffices.View)]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await manager.GetByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Talep bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle($"Silme Talebi #{id}");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(
            ("Depolar", "/branch-offices"),
            ("Silme Talepleri", "/branch-office-deletion-requests"),
            ($"#{id}", null));

        return View("~/Features/BranchOffices/Approvals/Views/Detail.cshtml", result.Data);
    }

    [HttpPost("/branch-offices/{id:int}/request-delete")]
    [Authorize(Policy = AppPermissions.BranchOffices.Delete)]
    public async Task<IActionResult> RequestDelete(int id, int? targetBranchOfficeId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            TempData.SetError("Oturum bilgisi okunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var result = await manager.RequestDeleteAsync(id, userId.Value, targetBranchOfficeId);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Silme talebi oluşturulamadı.");
            return RedirectToAction("Detail", "BranchOffice", new { id });
        }

        TempData.SetSuccess("Silme talebi başarıyla açıldı.");
        return RedirectToAction(nameof(Detail), new { id = result.Data });
    }

    [HttpPost("/branch-office-deletion-requests/{id:int}/approve")]
    [Authorize(Policy = AppPermissions.StockOffice.DeleteApprove)]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            TempData.SetError("Oturum bilgisi okunamadı.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await manager.ApproveAsync(id, userId.Value);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Talep onaylanamadı.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        TempData.SetSuccess(result.Message ?? "Talep onaylandı ve depo silindi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/branch-office-deletion-requests/{id:int}/reject")]
    [Authorize(Policy = AppPermissions.StockOffice.DeleteApprove)]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            TempData.SetError("Oturum bilgisi okunamadı.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await manager.RejectAsync(id, userId.Value, reason);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Talep reddedilemedi.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        TempData.SetSuccess("Silme talebi reddedildi.");
        return RedirectToAction(nameof(Index));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
