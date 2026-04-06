# Product Adding Wizard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the 3-step product creation wizard into a 6-step wizard with searchable brand/category, dynamic category attributes, cartesian variant generation, bulk image management, and post-save navigation.

**Architecture:** Extends the existing HTMX wizard pattern (partial views swapped into `#wizard-content`). State preserved via TempData JSON serialization between steps. New HTMX endpoints for brand search, category search, attribute loading, variant generation, and image upload. Business layer gets a leaf-category validation rule.

**Tech Stack:** ASP.NET Core 8 MVC, HTMX, Tabler UI, Vanilla JS, FluentValidation, EF Core (PostgreSQL), MinIO (images), xUnit + Moq + FluentAssertions (unit), Playwright + NUnit (E2E)

---

## File Structure

### Files to Modify
| File | Purpose |
|------|---------|
| `Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs` | Extend with attribute, variant-attribute, image assignment fields |
| `Entegrasyon.MVC/Features/Products/ProductController.cs` | Add new step actions + HTMX endpoints (brand search, category search, generate variants, upload image) |
| `Entegrasyon.MVC/Features/Products/Views/Create.cshtml` | Update step indicator from 3 to 6 steps |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep1.cshtml` | Replace dropdowns with searchable selectboxes + brand quick-add modal |
| `Entegrasyon.Business/Validation/FluentValidation/AddProductValidator.cs` | Relax Description/StockCode required rules (they are optional in spec) |

### Files to Create
| File | Purpose |
|------|---------|
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml` | Dynamic category attributes form (non-varianter, non-slicer) |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml` | Variant attribute selection + cartesian generation + bulk defaults + variant table |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_VariantTable.cshtml` | HTMX partial for generated variant table rows |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep4Images.cshtml` | Bulk image upload + assign to variants + main photo selection |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep5Review.cshtml` | Full review with attributes, variants, images |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Success.cshtml` | Success page with navigation options |
| `Test/Entegrasyon.Test/MVC/ProductWizardViewModelTests.cs` | Unit tests for cartesian product generation |
| `Test/Entegrasyon.Test/Business/LeafCategoryRuleTests.cs` | Unit tests for leaf category business rule + validator relaxation |
| `Test/Entegrasyon.E2E/Tests/P1_CoreFlows/ProductWizardTests.cs` | E2E tests for full wizard flow |

### Files to Delete
| File | Reason |
|------|--------|
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2.cshtml` | Replaced by `_CreateStep3Variants.cshtml` |
| `Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Review.cshtml` | Replaced by `_CreateStep5Review.cshtml` |

---

## Task 1: Extend ViewModels

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs`
- Create: `Test/Entegrasyon.Test/MVC/ProductWizardViewModelTests.cs`

- [ ] **Step 1: Write failing test for cartesian product helper**

Create test file:

```csharp
// File: Test/Entegrasyon.Test/MVC/ProductWizardViewModelTests.cs
using Entegrasyon.MVC.Features.Products.ViewModels;
using FluentAssertions;

namespace Entegrasyon.Test.MVC;

public class ProductWizardViewModelTests
{
    [Fact]
    public void GenerateVariants_TwoAttributes_ReturnsCartesianProduct()
    {
        // Arrange: Beden(S,M,L) x Renk(Kirmizi,Mavi) = 6 variants
        var selections = new List<VariantAttributeSelectionVm>
        {
            new()
            {
                CategoryAttributeId = 1,
                AttributeName = "Beden",
                IsVarianter = true,
                IsSlicer = false,
                AllowCustom = false,
                SelectedValues =
                [
                    new SelectedAttributeValueVm { ValueId = 10, ValueName = "S" },
                    new SelectedAttributeValueVm { ValueId = 11, ValueName = "M" },
                    new SelectedAttributeValueVm { ValueId = 12, ValueName = "L" }
                ]
            },
            new()
            {
                CategoryAttributeId = 2,
                AttributeName = "Renk",
                IsVarianter = false,
                IsSlicer = true,
                AllowCustom = true,
                SelectedValues =
                [
                    new SelectedAttributeValueVm { ValueId = null, ValueName = "Kirmizi", IsCustom = true },
                    new SelectedAttributeValueVm { ValueId = null, ValueName = "Mavi", IsCustom = true }
                ]
            }
        };

        var defaults = new DefaultVariantValuesVm
        {
            ListPrice = 299.90m,
            SalePrice = 249.90m,
            CostPrice = 120m,
            VatRate = 20,
            Stock = 50
        };

        // Act
        var variants = CreateProductVm.GenerateVariants(selections, defaults);

        // Assert
        variants.Should().HaveCount(6);
        variants[0].VariantAttributes.Should().HaveCount(2);
        variants[0].ListPrice.Should().Be(299.90m);
        variants[0].Stock.Should().Be(50);

        // Verify all combinations exist
        var combos = variants.Select(v =>
            string.Join("-", v.VariantAttributes.Select(a => a.ValueName))).ToList();
        combos.Should().Contain("S-Kirmizi");
        combos.Should().Contain("M-Mavi");
        combos.Should().Contain("L-Kirmizi");
    }

