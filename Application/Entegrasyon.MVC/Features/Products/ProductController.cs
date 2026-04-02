using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Products;

[Authorize]
public class ProductController(IProductService productService) : Controller
{
    [HttpGet("/products")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Urunler");
        ViewData.SetActiveNav("products");

        var result = await productService.GetProductsDetailsPageable(
            new SearchablePageDto(search ?? "", page - 1, 20));

        if (Request.IsHtmx())
            return PartialView("Partials/_ProductTable", result.Data);

        ViewBag.Search = search;
        return View(result.Data);
    }

    [HttpGet("/products/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await productService.GetProductDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Urun bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle(result.Data!.Title);
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Urunler", "/products"), (result.Data.Title, null));
        return View(result.Data);
    }

    [HttpGet("/products/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await productService.GetProductEditPageData(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Urun bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Urun Duzenle");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Urunler", "/products"), ("Duzenle", null));
        return View(result.Data);
    }

    [HttpGet("/products/add")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Urunler", "/products"), ("Yeni Urun", null));
        return View();
    }

    [HttpPost("/products/{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await productService.SoftDeleteProduct(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Urun silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Urun basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Urun silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
