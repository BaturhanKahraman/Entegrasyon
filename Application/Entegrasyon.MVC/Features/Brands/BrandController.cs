using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Requests;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Brands;

[Authorize]
public class BrandController(IBrandService brandService, IBrandMatchService brandMatchService) : HtmxController
{
    [HttpGet("/brands")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Markalar");
        ViewData.SetActiveNav("brands");

        var result = await brandService.GetBrandDetailPageable(
            new BrandDetailPaginatedRequest { SearchTerm = search, PageIndex = page - 1, PageSize = 20 });

        if (Request.IsHtmx())
            return PartialView("Partials/_BrandTable", result.Data);

        ViewBag.Search = search;
        return View(result.Data);
    }

    [HttpPost("/brands/create")]
    public async Task<IActionResult> Create([FromForm] string name)
    {
        var result = await brandService.AddBrand(new AddBrandDto { Name = name });

        return HtmxMutationResult(result, "Marka eklendi.", "Eklenemedi.", refreshEvent: "brandChanged");
    }

    [HttpGet("/brands/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await brandService.GetBrandDetail(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Marka bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        var mappings = await brandMatchService.GetBrandMappingsByBrandIdAsync(id);

        ViewData.SetPageTitle(result.Data!.Name);
        ViewData.SetActiveNav("brands");
        ViewData.SetBreadcrumb(("Markalar", "/brands"), (result.Data.Name, null));

        ViewBag.Mappings = mappings.Success ? mappings.Data : new List<Entity.Dtos.Brand.BrandMarketPlaceMatchDto>();
        return View(result.Data);
    }

    [HttpPost("/brands/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await brandService.DeleteBrand(id);

        return HtmxMutationResult(result, "Marka silindi.", "Silinemedi.");
    }
}
