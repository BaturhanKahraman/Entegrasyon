using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.MVC.Features.Products.ViewModels;
using Entegrasyon.Entity.Dtos.Product.Discount;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Products;

[Authorize]
public class ProductController(
    IProductService productService,
    IBrandService brandService,
    ICategoryService categoryService,
    IImageManager imageManager,
    IDiscountManager discountManager,
    ILabelService labelService,
    IBulkOperationManager bulkOperationManager) : Controller
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

    // ── Edit ──────────────────────────────────────────────────────────

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

    [HttpPost("/products/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, EditProductVm vm)
    {
        if (!ModelState.IsValid)
        {
            var pageData = await productService.GetProductEditPageData(id);
            if (!pageData.Success) return RedirectToAction(nameof(Index));
            ViewData.SetPageTitle("Urun Duzenle");
            ViewData.SetActiveNav("products");
            return View(pageData.Data);
        }

        var dto = new EditProductDto(
            id,
            vm.Title,
            vm.Description ?? "",
            vm.StockCode ?? "",
            vm.Season ?? "",
            vm.Year ?? "",
            vm.BrandId,
            vm.CategoryId,
            new List<EditProductVariantDto>(),
            new List<AttributeKeyValueDto>(),
            new List<int>()
        );

        var result = await productService.UpdateProduct(dto);
        if (result.Success)
            TempData.SetSuccess("Urun basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Urun guncellenemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Create Wizard ─────────────────────────────────────────────────

    [HttpGet("/products/add")]
    public async Task<IActionResult> Create()
    {
        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Urunler", "/products"), ("Yeni Urun", null));

        await LoadCreateDropdowns();
        return View(new CreateProductVm());
    }

    [HttpPost("/products/add/step1")]
    public async Task<IActionResult> CreateStep1(CreateProductVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Title) || vm.BrandId == 0 || vm.CategoryId == 0)
        {
            await LoadCreateDropdowns();

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep1", vm);

            ViewData.SetPageTitle("Yeni Urun");
            ViewData.SetActiveNav("products");
            return View(nameof(Create), vm);
        }

        // Resolve brand/category names for the review step
        var brand = await brandService.GetBrandById(vm.BrandId);
        if (brand.Success) vm.BrandName = brand.Data!.Name;

        var categories = await categoryService.GetLeafCategoriesAsync();
        vm.CategoryName = categories.FirstOrDefault(c => c.Id == vm.CategoryId)?.Name;

        TempData["CreateProduct"] = JsonSerializer.Serialize(vm);

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep2", vm);

        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [HttpPost("/products/add/step2")]
    public IActionResult CreateStep2(CreateProductVm vm)
    {
        TempData["CreateProduct"] = JsonSerializer.Serialize(vm);

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep3Review", vm);

        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [HttpPost("/products/add/save")]
    public async Task<IActionResult> CreateSave()
    {
        var json = TempData.Peek("CreateProduct") as string;
        if (json is null) return RedirectToAction(nameof(Create));

        var vm = JsonSerializer.Deserialize<CreateProductVm>(json)!;

        var dto = new AddProductDto
        {
            Title = vm.Title,
            Description = vm.Description,
            StockCode = vm.StockCode,
            Season = vm.Season,
            Year = vm.Year,
            BrandId = vm.BrandId,
            CategoryId = vm.CategoryId,
            ProductVariants = vm.Variants.Select(v => new AddProductVariantDto
            {
                Barcode = v.Barcode,
                ListPrice = v.ListPrice,
                SalePrice = v.SalePrice,
                CostPrice = v.CostPrice,
                VatRate = v.VatRate,
                DimensionalWeight = v.DimensionalWeight,
                CurrencyType = "TRY",
                BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = v.Stock }]
            }).ToList()
        };

        var result = await productService.AddProduct(dto);
        if (result.Success)
        {
            TempData.SetSuccess($"'{vm.Title}' basariyla eklendi.");
            return RedirectToAction(nameof(Detail), new { id = result.Data!.Id });
        }

        TempData.SetError(result.Message ?? "Urun eklenemedi.");
        return RedirectToAction(nameof(Create));
    }

    // ── Other Actions ────────────────────────────────────────────────

    [HttpGet("/products/{id:guid}/sync")]
    public async Task<IActionResult> SyncDetail(Guid id)
    {
        var result = await productService.GetProductDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Urun bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Senkronizasyon Durumu");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Urunler", "/products"),
            (result.Data!.Title, $"/products/{id}"),
            ("Senkronizasyon", null));
        return View("~/Features/Products/Views/SyncDetail.cshtml", result.Data);
    }

    [HttpGet("/products/{id:guid}/sync/trendyol/send")]
    public async Task<IActionResult> TrendyolSend(Guid id)
    {
        var result = await productService.GetProductDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Urun bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Trendyol Gonderim");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Urunler", "/products"),
            (result.Data!.Title, $"/products/{id}"),
            ("Trendyol Gonderim", null));
        return View("~/Features/Products/Views/TrendyolSend.cshtml", result.Data);
    }

    [HttpGet("/products/{id:guid}/variants")]
    public async Task<IActionResult> Variants(Guid id)
    {
        var result = await productService.GetProductDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Urun bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Varyantlar");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Urunler", "/products"),
            (result.Data!.Title, $"/products/{id}"),
            ("Varyantlar", null));
        return View(result.Data);
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

    // ── Images ───────────────────────────────────────────────────────

    [HttpPost("/products/{id:guid}/images/upload")]
    public async Task<IActionResult> UploadImages(Guid id, List<IFormFile> files, Guid variantId, bool isMain = false)
    {
        if (files.Count == 0)
        {
            TempData.SetError("Dosya secilmedi.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var streams = files.Select((f, idx) => new VariantImageStream(
            variantId,
            f.OpenReadStream(),
            f.FileName,
            IsMain: idx == 0 && isMain
        ));

        var result = await imageManager.AddProductImages(id, streams);

        if (result.Success)
            TempData.SetSuccess($"{files.Count} gorsel yuklendi.");
        else
            TempData.SetError(result.Message ?? "Gorsel yuklenemedi.");

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("/products/{id:guid}/images/{imageId:int}/delete")]
    public IActionResult DeleteImage(Guid id, int imageId)
    {
        // TODO: implement when IImageManager has delete method
        TempData.SetWarning("Gorsel silme henuz desteklenmiyor.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── Discount ─────────────────────────────────────────────────────

    [HttpGet("/products/{id:guid}/discount")]
    public async Task<IActionResult> Discount(Guid id)
    {
        var result = await discountManager.GetDiscountPreviewAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return PartialView("Partials/_DiscountDialog", result.Data);
    }

    [HttpPost("/products/{id:guid}/discount")]
    public async Task<IActionResult> ApplyDiscount(Guid id, [FromForm] decimal discountPercentage, [FromForm] List<int> marketplaceIds)
    {
        var dto = new ApplyDiscountDto(id, discountPercentage, marketplaceIds);
        var result = await discountManager.ApplyDiscountAsync(dto);
        if (!result.Success)
        {
            Response.StatusCode = 422;
            return Content(result.Message ?? "Islem basarisiz.");
        }

        TempData.SetSuccess($"{result.Data!.VariantsUpdated} varyanta %{discountPercentage} indirim uyguland\u0131.");
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Barcode ─────────────────────────────────────────────────────

    [HttpGet("/products/{id:guid}/barcode")]
    public async Task<IActionResult> PrintBarcode(Guid id, [FromQuery] Guid? variantId)
    {
        if (variantId == null)
        {
            var product = await productService.GetProductDetailById(id);
            if (!product.Success) return NotFound();
            variantId = product.Data!.ProductVariantsDetails?.FirstOrDefault()?.Id;
            if (variantId == null) return BadRequest("Urun varyanti bulunamadi.");
        }

        var result = await labelService.GenerateProductLabel(variantId.Value);
        if (!result.Success) return BadRequest(result.Message);

        var ext = result.Data!.PrinterLanguage == "ZPL" ? "zpl" : "bin";
        return File(result.Data.RawBytes, "application/octet-stream", $"barcode-{variantId}.{ext}");
    }

    // ── Export ───────────────────────────────────────────────────────

    [HttpGet("/products/export")]
    public async Task<IActionResult> Export(int? categoryId = null, int? brandId = null)
    {
        var filter = new ExportFilterDto(CategoryId: categoryId, BrandId: brandId);
        var result = await bulkOperationManager.ExportProductsAsync(filter);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Export basarisiz.");
            return RedirectToAction(nameof(Index));
        }
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"urunler-{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task LoadCreateDropdowns()
    {
        var brands = await brandService.GetBrandListDetails();
        ViewBag.Brands = brands.Success ? brands.Data : new List<Entity.Dtos.Brand.BrandListDetailDto>();

        var categories = await categoryService.GetLeafCategoriesAsync();
        ViewBag.Categories = categories;
    }
}
