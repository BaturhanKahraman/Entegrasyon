using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
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
        ViewData.SetPageTitle("Ürünler");
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
            TempData.SetError(result.Message ?? "Ürün bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle(result.Data!.Title);
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Ürünler", "/products"), (result.Data.Title, null));
        return View(result.Data);
    }

    // ── Edit ──────────────────────────────────────────────────────────

    [HttpGet("/products/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await productService.GetProductEditPageData(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Ürün bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Ürün Düzenle");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Ürünler", "/products"), ("Düzenle", null));
        return View(result.Data);
    }

    [HttpPost("/products/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, EditProductVm vm)
    {
        if (!ModelState.IsValid)
        {
            var pageData = await productService.GetProductEditPageData(id);
            if (!pageData.Success) return RedirectToAction(nameof(Index));
            ViewData.SetPageTitle("Ürün Düzenle");
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
            TempData.SetSuccess("Ürün başarıyla güncellendi.");
        else
            TempData.SetError(result.Message ?? "Ürün güncellenemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Create Wizard ─────────────────────────────────────────────────

    private const string CreateProductSessionKey = "CreateProduct";

    private CreateProductVm GetWizardState()
    {
        var json = HttpContext.Session.GetString(CreateProductSessionKey);
        return json is not null
            ? JsonSerializer.Deserialize<CreateProductVm>(json) ?? new CreateProductVm()
            : new CreateProductVm();
    }

    private void SaveWizardState(CreateProductVm vm)
    {
        HttpContext.Session.SetString(CreateProductSessionKey, JsonSerializer.Serialize(vm));
    }

    [HttpGet("/products/add")]
    public async Task<IActionResult> Create()
    {
        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(("Ürünler", "/products"), ("Yeni Ürün", null));

        // Fresh wizard: eski session state'i temizle ki onceki denemeden kalan
        // veri kullaniciyi sasirtmasin.
        HttpContext.Session.Remove(CreateProductSessionKey);

        await LoadCreateDropdowns();
        return View(new CreateProductVm());
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step1")]
    public async Task<IActionResult> CreateStep1(CreateProductVm vm)
    {
        // Session-first pattern: once mevcut state'i oku, form'dan sadece Step 1
        // alanlarini merge et, HEMEN session'a yaz. Validation'dan once yazmak
        // kritik — hata olsa bile kullanicinin girdigi veri kaybolmaz.
        var state = GetWizardState();
        state.Title = vm.Title;
        state.Description = vm.Description;
        state.StockCode = vm.StockCode;
        state.Season = vm.Season;
        state.Year = vm.Year;
        state.BrandId = vm.BrandId;
        state.CategoryId = vm.CategoryId;
        SaveWizardState(state);

        // Step 1 validation — basit alanlar
        if (string.IsNullOrWhiteSpace(state.Title))
            ModelState.AddModelError(nameof(state.Title), "Ürün adı zorunludur.");
        if (state.BrandId == 0)
            ModelState.AddModelError(nameof(state.BrandId), "Marka seçiniz.");
        if (state.CategoryId == 0)
            ModelState.AddModelError(nameof(state.CategoryId), "Kategori seçiniz.");

        // Step 1 validation — stok kodu unique (fail-fast)
        // Daha once bu kontrol sadece DoSave icindeydi, kullanici Step 6'ya kadar
        // hata aldigini anlamiyordu. Simdi Step 1'de yakaliyoruz.
        if (ModelState.IsValid && !string.IsNullOrWhiteSpace(state.StockCode))
        {
            var isAvailable = await productService.IsStockCodeAvailableAsync(state.StockCode);
            if (!isAvailable)
                ModelState.AddModelError(nameof(state.StockCode),
                    "Bu stok kodu zaten kullanılıyor. Lütfen farklı bir stok kodu girin.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCreateDropdowns();
            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep1", state);

            ViewData.SetPageTitle("Yeni Ürün");
            ViewData.SetActiveNav("products");
            return View(nameof(Create), state);
        }

        // Basarili — brand/category isimlerini coz ve state'e yaz
        var brand = await brandService.GetBrandById(state.BrandId);
        if (brand.Success) state.BrandName = brand.Data!.Name;

        var categories = await categoryService.GetLeafCategoriesAsync();
        state.CategoryName = categories.FirstOrDefault(c => c.Id == state.CategoryId)?.Name;

        SaveWizardState(state);

        // Step 2 icin kategori attribute'larini yukle
        var attrResult = await categoryAttributeManager.GetCategoryAttributesByCategory(state.CategoryId);
        var attrs = attrResult.Success ? attrResult.Data! : [];
        ViewBag.NonVariantAttributes = attrs.Where(a => !a.IsVarianter && !a.IsSlicer).ToList();

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep2Attributes", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step2")]
    public async Task<IActionResult> CreateStep2(CreateProductVm vm)
    {
        // Session-first: mevcut state'i oku, form'dan sadece CategoryAttributes'u merge et
        var state = GetWizardState();
        state.CategoryAttributes = vm.CategoryAttributes ?? [];
        SaveWizardState(state);

        // Step 2 validation — zorunlu kategori özelliklerini kontrol et
        var missing = state.CategoryAttributes
            .Where(a => a.IsRequired && (a.ValueId is null or 0) && string.IsNullOrWhiteSpace(a.CustomValue))
            .ToList();

        if (missing.Count > 0)
        {
            foreach (var attr in missing)
                ModelState.AddModelError($"CategoryAttributes[{state.CategoryAttributes.IndexOf(attr)}].ValueId",
                    $"'{attr.AttributeName}' zorunlu alanıdır.");

            var attrResult = await categoryAttributeManager.GetCategoryAttributesByCategory(state.CategoryId);
            var attrs = attrResult.Success ? attrResult.Data! : [];
            ViewBag.NonVariantAttributes = attrs.Where(a => !a.IsVarianter && !a.IsSlicer).ToList();

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep2Attributes", state);

            ViewData.SetPageTitle("Yeni Ürün");
            ViewData.SetActiveNav("products");
            return View(nameof(Create), state);
        }

        // Step 3 icin varyanter/slicer attribute'lari yukle
        var allAttrs = await categoryAttributeManager.GetCategoryAttributesByCategory(state.CategoryId);
        ViewBag.VariantAttributes = (allAttrs.Success ? allAttrs.Data! : [])
            .Where(a => a.IsVarianter || a.IsSlicer).ToList();

        var branches = await branchOfficeManager.GetBranchList();
        ViewBag.BranchOffices = branches.Data ?? [];

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep3Variants", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
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
        // Session-first: mevcut state'i oku, form'dan Step 3 alanlarini (varyant secimleri,
        // default fiyatlar, olusturulan varyantlar) merge et.
        var state = GetWizardState();
        state.VariantAttributeSelections = vm.VariantAttributeSelections ?? [];
        state.DefaultValues = vm.DefaultValues ?? new DefaultVariantValuesVm();
        state.Variants = vm.Variants ?? [];
        SaveWizardState(state);

        if (state.Variants.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "En az bir varyant oluşturulmalıdır.");

            var allAttrs = await categoryAttributeManager.GetCategoryAttributesByCategory(state.CategoryId);
            ViewBag.VariantAttributes = (allAttrs.Success ? allAttrs.Data! : [])
                .Where(a => a.IsVarianter || a.IsSlicer).ToList();
            var branches = await branchOfficeManager.GetBranchList();
            ViewBag.BranchOffices = branches.Data ?? [];

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep3Variants", state);

            ViewData.SetPageTitle("Yeni Ürün");
            ViewData.SetActiveNav("products");
            return View(nameof(Create), state);
        }

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep4Images", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
    }

    [HttpPost("/products/add/upload-temp-image")]
    public async Task<IActionResult> UploadTempImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return Json(new { success = false, message = "Dosya bulunamadı." });

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
    public IActionResult CreateStep4(CreateProductVm formVm)
    {
        // Read full state from session, merge image assignments from form
        var json = HttpContext.Session.GetString("CreateProduct");
        var vm = json is not null ? JsonSerializer.Deserialize<CreateProductVm>(json)! : formVm;
        vm.ImageAssignments = formVm.ImageAssignments;
        HttpContext.Session.SetString("CreateProduct", JsonSerializer.Serialize(vm));

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep5Review", vm);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step5")]
    public IActionResult CreateStep5(CreateProductVm formVm)
    {
        // Read from session — don't overwrite with empty form VM
        var json = HttpContext.Session.GetString("CreateProduct");
        var vm = json is not null ? JsonSerializer.Deserialize<CreateProductVm>(json)! : formVm;
        HttpContext.Session.SetString("CreateProduct", JsonSerializer.Serialize(vm));

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep6Publish", vm);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/step6")]
    public async Task<IActionResult> CreateStep6(CreateProductVm formVm)
    {
        // Read full product data from TempData (set by CreateStep5)
        var json = HttpContext.Session.GetString("CreateProduct");
        if (json is null)
            return await DoSave(formVm);

        var vm = JsonSerializer.Deserialize<CreateProductVm>(json)!;

        // Merge SEO fields and ECommercePrice from form
        vm.SeoTitle = formVm.SeoTitle;
        vm.SeoDescription = formVm.SeoDescription;
        vm.SeoSlug = formVm.SeoSlug;
        vm.SeoKeywords = formVm.SeoKeywords;

        // Merge ECommercePrice + fallback: Session'da fiyat kaybolmuşsa hidden field'dan al
        for (int i = 0; i < vm.Variants.Count && i < formVm.Variants.Count; i++)
        {
            vm.Variants[i].ECommercePrice = formVm.Variants[i].ECommercePrice;

            if (vm.Variants[i].SalePrice == 0 && formVm.Variants[i].SalePrice > 0)
                vm.Variants[i].SalePrice = formVm.Variants[i].SalePrice;
            if ((vm.Variants[i].ListPrice ?? 0) == 0 && (formVm.Variants[i].ListPrice ?? 0) > 0)
                vm.Variants[i].ListPrice = formVm.Variants[i].ListPrice;
            if (vm.Variants[i].CostPrice == 0 && formVm.Variants[i].CostPrice > 0)
                vm.Variants[i].CostPrice = formVm.Variants[i].CostPrice;
            if (string.IsNullOrEmpty(vm.Variants[i].Barcode) && !string.IsNullOrEmpty(formVm.Variants[i].Barcode))
                vm.Variants[i].Barcode = formVm.Variants[i].Barcode;
        }

        return await DoSave(vm);
    }

    [SkipAutoValidation]
    [HttpPost("/products/add/save")]
    public async Task<IActionResult> CreateSave()
    {
        // Read from TempData (set by previous steps)
        var json = HttpContext.Session.GetString("CreateProduct");
        if (json is null)
        {
            if (Request.IsHtmx())
                return Content("<div class=\"alert alert-danger\">Ürün bilgileri eksik. Lütfen <a href=\"/products/add\">baştan başlatın</a>.</div>", "text/html");
            TempData.SetError("Ürün bilgileri eksik.");
            return RedirectToAction(nameof(Create));
        }

        var vm = JsonSerializer.Deserialize<CreateProductVm>(json)!;
        return await DoSave(vm);
    }

    private async Task<IActionResult> DoSave(CreateProductVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Title) || vm.CategoryId == 0)
        {
            if (Request.IsHtmx())
                return Content("<div class=\"alert alert-danger\">Ürün bilgileri eksik. Lütfen <a href=\"/products/add\">baştan başlatın</a>.</div>", "text/html");

            TempData.SetError("Ürün bilgileri eksik. Lütfen baştan başlatın.");
            return RedirectToAction(nameof(Create));
        }

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
            Description = vm.Description ?? "",
            StockCode = vm.StockCode ?? "",
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

        // Bug #3 fix: AddProduct, validation hatasında FluentValidation.ValidationException
        // fırlatıyordu → yakalanmayıp HTTP 500'e dönüşüyordu (kullanıcı 6 adımı doldurduktan
        // sonra hata sayfası görüyordu). Artık exception'ı yakalayıp aşağıdaki mevcut dostça
        // hata yoluna (Step 5 review + banner) bağlıyoruz; girilen veri session'da korunur.
        IDataResult<Product> result;
        try
        {
            result = await productService.AddProduct(dto);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var msg = string.Join(" • ", ex.Errors
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct());
            ViewBag.WizardError = string.IsNullOrWhiteSpace(msg)
                ? "Ürün kaydedilemedi. Lütfen varyant fiyatı ve stok bilgilerini kontrol edin."
                : msg;

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep5Review", vm);

            TempData.SetError((string)ViewBag.WizardError);
            return RedirectToAction(nameof(Create));
        }

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

            HttpContext.Session.Remove(CreateProductSessionKey);

            ViewBag.ProductId = product.Id;
            ViewBag.ProductTitle = vm.Title;
            ViewBag.VariantCount = vm.Variants.Count;
            ViewBag.ImageCount = imageCount;

            if (Request.IsHtmx())
                return PartialView("Partials/_CreateStep7Success");

            TempData.SetSuccess($"'{vm.Title}' basariyla eklendi.");
            return RedirectToAction(nameof(Detail), new { id = product.Id });
        }

        // Error path — wizard'i tamamen cokertmek yerine Step 5 review'a geri don
        // ve ustte hata banner'i goster. Session'daki veri korunur, kullanici
        // sorunu duzeltip (genelde baska bir tab'de stok kodunu degistirip) tekrar
        // deneyebilir.
        var errorMessage = result.Message ?? "Ürün eklenemedi. Lütfen bilgileri kontrol edin.";
        ViewBag.WizardError = errorMessage;

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep5Review", vm);

        TempData.SetError(errorMessage);
        return RedirectToAction(nameof(Create));
    }

    // ── Wizard Back Navigation ──────────────────────────────────────

    [HttpGet("/products/add/back-to-step1")]
    public async Task<IActionResult> BackToStep1()
    {
        var state = GetWizardState();
        await LoadCreateDropdowns();

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep1", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
    }

    [HttpGet("/products/add/back-to-step2")]
    public async Task<IActionResult> BackToStep2()
    {
        var state = GetWizardState();
        var attrResult = await categoryAttributeManager.GetCategoryAttributesByCategory(state.CategoryId);
        var attrs = attrResult.Success ? attrResult.Data! : [];
        ViewBag.NonVariantAttributes = attrs.Where(a => !a.IsVarianter && !a.IsSlicer).ToList();

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep2Attributes", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
    }

    [HttpGet("/products/add/back-to-step3")]
    public async Task<IActionResult> BackToStep3()
    {
        var state = GetWizardState();
        var allAttrs = await categoryAttributeManager.GetCategoryAttributesByCategory(state.CategoryId);
        ViewBag.VariantAttributes = (allAttrs.Success ? allAttrs.Data! : [])
            .Where(a => a.IsVarianter || a.IsSlicer).ToList();

        var branches = await branchOfficeManager.GetBranchList();
        ViewBag.BranchOffices = branches.Data ?? [];

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep3Variants", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
    }

    [HttpGet("/products/add/back-to-step4")]
    public IActionResult BackToStep4()
    {
        var state = GetWizardState();

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep4Images", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
    }

    [HttpGet("/products/add/back-to-step5")]
    public IActionResult BackToStep5()
    {
        var state = GetWizardState();

        if (Request.IsHtmx())
            return PartialView("Partials/_CreateStep5Review", state);

        ViewData.SetPageTitle("Yeni Ürün");
        ViewData.SetActiveNav("products");
        return View(nameof(Create), state);
    }

    // ── Other Actions ────────────────────────────────────────────────

    [HttpGet("/products/{id:guid}/sync")]
    public async Task<IActionResult> SyncDetail(Guid id)
    {
        var result = await productService.GetProductDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Ürün bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Senkronizasyon Durumu");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Ürünler", "/products"),
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
            TempData.SetError(result.Message ?? "Ürün bulunamadı.");
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

        ViewData.SetPageTitle("Trendyol Gönderimi");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Ürünler", "/products"),
            (result.Data.Title, $"/products/{id}"),
            ("Trendyol Gönderimi", null));
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
            TempData.SetSuccess(result.Message ?? "Ürün Trendyol'a gönderim için kuyruğa eklendi.");
        else
            TempData.SetError(result.Message ?? "Gönderim sırasında bir hata oluştu.");

        return RedirectToAction(nameof(SyncDetail), new { id });
    }

    [HttpGet("/products/{id:guid}/variants")]
    public async Task<IActionResult> Variants(Guid id)
    {
        var result = await productService.GetProductDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Ürün bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var syncDetail = await productSyncManager.GetProductSyncDetailAsync(id);
        if (syncDetail.Success)
            ViewBag.SyncDetail = syncDetail.Data;

        ViewData.SetPageTitle("Varyantlar");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Ürünler", "/products"),
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
            TempData.SetError(result.Message ?? "Varyant bulunamadı.");
            return RedirectToAction(nameof(Variants), new { id = productId });
        }

        ViewData.SetPageTitle($"Varyant — {result.Data!.Barcode}");
        ViewData.SetActiveNav("products");
        ViewData.SetBreadcrumb(
            ("Ürünler", "/products"),
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
            TempData.SetSuccess($"{files.Count} görsel yüklendi.");
        else
            TempData.SetError(result.Message ?? "Görsel yüklenemedi.");

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("/products/{id:guid}/images/{imageId:int}/delete")]
    public IActionResult DeleteImage(Guid id, int imageId)
    {
        // TODO: implement when IImageManager has delete method
        TempData.SetWarning("Görsel silme henüz desteklenmiyor.");
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
            if (variantId == null) return BadRequest("Ürün varyantı bulunamadı.");
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

    // ── Store Settings ────────────────────────────────────────────────

    [HttpGet("/products/{id:guid}/store-settings")]
    public async Task<IActionResult> StoreSettings(Guid id)
    {
        var result = await productService.GetProductDetailById(id);
        if (!result.Success || result.Data is null)
            return NotFound();

        var detail = result.Data;
        var variantAttrNames = await productService.GetVariantAttributeNamesAsync(id);

        var vm = new StoreSettingsVm
        {
            ProductId = detail.Id,
            ProductTitle = detail.Title,
            IsPublished = detail.IsPublished,
            SeoTitle = detail.SeoTitle,
            SeoSlug = detail.SeoSlug,
            SeoDescription = detail.SeoDescription,
            SeoKeywords = detail.SeoKeywords,
            VariantPrices = detail.ProductVariantsDetails.Select(v =>
            {
                variantAttrNames.TryGetValue(v.Id, out var attrLabel);
                return new VariantStorePriceVm
                {
                    VariantId = v.Id,
                    VariantName = string.IsNullOrEmpty(attrLabel) ? v.Barcode ?? "Varyant" : attrLabel,
                    SalePrice = v.SalePrice,
                    ECommercePrice = v.ECommercePrice > 0 ? v.ECommercePrice : v.SalePrice
                };
            }).ToList()
        };

        return PartialView("Partials/_StoreSettings", vm);
    }

    [HttpPost("/products/{id:guid}/store-settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStoreSettings(Guid id, StoreSettingsVm vm)
    {
        var prices = vm.VariantPrices.ToDictionary(v => v.VariantId, v => v.ECommercePrice);
        var result = await productService.UpdateStoreSettings(id, vm.SeoTitle, vm.SeoDescription, vm.SeoSlug, vm.SeoKeywords, prices);

        if (result.Success)
            TempData.SetSuccess(result.Message!);
        else
            TempData.SetError(result.Message!);

        return await StoreSettings(id);
    }

    [HttpPost("/products/{id:guid}/store-settings/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishToStore(Guid id, StoreSettingsVm vm)
    {
        var prices = vm.VariantPrices.ToDictionary(v => v.VariantId, v => v.ECommercePrice);
        var result = await productService.PublishProduct(id, vm.SeoTitle, vm.SeoDescription, vm.SeoSlug, vm.SeoKeywords, prices);

        if (result.Success)
            TempData.SetSuccess(result.Message!);
        else
            TempData.SetError(result.Message!);

        return await StoreSettings(id);
    }
}
