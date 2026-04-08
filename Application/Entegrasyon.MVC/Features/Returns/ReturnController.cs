using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Returns;

[Authorize]
public class ReturnController(
    IStorefrontReturnManager returnManager,
    ITenantContext tenantContext) : HtmxController
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    [HttpGet("/returns")]
    public async Task<IActionResult> Index(string? status = null)
    {
        ViewData.SetPageTitle("Iade Yonetimi");
        ViewData.SetActiveNav("returns");

        var result = await returnManager.GetAllReturnsAsync(TenantId);
        var returns = result.Data ?? [];

        if (status is not null && Enum.TryParse<ReturnStatus>(status, out var parsed))
            returns = returns.Where(r => r.Status == parsed).ToList();

        ViewBag.Status = status;

        if (Request.IsHtmx())
            return PartialView("~/Features/Returns/Views/Partials/_ReturnTable.cshtml", returns);

        return View("~/Features/Returns/Views/Index.cshtml", returns);
    }

    [HttpPost("/returns/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromForm] string? reviewNote, [FromForm] decimal? refundAmount)
    {
        var result = await returnManager.UpdateReturnStatusAsync(id, ReturnStatus.Approved, reviewNote, refundAmount);
        return HtmxMutationResult(result, "Iade talebi onaylandi.", refreshEvent: "returnStatusChanged");
    }

    [HttpPost("/returns/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromForm] string? reviewNote)
    {
        var result = await returnManager.UpdateReturnStatusAsync(id, ReturnStatus.Rejected, reviewNote, null);
        return HtmxMutationResult(result, "Iade talebi reddedildi.", refreshEvent: "returnStatusChanged");
    }

    [HttpPost("/returns/{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await returnManager.UpdateReturnStatusAsync(id, ReturnStatus.Completed, null, null);
        return HtmxMutationResult(result, "Iade tamamlandi.", refreshEvent: "returnStatusChanged");
    }
}
