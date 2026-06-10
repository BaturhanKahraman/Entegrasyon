using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Requests;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Picking;

[Authorize]
public class PickingController(IOrderManager orderManager) : HtmxController
{
    [HttpGet("/picking")]
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData.SetPageTitle("Siparis Hazirlama");
        ViewData.SetActiveNav("picking");

        var result = await orderManager.GetOrdersAsync(new OrderPaginatedRequest
        {
            Status = "Created",
            PageIndex = page - 1,
            PageSize = 30
        });

        if (Request.IsHtmx())
            return PartialView("~/Features/Picking/Views/Partials/_PickingTable.cshtml", result.Data);

        return View("~/Features/Picking/Views/Index.cshtml", result.Data);
    }

    [HttpGet("/picking/{id:guid}/items")]
    public async Task<IActionResult> OrderItems(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success || result.Data is null)
            return Content("<div class='text-danger p-2'>Siparis bulunamadı.</div>", "text/html");

        return PartialView("~/Features/Picking/Views/Partials/_OrderItems.cshtml", result.Data);
    }

    [HttpPost("/picking/{id:guid}/pack")]
    public async Task<IActionResult> Pack(Guid id)
    {
        var result = await orderManager.UpdateOrderStatusAsync(id, "Picking");
        return HtmxMutationResult(result, "Siparis hazirlama baslatildi.", refreshEvent: "pickingChanged");
    }

    [HttpPost("/picking/batch-pack")]
    public async Task<IActionResult> BatchPack([FromForm] List<Guid> orderIds)
    {
        var successCount = 0;
        foreach (var orderId in orderIds)
        {
            var result = await orderManager.UpdateOrderStatusAsync(orderId, "Picking");
            if (result.Success) successCount++;
        }

        var msg = $"{successCount}/{orderIds.Count} siparis hazirlama baslatildi.";
        Response.HtmxTriggerWithData("showToast", new { message = msg, type = "success" });
        Response.HtmxTrigger("pickingChanged");
        return Content("");
    }
}