    [Fact]
    public void GenerateVariants_SingleAttribute_ReturnsOnePerValue()
    {
        var selections = new List<VariantAttributeSelectionVm>
        {
            new()
            {
                CategoryAttributeId = 1,
                AttributeName = "Beden",
                IsVarianter = true,
                IsSlicer = false,
                AllowCustom = false,
                SelectedValues =
                [
                    new SelectedAttributeValueVm { ValueId = 10, ValueName = "S" },
                    new SelectedAttributeValueVm { ValueId = 11, ValueName = "M" }
                ]
            }
        };

        var defaults = new DefaultVariantValuesVm { ListPrice = 100, SalePrice = 90, CostPrice = 50, VatRate = 20, Stock = 10 };

        var variants = CreateProductVm.GenerateVariants(selections, defaults);

        variants.Should().HaveCount(2);
        variants[0].VariantAttributes.Should().ContainSingle(a => a.ValueName == "S");
        variants[1].VariantAttributes.Should().ContainSingle(a => a.ValueName == "M");
    }

    [Fact]
    public void GenerateVariants_NoSelections_ReturnsEmpty()
    {
        var variants = CreateProductVm.GenerateVariants([], new DefaultVariantValuesVm());
        variants.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductWizardViewModelTests" -v minimal`
Expected: FAIL -- types `VariantAttributeSelectionVm`, `DefaultVariantValuesVm`, `GenerateVariants` don't exist

- [ ] **Step 3: Implement the ViewModel extensions**

Replace the entire file:

```csharp
// File: Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs
namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class CreateProductVm
{
    // Step 1: General
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string StockCode { get; set; } = "";
    public string? Season { get; set; }
    public string? Year { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string? BrandName { get; set; }
    public string? CategoryName { get; set; }

    // Step 2: Category Attributes (non-varianter, non-slicer)
    public List<AttributeValueVm> CategoryAttributes { get; set; } = [];

    // Step 3: Variant Generation
    public List<VariantAttributeSelectionVm> VariantAttributeSelections { get; set; } = [];
    public DefaultVariantValuesVm DefaultValues { get; set; } = new();
    public List<CreateVariantVm> Variants { get; set; } = [];

    // Step 4: Image assignments (temp file keys from upload)
    public List<VariantImageAssignmentVm> ImageAssignments { get; set; } = [];

    /// <summary>
    /// Cartesian product of selected variant/slicer attribute values.
    /// Each combination becomes one CreateVariantVm filled with default values.
    /// </summary>
    public static List<CreateVariantVm> GenerateVariants(
        List<VariantAttributeSelectionVm> selections,
        DefaultVariantValuesVm defaults)
    {
        var nonEmpty = selections.Where(s => s.SelectedValues.Count > 0).ToList();
        if (nonEmpty.Count == 0) return [];

        // Start with first attribute's values as seed
        IEnumerable<List<VariantAttributeValueVm>> combos = nonEmpty[0].SelectedValues
            .Select(v => new List<VariantAttributeValueVm>
            {
                new()
                {
                    CategoryAttributeId = nonEmpty[0].CategoryAttributeId,
                    AttributeName = nonEmpty[0].AttributeName,
                    ValueId = v.ValueId,
                    ValueName = v.ValueName,
                    IsCustom = v.IsCustom,
                    IsVarianter = nonEmpty[0].IsVarianter,
                    IsSlicer = nonEmpty[0].IsSlicer
                }
            });

        // Cross-join with remaining attributes
        for (int i = 1; i < nonEmpty.Count; i++)
        {
            var attr = nonEmpty[i];
            combos = combos.SelectMany(existing =>
                attr.SelectedValues.Select(v =>
                    existing.Concat([new VariantAttributeValueVm
                    {
                        CategoryAttributeId = attr.CategoryAttributeId,
                        AttributeName = attr.AttributeName,
                        ValueId = v.ValueId,
                        ValueName = v.ValueName,
                        IsCustom = v.IsCustom,
                        IsVarianter = attr.IsVarianter,
                        IsSlicer = attr.IsSlicer
                    }]).ToList()));
        }

        return combos.Select(attrs => new CreateVariantVm
        {
            VariantAttributes = attrs,
            ListPrice = defaults.ListPrice,
            SalePrice = defaults.SalePrice,
            CostPrice = defaults.CostPrice,
            VatRate = defaults.VatRate,
            Stock = defaults.Stock
        }).ToList();
    }
}

public class CreateVariantVm
{
    public string Barcode { get; set; } = "";
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
    public decimal DimensionalWeight { get; set; }
    public int Stock { get; set; }
    public List<VariantAttributeValueVm> VariantAttributes { get; set; } = [];
}

public class VariantAttributeValueVm
{
    public int CategoryAttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public int? ValueId { get; set; }
    public string ValueName { get; set; } = "";
    public bool IsCustom { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
}

/// <summary>Per-attribute value selections for the variant generation step.</summary>
public class VariantAttributeSelectionVm
{
    public int CategoryAttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
    public bool AllowCustom { get; set; }
    public List<SelectedAttributeValueVm> SelectedValues { get; set; } = [];
}

public class SelectedAttributeValueVm
{
    public int? ValueId { get; set; }
    public string ValueName { get; set; } = "";
    public bool IsCustom { get; set; }
}

/// <summary>Bulk default values applied to all generated variants.</summary>
public class DefaultVariantValuesVm
{
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
    public int Stock { get; set; }
}

/// <summary>Non-varianter, non-slicer attribute value from Step 2.</summary>
public class AttributeValueVm
{
    public int CategoryAttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public int? ValueId { get; set; }
    public string? CustomValue { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowCustom { get; set; }
}

/// <summary>Maps uploaded images to variants in Step 4.</summary>
public class VariantImageAssignmentVm
{
    public int VariantIndex { get; set; }
    public List<string> TempImageKeys { get; set; } = [];
    public int? MainImageIndex { get; set; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductWizardViewModelTests" -v minimal`
Expected: 3 PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs \
      Test/Entegrasyon.Test/MVC/ProductWizardViewModelTests.cs
git commit -m "feat(product-wizard): extend ViewModels with attribute/variant/image types + cartesian generator"
```

---

## Task 2: Relax AddProductValidator and Add Leaf Category Check

**Files:**
- Modify: `Application/Entegrasyon.Business/Validation/FluentValidation/AddProductValidator.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs`
- Create: `Test/Entegrasyon.Test/Business/LeafCategoryRuleTests.cs`

- [ ] **Step 1: Write failing test for relaxed validator**

```csharp
// File: Test/Entegrasyon.Test/Business/LeafCategoryRuleTests.cs
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using FluentAssertions;

namespace Entegrasyon.Test.Business;

public class LeafCategoryRuleTests
{
    [Fact]
    public async Task AddProduct_WithZeroCategoryId_FailsValidation()
    {
        var validator = new Entegrasyon.Business.Validation.FluentValidation.AddProductValidator();
        var dto = new AddProductDto
        {
            Title = "Test Product",
            CategoryId = 0,
            BrandId = 1,
            ProductVariants = [new AddProductVariantDto
            {
                Barcode = "123",
                ListPrice = 100,
                SalePrice = 90,
                CurrencyType = "TRY",
                BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 10 }]
            }]
        };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public async Task AddProduct_WithOptionalDescriptionAndStockCode_IsValid()
    {
        var validator = new Entegrasyon.Business.Validation.FluentValidation.AddProductValidator();
        var dto = new AddProductDto
        {
            Title = "Test Product",
            Description = "",
            StockCode = "",
            CategoryId = 5,
            BrandId = 1,
            ProductVariants = [new AddProductVariantDto
            {
                Barcode = "123",
                ListPrice = 100,
                SalePrice = 90,
                CurrencyType = "TRY",
                BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 10 }]
            }]
        };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~LeafCategoryRuleTests" -v minimal`
Expected: `AddProduct_WithOptionalDescriptionAndStockCode_IsValid` FAILS because Description and StockCode are currently required in validator

- [ ] **Step 3: Update the validator -- Description and StockCode are now optional**

```csharp
// File: Application/Entegrasyon.Business/Validation/FluentValidation/AddProductValidator.cs
using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductValidator : AbstractValidator<AddProductDto>
{
    public AddProductValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Urun adi bos gecilemez");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Urun kategorisi bos gecilemez");
        RuleFor(x => x.ProductVariants).NotEmpty().WithMessage("Urun varyantlari bos gecilemez");

        RuleForEach(x => x.ProductVariants).SetValidator(new AddProductVariantValidator());
    }
}
```

- [ ] **Step 4: Add leaf category check in ProductManager.AddProduct**

In `Application/Entegrasyon.Business/Concrete/ProductManager.cs`, find the line after the stock code conflict check (`if (stockCodeConflict) return ...`) and add immediately after it:

```csharp
// Leaf category check: category must have no children
var hasChildren = await dbContext.Categories.AnyAsync(c => c.ParentCategoryId == dto.CategoryId && !c.IsDeleted);
if (hasChildren)
    return new ErrorDataResult<Product>(null!, "Sadece alt kategorisi olmayan (yaprak) kategoriler secilebilir.");
```

- [ ] **Step 5: Run tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~LeafCategoryRuleTests" -v minimal`
Expected: 2 PASS

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Validation/FluentValidation/AddProductValidator.cs \
      Application/Entegrasyon.Business/Concrete/ProductManager.cs \
      Test/Entegrasyon.Test/Business/LeafCategoryRuleTests.cs
git commit -m "feat(product-wizard): relax validator for optional fields + add leaf category check"
```

---

## Task 3: Update Wizard Container (Create.cshtml -- 6 Steps)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Create.cshtml`

- [ ] **Step 1: Update Create.cshtml with 6-step indicator and improved step detection**

Replace the full file content:

```html
@model Entegrasyon.MVC.Features.Products.ViewModels.CreateProductVm

@{
    ViewData.SetBreadcrumb(("Urunler", "/products"), ("Yeni Urun", null));
}

<div class="d-flex justify-content-end mb-3">
    <a href="/products" class="btn btn-outline-secondary">
        Listeye Don
    </a>
</div>

        <!-- Step indicator -->
        <div class="card mb-3">
            <div class="card-body">
                <div class="steps steps-counter mb-4" id="wizard-steps">
                    <a href="#" class="step-item active" data-step="1">Genel Bilgiler</a>
                    <a href="#" class="step-item" data-step="2">Ozellikler</a>
                    <a href="#" class="step-item" data-step="3">Varyantlar</a>
                    <a href="#" class="step-item" data-step="4">Gorseller</a>
                    <a href="#" class="step-item" data-step="5">Onay</a>
                    <a href="#" class="step-item" data-step="6">Sonuc</a>
                </div>
            </div>
        </div>

        <!-- Wizard content area -->
        <div id="wizard-content">
            <partial name="Partials/_CreateStep1" model="Model" />
        </div>

@section Scripts {
<script>
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        if (evt.detail.target.id !== 'wizard-content') return;
        var stepEl = evt.detail.target.querySelector('[data-wizard-step]');
        if (!stepEl) return;
        var currentStep = parseInt(stepEl.getAttribute('data-wizard-step'));
        document.querySelectorAll('.step-item').forEach(function(s) {
            var step = parseInt(s.getAttribute('data-step'));
            s.classList.toggle('active', step === currentStep);
        });
    });
</script>
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/Views/Create.cshtml
git commit -m "feat(product-wizard): update wizard container to 6-step indicator"
```

---

## Task 4: Step 1 -- Searchable Brand with Quick-Add + Searchable Category

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep1.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs`

**Important:** Add `ICategoryAttributeManager categoryAttributeManager` to the ProductController primary constructor parameters.

- [ ] **Step 1: Add brand search, brand quick-add, and category search endpoints to ProductController**

Add these methods before `LoadCreateDropdowns`:

```csharp
// -- Brand Search (HTMX) --
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

// -- Category Search (HTMX) --
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
```

- [ ] **Step 2: Update CreateStep1 action to load attributes and route to Step 2**

Replace the existing `CreateStep1` method:

```csharp
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

