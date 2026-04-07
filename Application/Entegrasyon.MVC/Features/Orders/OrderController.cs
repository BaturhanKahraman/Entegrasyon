using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Requests;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Orders;

[Authorize]
public class OrderController(
    IOrderManager orderManager,
    ITrendyolOrderService trendyolOrderService) : Controller
{
    [HttpGet("/orders")]
    public async Task<IActionResult> Index(string? search = null, string? status = null, int page = 1)
    {
        ViewData.SetPageTitle("Siparişler");
        ViewData.SetActiveNav("orders");

        var result = await orderManager.GetOrdersAsync(new OrderPaginatedRequest
        {
            SearchTerm = search,
            Status = status,
            PageIndex = page - 1,
            PageSize = 20
        });

        ViewBag.Search = search;
        ViewBag.Status = status;

        if (Request.IsHtmx())
            return PartialView("~/Features/Orders/Views/Partials/_OrderTable.cshtml", result.Data);

        return View("~/Features/Orders/Views/Index.cshtml", result.Data);
    }

    [HttpGet("/marketplace/orders")]
    public async Task<IActionResult> MarketplaceOrders(int mp = 1, int page = 1)
    {
        ViewData.SetPageTitle("Pazaryeri Siparişleri");
        ViewData.SetActiveNav("orders");

        var result = await orderManager.GetOrdersAsync(new OrderPaginatedRequest
        {
            MarketPlaceId = mp,
            PageIndex = page - 1,
            PageSize = 20
        });

        ViewBag.MarketPlaceId = mp;

        if (Request.IsHtmx())
            return PartialView("~/Features/Orders/Views/Partials/_OrderTable.cshtml", result.Data);

        return View("~/Features/Orders/Views/MarketplaceOrders.cshtml", result.Data);
    }

    [HttpGet("/orders/{id:guid}")]
    public async Task<IActionResult> OrderDetail(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sipariş bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        var order = result.Data!;
        var title = order.OrderNumber ?? order.Id.ToString()[..8];

        ViewData.SetPageTitle($"Sipariş #{title}");
        ViewData.SetActiveNav("orders");
        ViewData.SetBreadcrumb(("Siparişler", "/orders"), ($"#{title}", null));

        return View("~/Features/Orders/Views/OrderDetail.cshtml", order);
    }

    [HttpGet("/orders/{id:guid}/print")]
    public async Task<IActionResult> Print(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sipariş bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        return View("~/Features/Orders/Views/Print.cshtml", result.Data!);
    }

    [HttpGet("/marketplace/orders/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sipariş bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        var order = result.Data!;
        var title = order.OrderNumber ?? order.Id.ToString()[..8];

        ViewData.SetPageTitle($"Sipariş #{title}");
        ViewData.SetActiveNav("orders");
        ViewData.SetBreadcrumb(("Siparişler", "/orders"), ($"#{title}", null));

        return View("~/Features/Orders/Views/Detail.cshtml", order);
    }

    [HttpPost("/orders/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromForm] string newStatus)
    {
        var result = await orderManager.UpdateOrderStatusAsync(id, newStatus);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sipariş durumu guncellendi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Durum guncellenemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Sipariş durumu basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Sipariş durumu guncellenemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/orders/{id:guid}/mark-unsupplied")]
    public async Task<IActionResult> MarkUnsupplied(Guid id)
    {
        var orderResult = await orderManager.GetOrderByIdAsync(id);
        if (!orderResult.Success || orderResult.Data is null)
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sipariş bulunamadi.", type = "danger" });
                return StatusCode(404);
            }

            TempData.SetError("Sipariş bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        var order = orderResult.Data;

        if (order.MarketPlaceId != 1 || order.ShipmentPackageId is null)
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Bu islem sadece Trendyol Siparişleri icin gecerlidir.", type = "danger" });
                return StatusCode(422);
            }

            TempData.SetError("Bu islem sadece Trendyol Siparişleri icin gecerlidir.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var lineIds = order.OrderItems
            .Where(i => i.LineId.HasValue)
            .Select(i => i.LineId!.Value)
            .ToList();

        if (lineIds.Count == 0)
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sipariş kalemlerinde satir ID bulunamadi.", type = "danger" });
                return StatusCode(422);
            }

            TempData.SetError("Sipariş kalemlerinde satir ID bulunamadi.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await trendyolOrderService.MarkUnsuppliedAsync(
            order.ShipmentPackageId.Value, lineIds);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sipariş tedarik edilemez olarak isaretlendi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Islem basarisiz.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Sipariş tedarik edilemez olarak isaretlendi.");
        else
            TempData.SetError(result.Message ?? "Sipariş tedarik edilemez olarak isaretlenemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }
}
