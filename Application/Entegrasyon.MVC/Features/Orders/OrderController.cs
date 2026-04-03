using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Orders;

[Authorize]
public class OrderController(IOrderManager orderManager) : Controller
{
    [HttpGet("/orders")]
    public async Task<IActionResult> Index(string? search = null, string? status = null, int page = 1)
    {
        ViewData.SetPageTitle("Siparisler");
        ViewData.SetActiveNav("orders");

        var result = await orderManager.GetOrdersAsync(marketPlaceId: null, page: page - 1, pageSize: 20);

        var orders = result.Data ?? [];

        // Client-side filtering for search & status
        if (!string.IsNullOrWhiteSpace(search))
        {
            orders = orders
                .Where(o =>
                    (o.OrderNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (o.CustomerFirstName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (o.CustomerLastName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            orders = orders
                .Where(o => o.MarketplaceOrderStatus?.Equals(status, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        ViewBag.Search = search;
        ViewBag.Status = status;

        if (Request.IsHtmx())
            return PartialView("~/Features/Orders/Views/Partials/_OrderTable.cshtml", orders);

        return View("~/Features/Orders/Views/Index.cshtml", orders);
    }

    [HttpGet("/marketplace/orders")]
    public async Task<IActionResult> MarketplaceOrders(int mp = 1, int page = 1)
    {
        ViewData.SetPageTitle("Pazaryeri Siparisleri");
        ViewData.SetActiveNav("orders");

        var result = await orderManager.GetOrdersAsync(marketPlaceId: mp, page: page - 1, pageSize: 20);
        var orders = result.Data ?? [];

        ViewBag.MarketPlaceId = mp;

        if (Request.IsHtmx())
            return PartialView("~/Features/Orders/Views/Partials/_OrderTable.cshtml", orders);

        return View("~/Features/Orders/Views/MarketplaceOrders.cshtml", orders);
    }

    [HttpGet("/marketplace/orders/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Siparis bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        var order = result.Data!;
        var title = order.OrderNumber ?? order.Id.ToString()[..8];

        ViewData.SetPageTitle($"Siparis #{title}");
        ViewData.SetActiveNav("orders");
        ViewData.SetBreadcrumb(("Siparisler", "/orders"), ($"#{title}", null));

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
                    new { message = "Siparis durumu guncellendi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Durum guncellenemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Siparis durumu basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Siparis durumu guncellenemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }
}
