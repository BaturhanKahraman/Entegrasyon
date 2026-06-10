using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.MVC.Features.BranchOffices.ViewModels;
using Entegrasyon.MVC.Infrastructure.BranchOffices;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.BranchOffices;

[Authorize]
public class BranchOfficeController(
    IBranchOfficeManager branchOfficeManager,
    IOfficeStockManager officeStockManager,
    IActiveBranchOfficeAccessor activeBranchOfficeAccessor) : Controller
{
    /// <summary>
    /// Kullanıcının aktif deposunu (şube ofisi) değiştirir.
    /// Session'a yazar, remember=true ise User.LastSelectedBranchOfficeId + RememberLastBranchOffice'e de yazar.
    /// HTMX çağrısında "HX-Refresh: true" header'ı ile sayfa yenilenir.
    /// </summary>
    [HttpPost("/branch-offices/switch")]
    public async Task<IActionResult> Switch(int branchOfficeId, bool remember = false)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            TempData.SetError("Oturum bilgisi okunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var result = await activeBranchOfficeAccessor.SwitchAsync(userId, branchOfficeId, remember);

        if (!result.Success)
        {
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.StatusCode = 400;
                return Content(result.Message ?? "Depo değiştirilemedi.");
            }
            TempData.SetError(result.Message ?? "Depo değiştirilemedi.");
            return RedirectToAction(nameof(Index));
        }

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Refresh", "true");
            return Content(result.Message ?? "Aktif depo değiştirildi.");
        }

        TempData.SetSuccess(result.Message ?? "Aktif depo değiştirildi.");
        return RedirectToAction(nameof(Index));
    }


    [HttpGet("/branch-offices")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Depolar");
        ViewData.SetActiveNav("branch-offices");

        var result = await branchOfficeManager.GetPageBranchListAsync();

        return View(result.Data);
    }

    [HttpGet("/branch-offices/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await branchOfficeManager.GetBranchDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Depo bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle(result.Data!.Name);
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Depolar", "/branch-offices"), (result.Data.Name, null));

        var stocks = await branchOfficeManager.GetBranchStocksAsync(id);
        ViewBag.Stocks = stocks.Data;

        return View(result.Data);
    }

    [HttpGet("/branch-offices/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Depo");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Depolar", "/branch-offices"), ("Yeni Depo", null));

        return View(new BranchOfficeCreateVm());
    }

    [HttpPost("/branch-offices/create")]
    public async Task<IActionResult> Create(BranchOfficeCreateVm model)
    {
        ViewData.SetPageTitle("Yeni Depo");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Depolar", "/branch-offices"), ("Yeni Depo", null));

        if (!ModelState.IsValid)
            return View(model);

        var result = await branchOfficeManager.AddBranch(new BranchOfficeAddDto(model.Name, model.Address));

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Depo eklenemedi.");
            return View(model);
        }

        TempData.SetSuccess("Depo basariyla eklendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/branch-offices/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await branchOfficeManager.GetBranchDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Depo bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        // Address'i ayrı çek — GetBranchDetailById DTO'su Address içermiyor
        var entity = await branchOfficeManager.GetBranchById(id);

        ViewData.SetPageTitle($"{result.Data!.Name} - Duzenle");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Depolar", "/branch-offices"), (result.Data.Name, $"/branch-offices/{id}"), ("Duzenle", null));

        var vm = new BranchOfficeEditVm
        {
            Id = id,
            Name = result.Data.Name,
            Address = entity?.Address
        };
        return View(vm);
    }

    [HttpPost("/branch-offices/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, BranchOfficeEditVm model)
    {
        ViewData.SetActiveNav("branch-offices");

        if (!ModelState.IsValid)
        {
            ViewData.SetPageTitle($"{model.Name} - Duzenle");
            ViewData.SetBreadcrumb(("Depolar", "/branch-offices"), (model.Name, $"/branch-offices/{id}"), ("Duzenle", null));
            model.Id = id;
            return View(model);
        }

        var result = await branchOfficeManager.Update(new BranchOfficeEditDto(id, model.Name, model.Address));

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Depo guncellenemedi.");
            ViewData.SetPageTitle($"{model.Name} - Duzenle");
            ViewData.SetBreadcrumb(("Depolar", "/branch-offices"), (model.Name, $"/branch-offices/{id}"), ("Duzenle", null));
            model.Id = id;
            return View(model);
        }

        TempData.SetSuccess("Depo basariyla guncellendi.");
        return RedirectToAction(nameof(Detail), new { id });
    }

    // NOT: Eski tek-adımlı Delete endpoint'i Faz 7 cleanup ile kaldırıldı.
    // Yeni iki-aşamalı silme akışı için POST /branch-offices/{id}/request-delete kullanılır
    // (bkz. BranchOfficeDeletionRequestController). Manager.Delete(int) metodu ve unit testleri
    // şimdilik geri dönüş güvenliği için duruyor; yeni akış stage'de doğrulandıktan sonra silinecek.

    [HttpGet("/branch-offices/{id:int}/transfer")]
    public async Task<IActionResult> TransferDialog(int id)
    {
        var branchResult = await branchOfficeManager.GetBranchDetailById(id);
        if (!branchResult.Success)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Depo bulunamadı.", type = "danger" });
            return StatusCode(422);
        }

        var allBranches = await branchOfficeManager.GetPageBranchListAsync();
        var stocks = await branchOfficeManager.GetBranchStocksAsync(id);

        var vm = new StockTransferVm
        {
            SourceBranchId = id,
            SourceBranchName = branchResult.Data!.Name,
            AvailableBranches = allBranches.Data?.Where(b => b.Id != id).ToList() ?? [],
            SourceStocks = stocks.Data?.Where(s => s.CurrentStock > 0).ToList() ?? []
        };

        return PartialView("_StockTransferDialog", vm);
    }

    [HttpPost("/branch-offices/transfer")]
    public async Task<IActionResult> Transfer([FromForm] StockTransferPostVm model)
    {
        if (model.SourceBranchId == model.TargetBranchId)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Kaynak ve hedef depo ayni olamaz.", type = "danger" });
            return StatusCode(422);
        }

        var items = model.Items
            .Where(i => i.Quantity > 0)
            .Select(i => new TransferItemDto(i.ProductVariantId, i.Quantity))
            .ToList();

        if (items.Count == 0)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "En az bir urun secmelisiniz.", type = "danger" });
            return StatusCode(422);
        }

        var result = await officeStockManager.TransferStockAsync(
            model.SourceBranchId, model.TargetBranchId, items);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = $"Transfer tamamlandi. {result.Data?.TransferredCount ?? 0} urun aktarildi.", type = "success" });
                Response.HtmxTrigger("transferCompleted");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Transfer başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess($"Transfer tamamlandi. {result.Data?.TransferredCount ?? 0} urun aktarildi.");
        else
            TempData.SetError(result.Message ?? "Transfer başarısız.");

        return RedirectToAction("Detail", new { id = model.SourceBranchId });
    }
}
