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
    /// Kullanıcının aktif şube ofisini değiştirir.
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
                return Content(result.Message ?? "Şube değiştirilemedi.");
            }
            TempData.SetError(result.Message ?? "Şube değiştirilemedi.");
            return RedirectToAction(nameof(Index));
        }

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Refresh", "true");
            return Content(result.Message ?? "Aktif şube değiştirildi.");
        }

        TempData.SetSuccess(result.Message ?? "Aktif şube değiştirildi.");
        return RedirectToAction(nameof(Index));
    }


    [HttpGet("/branch-offices")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Subeler");
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
            TempData.SetError(result.Message ?? "Sube bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle(result.Data!.Name);
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Subeler", "/branch-offices"), (result.Data.Name, null));

        var stocks = await branchOfficeManager.GetBranchStocksAsync(id);
        ViewBag.Stocks = stocks.Data;

        return View(result.Data);
    }

    [HttpGet("/branch-offices/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Sube");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Subeler", "/branch-offices"), ("Yeni Sube", null));

        return View(new BranchOfficeCreateVm());
    }

    [HttpPost("/branch-offices/create")]
    public async Task<IActionResult> Create(BranchOfficeCreateVm model)
    {
        ViewData.SetPageTitle("Yeni Sube");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Subeler", "/branch-offices"), ("Yeni Sube", null));

        if (!ModelState.IsValid)
            return View(model);

        var result = await branchOfficeManager.AddBranch(new BranchOfficeAddDto(model.Name));

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sube eklenemedi.");
            return View(model);
        }

        TempData.SetSuccess("Sube basariyla eklendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/branch-offices/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await branchOfficeManager.GetBranchDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sube bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle($"{result.Data!.Name} - Duzenle");
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Subeler", "/branch-offices"), (result.Data.Name, $"/branch-offices/{id}"), ("Duzenle", null));

        var vm = new BranchOfficeEditVm { Id = id, Name = result.Data.Name };
        return View(vm);
    }

    [HttpPost("/branch-offices/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, BranchOfficeEditVm model)
    {
        ViewData.SetActiveNav("branch-offices");

        if (!ModelState.IsValid)
        {
            ViewData.SetPageTitle($"{model.Name} - Duzenle");
            ViewData.SetBreadcrumb(("Subeler", "/branch-offices"), (model.Name, $"/branch-offices/{id}"), ("Duzenle", null));
            model.Id = id;
            return View(model);
        }

        var result = await branchOfficeManager.Update(new BranchOfficeEditDto(id, model.Name));

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sube guncellenemedi.");
            ViewData.SetPageTitle($"{model.Name} - Duzenle");
            ViewData.SetBreadcrumb(("Subeler", "/branch-offices"), (model.Name, $"/branch-offices/{id}"), ("Duzenle", null));
            model.Id = id;
            return View(model);
        }

        TempData.SetSuccess("Sube basariyla guncellendi.");
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/branch-offices/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await branchOfficeManager.Delete(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sube silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Sube basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Sube silinemedi.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/branch-offices/{id:int}/transfer")]
    public async Task<IActionResult> TransferDialog(int id)
    {
        var branchResult = await branchOfficeManager.GetBranchDetailById(id);
        if (!branchResult.Success)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Sube bulunamadi.", type = "danger" });
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
                new { message = "Kaynak ve hedef sube ayni olamaz.", type = "danger" });
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
                new { message = result.Message ?? "Transfer basarisiz.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess($"Transfer tamamlandi. {result.Data?.TransferredCount ?? 0} urun aktarildi.");
        else
            TempData.SetError(result.Message ?? "Transfer basarisiz.");

        return RedirectToAction("Detail", new { id = model.SourceBranchId });
    }
}
