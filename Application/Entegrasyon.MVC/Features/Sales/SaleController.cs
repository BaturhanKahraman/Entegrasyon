using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Sales;

[Authorize]
public class SaleController(
    ISaleManager saleManager,
    ISaleReturnManager saleReturnManager,
    IPaymentMethodManager paymentMethodManager,
    ITenantContext tenantContext) : Controller
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Sale List ────────────────────────────────────────────────────────

    [HttpGet("/sales")]
    public async Task<IActionResult> Index(
        string? search = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        SaleSource? source = null,
        SaleStatus? status = null,
        int page = 0)
    {
        var startLocal = startDate.HasValue ? TurkeyTime.StartOfDay(startDate.Value) : TurkeyTime.StartOfToday;
        var endLocal = endDate.HasValue ? TurkeyTime.StartOfDay(endDate.Value) : TurkeyTime.StartOfToday.AddDays(1);
        var start = startLocal.ToUniversalTime();
        var end = endLocal.ToUniversalTime();

        var dto = new SalePageableDto(
            CustomerId: null,
            DateBetweenStart: start,
            DateBetweenEnd: end,
            SalePersonId: Guid.Empty,
            SaleSource: source,
            SaleStatus: status,
            FullTextSearchKey: search ?? "",
            PageIndex: page);

        var salesResult = await saleManager.GetSalesPageable(dto);
        var summaryResult = await saleManager.GetSalesSummaryAsync(dto);

        ViewData.SetPageTitle("Satışlar");
        ViewData.SetActiveNav("sales");
        ViewBag.Summary = summaryResult.Data;
        ViewBag.CurrentFilters = dto;

        if (Request.IsHtmx())
            return PartialView("Partials/_SaleTable", salesResult.Data);

        return View(salesResult.Data);
    }

    // ── Sale Detail ──────────────────────────────────────────────────────

    [HttpGet("/sales/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
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
        ViewBag.PaymentMethods = paymentMethods.Data ?? [];

        return View(result.Data);
    }

    [HttpGet("/sales/{id:guid}/print")]
    public async Task<IActionResult> Print(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
            return NotFound();

        return View(result.Data);
    }

    [HttpPost("/sales/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await saleManager.CancelSaleAsync(id, userId);

        if (result.Success)
            TempData.SetSuccess(result.Message ?? "Satış iptal edildi.");
        else
            TempData.SetError(result.Message ?? "Satış iptal edilemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Return Flow ──────────────────────────────────────────────────────

    [HttpGet("/sales/{id:guid}/return-dialog")]
    public async Task<IActionResult> ReturnDialog(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
            return BadRequest(result.Message ?? "Satış bulunamadı.");

        var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(TenantId);
        ViewBag.PaymentMethods = paymentMethods.Data ?? [];

        return PartialView("Partials/_SaleReturnDialog", result.Data);
    }

    [HttpPost("/sales/{saleId:guid}/return")]
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
            return RedirectToAction(nameof(Detail), new { id = saleId });
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

        return RedirectToAction(nameof(Detail), new { id = saleId });
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

        return RedirectToAction(nameof(Detail), new { id = saleId });
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

        return RedirectToAction(nameof(Detail), new { id = saleId });
    }
}
