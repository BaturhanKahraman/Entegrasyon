using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Categories;

[Authorize]
public class CategoryController(
    ICategoryService categoryService,
    IProductService productService) : Controller
{
    /// <summary>Split layout: tree on left, detail on right</summary>
    [HttpGet("/categories")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Kategoriler");
        ViewData.SetActiveNav("categories");

        var categories = await categoryService.GetAllCategoriesWithHierarchyAsync();
        return View(categories);
    }

    /// <summary>HTMX: tree node clicked -> load detail panel</summary>
    [HttpGet("/categories/{id:int}/detail")]
    public async Task<IActionResult> Detail(int id)
    {
        var category = await categoryService.GetCategoryDetailById(id);
        if (category is null) return NotFound();

        var productCount = await productService.GetProductCountByCategoryId(id);
        ViewBag.ProductCount = productCount;
        return PartialView("Partials/_CategoryDetail", category);
    }

    /// <summary>Create form</summary>
    [HttpGet("/categories/add")]
    public async Task<IActionResult> Create()
    {
        ViewData.SetPageTitle("Yeni Kategori");
        ViewData.SetActiveNav("categories");

        var parents = await categoryService.GetValidParentCandidatesAsync();
        ViewBag.Parents = parents;
        return View();
    }

    /// <summary>Edit page</summary>
    [HttpGet("/categories/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await categoryService.GetCategoryEditPageData(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Kategori bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Kategori Duzenle");
        ViewData.SetActiveNav("categories");
        return View(result.Data);
    }

    /// <summary>Category import page (multi-marketplace)</summary>
    [HttpGet("/categories/import")]
    public IActionResult Import()
    {
        ViewData.SetPageTitle("Kategori Aktarimi");
        ViewData.SetActiveNav("categories");
        ViewData.SetBreadcrumb(("Kategoriler", "/categories"), ("Aktarim", null));
        return View();
    }

    /// <summary>HTMX: soft delete</summary>
    [HttpPost("/categories/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await categoryService.SoftDelete(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Kategori silindi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Kategori silindi.");
        else
            TempData.SetError(result.Message ?? "Kategori silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