    // Resolve brand/category names
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
```

- [ ] **Step 3: Rewrite _CreateStep1.cshtml with searchable selectboxes and brand modals**

Full file content -- uses `data-wizard-step="1"` marker, debounced fetch for search, Bootstrap modals for brand add. Key elements:
- `#brand-input` text field with `#brand-dropdown` results div
- `#category-input` text field with `#category-dropdown` results div
- Hidden inputs `BrandId` and `CategoryId` set when user selects from dropdown
- Brand not found: on blur without selection, opens `#brand-add-modal`
- After brand add success: opens `#brand-info-modal` with marketplace matching warning
- JS uses `fetch()` with 300ms debounce, `textContent` for safe DOM insertion (no raw HTML)

**Note for implementer:** The JS for searchable dropdowns must use `document.createTextNode()` or `textContent` for user-provided strings to prevent XSS. Never use `element.innerHTML = userInput`. Create option elements with `document.createElement('a')` and set `el.textContent = item.name`.

(The full `.cshtml` file with all HTML and JS is large -- implementer should follow the spec's Step 1 requirements and the existing Tabler UI patterns in the codebase. Key behaviors: searchable input, dropdown results, hidden ID field, brand add modal chain, category leaf-only search.)

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep1.cshtml \
      Application/Entegrasyon.MVC/Features/Products/ProductController.cs
git commit -m "feat(product-wizard): searchable brand/category + brand quick-add modal"
```

---

## Task 5: Step 2 -- Dynamic Category Attributes

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs`

- [ ] **Step 1: Create _CreateStep2Attributes.cshtml**

Key elements:
- `data-wizard-step="2"` marker
- Hidden fields for all Step 1 data (Title, Description, StockCode, Season, Year, BrandId, CategoryId, BrandName, CategoryName)
- `hx-post="/products/add/step2"` targeting `#wizard-content`
- Iterates `ViewBag.NonVariantAttributes` (type `List<CategoryAttributeDto>`)
- For each attribute: hidden fields for `CategoryAttributes[i].CategoryAttributeId`, `AttributeName`, `IsRequired`, `AllowCustom`
- If `AllowCustom == true`: text input for `CategoryAttributes[i].CustomValue`
- If `AllowCustom == false`: `<select>` from `CategoryAttributeValues` for `CategoryAttributes[i].ValueId`
- Required attributes get `required` HTML attribute and red asterisk label
- Footer: "Geri" link + "Devam: Varyantlar" submit button

```html
@model Entegrasyon.MVC.Features.Products.ViewModels.CreateProductVm
@using Entegrasyon.Entity.Dtos.Category

@{
    var attrs = ViewBag.NonVariantAttributes as List<CategoryAttributeDto> ?? [];
}

<div data-wizard-step="2">
<form hx-post="/products/add/step2" hx-target="#wizard-content" hx-swap="innerHTML">
    <input type="hidden" name="Title" value="@Model.Title" />
    <input type="hidden" name="Description" value="@Model.Description" />
    <input type="hidden" name="StockCode" value="@Model.StockCode" />
    <input type="hidden" name="Season" value="@Model.Season" />
    <input type="hidden" name="Year" value="@Model.Year" />
    <input type="hidden" name="BrandId" value="@Model.BrandId" />
    <input type="hidden" name="CategoryId" value="@Model.CategoryId" />
    <input type="hidden" name="BrandName" value="@Model.BrandName" />
    <input type="hidden" name="CategoryName" value="@Model.CategoryName" />

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Kategori Ozellikleri</h3>
            <span class="card-subtitle">@Model.CategoryName</span>
        </div>
        <div class="card-body">
            @if (attrs.Count == 0)
            {
                <div class="alert alert-info">Bu kategorinin ek ozelligi bulunmamaktadir.</div>
            }
            else
            {
                <div class="row">
                    @for (int i = 0; i < attrs.Count; i++)
                    {
                        var attr = attrs[i];
                        <div class="col-md-6 mb-3">
                            <label class="form-label @(attr.IsRequired ? "required" : "")">
                                @attr.CategoriyAttributeHumanized
                            </label>
                            <input type="hidden" name="CategoryAttributes[@i].CategoryAttributeId" value="@attr.Id" />
                            <input type="hidden" name="CategoryAttributes[@i].AttributeName" value="@attr.CategoriyAttributeHumanized" />
                            <input type="hidden" name="CategoryAttributes[@i].IsRequired" value="@attr.IsRequired" />
                            <input type="hidden" name="CategoryAttributes[@i].AllowCustom" value="@attr.AllowCustom" />

                            @if (attr.AllowCustom)
                            {
                                <input type="text" name="CategoryAttributes[@i].CustomValue"
                                       class="form-control" placeholder="Deger girin..."
                                       @(attr.IsRequired ? "required" : "") />
                            }
                            else
                            {
                                <select name="CategoryAttributes[@i].ValueId" class="form-select"
                                        @(attr.IsRequired ? "required" : "")>
                                    <option value="">Secin...</option>
                                    @foreach (var val in attr.CategoryAttributeValues)
                                    {
                                        <option value="@val.Id">@val.Name</option>
                                    }
                                </select>
                            }
                        </div>
                    }
                </div>
            }
        </div>
        <div class="card-footer d-flex justify-content-between">
            <a href="/products/add" class="btn btn-ghost-secondary">&larr; Geri</a>
            <button type="submit" class="btn btn-primary">Devam: Varyantlar &rarr;</button>
        </div>
    </div>
</form>
</div>
```

- [ ] **Step 2: Add CreateStep2 controller action**

Replace the existing `CreateStep2` method:

```csharp
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

    if (Request.IsHtmx())
        return PartialView("Partials/_CreateStep3Variants", vm);

    ViewData.SetPageTitle("Yeni Urun");
    ViewData.SetActiveNav("products");
    return View(nameof(Create), vm);
}
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml \
      Application/Entegrasyon.MVC/Features/Products/ProductController.cs
git commit -m "feat(product-wizard): Step 2 dynamic category attributes"
```

---

## Task 6: Step 3 -- Variant Generation (Cartesian Product)

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml`
- Create: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_VariantTable.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs`

- [ ] **Step 1: Add generate-variants and step3 endpoints to controller**

```csharp
[HttpPost("/products/add/generate-variants")]
public IActionResult GenerateVariants([FromForm] CreateProductVm vm)
{
    vm.Variants = CreateProductVm.GenerateVariants(vm.VariantAttributeSelections, vm.DefaultValues);
    return PartialView("Partials/_VariantTable", vm);
}

[HttpPost("/products/add/step3")]
public IActionResult CreateStep3(CreateProductVm vm)
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
```

- [ ] **Step 2: Create _VariantTable.cshtml partial**

Renders the generated variant rows as a table. Each row has:
- Badge labels for each variant attribute value (readonly)
- Hidden fields for variant attribute data (CategoryAttributeId, AttributeName, ValueId, ValueName, IsCustom, IsVarianter, IsSlicer)
- Editable inputs for Barcode, ListPrice, SalePrice, CostPrice, VatRate, Stock
- Index-based form naming: `Variants[i].Property` and `Variants[i].VariantAttributes[j].Property`

```html
@model Entegrasyon.MVC.Features.Products.ViewModels.CreateProductVm

@if (Model.Variants.Count == 0)
{
    <div class="alert alert-warning">Varyant olusturmak icin en az bir ozellik degeri secin.</div>
}
else
{
    <div class="table-responsive">
        <table class="table table-vcenter card-table table-hover table-sm">
            <thead>
                <tr>
                    @if (Model.Variants[0].VariantAttributes.Count > 0)
                    {
                        @foreach (var attr in Model.Variants[0].VariantAttributes)
                        {
                            <th>@attr.AttributeName</th>
                        }
                    }
                    <th>Barkod</th>
                    <th>Liste Fiyat</th>
                    <th>Satis Fiyat</th>
                    <th>Maliyet</th>
                    <th>KDV %</th>
                    <th>Stok</th>
                </tr>
            </thead>
            <tbody>
                @for (int i = 0; i < Model.Variants.Count; i++)
                {
                    var v = Model.Variants[i];
                    <tr>
                        @for (int j = 0; j < v.VariantAttributes.Count; j++)
                        {
                            var va = v.VariantAttributes[j];
                            <td>
                                <span class="badge bg-azure-lt">@va.ValueName</span>
                                <input type="hidden" name="Variants[@i].VariantAttributes[@j].CategoryAttributeId" value="@va.CategoryAttributeId" />
                                <input type="hidden" name="Variants[@i].VariantAttributes[@j].AttributeName" value="@va.AttributeName" />
                                <input type="hidden" name="Variants[@i].VariantAttributes[@j].ValueId" value="@va.ValueId" />
                                <input type="hidden" name="Variants[@i].VariantAttributes[@j].ValueName" value="@va.ValueName" />
                                <input type="hidden" name="Variants[@i].VariantAttributes[@j].IsCustom" value="@va.IsCustom" />
                                <input type="hidden" name="Variants[@i].VariantAttributes[@j].IsVarianter" value="@va.IsVarianter" />
                                <input type="hidden" name="Variants[@i].VariantAttributes[@j].IsSlicer" value="@va.IsSlicer" />
                            </td>
                        }
                        <td><input name="Variants[@i].Barcode" value="@v.Barcode" class="form-control form-control-sm" placeholder="Otomatik" style="width:120px" /></td>
                        <td><input name="Variants[@i].ListPrice" value="@v.ListPrice" type="number" step="0.01" class="form-control form-control-sm" style="width:100px" /></td>
                        <td><input name="Variants[@i].SalePrice" value="@v.SalePrice" type="number" step="0.01" class="form-control form-control-sm" style="width:100px" /></td>
                        <td><input name="Variants[@i].CostPrice" value="@v.CostPrice" type="number" step="0.01" class="form-control form-control-sm" style="width:100px" /></td>
                        <td><input name="Variants[@i].VatRate" value="@v.VatRate" type="number" step="1" class="form-control form-control-sm" style="width:70px" /></td>
                        <td><input name="Variants[@i].Stock" value="@v.Stock" type="number" class="form-control form-control-sm" style="width:70px" /></td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
    <p class="text-secondary small mt-1">@Model.Variants.Count varyant olusturuldu. Barkod bos birakilirsa otomatik olusturulur.</p>
}
```

- [ ] **Step 3: Create _CreateStep3Variants.cshtml**

Key elements:
- `data-wizard-step="3"` marker
- `hx-post="/products/add/step3"` targeting `#wizard-content`
- Hidden fields for Step 1 + Step 2 data (all previous step values carried forward)
- Iterates `ViewBag.VariantAttributes` (type `List<CategoryAttributeDto>`)
- For each attribute: badge showing Varyanter (red) or Dilimleyici (yellow)
- If `AllowCustom == true`: tag-input (type text, on Enter/comma adds tag badge with hidden fields)
- If `AllowCustom == false`: checkbox list from CategoryAttributeValues
- Bulk defaults section: ListPrice, SalePrice, CostPrice, VatRate, Stock inputs with `DefaultValues.*` names
- "Varyantlari Olustur" button: JS `fetch('/products/add/generate-variants')` with form data, injects response into `#variant-table-container`
- `#variant-table-container` div for HTMX-loaded variant table
- "Devam: Gorseller" button (initially disabled, enabled after variants generated)
- JS for tag-input: on keydown Enter/comma, creates badge element with `textContent` (safe), adds hidden fields `VariantAttributeSelections[i].SelectedValues[n].ValueName` and `.IsCustom=true`
- JS for checkbox sync: on change, creates hidden fields `VariantAttributeSelections[i].SelectedValues[n].ValueId` and `.ValueName`

**Note for implementer:** All dynamic DOM content must use `document.createElement()` + `textContent` -- never assign user input to `innerHTML`. This prevents XSS vulnerabilities.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_VariantTable.cshtml \
      Application/Entegrasyon.MVC/Features/Products/ProductController.cs
git commit -m "feat(product-wizard): Step 3 variant generation with cartesian product"
```

---

## Task 7: Step 4 -- Image Management

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep4Images.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs`

- [ ] **Step 1: Add temp image upload endpoint to controller**

```csharp
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
```

- [ ] **Step 2: Add step4 controller action**

```csharp
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
```

- [ ] **Step 3: Create _CreateStep4Images.cshtml**

Key elements:
- `data-wizard-step="4"` marker
- `hx-post="/products/add/step4"` with `enctype="multipart/form-data"`
- Hidden fields for ALL previous step data (Step 1 + Step 2 + Step 3 including all Variants and their VariantAttributes)
- Upload zone: `<input type="file" multiple accept="image/*">` + drag-and-drop div
- On file select: JS uploads each file via `fetch('/products/add/upload-temp-image')`, stores `{tempKey, fileName, objectUrl}` in array
- Uploaded images displayed as numbered thumbnails using `URL.createObjectURL(file)` for preview
- Variant assignment section: for each variant, shows label (attribute values joined) + "Gorsel Ata" button
- On assign click: `prompt()` asks for comma-separated image numbers, first = main photo
- Creates hidden fields: `ImageAssignments[variantIndex].TempImageKeys[n]`, `ImageAssignments[variantIndex].MainImageIndex`, `ImageAssignments[variantIndex].VariantIndex`
- Footer: "Geri" + "Devam: Onay" buttons
- Images are optional -- user can proceed without uploading

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep4Images.cshtml \
      Application/Entegrasyon.MVC/Features/Products/ProductController.cs
git commit -m "feat(product-wizard): Step 4 bulk image upload + variant assignment"
```

---

## Task 8: Step 5 Review + Step 6 Success + Updated CreateSave

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep5Review.cshtml`
- Create: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Success.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs`
- Delete: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2.cshtml`
- Delete: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Review.cshtml`

- [ ] **Step 1: Create _CreateStep5Review.cshtml**

Shows:
- `data-wizard-step="5"` marker
- Datagrid: Title, StockCode, Brand, Category, Season, Year
- Category attributes section (iterate `CategoryAttributes` where ValueId > 0 or CustomValue present)
- Description if present
- Variant table with attribute badges, Barcode, prices, VatRate, Stock, image count per variant
- Image count from `ImageAssignments.FirstOrDefault(a => a.VariantIndex == idx)?.TempImageKeys.Count`
- Footer: "Bastan Baslat" link + "Urunu Kaydet" button via `hx-post="/products/add/save"` targeting `#wizard-content`

- [ ] **Step 2: Create _CreateStep6Success.cshtml**

Shows:
- `data-wizard-step="6"` marker
- Green checkmark icon + "ProductTitle basariyla eklendi!" heading
- Subtitle: "X varyant . Y gorsel"
- Three buttons:
  1. `<a href="/products/{ProductId}/sync">Senkronizasyona Git</a>` (primary)
  2. `<a href="/products">Urun Listesine Don</a>` (outline)
  3. `<a href="/products/add">Yeni Urun Ekle</a>` (outline)
- Data from ViewBag: ProductId, ProductTitle, VariantCount, ImageCount

```html
@{
    var productId = ViewBag.ProductId as Guid?;
    var productTitle = ViewBag.ProductTitle as string ?? "Urun";
    var variantCount = ViewBag.VariantCount as int? ?? 0;
    var imageCount = ViewBag.ImageCount as int? ?? 0;
}

<div data-wizard-step="6">
<div class="card">
    <div class="card-body text-center py-5">
        <div class="mb-3">
            <svg xmlns="http://www.w3.org/2000/svg" class="icon icon-lg text-green" width="48" height="48" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none"><path d="M5 12l5 5l10 -10" /></svg>
        </div>
        <h2 class="text-green">"@productTitle" basariyla eklendi!</h2>
        <p class="text-secondary">@variantCount varyant &middot; @imageCount gorsel</p>
        <div class="d-flex gap-3 justify-content-center mt-4">
            <a href="/products/@productId/sync" class="btn btn-primary">Senkronizasyona Git</a>
            <a href="/products" class="btn btn-outline-secondary">Urun Listesine Don</a>
            <a href="/products/add" class="btn btn-outline-secondary">Yeni Urun Ekle</a>
        </div>
    </div>
</div>
</div>
```

- [ ] **Step 3: Update CreateSave to map new ViewModel structure**

Replace the `CreateSave` method. Key changes:
- Map `vm.CategoryAttributes` to `AttributeKeyValue` list for `AddProductDto.AttributeKeyValues`
- Map `v.VariantAttributes` to `ProductVariantAttribute` list for each `AddProductVariantDto`
- After successful save, upload temp images per variant using `imageManager.AddProductImages()`
- Clean up temp files from `Path.GetTempPath()/product-wizard-images/`
- Set ViewBag (ProductId, ProductTitle, VariantCount, ImageCount) and return `_CreateStep6Success` partial

```csharp
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
        AttributeKeyValues = attributeKeyValues,
        ProductVariants = vm.Variants.Select(v => new AddProductVariantDto
        {
            Barcode = v.Barcode,
            ListPrice = v.ListPrice,
            SalePrice = v.SalePrice,
            CostPrice = v.CostPrice,
            VatRate = v.VatRate,
            DimensionalWeight = v.DimensionalWeight,
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
            BranchOfficeStocks = [new AddBranchOfficeStockDto
                { BranchOfficeId = 1, FirstTotalStock = v.Stock }]
        }).ToList()
    };

    var result = await productService.AddProduct(dto);

    if (result.Success)
    {
        var product = result.Data!;
        var imageCount = 0;

        // Upload temp images per variant
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
            return PartialView("Partials/_CreateStep6Success");

        TempData.SetSuccess($"'{vm.Title}' basariyla eklendi.");
        return RedirectToAction(nameof(Detail), new { id = product.Id });
    }

    TempData.SetError(result.Message ?? "Urun eklenemedi.");
    return RedirectToAction(nameof(Create));
}
```

- [ ] **Step 4: Delete old step views**

```bash
rm Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2.cshtml
rm Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Review.cshtml
```

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep5Review.cshtml \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Success.cshtml \
      Application/Entegrasyon.MVC/Features/Products/ProductController.cs
git rm Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2.cshtml \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Review.cshtml
git commit -m "feat(product-wizard): Step 5 review + Step 6 success + updated CreateSave"
```

---

## Task 9: Build and Verify

- [ ] **Step 1: Build the solution**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded with 0 errors

- [ ] **Step 2: Fix any build errors**

Common issues to check:
- `ICategoryAttributeManager` added to ProductController constructor
- `VariantImageStream` import (check namespace -- it is defined in `Business/Abstract/IImageManager.cs`)
- `using Entegrasyon.Entity.Dtos.Category;` in controller if `CategoryAttributeDto` used directly
- Missing `using` for `System.Text.Json` (already present)

- [ ] **Step 3: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All tests pass including 3 new `ProductWizardViewModelTests` and 2 `LeafCategoryRuleTests`

- [ ] **Step 4: Commit if fixes were needed**

```bash
git add -A
git commit -m "fix(product-wizard): resolve build issues"
```

---

## Task 10: Manual Browser Testing

Before E2E tests, manually verify the wizard in Chrome.

- [ ] **Step 1: Start the application**

Run: `cd Application/Entegrasyon.MVC && dotnet run`
Open: `http://localhost:5100/products/add` (login: admin / 123456789)

- [ ] **Step 2: Walk through all 6 steps**

1. Step 1: Type brand name, verify search. Try non-existent brand, verify add modal chain
2. Step 1: Type category name, verify leaf-only search
3. Step 2: Verify attributes load. Fill required ones. Try submitting with empty required field
4. Step 3: Select variant values (checkboxes + tags). Set defaults. Click generate. Verify table. Override one price
5. Step 4: Upload images. Assign to variants. Select main photo
6. Step 5: Review all data. Click save
7. Step 6: Verify success page. Test navigation buttons

- [ ] **Step 3: Fix any issues found during testing**

- [ ] **Step 4: Commit fixes**

```bash
git add -A
git commit -m "fix(product-wizard): browser testing fixes"
```

---

## Task 11: E2E Tests

**Files:**
- Create: `Test/Entegrasyon.E2E/Tests/P1_CoreFlows/ProductWizardTests.cs`

- [ ] **Step 1: Write E2E tests**

```csharp
// File: Test/Entegrasyon.E2E/Tests/P1_CoreFlows/ProductWizardTests.cs
using System.Text.RegularExpressions;
using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.Pages;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

[TestFixture]
[Order(50)]
public class ProductWizardTests : E2ETestBase
{
    [SetUp]
    public async Task Login()
    {
        var loginPage = new LoginPage(Page);
        await loginPage.GoToAsync(BaseUrl);
        await loginPage.LoginAsync("admin", "123456789");
        await Expect(Page).ToHaveURLAsync(new Regex("/dashboard"));
    }

    [Test]
    [Order(1)]
    public async Task FullWizardFlow_CreatesProductWithVariants()
    {
        await Page.GotoAsync($"{BaseUrl}/products/add");
        await Expect(Page.Locator("[data-wizard-step='1']")).ToBeVisibleAsync();

        // Step 1: Fill general info
        await Page.FillAsync("input[name='Title']", "E2E Test Urun " + DateTime.Now.Ticks);

        // Search and select first brand
        await Page.FillAsync("#brand-input", "Ni");
        await Page.WaitForSelectorAsync("#brand-dropdown .dropdown-item");
        await Page.ClickAsync("#brand-dropdown .dropdown-item:first-child");

        // Search and select first category
        await Page.FillAsync("#category-input", "Ti");
        await Page.WaitForSelectorAsync("#category-dropdown .dropdown-item");
        await Page.ClickAsync("#category-dropdown .dropdown-item:first-child");

        // Submit Step 1
        await Page.ClickAsync("button[type='submit']");
        await Expect(Page.Locator("[data-wizard-step='2']")).ToBeVisibleAsync();

        // Step 2: Fill required attributes
        var requiredInputs = Page.Locator("[data-wizard-step='2'] [required]");
        var count = await requiredInputs.CountAsync();
        for (int i = 0; i < count; i++)
        {
            var input = requiredInputs.Nth(i);
            var tagName = await input.EvaluateAsync<string>("el => el.tagName");
            if (tagName == "SELECT")
                await input.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            else
                await input.FillAsync("E2E Test Deger");
        }

        await Page.ClickAsync("button[type='submit']");
        await Expect(Page.Locator("[data-wizard-step='3']")).ToBeVisibleAsync();

        // Step 3: Fill defaults and generate variants
        await Page.FillAsync("input[name='DefaultValues.ListPrice']", "199.90");
        await Page.FillAsync("input[name='DefaultValues.SalePrice']", "149.90");
        await Page.FillAsync("input[name='DefaultValues.CostPrice']", "80");
        await Page.FillAsync("input[name='DefaultValues.Stock']", "25");

        // Select checkbox values if available
        var checkboxes = Page.Locator(".attr-checkbox");
        var cbCount = await checkboxes.CountAsync();
        if (cbCount >= 2)
        {
            await checkboxes.Nth(0).CheckAsync();
            await checkboxes.Nth(1).CheckAsync();
        }

        await Page.ClickAsync("#btn-generate");
        await Page.WaitForSelectorAsync("#variant-table-container table");

        var nextBtn = Page.Locator("#btn-step3-next");
        await Expect(nextBtn).ToBeEnabledAsync();
        await nextBtn.ClickAsync();
        await Expect(Page.Locator("[data-wizard-step='4']")).ToBeVisibleAsync();

        // Step 4: Skip images (optional)
        await Page.ClickAsync("button[type='submit']");
        await Expect(Page.Locator("[data-wizard-step='5']")).ToBeVisibleAsync();

        // Step 5: Review and save
        await Expect(Page.Locator(".datagrid")).ToBeVisibleAsync();
        await Page.ClickAsync("button.btn-success");
        await Expect(Page.Locator("[data-wizard-step='6']")).ToBeVisibleAsync();

        // Step 6: Verify success
        await Expect(Page.GetByText("basariyla eklendi")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Senkronizasyona Git")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Urun Listesine Don")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Yeni Urun Ekle")).ToBeVisibleAsync();
    }

    [Test]
    [Order(2)]
    public async Task Step1_BrandNotFound_ShowsAddModal()
    {
        await Page.GotoAsync($"{BaseUrl}/products/add");

        await Page.FillAsync("#brand-input", "MevdutOlmayanMarka" + DateTime.Now.Ticks);
        await Page.ClickAsync("input[name='Title']"); // Blur brand input

        await Expect(Page.Locator("#brand-add-modal")).ToBeVisibleAsync();
        await Expect(Page.GetByText("markasi bulunamadi")).ToBeVisibleAsync();
    }
}
```

- [ ] **Step 2: Run E2E tests (app must be running on localhost:5100)**

Run: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --filter "FullyQualifiedName~ProductWizardTests" -v minimal`
Expected: 2 PASS

- [ ] **Step 3: Fix any failures and re-run**

- [ ] **Step 4: Commit**

```bash
git add Test/Entegrasyon.E2E/Tests/P1_CoreFlows/ProductWizardTests.cs
git commit -m "test(product-wizard): E2E tests for full wizard flow and brand add modal"
```

---

## Task 12: Final Verification and Cleanup

- [ ] **Step 1: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All pass

- [ ] **Step 2: Run integration tests**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj -v minimal`
Expected: All pass

- [ ] **Step 3: Run E2E tests**

Run: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj -v minimal`
Expected: All pass

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat(product-wizard): complete 6-step product creation wizard"
```
