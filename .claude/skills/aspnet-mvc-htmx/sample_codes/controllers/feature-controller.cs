// Features/Products/ProductController.cs
// Primary constructor DI + Feature folder + HTMX-aware + PRG
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.MVC.Features.Products.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Entegrasyon.MVC.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Products;

[Authorize]
public class ProductController(
    IProductService productService,
    IBrandService brandService,
    ICategoryService categoryService) : Controller
{
    // ── LIST (full + partial via HTMX) ────────────────────────────────
    [HttpGet("/products")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Ürünler");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Ürünler", null));

        var result = await productService.GetPageable(
            new SearchablePageDto(search ?? "", page - 1, 20));

        // Aynı endpoint, iki davranış
        if (Request.IsHtmx())
            return PartialView("Partials/_ProductTable", result.Data);

        ViewBag.Search = search;
        return View(result.Data);
    }

    // ── DETAIL ────────────────────────────────────────────────────────
    [HttpGet("/products/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await productService.GetDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Ürün bulunamadı.");
            return RedirectToAction(nameof(Index));        // PRG
        }

        ViewData.SetPageTitle(result.Data!.Title);
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Ürünler", "/products"), (result.Data.Title, null));
        return View(result.Data);
    }

    // ── CREATE (GET form) ─────────────────────────────────────────────
    [HttpGet("/products/add")]
    public async Task<IActionResult> Add()
    {
        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Ürünler", "/products"), ("Yeni", null));

        var vm = new CreateProductVm
        {
            Brands = await brandService.GetSelectListAsync(),
            Categories = await categoryService.GetLeafSelectListAsync()
        };
        return View(vm);
    }

    // ── CREATE (POST) ─────────────────────────────────────────────────
    // AutoValidationFilter ModelState'i otomatik handle eder.
    // Buraya gelen vm valid demektir.
    [HttpPost("/products/add")]
    public async Task<IActionResult> Add(CreateProductVm vm)
    {
        var dto = new AddProductDto(vm.Title, vm.StockCode, vm.CategoryId /* ... */);
        var result = await productService.Add(dto);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Ürün eklenemedi.");
            return RedirectToAction(nameof(Add));          // PRG
        }

        TempData.SetSuccess("Ürün oluşturuldu.");
        return RedirectToAction(nameof(Detail), new { id = result.Data!.Id });
    }

    // ── EDIT ──────────────────────────────────────────────────────────
    [HttpGet("/products/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await productService.GetEditPageData(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Ürün bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Ürün Düzenle");
        ViewData.SetActiveNav("products");
        return View(result.Data);
    }

    [HttpPost("/products/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, EditProductVm vm)
    {
        var dto = new EditProductDto(id, vm.Title /* ... */);
        var result = await productService.Update(dto);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Güncellenemedi.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData.SetSuccess("Ürün güncellendi.");
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── DELETE (HTMX-friendly) ────────────────────────────────────────
    [HttpDelete("/products/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await productService.Delete(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTrigger("productDeleted");
                Response.HtmxTriggerWithData("notyf:success", new { message = "Ürün silindi." });
                return new EmptyResult();    // satır direkt swap out olsun
            }
            // BusinessRuleExceptionHandler 422+alert döner; burada Success=false
            // genelde "tutarsızlık" anlamına gelir.
            Response.HtmxTriggerWithData("notyf:error", new { message = result.Message });
            return new EmptyResult();
        }

        if (result.Success) TempData.SetSuccess("Ürün silindi.");
        else TempData.SetError(result.Message ?? "Silinemedi.");
        return RedirectToAction(nameof(Index));
    }

    // ── WIZARD STEP (kendi validation'ı, AutoValidationFilter SKIP) ────
    [HttpPost("/products/add/step/{step:int}")]
    [SkipAutoValidation]
    public IActionResult Step(int step, CreateProductVm vm)
    {
        // Step'e özgü doğrulama
        if (step == 1 && string.IsNullOrWhiteSpace(vm.Title))
        {
            ModelState.AddModelError(nameof(vm.Title), "Başlık zorunlu.");
            return PartialView("Partials/_CreateStep1", vm);
        }
        return PartialView($"Partials/_CreateStep{step + 1}", vm);
    }
}
