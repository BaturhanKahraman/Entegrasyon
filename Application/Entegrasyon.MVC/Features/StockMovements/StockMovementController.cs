using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.StockMovements.ViewModels;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.StockMovements;

[Authorize]
public class StockMovementController(
    IOfficeStockManager officeStockManager,
    IBranchOfficeManager branchOfficeManager) : HtmxController
{
    [HttpGet("/stock/movements")]
    public async Task<IActionResult> Index(
        int? branchOfficeId = null,
        StockMovementType? type = null,
        int page = 1)
    {
        ViewData.SetPageTitle("Stok Hareketleri");
        ViewData.SetActiveNav("stock-movements");

        var pageIndex = page - 1;
        var result = await officeStockManager.GetStockMovementsAsync(pageIndex, 50, branchOfficeId, type);
        var branchResult = await branchOfficeManager.GetBranchList();

        ViewBag.Branches = branchResult.Data ?? [];
        ViewBag.SelectedBranchOfficeId = branchOfficeId;
        ViewBag.SelectedType = type;
        ViewBag.CurrentPage = page;

        if (Request.IsHtmx())
            return PartialView("Partials/_MovementTable", result.Data);

        return View(result.Data);
    }

    [HttpPost("/stock/movements/adjust")]
    public async Task<IActionResult> Adjust([FromForm] StockAdjustVm model)
    {
        Entegrasyon.Entity.Results.IResult result;

        if (model.IsIncrease)
        {
            result = await officeStockManager.IncreaseStockAtomicAsync(
                model.BranchOfficeId, model.ProductVariantId, model.Quantity,
                StockMovementType.ManualAdjustment,
                referenceType: "ManualAdjustment",
                referenceId: null);
        }
        else
        {
            result = await officeStockManager.DecreaseStockAtomicAsync(
                model.BranchOfficeId, model.ProductVariantId, model.Quantity,
                StockMovementType.ManualAdjustment,
                referenceType: "ManualAdjustment",
                referenceId: null);
        }

        return HtmxMutationResult(
            result,
            model.IsIncrease ? "Stok artırıldı." : "Stok düşüldü.",
            "Stok ayarlanamadı.",
            refreshEvent: "stockAdjusted");
    }
}
