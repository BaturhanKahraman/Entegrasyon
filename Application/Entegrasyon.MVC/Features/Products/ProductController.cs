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
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Entegrasyon.MVC.Infrastructure.Filters;

namespace Entegrasyon.MVC.Features.Products;

[Authorize]
public class ProductController(
    IProductService productService,
    IBrandService brandService,
    ICategoryService categoryService,
    IImageManager imageManager,
    IDiscountManager discountManager,
    ILabelService labelService,
    IBulkOperationManager bulkOperationManager,
    IProductSyncManager productSyncManager,
    ITrendyolProductService trendyolProductService,
    IMarketplaceOverrideManager marketplaceOverrideManager,
    IProductVariantManager productVariantManager,
    ICategoryAttributeManager categoryAttributeManager,
    IBranchOfficeManager branchOfficeManager) : Controller
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

    [SkipAutoValidation]
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

        // Load category attributes for Step 2
        var attrResult = await categoryAttributeManager.GetCategoryAttributesByCategory(vm.CategoryId);
        var attrs = attrResult.Success ? attrResult.Data! : [];
        ViewBag.NonVariantAttributes = attrs.Where(a => !a.IsVarianter && !a.IsSlicer).ToList();

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep2Attributes", vm);

        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step2")]
    public async Task<IActionResult> CreateStep2(CreateProductVm vm)
    {
        // Validate required category attributes
        var missing = vm.CategoryAttributes
            .Where(a => a.IsRequired && (a.ValueId is null or 0) && string.IsNullOrWhiteSpace(a.CustomValue))
            .ToList();

        if (missing.Count > 0)
        {
            var attrResult = await categoryAttributeManager.GetCategoryAttributesByCategory(vm.CategoryId);
            var attrs = attrResult.Success ? attrResult.Data! : [];
            ViewBag.NonVariantAttributes = attrs.Where(a => !a.IsVarianter && !a.IsSlicer).ToList();

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep2Attributes", vm);

            ViewData.SetPageTitle("Yeni Urun");
            ViewData.SetActiveNav("products");
            return View(nameof(Create), vm);
        }

        TempData["CreateProduct"] = JsonSerializer.Serialize(vm);

        // Load varianter/slicer attributes for Step 3
        var allAttrs = await categoryAttributeManager.GetCategoryAttributesByCategory(vm.CategoryId);
        ViewBag.VariantAttributes = (allAttrs.Success ? allAttrs.Data! : [])
            .Where(a => a.IsVarianter || a.IsSlicer).ToList();

        var branches = await branchOfficeManager.GetBranchList();
        ViewBag.BranchOffices = branches.Data ?? [];

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep3Variants", vm);

        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/generate-variants")]
    public async Task<IActionResult> GenerateVariants([FromForm] CreateProductVm vm)
    {
        vm.Variants = CreateProductVm.GenerateVariants(vm.VariantAttributeSelections, vm.DefaultValues);

        // Load branch offices for stock inputs
        var branches = await branchOfficeManager.GetBranchList();
        ViewBag.BranchOffices = branches.Data ?? [];

        return PartialView("Partials/_VariantTable", vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step3")]
    public async Task<IActionResult> CreateStep3(CreateProductVm vm)
    {
        if (vm.Variants.Count == 0)
        {
            TempData.SetError("En az bir varyant olusturulmalidir.");
            return RedirectToAction(nameof(Create));
        }

        TempData["CreateProduct"] = JsonSerializer.Serialize(vm);

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep4Images", vm);

        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [HttpPost("/products/add/upload-temp-image")]
    public async Task<IActionResult> UploadTempImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return Json(new { success = false, message = "Dosya bulunamadi." });

        var tempKey = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
        var tempDir = Path.Combine(Path.GetTempPath(), "product-wizard-images");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, tempKey);

        await using var stream = new FileStream(tempPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Json(new { success = true, tempKey, fileName = file.FileName });
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step4")]
    public IActionResult CreateStep4(CreateProductVm vm)
    {
        TempData["CreateProduct"] = JsonSerializer.Serialize(vm);

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep5Review", vm);

        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step5")]
    public IActionResult CreateStep5(CreateProductVm vm)
    {
        TempData["CreateProduct"] = JsonSerializer.Serialize(vm);

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep6Publish", vm);

        ViewData.SetPageTitle("Yeni Urun");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step6")]
    public async Task<IActionResult> CreateStep6(CreateProductVm vm)
    {
        TempData["CreateProduct"] = JsonSerializer.Serialize(vm);
        return await CreateSave();
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/save")]
    public async Task<IActionResult> CreateSave()
    {
        var json = TempData.Peek("CreateProduct") as string;
        if (json is null) return RedirectToAction(nameof(Create));

        var vm = JsonSerializer.Deserialize<CreateProductVm>(json)!;

        var attributeKeyValues = vm.CategoryAttributes
            .Where(a => a.ValueId > 0 || !string.IsNullOrWhiteSpace(a.CustomValue))
            .Select(a => new Entity.Categories.AttributeKeyValue
            {
                CategoryAttributeId = a.CategoryAttributeId,
                AttributeValueId = a.ValueId > 0 ? a.ValueId : null,
                CustomValue = a.CustomValue
            }).ToList();

        var dto = new AddProductDto
        {
            Title = vm.Title,
            Description = vm.Description,
            StockCode = vm.StockCode,
            Season = vm.Season,
            Year = vm.Year,
            BrandId = vm.BrandId,
            CategoryId = vm.CategoryId,
            SeoTitle = vm.SeoTitle,
            SeoDescription = vm.SeoDescription,
            SeoSlug = vm.SeoSlug,
            SeoKeywords = vm.SeoKeywords,
            AttributeKeyValues = attributeKeyValues,
            ProductVariants = vm.Variants.Select(v => new AddProductVariantDto
            {
                Barcode = v.Barcode,
                ListPrice = v.ListPrice,
                SalePrice = v.SalePrice,
                CostPrice = v.CostPrice,
                VatRate = v.VatRate,
                DimensionalWeight = v.DimensionalWeight,
                ECommercePrice = v.ECommercePrice,
                CurrencyType = "TRY",
                ProductVariantAttributes = v.VariantAttributes.Select(va =>
                    new Entity.Products.ProductVariantAttribute
                    {
                        CategoryAttributeValueId = va.ValueId,
                        CategoryAttributeValue = va.ValueName,
                        CustomValue = va.IsCustom ? va.ValueName : null,
                        IsVarianter = va.IsVarianter,
                        IsSlicer = va.IsSlicer
                    }).ToList(),
                BranchOfficeStocks = v.BranchOfficeStocks.Count > 0
                    ? v.BranchOfficeStocks
                        .Where(s => s.Stock > 0)
                        .Select(s => new AddBranchOfficeStockDto { BranchOfficeId = s.BranchOfficeId, FirstTotalStock = s.Stock })
                        .ToList()
                    : []
            }).ToList()
        };

        var result = await productService.AddProduct(dto);

        if (result.Success)
        {
            var product = result.Data!;
            var imageCount = 0;

            foreach (var assignment in vm.ImageAssignments.Where(a => a.TempImageKeys.Count > 0))
            {
                if (assignment.VariantIndex >= product.ProductVariants.Count) continue;
                var variant = product.ProductVariants.ElementAt(assignment.VariantIndex);
                var streams = new List<VariantImageStream>();
                var tempDir = Path.Combine(Path.GetTempPath(), "product-wizard-images");

                for (int i = 0; i < assignment.TempImageKeys.Count; i++)
                {
                    var tempPath = Path.Combine(tempDir, assignment.TempImageKeys[i]);
                    if (!System.IO.File.Exists(tempPath)) continue;
                    var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                    streams.Add(new VariantImageStream(
                        variant.Id, fs, assignment.TempImageKeys[i],
                        IsMain: i == (assignment.MainImageIndex ?? 0)));
                    imageCount++;
                }

                if (streams.Count > 0)
                    await imageManager.AddProductImages(product.Id, streams);
            }

            // Cleanup temp files
            var tempCleanDir = Path.Combine(Path.GetTempPath(), "product-wizard-images");
            if (Directory.Exists(tempCleanDir))
            {
                foreach (var f in Directory.GetFiles(tempCleanDir))
                    System.IO.File.Delete(f);
            }

            TempData.Remove("CreateProduct");

            ViewBag.ProductId = product.Id;
            ViewBag.ProductTitle = vm.Title;
            ViewBag.VariantCount = vm.Variants.Count;
            ViewBag.ImageCount = imageCount;

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep7Success");

            TempData.SetSuccess($"'{vm.Title}' basariyla eklendi.");
            return RedirectToAction(nameof(Detail), new { id = product.Id });
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

        var vm = new TrendyolSendVm
        {
            ProductId = id,
            ProductTitle = result.Data!.Title
        };

        // Preflight checks
        var preflightResult = await productSyncManager.GetSendPreflightAsync(id, marketPlaceId: 1);
        if (preflightResult.Success && preflightResult.Data is not null)
        {
            vm.Preflight = preflightResult.Data;

            // Load existing overrides
            var overrideResult = await marketplaceOverrideManager.GetOverridesAsync(id, marketPlaceId: 1);
            if (overrideResult.Success && overrideResult.Data is not null)
            {
                vm.TitleOverride = overrideResult.Data.TitleOverride;
                vm.DescriptionOverride = overrideResult.Data.DescriptionOverride;

                // Variant price overrides
                vm.Variants = result.Data.ProductVariantsDetails.Select(v =>
                {
                    var ovr = overrideResult.Data.VariantOverrides?
                        .FirstOrDefault(o => o.ProductVariantId == v.Id);
                    return new VariantOverrideVm
                    {
                        VariantId = v.Id,
                        Barcode = v.Barcode,
                        ListPrice = v.ListPrice,
                        SalePrice = v.SalePrice,
                        ListPriceOverride = ovr?.ListPriceOverride,
                        SalePriceOverride = ovr?.SalePriceOverride
                    };
                }).ToList();
            }
            else
            {
                vm.Variants = result.Data.ProductVariantsDetails.Select(v => new VariantOverrideVm
                {
                    VariantId = v.Id,
                    Barcode = v.Barcode,
                    ListPrice = v.ListPrice,
                    SalePrice = v.SalePrice
                }).ToList();
            }

            // If all preflight checks pass, load preview
            if (vm.PreflightPassed)
            {
                var overrides = new MarketplaceOverrideDetailDto
                {
                    MarketPlaceId = 1,
                    MarketPlaceName = "Trendyol",
                    TitleOverride = vm.TitleOverride,
                    DescriptionOverride = vm.DescriptionOverride
                };
                var previewResult = await trendyolProductService.GetSendPreviewAsync(id, overrides);
                if (previewResult.Success && previewResult.Data is not null)
                    vm.Preview = previewResult.Data;
            }
        }

        ViewData.SetPageTitle("Trendyol Gonderim");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Urunler", "/products"),
            (result.Data.Title, $"/products/{id}"),
            ("Trendyol Gonderim", null));
        return View("~/Features/Products/Views/TrendyolSend.cshtml", vm);
    }

    [HttpPost("/products/{id:guid}/sync/trendyol/send")]
    public async Task<IActionResult> TrendyolSendPost(Guid id, TrendyolSendVm vm)
    {
        // Save overrides
        var variantOverrides = vm.Variants?
            .Where(v => v.ListPriceOverride.HasValue || v.SalePriceOverride.HasValue)
            .Select(v => new Entity.Dtos.Product.Marketplace.VariantPriceOverrideDto
            {
                ProductVariantId = v.VariantId,
                ListPriceOverride = v.ListPriceOverride,
                SalePriceOverride = v.SalePriceOverride
            }).ToList() ?? [];

        if (!string.IsNullOrWhiteSpace(vm.TitleOverride) || !string.IsNullOrWhiteSpace(vm.DescriptionOverride) || variantOverrides.Count > 0)
        {
            var saveDto = new SaveMarketplaceOverridesDto
            {
                ProductId = id,
                MarketPlaceId = 1,
                TitleOverride = vm.TitleOverride?.Trim(),
                DescriptionOverride = vm.DescriptionOverride?.Trim(),
                VariantOverrides = variantOverrides
            };
            await marketplaceOverrideManager.SaveOverridesAsync(saveDto);
        }

        // Queue the product for sync
        var result = await productSyncManager.SyncProductAsync(id, marketPlaceId: 1);
        if (result.Success)
            TempData.SetSuccess(result.Message ?? "Urun Trendyol'a gonderim icin kuyruga eklendi.");
        else
            TempData.SetError(result.Message ?? "Gonderim sirasinda bir hata olustu.");

        return RedirectToAction(nameof(SyncDetail), new { id });
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

        var syncDetail = await productSyncManager.GetProductSyncDetailAsync(id);
        if (syncDetail.Success)
            ViewBag.SyncDetail = syncDetail.Data;

        ViewData.SetPageTitle("Varyantlar");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Urunler", "/products"),
            (result.Data!.Title, $"/products/{id}"),
            ("Varyantlar", null));
        return View(result.Data);
    }

    [HttpGet("/products/{productId:guid}/variants/{variantId:guid}/detail")]
    public async Task<IActionResult> VariantDetail(Guid productId, Guid variantId)
    {
        var result = await productVariantManager.GetVariantDetailPage(variantId);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Varyant bulunamadi.");
            return RedirectToAction(nameof(Variants), new { id = productId });
        }

        ViewData.SetPageTitle($"Varyant — {result.Data!.Barcode}");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Urunler", "/products"),
            (result.Data.ProductTitle, $"/products/{productId}"),
            ("Varyantlar", $"/products/{productId}/variants"),
            (result.Data.Barcode, null));
        return View(result.Data);
    }

    // ── Variant CRUD ─────────────────────────────────────────────────

    [HttpGet("/products/{productId:guid}/variants/add")]
    public async Task<IActionResult> VariantAdd(Guid productId)
    {
        var otherImages = await GetOtherVariantImages(productId, excludeVariantId: null);
        var vm = new VariantFormVm(productId, null, otherImages);
        return PartialView("Partials/_VariantAddDialog", vm);
    }

    [HttpPost("/products/{productId:guid}/variants/add")]
    public async Task<IActionResult> VariantAddPost(
        Guid productId,
        [FromForm] AddProductVariantDto dto,
        List<IFormFile> files,
        [FromForm] List<int> existingImageIds)
    {
        var result = await productVariantManager.AddVariant(productId, dto);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Varyant eklenemedi.");
            return RedirectToAction(nameof(Variants), new { id = productId });
        }

        // Yeni eklenen varyantın ID'sini bulmak için son varyantı çek
        var product = await productService.GetProductDetailById(productId);
        var newVariant = product.Data?.ProductVariantsDetails?.OrderByDescending(v => v.Id).FirstOrDefault();
        if (newVariant is not null)
        {
            // Yeni resim yükle
            if (files.Count > 0)
            {
                var streams = files.Select((f, idx) => new VariantImageStream(
                    newVariant.Id, f.OpenReadStream(), f.FileName, IsMain: idx == 0));
                await imageManager.AddProductImages(productId, streams);
            }

            // Mevcut resimlerden kopyala
            if (existingImageIds.Count > 0)
                await imageManager.CloneImagesToVariant(newVariant.Id, existingImageIds);
        }

        TempData.SetSuccess("Varyant basariyla eklendi.");
        return RedirectToAction(nameof(Variants), new { id = productId });
    }

    [HttpGet("/products/{productId:guid}/variants/{variantId:guid}/edit")]
    public async Task<IActionResult> VariantEdit(Guid productId, Guid variantId)
    {
        var result = await productVariantManager.GetVariantEditDetail(variantId);
        if (!result.Success)
            return StatusCode(404, result.Message);

        var otherImages = await GetOtherVariantImages(productId, excludeVariantId: variantId);
        var vm = new VariantFormVm(productId, result.Data, otherImages);
        return PartialView("Partials/_VariantEditDialog", vm);
    }

    [HttpPost("/products/{productId:guid}/variants/{variantId:guid}/edit")]
    public async Task<IActionResult> VariantEditPost(
        Guid productId,
        Guid variantId,
        [FromForm] EditProductVariantDto dto,
        List<IFormFile> files,
        [FromForm] List<int> existingImageIds)
    {
        var result = await productVariantManager.UpdateVariant(dto);

        // Yeni resim yükle
        if (files.Count > 0)
        {
            var streams = files.Select((f, idx) => new VariantImageStream(
                variantId, f.OpenReadStream(), f.FileName, IsMain: false));
            await imageManager.AddProductImages(productId, streams);
        }

        // Mevcut resimlerden kopyala
        if (existingImageIds.Count > 0)
            await imageManager.CloneImagesToVariant(variantId, existingImageIds);

        if (result.Success)
            TempData.SetSuccess("Varyant basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Varyant guncellenemedi.");

        return RedirectToAction(nameof(Variants), new { id = productId });
    }

    [HttpPost("/products/{productId:guid}/variants/{variantId:guid}/delete")]
    public async Task<IActionResult> VariantDelete(Guid productId, Guid variantId)
    {
        var result = await productVariantManager.SoftDeleteVariant(variantId);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Varyant silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Varyant silindi.");
        else
            TempData.SetError(result.Message ?? "Varyant silinemedi.");

        return RedirectToAction(nameof(Variants), new { id = productId });
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

    // ── Search / Quick-Add ─────────────────────────────────────────

    [HttpGet("/products/add/brand-search")]
    public async Task<IActionResult> BrandSearch([FromQuery] string q)
    {
        var brands = await brandService.GetBrandListDetails();
        var filtered = (brands.Data ?? [])
            .Where(b => b.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToList();
        return Json(filtered.Select(b => new { b.Id, b.Name }));
    }

    [HttpPost("/products/add/brand-quick-add")]
    public async Task<IActionResult> BrandQuickAdd([FromForm] string brandName)
    {
        var result = await brandService.AddBrand(new Entity.Dtos.Brand.AddBrandDto { Name = brandName });
        if (!result.Success)
            return Json(new { success = false, message = result.Message });
        var brand = ((Entity.Results.SuccessDataResult<Entity.Brands.Brand>)result).Data;
        return Json(new { success = true, id = brand!.Id, name = brand.Name });
    }

    [HttpGet("/products/add/category-search")]
    public async Task<IActionResult> CategorySearch([FromQuery] string q)
    {
        var categories = await categoryService.GetLeafCategoriesAsync();
        var filtered = categories
            .Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToList();
        return Json(filtered.Select(c => new { c.Id, c.Name }));
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task LoadCreateDropdowns()
    {
        var brands = await brandService.GetBrandListDetails();
        ViewBag.Brands = brands.Success ? brands.Data : new List<Entity.Dtos.Brand.BrandListDetailDto>();

        var categories = await categoryService.GetLeafCategoriesAsync();
        ViewBag.Categories = categories;
    }

    private async Task<List<VariantImageGroup>> GetOtherVariantImages(Guid productId, Guid? excludeVariantId)
    {
        var data = await productVariantManager.GetProductVariantImages(productId, excludeVariantId);
        return data.Select(v => new VariantImageGroup(
            v.VariantId,
            v.Barcode,
            v.Images.Select(i => new VariantImageDto(i.ImageId, i.Src, i.IsMain)).ToList()
        )).ToList();
    }
}
