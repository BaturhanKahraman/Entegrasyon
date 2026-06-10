using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

[Authorize]
public class CommissionRatesController(
    ICommissionCalculator commissionCalculator,
    IMarketPlaceManager marketPlaceManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/CommissionRates";

    [HttpGet("/marketplace/commission-rates")]
    public async Task<IActionResult> Index(int mp = 1)
    {
        ViewData.SetPageTitle("Komisyon Oranlari");
        ViewData.SetActiveNav("commission-rates");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyonu", "/marketplace/sync"),
            ("Komisyon Oranlari", null));

        var marketPlaces = await marketPlaceManager.GetAllAsync();
        var ratesResult = await commissionCalculator.GetCommissionRatesAsync(mp);

        var vm = new CommissionRatesVm
        {
            SelectedMarketPlaceId = mp,
            MarketPlaces = marketPlaces.Data ?? [],
            Rates = ratesResult.Success ? ratesResult.Data ?? [] : []
        };

        if (Request.IsHtmx() && Request.HtmxTarget() == "rates-table-container")
            return PartialView($"{ViewBase}/Partials/_RatesTable.cshtml", vm);

        return View($"{ViewBase}/Index.cshtml", vm);
    }

    [HttpPost("/marketplace/commission-rates/save")]
    public async Task<IActionResult> Create(SaveCommissionRateDto dto)
    {
        var result = await commissionCalculator.SaveCommissionRateAsync(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Komisyon orani kaydedildi.", type = "success" });
                Response.HtmxTrigger("refreshRates");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Komisyon orani kaydedilemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Komisyon orani kaydedildi.");
        else
            TempData.SetError(result.Message ?? "Komisyon orani kaydedilemedi.");

        return RedirectToAction(nameof(Index), new { mp = dto.MarketPlaceId });
    }

    [HttpPost("/marketplace/commission-rates/{rateId:int}/edit")]
    public async Task<IActionResult> Edit(int rateId, SaveCommissionRateDto dto)
    {
        dto.Id = rateId;
        var result = await commissionCalculator.SaveCommissionRateAsync(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Komisyon orani guncellendi.", type = "success" });
                Response.HtmxTrigger("refreshRates");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Komisyon orani guncellenemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Komisyon orani guncellendi.");
        else
            TempData.SetError(result.Message ?? "Komisyon orani guncellenemedi.");

        return RedirectToAction(nameof(Index), new { mp = dto.MarketPlaceId });
    }

    [HttpPost("/marketplace/commission-rates/{rateId:int}/delete")]
    public async Task<IActionResult> Delete(int rateId, int mp = 1)
    {
        var result = await commissionCalculator.DeleteCommissionRateAsync(rateId);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Komisyon orani silindi.", type = "success" });
                Response.HtmxTrigger("refreshRates");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Komisyon orani silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Komisyon orani silindi.");
        else
            TempData.SetError(result.Message ?? "Komisyon orani silinemedi.");

        return RedirectToAction(nameof(Index), new { mp });
    }
}
