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
public class BrandController(IBrandService brandService) : HtmxController
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

    [HttpPost("/brands/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await brandService.DeleteBrand(id);

        return HtmxMutationResult(result, "Marka silindi.", "Silinemedi.");
    }
}
