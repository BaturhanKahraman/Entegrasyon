using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Shipping;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Shipping;

[Authorize]
public class ShippingController(IShipmentTrackingManager shipmentTrackingManager) : Controller
{
    [HttpGet("/shipping")]
    public async Task<IActionResult> Index(
        string? status = null,
        int? cargoCompanyId = null,
        int page = 1)
    {
        ViewData.SetPageTitle("Kargo Takibi");
        ViewData.SetActiveNav("shipping");

        ShipmentStatus? parsedStatus = null;
        if (Enum.TryParse<ShipmentStatus>(status, out var s))
            parsedStatus = s;

        var result = await shipmentTrackingManager.GetAllShipmentsAsync(new ShipmentPaginatedRequest
        {
            Status = parsedStatus,
            CargoCompanyId = cargoCompanyId,
            PageIndex = page - 1,
            PageSize = 20
        });

        ViewBag.Status = status;

        if (Request.IsHtmx())
            return PartialView("Partials/_ShipmentTable", result.Data);

        return View(result.Data);
    }

    [HttpPost("/shipping/{id:long}/refresh")]
    public async Task<IActionResult> Refresh(long id)
    {
        var result = await shipmentTrackingManager.RefreshTrackingStatusAsync(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Takip durumu guncellendi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Guncellenemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Takip durumu guncellendi.");
        else
            TempData.SetError(result.Message ?? "Takip durumu guncellenemedi.");

        return RedirectToAction(nameof(Index));
    }
}
