using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Entegrasyon.MVC.Infrastructure.Extensions;
using System.Security.Claims;

namespace Entegrasyon.MVC.Features.BulkOperations;

[Authorize]
public class BulkOperationController(IBulkOperationManager bulkOperationManager) : Controller
{
    [HttpGet("/bulk-operations")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Toplu İşlemler");
        ViewData.SetActiveNav("bulk-operations");

        var recentOps = await bulkOperationManager.GetRecentOperationsAsync(20);
        return View(recentOps.Data);
    }

    [HttpPost("/bulk-operations/validate")]
    public async Task<IActionResult> Validate(IFormFile file, [FromForm] BulkOperationType type)
    {
        if (file == null || file.Length == 0)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Dosya secilmedi.", type = "danger" });
            return StatusCode(422);
        }

        await using var stream = file.OpenReadStream();
        var result = await bulkOperationManager.ValidateImportAsync(stream, type);

        if (!result.Success)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Dogrulama başarısız.", type = "danger" });
            return StatusCode(422);
        }

        ViewBag.OperationType = type;
        return PartialView("_ValidationPreview", result.Data);
    }

    [HttpPost("/bulk-operations/import")]
    public async Task<IActionResult> Import(IFormFile file, [FromForm] BulkOperationType type)
    {
        if (file == null || file.Length == 0)
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Dosya secilmedi.", type = "danger" });
                return StatusCode(422);
            }

            TempData.SetError("Dosya secilmedi.");
            return RedirectToAction(nameof(Index));
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var stream = file.OpenReadStream();

        var result = type switch
        {
            BulkOperationType.ProductImport => await bulkOperationManager.ImportProductsAsync(stream, file.FileName, userId),
            BulkOperationType.PriceImport => await bulkOperationManager.ImportPricesAsync(stream, file.FileName, userId),
            BulkOperationType.StockImport => await bulkOperationManager.ImportStockAsync(stream, file.FileName, userId),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = $"Import tamamlandi. {result.Data?.SuccessCount ?? 0} basarili, {result.Data?.ErrorCount ?? 0} Hatalı.", type = "success" });
                Response.HtmxTrigger("importCompleted");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Import başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess($"Import tamamlandi. {result.Data?.SuccessCount ?? 0} basarili.");
        else
            TempData.SetError(result.Message ?? "Import başarısız.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/bulk-operations/export-columns")]
    public async Task<IActionResult> ExportColumns(BulkOperationType type)
    {
        var result = await bulkOperationManager.GetAvailableColumnsAsync(type);

        if (!result.Success)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Kolonlar alinamadi.", type = "danger" });
            return StatusCode(422);
        }

        ViewBag.ExportType = type;
        return PartialView("_ExportColumnSelector", result.Data);
    }

    [HttpPost("/bulk-operations/export-with-columns")]
    public async Task<IActionResult> ExportWithColumns(
        [FromForm] BulkOperationType type,
        [FromForm] List<string>? selectedColumns,
        [FromForm] DateTimeOffset? dateFrom = null,
        [FromForm] DateTimeOffset? dateTo = null)
    {
        // Tarih girilmezse (null) tüm veri export edilir — geriye dönük uyumlu.
        var filter = new ExportFilterDto(
            DateFrom: dateFrom,
            DateTo: dateTo,
            SelectedColumns: selectedColumns);

        var result = type switch
        {
            BulkOperationType.ProductExport => await bulkOperationManager.ExportProductsAsync(filter),
            BulkOperationType.PriceExport => await bulkOperationManager.ExportPricesAsync(filter),
            BulkOperationType.StockExport => await bulkOperationManager.ExportStockAsync(filter),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Export başarısız.");
            return RedirectToAction(nameof(Index));
        }

        var fileName = type switch
        {
            BulkOperationType.ProductExport => "urunler.xlsx",
            BulkOperationType.PriceExport => "fiyatlar.xlsx",
            BulkOperationType.StockExport => "stoklar.xlsx",
            _ => "export.xlsx"
        };

        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("/bulk-operations/export")]
    public async Task<IActionResult> Export(BulkOperationType type)
    {
        var filter = new ExportFilterDto();

        var result = type switch
        {
            BulkOperationType.ProductExport => await bulkOperationManager.ExportProductsAsync(filter),
            BulkOperationType.PriceExport => await bulkOperationManager.ExportPricesAsync(filter),
            BulkOperationType.StockExport => await bulkOperationManager.ExportStockAsync(filter),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Export başarısız.");
            return RedirectToAction(nameof(Index));
        }

        var fileName = type switch
        {
            BulkOperationType.ProductExport => "urunler.xlsx",
            BulkOperationType.PriceExport => "fiyatlar.xlsx",
            BulkOperationType.StockExport => "stoklar.xlsx",
            _ => "export.xlsx"
        };

        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("/bulk-operations/history")]
    public async Task<IActionResult> History()
    {
        var recentOps = await bulkOperationManager.GetRecentOperationsAsync(50);

        if (Request.IsHtmx())
            return PartialView("_OperationHistory", recentOps.Data);

        return View("Index", recentOps.Data);
    }

    [HttpGet("/bulk-operations/template")]
    public async Task<IActionResult> DownloadTemplate(BulkOperationType type)
    {
        var result = await bulkOperationManager.GetImportTemplateAsync(type);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sablon indirilemedi.");
            return RedirectToAction(nameof(Index));
        }

        var fileName = $"sablon_{type.ToString().ToLower()}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
