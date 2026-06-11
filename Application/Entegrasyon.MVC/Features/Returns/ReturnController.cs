using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

namespace Entegrasyon.MVC.Features.Returns;

[Authorize]
public class ReturnController(
    ISaleManager saleManager,
    ISaleReturnManager saleReturnManager,
    IReturnReasonManager returnReasonManager,
    IDbContextFactory<IntegrationDbContext> contextFactory) : HtmxController
{
    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── INDEX ────────────────────────────────────────────────────────────

    [HttpGet("/returns")]
    public async Task<IActionResult> Index(
        ReturnStatus? status = null,
        ReturnSource? source = null,
        string? search = null)
    {
        ViewData.SetPageTitle("İade Yönetimi");
        ViewData.SetActiveNav("returns");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.SaleReturns
            .Include(r => r.ReturnReason)
            .Include(r => r.ReturnedBy)
            .Include(r => r.Sale).ThenInclude(s => s!.Customer)
            .Include(r => r.Order)
            .Include(r => r.Items)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue) query = query.Where(r => r.ReturnStatus == status.Value);
        if (source.HasValue) query = query.Where(r => r.Source == source.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            var pattern = $"%{needle}%";
            query = query.Where(r => r.Sale != null && (
                EF.Functions.ILike(r.Sale.SaleNumber, pattern) ||
                (r.Sale.ReturnCode != null && EF.Functions.ILike(r.Sale.ReturnCode, pattern)) ||
                (r.Sale.Customer != null && r.Sale.Customer.FullName != null &&
                    EF.Functions.ILike(r.Sale.Customer.FullName, pattern))));
        }

        var returns = await query.Take(200).ToListAsync();

        ViewBag.Status = status;
        ViewBag.Source = source;
        ViewBag.Search = search;

        if (Request.IsHtmx())
            return PartialView("~/Features/Returns/Views/Partials/_ReturnTable.cshtml", returns);

        // KPI snapshot (global, tablo filtresinden bağımsız) yalnız tam-sayfa render'da.
        var kpis = await saleReturnManager.GetReturnKpisAsync();
        ViewBag.ReturnTotalCount = kpis.TotalCount;
        ViewBag.ReturnPendingCount = kpis.PendingCount;
        ViewBag.ReturnApprovedCount = kpis.ApprovedCount;
        ViewBag.ReturnCompletedRefundTotal = kpis.CompletedRefundTotal;

        return View("~/Features/Returns/Views/Index.cshtml", returns);
    }

    // ── DETAIL ───────────────────────────────────────────────────────────

    [HttpGet("/returns/{id:long}")]
    public async Task<IActionResult> Detail(long id)
    {
        ViewData.SetPageTitle("İade Detayı");
        ViewData.SetActiveNav("returns");

        var result = await saleReturnManager.GetReturnByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError("İade kaydı bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var reasons = await returnReasonManager.GetAllAsync();
        ViewBag.ReturnReasons = reasons.Data ?? [];

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var branchOffices = await dbContext.BranchOffices.AsNoTracking()
            .Where(b => !b.IsDeleted).OrderBy(b => b.Name).ToListAsync();
        ViewBag.BranchOffices = branchOffices;

        return View("~/Features/Returns/Views/Detail.cshtml", result.Data);
    }

    // ── ACTIONS ──────────────────────────────────────────────────────────

    [HttpPost("/returns/{id:long}/submit")]
    public async Task<IActionResult> Submit(long id)
    {
        var result = await saleReturnManager.SubmitReturnAsync(id, GetCurrentUserId());
        return HtmxMutationResult(result, "İade onaya sunuldu.", refreshEvent: "returnStatusChanged");
    }

    [HttpPost("/returns/{id:long}/approve")]
    public async Task<IActionResult> Approve(long id)
    {
        var result = await saleReturnManager.ApproveReturnAsync(id, GetCurrentUserId());
        return HtmxMutationResult(result, "İade onaylandı.", refreshEvent: "returnStatusChanged");
    }

    [HttpPost("/returns/{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, [FromForm] string reason)
    {
        var result = await saleReturnManager.RejectReturnAsync(id, GetCurrentUserId(), reason);
        return HtmxMutationResult(result, "İade reddedildi.", refreshEvent: "returnStatusChanged");
    }

    [HttpPost("/returns/{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, [FromForm] string reason)
    {
        var dto = new CancelSaleReturnDto(id, GetCurrentUserId(), reason);
        var result = await saleReturnManager.CancelReturnAsync(dto);
        return HtmxMutationResult(result, "İade iptal edildi.", refreshEvent: "returnStatusChanged");
    }

    [HttpPost("/returns/{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, [FromForm] int branchOfficeId, [FromForm] List<long> itemIds)
    {
        var dto = new CompleteSaleReturnDto(id, GetCurrentUserId(), branchOfficeId, itemIds);
        var result = await saleReturnManager.CompleteReturnAsync(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast", new { message = "İade tamamlandı.", type = "success" });
                Response.HtmxTrigger("returnStatusChanged");
                return Content("");
            }
            Response.HtmxTriggerWithData("showToast", new { message = result.Message ?? "İşlem başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success) TempData.SetSuccess("İade tamamlandı.");
        else TempData.SetError(result.Message ?? "İşlem başarısız.");
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/returns/items/{itemId:long}/restore")]
    public async Task<IActionResult> RestoreItem(long itemId, [FromForm] int branchOfficeId)
    {
        var result = await saleReturnManager.RestoreItemToStockAsync(itemId, GetCurrentUserId(), branchOfficeId);
        return HtmxMutationResult(result, "Ürün envantere eklendi.", refreshEvent: "returnStatusChanged");
    }

    // ── LOOKUP (kod ile yeni iade başlatma) ─────────────────────────────

    [HttpPost("/returns/lookup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lookup([FromForm] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = "Kod boş olamaz.", level = "error" });
            return NoContent();
        }

        var result = await saleManager.GetSaleByCodeAsync(code);
        if (!result.Success || result.Data is null)
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Satış bulunamadı.", level = "error" });
            return NoContent();
        }

        return PartialView("~/Features/Sales/Views/Partials/_SaleReturnDialog.cshtml", result.Data);
    }
}
