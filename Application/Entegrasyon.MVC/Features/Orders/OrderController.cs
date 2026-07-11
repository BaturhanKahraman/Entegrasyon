using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Orders;

[Authorize]
public class OrderController(
    IOrderManager orderManager,
    ITrendyolOrderService trendyolOrderService) : Controller
{
    // ── Geriye Uyumluluk: Eski /orders ve /marketplace/orders URL'leri ─────

    [HttpGet("/orders")]
    public IActionResult IndexRedirect() => RedirectPermanent("/sales");

    [HttpGet("/marketplace/orders")]
    public IActionResult MarketplaceRedirect(int mp = 0)
    {
        var source = mp switch
        {
            1 => "Trendyol",
            2 => "N11",
            3 => "Hepsiburada",
            4 => "Amazon",
            5 => "Pazarama",
            7 => "PttAvm",
            8 => "Ciceksepeti",
            _ => null
        };
        var url = source is null ? "/sales" : $"/sales?source={source}";
        return RedirectPermanent(url);
    }

    [HttpGet("/orders/{id:guid}")]
    public IActionResult DetailRedirect(Guid id)
        => RedirectPermanent($"/sales/order/{id}");

    [HttpGet("/orders/{id:guid}/print")]
    public IActionResult PrintRedirect(Guid id)
        => RedirectPermanent($"/sales/order/{id}/print");

    [HttpGet("/marketplace/orders/{id:guid}")]
    public IActionResult MarketplaceDetailRedirect(Guid id)
        => RedirectPermanent($"/sales/order/{id}");

    // ── Sipariş İşlemleri (yeni /sales/order prefix'inde) ───────────────────

    [HttpGet("/sales/order/{id:guid}/print")]
    public async Task<IActionResult> Print(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sipariş bulunamadı.");
            return Redirect("/sales");
        }

        return View("~/Features/Sales/Views/OrderPrint.cshtml", result.Data!);
    }

    [HttpPost("/sales/order/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromForm] string newStatus)
    {
        var result = await orderManager.UpdateOrderStatusAsync(id, newStatus);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sipariş durumu güncellendi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Durum güncellenemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Sipariş durumu başarıyla güncellendi.");
        else
            TempData.SetError(result.Message ?? "Sipariş durumu güncellenemedi.");

        return Redirect($"/sales/order/{id}");
    }

    [HttpPost("/sales/order/{id:guid}/mark-unsupplied")]
    public async Task<IActionResult> MarkUnsupplied(Guid id, [FromForm] int reasonId)
    {
        var orderResult = await orderManager.GetOrderByIdAsync(id);
        if (!orderResult.Success || orderResult.Data is null)
            return ToastError("Sipariş bulunamadı.", 404, "/sales");

        var order = orderResult.Data;

        if (order.MarketPlaceId != 1 || order.ShipmentPackageId is null)
            return ToastError("Bu işlem sadece Trendyol siparişleri için geçerlidir.", 422, $"/sales/order/{id}");

        var lineIds = order.OrderItems
            .Where(i => i.LineId.HasValue)
            .Select(i => i.LineId!.Value)
            .ToList();

        if (lineIds.Count == 0)
            return ToastError("Sipariş kalemlerinde satır ID bulunamadı.", 422, $"/sales/order/{id}");

        if (!TrendyolUnsuppliedReasons.IsValid(reasonId))
            return ToastError("Geçerli bir tedarik edilemedi sebebi seçin.", 422, $"/sales/order/{id}");

        var result = await trendyolOrderService.MarkUnsuppliedAsync(
            order.ShipmentPackageId.Value, lineIds, reasonId);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sipariş tedarik edilemez olarak işaretlendi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "İşlem başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Sipariş tedarik edilemez olarak işaretlendi.");
        else
            TempData.SetError(result.Message ?? "Sipariş tedarik edilemez olarak işaretlenemedi.");

        return Redirect($"/sales/order/{id}");
    }

    /// <summary>HTMX toast (danger) + statusCode, ya da PRG TempData.SetError + redirect.</summary>
    private IActionResult ToastError(string message, int statusCode, string redirectUrl)
    {
        if (Request.IsHtmx())
        {
            Response.HtmxTriggerWithData("showToast", new { message, type = "danger" });
            return StatusCode(statusCode);
        }

        TempData.SetError(message);
        return Redirect(redirectUrl);
    }
}
