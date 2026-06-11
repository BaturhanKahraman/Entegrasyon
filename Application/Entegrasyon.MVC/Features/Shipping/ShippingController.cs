using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Shipping;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Shipping;

[Authorize]
public class ShippingController(
    IShipmentTrackingManager shipmentTrackingManager,
    ICargoCompaniesManager cargoCompaniesManager) : HtmxController
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

        // KPI snapshot (global, tablo filtresinden bağımsız) yalnız tam-sayfa render'da.
        var kpis = await shipmentTrackingManager.GetShipmentKpisAsync();
        ViewBag.ShipmentInTransitCount = kpis.InTransitCount;
        ViewBag.ShipmentDeliveredCount = kpis.DeliveredCount;
        ViewBag.ShipmentProblemCount = kpis.ProblemCount;

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

    [HttpGet("/shipping/companies")]
    public async Task<IActionResult> Companies(string? search = null)
    {
        ViewData.SetPageTitle("Kargo Firmalari");
        ViewData.SetActiveNav("shipping");

        var result = string.IsNullOrWhiteSpace(search)
            ? await cargoCompaniesManager.GetCargoCompanies()
            : await cargoCompaniesManager.GetCargoCompanies(search);

        ViewBag.Search = search;

        if (Request.IsHtmx())
            return PartialView("Partials/_CompanyTable", result.Data);

        return View(result.Data);
    }

    [HttpPost("/shipping/companies/create")]
    public async Task<IActionResult> CreateCompany([FromForm] AddCargoCompanyDto dto)
    {
        var result = await cargoCompaniesManager.AddCargoCompany(dto);
        return HtmxMutationResult(result, "Kargo firmasi eklendi.", "Eklenemedi.", refreshEvent: "companyChanged");
    }

    [HttpPost("/shipping/companies/{id:int}/delete")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var result = await cargoCompaniesManager.DeleteCargoCompany(new CargoCompany { Id = id });
        return HtmxMutationResult(result, "Kargo firmasi silindi.", "Silinemedi.");
    }

    [HttpGet("/shipping/{id:long}")]
    public async Task<IActionResult> Detail(long id)
    {
        ViewData.SetPageTitle("Kargo Detay");
        ViewData.SetActiveNav("shipping");
        ViewData.SetBreadcrumb(("Kargo Takip", "/shipping"), ("Detay", null));

        var result = await shipmentTrackingManager.GetShipmentHistoryAsync(id);

        return View(result.Data ?? []);
    }
}
