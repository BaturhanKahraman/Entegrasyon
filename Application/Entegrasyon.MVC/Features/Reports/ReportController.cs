using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Requests;
using Entegrasyon.MVC.Features.Reports.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Reports;

[Authorize]
public class ReportController(
    IReportManager reportManager,
    ICustomerReportManager customerReportManager,
    IStorefrontReturnManager storefrontReturnManager,
    IShipmentTrackingManager shipmentTrackingManager,
    IBranchOfficeManager branchOfficeManager,
    IStockTransferRequestManager stockTransferRequestManager) : Controller
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
        ViewData.SetActiveNav("reports-profit-loss");
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
        ViewData.SetPageTitle("Ürün Performansı");
        ViewData.SetActiveNav("reports-product-performance");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Ürün Performansı", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var data = await reportManager.GetProductPerformanceAsync(
            new ProductPerformanceFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(data);
    }

    [HttpGet("/reports/stock-alerts")]
    public async Task<IActionResult> StockAlerts(
        int threshold = 10, int page = 1, int? branchOfficeId = null, StockAlertLevel? alertLevel = null)
    {
        ViewData.SetPageTitle("Stok Uyarilari");
        ViewData.SetActiveNav("reports-stock-alerts");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Stok Uyarilari", null));

        var data = await reportManager.GetStockAlertReportAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = threshold,
            BranchOfficeId = branchOfficeId,
            AlertLevel = alertLevel,
            PageIndex = page - 1,
            PageSize = 20
        });

        ViewBag.Threshold = threshold;
        ViewBag.BranchOfficeId = branchOfficeId;
        ViewBag.AlertLevel = alertLevel;

        // Şube filtresi dropdown'u (Tom Select) için şube listesi.
        var branches = await branchOfficeManager.GetBranchList();
        ViewBag.Branches = branches.Data ?? [];

        if (Request.IsHtmx())
            return PartialView("Partials/_StockAlertTable", data.Items);

        return View(data);
    }

    /// <summary>
    /// Stok uyarı sayfasından seçili satırlardan toplu stok transfer talebi oluşturur.
    /// Her satır kendi kaynak şubesini taşır; hedef şube tektir (formdan).
    /// Bir StockTransferRequest tek source/target alır → satırlar kaynak şube başına
    /// gruplanıp her grup için ayrı talep açılır. Asıl validation + iş kuralları + çift
    /// loglama mutasyon katmanında (StockTransferRequestManager.CreateAsync) yapılır.
    /// PRG: TempData + RedirectToAction(StockAlerts).
    /// </summary>
    [HttpPost("/reports/stock-alerts/bulk-transfer")]
    [Authorize(Policy = AppPermissions.Stock.Transfer)]
    public async Task<IActionResult> BulkTransfer(StockAlertBulkTransferVm vm, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            TempData.SetError("Oturum bilgisi okunamadı.");
            return RedirectToAction(nameof(StockAlerts));
        }

        // Geçerli satırlar: varyant dolu + miktar pozitif.
        var validLines = (vm.Lines ?? [])
            .Where(l => l.ProductVariantId != Guid.Empty && l.Quantity > 0)
            .ToList();

        if (validLines.Count == 0)
        {
            TempData.SetError("Transfer için geçerli ürün seçilmedi.");
            return RedirectToAction(nameof(StockAlerts));
        }

        // Kaynak şube başına grupla — her grup tek talep. Hedef=kaynak olan grup atlanır.
        var groups = validLines
            .Where(l => l.SourceBranchOfficeId != vm.TargetBranchOfficeId)
            .GroupBy(l => l.SourceBranchOfficeId)
            .ToList();

        if (groups.Count == 0)
        {
            TempData.SetError("Kaynak ve hedef şube aynı olamaz.");
            return RedirectToAction(nameof(StockAlerts));
        }

        var created = 0;
        var errors = new List<string>();
        foreach (var group in groups)
        {
            var items = group
                .Select(l => new TransferItemDto(l.ProductVariantId, l.Quantity))
                .ToList();

            var result = await stockTransferRequestManager.CreateAsync(
                group.Key, vm.TargetBranchOfficeId, items, userId.Value, ct);

            if (result.Success)
                created++;
            else
                errors.Add(result.Message ?? "Talep oluşturulamadı.");
        }

        if (created == 0)
            TempData.SetError(errors.Count > 0 ? string.Join(" ", errors) : "Transfer talebi oluşturulamadı.");
        else if (errors.Count > 0)
            TempData.SetSuccess($"{created} transfer talebi oluşturuldu. {errors.Count} grup başarısız: {string.Join(" ", errors)}");
        else
            TempData.SetSuccess($"{created} stok transfer talebi başarıyla oluşturuldu.");

        return RedirectToAction(nameof(StockAlerts));
    }

    [HttpGet("/reports/inventory")]
    public async Task<IActionResult> Inventory(
        int? branchOfficeId = null,
        StockFilter stockFilter = StockFilter.All,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool unsoldOnly = false)
    {
        ViewData.SetPageTitle("Envanter Raporu");
        ViewData.SetActiveNav("reports-inventory");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Envanter", null));

        var report = await reportManager.GetInventoryReportAsync(
            new InventoryReportFilterDto(branchOfficeId, stockFilter, startDate, endDate, unsoldOnly));

        // Form state'ini koru: view bu ViewBag anahtarlarını okuyup filtre alanlarını işaretler.
        ViewBag.BranchOfficeId = branchOfficeId;
        ViewBag.StockFilter = stockFilter;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.UnsoldOnly = unsoldOnly;

        // Şube filtresi dropdown'u için şube listesi (StockAlerts ile aynı desen).
        var branches = await branchOfficeManager.GetBranchList();
        ViewBag.Branches = branches.Data ?? [];

        return View(report);
    }

    [HttpGet("/reports/marketplace")]
    public async Task<IActionResult> Marketplace(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        ViewData.SetPageTitle("Pazaryeri Raporu");
        ViewData.SetActiveNav("reports-marketplace");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Pazaryeri", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var data = await reportManager.GetMarketplaceSummaryAsync(
            new MarketplaceSummaryFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(data);
    }

    [HttpGet("/reports/customers")]
    public async Task<IActionResult> Customers(string? segment = null, int page = 1)
    {
        ViewData.SetPageTitle("Musteri Raporu");
        ViewData.SetActiveNav("reports-customers");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Musteriler", null));

        // RFM-zenginleştirilmiş sayfalı liste + segment filtresi (""/null=tümü, "vip"|"risk"|"yeni"|"dormant").
        var result = await customerReportManager.GetRfmPageableAsync(segment, page - 1, 50);

        // Form state — view "" | "vip" | "risk" | "yeni" | "dormant" değerlerini okur.
        ViewBag.Segment = string.IsNullOrWhiteSpace(segment) ? "" : segment.Trim().ToLowerInvariant();

        // Cohort retention matrisi (view ViewBag.Cohort üzerinden IEnumerable<{CohortMonth,Size,RetentionPct}> okur).
        ViewBag.Cohort = await customerReportManager.GetCohortRetentionAsync();

        // Gönderilebilir kupon listesi (view select dropdown'unda {Id,Code,Description} okur).
        ViewBag.Coupons = await customerReportManager.GetSendableCouponsAsync();

        return View(result.Data);
    }

    /// <summary>
    /// Müşteri raporundan seçili müşterilere toplu indirim kodu (dormant geri kazanım) gönderir.
    /// MUTASYON → asıl validation + iş kuralları + çift loglama mutasyon katmanında
    /// (CustomerReportManager.SendRecoveryCouponAsync). PRG: TempData + RedirectToAction(Customers).
    /// </summary>
    [HttpPost("/reports/customers/send-coupon")]
    [Authorize(Policy = AppPermissions.Reports.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCoupon(SendRecoveryCouponDto dto, CancellationToken ct = default)
    {
        var result = await customerReportManager.SendRecoveryCouponAsync(dto, GetCurrentUserId(), ct);

        if (result.Success)
            TempData.SetSuccess(result.Message ?? "İndirim kodu gönderildi.");
        else
            TempData.SetError(result.Message ?? "İndirim kodu gönderilemedi.");

        return RedirectToAction(nameof(Customers), new { segment = dto.Segment });
    }

    [HttpGet("/reports/returns")]
    public async Task<IActionResult> Returns()
    {
        ViewData.SetPageTitle("Iade Raporu");
        ViewData.SetActiveNav("reports-returns");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Iade", null));

        var result = await storefrontReturnManager.GetAllReturnsAsync(1);
        return View(result.Data ?? []);
    }

    [HttpGet("/reports/tax")]
    public async Task<IActionResult> Tax(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        ViewData.SetPageTitle("Vergi Raporu");
        ViewData.SetActiveNav("reports-tax");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Vergi", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var report = await reportManager.GetSalesReportAsync(new SalesReportFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(report);
    }

    [HttpGet("/reports/category-sales")]
    public async Task<IActionResult> CategorySales(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        ViewData.SetPageTitle("Kategori Bazli Satis");
        ViewData.SetActiveNav("reports-category-sales");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Kategori Satis", null));

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var data = await reportManager.GetProductPerformanceAsync(
            new ProductPerformanceFilterDto(start, end));

        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(data);
    }

    [HttpGet("/reports/shipping")]
    public async Task<IActionResult> Shipping()
    {
        ViewData.SetPageTitle("Kargo Raporu");
        ViewData.SetActiveNav("reports-shipping");
        ViewData.SetBreadcrumb(("Raporlar", null), ("Kargo", null));

        var summary = await shipmentTrackingManager.GetCargoSummaryAsync();
        return View(summary.Data);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
