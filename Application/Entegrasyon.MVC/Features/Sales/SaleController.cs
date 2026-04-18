using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Features.Sales.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Sales;

[Authorize]
public class SaleController(
    IUnifiedSaleManager unifiedSaleManager,
    ISaleManager saleManager,
    ISaleReturnManager saleReturnManager,
    IPaymentMethodManager paymentMethodManager,
    IOrderManager orderManager,
    ITenantContext tenantContext) : Controller
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Birleşik Liste ──────────────────────────────────────────────────

    [HttpGet("/sales")]
    public async Task<IActionResult> Index(
        UnifiedSaleSource? source = null,
        UnifiedSaleStatus? status = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? search = null,
        int page = 0)
    {
        var startLocal = startDate.HasValue
            ? TurkeyTime.StartOfDay(startDate.Value)
            : TurkeyTime.StartOfToday.AddDays(-7);
        var endLocal = endDate.HasValue
            ? TurkeyTime.StartOfDay(endDate.Value).AddDays(1)
            : TurkeyTime.StartOfToday.AddDays(1);
        var start = startLocal.ToUniversalTime();
        var end = endLocal.ToUniversalTime();

        var filter = new UnifiedSaleFilterDto(
            Source: source,
            Status: status,
            StartDate: start,
            EndDate: end,
            SearchText: search,
            CustomerId: null,
            PageIndex: page,
            PageSize: 25);

        var pageResult = await unifiedSaleManager.GetPageableAsync(filter);
        var summaryResult = await unifiedSaleManager.GetSummaryAsync(filter);
        var countsResult = await unifiedSaleManager.GetSourceCountsAsync(filter);

        var vm = new UnifiedSaleIndexViewModel
        {
            Page = pageResult.Data ?? new Entegrasyon.Entity.Pageable<UnifiedSaleListItemDto>([], 0, 25, 0),
            Summary = summaryResult.Data ?? new UnifiedSaleSummaryDto(0m, 0, 0m, 0d),
            SourceCounts = countsResult.Data ?? [],
            Filter = filter
        };

        ViewData.SetPageTitle("Satışlar");
        ViewData.SetActiveNav("sales");

        if (Request.IsHtmx())
            return PartialView("Partials/_UnifiedSaleTable", vm);

        return View(vm);
    }

    // ── Sale Detay (POS / Manuel) ───────────────────────────────────────

    [HttpGet("/sales/sale/{id:guid}")]
    public async Task<IActionResult> SaleDetail(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData.SetError(result.Message ?? "Satış bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(TenantId);

        ViewData.SetPageTitle($"Satış Detay — {result.Data.SaleNumber}");
        ViewData.SetActiveNav("sales");
        ViewData.SetBreadcrumb(("Satışlar", "/sales"), ($"#{result.Data.SaleNumber}", null));
        ViewBag.PaymentMethods = paymentMethods.Data ?? [];

        return View("SaleDetail", result.Data);
    }

    // ── Order Detay (Marketplace / Storefront) ──────────────────────────

    [HttpGet("/sales/order/{id:guid}")]
    public async Task<IActionResult> OrderDetail(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData.SetError(result.Message ?? "Sipariş bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var order = result.Data;
        var title = order.OrderNumber ?? order.Id.ToString()[..8];

        ViewData.SetPageTitle($"Sipariş #{title}");
        ViewData.SetActiveNav("sales");
        ViewData.SetBreadcrumb(("Satışlar", "/sales"), ($"#{title}", null));

        return View("OrderDetail", order);
    }

    // ── Eski /sales/{id} route'unu yeni /sales/sale/{id}'e yönlendir ─────

    [HttpGet("/sales/{id:guid}")]
    public IActionResult LegacySaleDetail(Guid id)
        => RedirectToActionPermanent(nameof(SaleDetail), new { id });

    // ── Print / Cancel / Return (mevcut — redirect'ler güncellendi) ─────

    [HttpGet("/sales/sale/{id:guid}/print")]
    public async Task<IActionResult> Print(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
            return NotFound();

        return View(result.Data);
    }

    [HttpPost("/sales/sale/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await saleManager.CancelSaleAsync(id, userId);

        if (result.Success)
            TempData.SetSuccess(result.Message ?? "Satış iptal edildi.");
        else
            TempData.SetError(result.Message ?? "Satış iptal edilemedi.");

        return RedirectToAction(nameof(SaleDetail), new { id });
    }

    [HttpGet("/sales/sale/{id:guid}/return-dialog")]
    public async Task<IActionResult> ReturnDialog(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
            return BadRequest(result.Message ?? "Satış bulunamadı.");

        var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(TenantId);
        ViewBag.PaymentMethods = paymentMethods.Data ?? [];

        return PartialView("Partials/_SaleReturnDialog", result.Data);
    }

    [HttpPost("/sales/sale/{saleId:guid}/return")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReturn(
        Guid saleId,
        string returnReason,
        int? refundPaymentMethodId,
        string? note,
        List<Guid> itemIds,
        List<int> itemQuantities,
        List<string?> itemReasons)
    {
        var items = new List<SaleReturnItemDto>();
        for (int i = 0; i < itemIds.Count; i++)
        {
            if (itemQuantities[i] <= 0) continue;
            items.Add(new SaleReturnItemDto(
                SaleItemId: itemIds[i],
                OrderItemId: null,
                Quantity: itemQuantities[i],
                Reason: itemReasons.Count > i ? itemReasons[i] : null));
        }

        if (items.Count == 0)
        {
            TempData.SetError("En az bir kalem seçmelisiniz.");
            return RedirectToAction(nameof(SaleDetail), new { id = saleId });
        }

        var dto = new CreateSaleReturnDto(
            SaleId: saleId,
            OrderId: null,
            ReturnedByUserId: GetCurrentUserId(),
            Source: ReturnSource.InPerson,
            ReturnReasonId: null,
            CustomReason: returnReason,
            RefundPaymentMethodId: refundPaymentMethodId,
            Note: note,
            Items: items);

        var result = await saleReturnManager.CreateReturnAsync(dto);
        if (result.Success)
            TempData.SetSuccess(result.Message ?? "İade talebi oluşturuldu.");
        else
            TempData.SetError(result.Message ?? "İade talebi oluşturulamadı.");

        return RedirectToAction(nameof(SaleDetail), new { id = saleId });
    }

    [HttpPost("/sales/returns/{returnId:long}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveReturn(long returnId, Guid saleId)
    {
        var result = await saleReturnManager.ApproveReturnAsync(returnId, GetCurrentUserId());
        if (result.Success)
            TempData.SetSuccess(result.Message ?? "İade onaylandı.");
        else
            TempData.SetError(result.Message ?? "İade onaylanamadı.");

        return RedirectToAction(nameof(SaleDetail), new { id = saleId });
    }

    [HttpPost("/sales/returns/{returnId:long}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectReturn(long returnId, Guid saleId, string reason)
    {
        var result = await saleReturnManager.RejectReturnAsync(returnId, GetCurrentUserId(), reason ?? "");
        if (result.Success)
            TempData.SetSuccess(result.Message ?? "İade reddedildi.");
        else
            TempData.SetError(result.Message ?? "İade reddedilemedi.");

        return RedirectToAction(nameof(SaleDetail), new { id = saleId });
    }
}
