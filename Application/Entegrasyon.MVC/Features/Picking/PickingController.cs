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

    [HttpPost("/picking/{id:guid}/pack")]
    public async Task<IActionResult> Pack(Guid id)
    {
        var result = await orderManager.UpdateOrderStatusAsync(id, "Picking");

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Siparis hazirlama baslatildi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Durum guncellenemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Siparis hazirlama baslatildi.");
        else
            TempData.SetError(result.Message ?? "Siparis durumu guncellenemedi.");

        return RedirectToAction(nameof(Index));
    }
}
