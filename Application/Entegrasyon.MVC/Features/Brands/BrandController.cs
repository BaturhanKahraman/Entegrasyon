using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Requests;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Brands;

[Authorize]
public class BrandController(IBrandService brandService) : Controller
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

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Marka eklendi.", type = "success" });
                Response.Headers.Append("HX-Trigger", "brandChanged");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eklenemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Marka basariyla eklendi.");
        else
            TempData.SetError(result.Message ?? "Marka eklenemedi.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/brands/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await brandService.DeleteBrand(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Marka silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Marka basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Marka silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
