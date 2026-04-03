using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Reports;

[Authorize]
public class ReportController(IReportManager reportManager) : Controller
{
    [HttpGet("/reports/sales")]
    public async Task<IActionResult> Sales(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        ViewData.SetPageTitle("Satis Raporu");
        ViewData.SetActiveNav("reports");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Satis", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var report = await reportManager.GetSalesReportAsync(new SalesReportFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(report);
    }

    [HttpGet("/reports/profit-loss")]
    public async Task<IActionResult> ProfitLoss(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        ViewData.SetPageTitle("Kar/Zarar Raporu");
        ViewData.SetActiveNav("reports");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Kar/Zarar", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var report = await reportManager.GetProfitLossReportAsync(
            new ProfitLossReportFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(report);
    }

    [HttpGet("/reports/product-performance")]
    public async Task<IActionResult> ProductPerformance(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        ViewData.SetPageTitle("Urun Performansi");
        ViewData.SetActiveNav("reports");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Urun Performansi", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var data = await reportManager.GetProductPerformanceAsync(
            new ProductPerformanceFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(data);
    }

    [HttpGet("/reports/stock-alerts")]
    public async Task<IActionResult> StockAlerts(int threshold = 10, int page = 1)
    {
        ViewData.SetPageTitle("Stok Uyarilari");
        ViewData.SetActiveNav("reports");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Stok Uyarilari", null));

        var data = await reportManager.GetStockAlertsAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = threshold,
            PageIndex = page - 1,
            PageSize = 20
        });

        ViewBag.Threshold = threshold;

        if (Request.IsHtmx())
            return PartialView("Partials/_StockAlertTable", data);

        return View(data);
    }

    [HttpGet("/reports/inventory")]
    public async Task<IActionResult> Inventory(int? branchOfficeId = null, StockFilter stockFilter = StockFilter.All)
    {
        ViewData.SetPageTitle("Envanter Raporu");
        ViewData.SetActiveNav("reports");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Envanter", null));

        var report = await reportManager.GetInventoryReportAsync(
            new InventoryReportFilterDto(branchOfficeId, stockFilter));

        return View(report);
    }

    [HttpGet("/reports/marketplace")]
    public async Task<IActionResult> Marketplace(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        ViewData.SetPageTitle("Pazaryeri Raporu");
        ViewData.SetActiveNav("reports");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Pazaryeri", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var data = await reportManager.GetMarketplaceSummaryAsync(
            new MarketplaceSummaryFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(data);
    }
}
