using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

[Authorize]
public class BrandMappingController(
    IBrandMatchService brandMatchService,
    IBrandService brandService,
    IMarketPlaceManager marketPlaceManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/BrandMapping";

    [HttpGet("/marketplace/sync/brands")]
    public async Task<IActionResult> Index(int mp = 1, string? search = null, string? returnUrl = null)
    {
        ViewData.SetPageTitle("Marka Eslemesi");
        ViewData.SetActiveNav("marketplace-sync");

        var marketPlaces = await marketPlaceManager.GetAllAsync();
        var summary = await brandMatchService.GetBrandMappingsSummaryAsync();
        var mappings = await brandMatchService.GetAllBrandMappingsAsync(mp);
        var unmappedBrands = await brandMatchService.GetUnmappedBrandsAsync(mp);

        var vm = new BrandMappingVm
        {
            SelectedMarketPlaceId = mp,
            MarketPlaces = marketPlaces.Data ?? [],
            Summary = summary,
            Mappings = mappings,
            UnmappedBrands = unmappedBrands,
            SearchTerm = search,
            ReturnUrl = returnUrl
        };

        if (Request.IsHtmx() && Request.HtmxTarget() == "brand-list-container")
            return PartialView($"{ViewBase}/Partials/_BrandList.cshtml", vm);

        return View($"{ViewBase}/Index.cshtml", vm);
    }

    [HttpGet("/marketplace/sync/brands/{brandId:int}/detail")]
    public async Task<IActionResult> Detail(int brandId, int mp = 1, string? returnUrl = null)
    {
        var brandResult = await brandService.GetBrandById(brandId);
        if (!brandResult.Success || brandResult.Data is null)
            return NotFound();

        var brand = brandResult.Data;
        var mappings = await brandMatchService.GetAllBrandMappingsAsync(mp);
        var mapping = mappings.FirstOrDefault(m => m.ApplicationBrandId == brandId);

        var vm = new BrandDetailVm
        {
            BrandId = brand.Id,
            BrandName = brand.Name ?? string.Empty,
            MarketPlaceId = mp,
            IsMapped = mapping is not null,
            Mapping = mapping,
            ReturnUrl = returnUrl
        };

        return PartialView($"{ViewBase}/Partials/_BrandDetail.cshtml", vm);
    }

    [HttpPost("/marketplace/sync/brands/{brandId:int}/map")]
    public async Task<IActionResult> CreateMapping(int brandId, int mp, int externalBrandId, string? externalBrandName, string? returnUrl)
    {
        var dto = new CreateBrandMarketPlaceMatchDto
        {
            ApplicationBrandId = brandId,
            MarketPlaceId = mp,
            MarketPlaceBrandId = externalBrandId,
            MarketPlaceBrandName = externalBrandName
        };

        var result = await brandMatchService.CreateBrandMappingAsync(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    Response.HtmxTriggerWithData("showToast",
                        new { message = "Marka Eşleşmesi olusturuldu. Yonlendiriliyorsunuz...", type = "success" });
                    Response.Headers["HX-Redirect"] = returnUrl;
                    return Content("");
                }

                Response.HtmxTriggerWithData("showToast",
                    new { message = "Marka Eşleşmesi olusturuldu.", type = "success" });
                Response.HtmxTrigger("refreshList");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme olusturulamadi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Marka Eşleşmesi olusturuldu.");
        else
            TempData.SetError(result.Message ?? "Eslestirme olusturulamadi.");

        if (result.Success && !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index), new { mp });
    }

    [HttpPost("/marketplace/sync/brands/{brandId:int}/unmap")]
    public async Task<IActionResult> RemoveMapping(int brandId, int mp)
    {
        var result = await brandMatchService.RemoveBrandMappingAsync(brandId, mp);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Marka Eşleşmesi kaldirildi.", type = "success" });
                Response.HtmxTrigger("refreshList");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme kaldirilamadi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Marka Eşleşmesi kaldirildi.");
        else
            TempData.SetError(result.Message ?? "Eslestirme kaldirilamadi.");

        return RedirectToAction(nameof(Index), new { mp });
    }
}
