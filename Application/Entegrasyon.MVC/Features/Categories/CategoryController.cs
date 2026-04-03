using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Features.Categories.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Categories;

[Authorize]
public class CategoryController(
    ICategoryService categoryService,
    ICategoryAttributeManager categoryAttributeManager,
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

    // ── Create Wizard ─────────────────────────────────────────────────

    /// <summary>Create wizard - Step 1</summary>
    [HttpGet("/categories/add")]
    public async Task<IActionResult> Create()
    {
        ViewData.SetPageTitle("Yeni Kategori");
        ViewData.SetActiveNav("categories");
        ViewData.SetBreadcrumb(("Kategoriler", "/categories"), ("Yeni Kategori", null));

        var parents = await categoryService.GetValidParentCandidatesAsync();
        ViewBag.Parents = parents;
        return View(new CategoryCreateVm());
    }

    /// <summary>Create wizard - validate Step 1, show Step 2 (attributes)</summary>
    [HttpPost("/categories/add/step1")]
    public async Task<IActionResult> CreateStep1(CategoryCreateVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            var parents = await categoryService.GetValidParentCandidatesAsync();
            ViewBag.Parents = parents;

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep1", vm);

            ViewData.SetPageTitle("Yeni Kategori");
            ViewData.SetActiveNav("categories");
            return View(nameof(Create), vm);
        }

        // Resolve parent name for review
        if (vm.SuperCategoryId.HasValue)
        {
            var parentDetail = await categoryService.GetCategoryDetailById(vm.SuperCategoryId.Value);
            vm.SuperCategoryName = parentDetail?.Name;
        }

        TempData["CreateCategory"] = JsonSerializer.Serialize(vm);

        // Load available attributes for Step 2
        var attrsResult = await categoryAttributeManager.GetCategoryAttributes();
        var allAttrs = attrsResult.Success ? attrsResult.Data! : [];

        vm.Attributes = allAttrs.Select(a => new CategoryAttributeSelectionVm
        {
            AttributeId = a.Id,
            AttributeName = a.CategoryAttributeHumanized ?? a.CategoryAttributeKey ?? $"Attr#{a.Id}",
            AllowCustom = a.AllowCustom
        }).ToList();

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep2Attributes", vm);

        ViewData.SetPageTitle("Yeni Kategori");
        ViewData.SetActiveNav("categories");
        return View(nameof(Create), vm);
    }

    /// <summary>Create wizard - validate Step 2, show Step 3 (review)</summary>
    [HttpPost("/categories/add/step2")]
    public IActionResult CreateStep2(CategoryCreateVm vm)
    {
        // Restore base fields from TempData
        var json = TempData.Peek("CreateCategory") as string;
        if (json is not null)
        {
            var saved = JsonSerializer.Deserialize<CategoryCreateVm>(json)!;
            vm.Name = saved.Name;
            vm.SuperCategoryId = saved.SuperCategoryId;
            vm.SuperCategoryName = saved.SuperCategoryName;
            vm.IsFavorite = saved.IsFavorite;
            vm.DefaultVatRate = saved.DefaultVatRate;
        }

        // Keep only selected attributes
        vm.Attributes = vm.Attributes.Where(a => a.Selected).ToList();

        TempData["CreateCategory"] = JsonSerializer.Serialize(vm);

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep3Review", vm);

        ViewData.SetPageTitle("Yeni Kategori");
        ViewData.SetActiveNav("categories");
        return View(nameof(Create), vm);
    }

    /// <summary>Create wizard - final save</summary>
    [HttpPost("/categories/add/save")]
    public async Task<IActionResult> CreateSave()
    {
        var json = TempData.Peek("CreateCategory") as string;
        if (json is null) return RedirectToAction(nameof(Create));

        var vm = JsonSerializer.Deserialize<CategoryCreateVm>(json)!;

        var dto = new AddCategoryDto(
            vm.Name,
            vm.Attributes.Select(a => new AddCategoryAttributeDto
            {
                Id = a.AttributeId,
                IsRequired = a.IsRequired,
                IsVarianter = a.IsVarianter,
                IsSlicer = a.IsSlicer,
                AllowCustom = a.AllowCustom,
                CategoryAttributeKey = a.AttributeName,
                CategoryAttributeHumanized = a.AttributeName
            }),
            vm.SuperCategoryId,
            vm.IsFavorite,
            vm.DefaultVatRate
        );

        var result = await categoryService.AddCategory(dto);
        if (result.Success)
        {
            TempData.SetSuccess($"'{vm.Name}' kategorisi basariyla olusturuldu.");
            return RedirectToAction(nameof(Index));
        }

        TempData.SetError(result.Message ?? "Kategori olusturulamadi.");
        return RedirectToAction(nameof(Create));
    }

    // ── Edit ──────────────────────────────────────────────────────────

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

    [HttpPost("/categories/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, [FromForm] string name, [FromForm] int? superCategoryId,
        [FromForm] bool isFavorite, [FromForm] decimal? defaultVatRate, [FromForm] bool isImported)
    {
        var dto = new EditCategoryDto(id, name, superCategoryId, isFavorite, isImported, defaultVatRate);
        var result = await categoryService.UpdateCategory(dto);

        if (result.Success)
            TempData.SetSuccess("Kategori basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Kategori guncellenemedi.");

        return RedirectToAction(nameof(Index));
    }

    // ── Import ────────────────────────────────────────────────────────

    /// <summary>Category import page (multi-marketplace)</summary>
    [HttpGet("/categories/import")]
    public IActionResult Import()
    {
        ViewData.SetPageTitle("Kategori Aktarimi");
        ViewData.SetActiveNav("categories");
        ViewData.SetBreadcrumb(("Kategoriler", "/categories"), ("Aktarim", null));
        return View();
    }

    [HttpPost("/categories/import/{marketplace}")]
    public IActionResult TriggerImport(string marketplace)
    {
        TempData.SetSuccess($"{marketplace} kategori import'u baslatildi.");
        return RedirectToAction(nameof(Import));
    }

    // ── Delete ────────────────────────────────────────────────────────

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
