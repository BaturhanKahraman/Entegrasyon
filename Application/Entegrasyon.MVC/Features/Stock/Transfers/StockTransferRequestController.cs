using System.Security.Claims;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.MVC.Features.Stock.Transfers.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Stock.Transfers;

/// <summary>
/// Standalone stok transfer talep iş akışı controller'ı.
/// Endpoint yetki dağıtımı:
///   GET  /stock-transfer-requests → Stock.Transfer (liste/görme)
///   GET  /stock-transfer-requests/create → Stock.Transfer
///   POST /stock-transfer-requests/create → Stock.Transfer
///   POST /stock-transfer-requests/{id}/approve → Stock.TransferApprove (self-approval serbest)
///   POST /stock-transfer-requests/{id}/reject → Stock.TransferApprove
/// </summary>
[Authorize]
public class StockTransferRequestController(
    IStockTransferRequestManager manager,
    IBranchOfficeManager branchOfficeManager) : Controller
{
    [HttpGet("/stock-transfer-requests")]
    [Authorize(Policy = AppPermissions.Stock.Transfer)]
    public async Task<IActionResult> Index(StockTransferRequestStatus? status = null, int page = 0)
    {
        ViewData.SetPageTitle("Stok Transfer Talepleri");
        ViewData.SetActiveNav("stock-transfers");
        ViewData.SetBreadcrumb(("Stok", "/branch-offices"), ("Transfer Talepleri", null));

        var result = await manager.GetPagedAsync(page, 50, status);
        ViewBag.Status = status;
        ViewBag.PendingCount = await manager.GetPendingCountAsync();

        return View("~/Features/Stock/Transfers/Views/Index.cshtml", result.Data);
    }

    [HttpGet("/stock-transfer-requests/{id:int}")]
    [Authorize(Policy = AppPermissions.Stock.Transfer)]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await manager.GetByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Talep bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle($"Stok Transfer Talebi #{id}");
        ViewData.SetActiveNav("stock-transfers");
        ViewData.SetBreadcrumb(
            ("Stok", "/branch-offices"),
            ("Transfer Talepleri", "/stock-transfer-requests"),
            ($"#{id}", null));

        return View("~/Features/Stock/Transfers/Views/Detail.cshtml", result.Data);
    }

    [HttpGet("/stock-transfer-requests/create")]
    [Authorize(Policy = AppPermissions.Stock.Transfer)]
    public async Task<IActionResult> Create()
    {
        ViewData.SetPageTitle("Yeni Stok Transfer Talebi");
        ViewData.SetActiveNav("stock-transfers");
        ViewData.SetBreadcrumb(
            ("Stok", "/branch-offices"),
            ("Transfer Talepleri", "/stock-transfer-requests"),
            ("Yeni Talep", null));

        var branches = await branchOfficeManager.GetBranchList();
        var vm = new StockTransferRequestCreateVm
        {
            AvailableBranches = branches.Data?.Select(b => new BranchOption(b.Id, b.Name ?? "")).ToList() ?? []
        };
        return View("~/Features/Stock/Transfers/Views/Create.cshtml", vm);
    }

    [HttpPost("/stock-transfer-requests/create")]
    [Authorize(Policy = AppPermissions.Stock.Transfer)]
    public async Task<IActionResult> Create(StockTransferRequestCreateVm vm)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            TempData.SetError("Oturum bilgisi okunamadı.");
            return RedirectToAction(nameof(Index));
        }

        if (vm.Items is null || vm.Items.Count == 0)
        {
            TempData.SetError("En az bir ürün eklemelisiniz.");
            return RedirectToAction(nameof(Create));
        }

        var dtoItems = vm.Items
            .Where(i => i.Quantity > 0 && i.ProductVariantId != Guid.Empty)
            .Select(i => new TransferItemDto(i.ProductVariantId, i.Quantity))
            .ToList();

        if (dtoItems.Count == 0)
        {
            TempData.SetError("Geçerli ürün satırı bulunamadı.");
            return RedirectToAction(nameof(Create));
        }

        var result = await manager.CreateAsync(
            vm.SourceBranchOfficeId,
            vm.TargetBranchOfficeId,
            dtoItems,
            userId.Value);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Talep oluşturulamadı.");
            return RedirectToAction(nameof(Create));
        }

        TempData.SetSuccess("Stok transfer talebi başarıyla oluşturuldu.");
        return RedirectToAction(nameof(Detail), new { id = result.Data });
    }

    [HttpPost("/stock-transfer-requests/{id:int}/approve")]
    [Authorize(Policy = AppPermissions.Stock.TransferApprove)]
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

        TempData.SetSuccess(result.Message ?? "Stok transferi gerçekleştirildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/stock-transfer-requests/{id:int}/reject")]
    [Authorize(Policy = AppPermissions.Stock.TransferApprove)]
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

        TempData.SetSuccess("Talep reddedildi.");
        return RedirectToAction(nameof(Index));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
