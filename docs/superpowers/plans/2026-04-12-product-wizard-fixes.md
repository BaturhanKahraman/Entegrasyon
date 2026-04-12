# Product Wizard — 5 Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wizard state koruma, validation düzeltmeleri, varsayılan stok kaldırma, "Atla Kaydet" exception fix ve ürün detay sayfasına mağaza ayarları ekleme.

**Architecture:** Session-based wizard state, FluentValidation pipeline, EF Core entity + migration, HTMX partial view pattern.

**Tech Stack:** ASP.NET Core 10 MVC, HTMX, Tabler UI, FluentValidation, EF Core 10 (PostgreSQL), Mapperly, Tom Select

**Spec:** `docs/superpowers/specs/2026-04-12-product-wizard-fixes-design.md`

---

## File Map

| Dosya | Eylem | Sorumluluk |
|---|---|---|
| `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml` | Modify | Fix 1: Seçili değerleri koruma |
| `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml` | Modify | Fix 2: DefaultStock kaldır, Fix 3: ListPrice label |
| `Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs` | Modify | Fix 2: DefaultStock kaldır, Fix 3: ListPrice nullable |
| `Application/Entegrasyon.Business/Validation/FluentValidation/AddProductVariantValidator.cs` | Modify | Fix 3: ListPrice opsiyonel |
| `Application/Entegrasyon.Business/Validation/FluentValidation/EditProductVariantValidator.cs` | Modify | Fix 3: ListPrice opsiyonel |
| `Test/Entegrasyon.Test/ValidationRules/AddProductVariantValidatorTests.cs` | Modify | Fix 3: Test güncelle |
| `Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs` | Modify | Fix 3: Test güncelle |
| `Application/Entegrasyon.MVC/Features/Products/ProductController.cs` | Modify | Fix 4: Session fallback, Fix 5: Store settings actions |
| `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Publish.cshtml` | Modify | Fix 4: Barcode hidden field |
| `Application/Entegrasyon.Entity/Products/Product.cs` | Modify | Fix 5: IsPublished ekle |
| `Application/Entegrasyon.Entity/Dtos/Product/ProductDetailDto.cs` | Modify | Fix 5: SEO + IsPublished alanları |
| `Application/Entegrasyon.Business/Concrete/ProductManager.cs` | Modify | Fix 5: UpdateStoreSettings, GetProductDetailById güncelle |
| `Application/Entegrasyon.Business/Abstract/IProductManager.cs` | Modify | Fix 5: UpdateStoreSettings interface |
| `Application/Entegrasyon.MVC/Features/Products/ViewModels/StoreSettingsVm.cs` | Create | Fix 5: Mağaza ayarları ViewModel |
| `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_StoreSettings.cshtml` | Create | Fix 5: Mağaza ayarları partial |
| `Application/Entegrasyon.MVC/Features/Products/Views/Detail.cshtml` | Modify | Fix 5: Mağaza ayarları card |

---

### Task 1: Fix 2 — Varsayılan Stok Alanını Kaldır

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs:131-138`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml:98-146`

- [ ] **Step 1: DefaultVariantValuesVm'den DefaultStock property'sini kaldır**

`Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs` — `DefaultVariantValuesVm` class'ından `DefaultStock` satırını sil:

```csharp
public class DefaultVariantValuesVm
{
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
    // DefaultStock KALDIRILDI — stok yönetimi şube bazlı yapılıyor
}
```

- [ ] **Step 2: Build edip DefaultStock referanslarını kontrol et**

Run: `dotnet build Entegrasyon.sln 2>&1 | grep -i "DefaultStock\|error CS"`
Expected: Hata varsa not et — view'daki referans da temizlenecek.

- [ ] **Step 3: _CreateStep3Variants.cshtml'den varsayılan stok input'unu kaldır**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml` — Satır 134-138 arasındaki "Varsayılan Stok" `<div>` bloğunu tamamen kaldır. Kalan 4 alan için grid'i `col-md-3` yap (daha dengeli görünüm):

Mevcut Bulk Defaults bölümü (satır 104-145) şu hale gelmeli:

```html
<div class="card-body">
    <div class="row">
        <div class="col-md-3 mb-3">
            <label class="form-label">Liste Fiyat</label>
            <div class="input-group">
                <span class="input-group-text">₺</span>
                <input type="text" name="DefaultValues.ListPrice" value="@Model.DefaultValues.ListPrice.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)"
                       class="form-control currency-input" inputmode="decimal" autocomplete="off" />
            </div>
        </div>
        <div class="col-md-3 mb-3">
            <label class="form-label">Satış Fiyat</label>
            <div class="input-group">
                <span class="input-group-text">₺</span>
                <input type="text" name="DefaultValues.SalePrice" value="@Model.DefaultValues.SalePrice.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)"
                       class="form-control currency-input" inputmode="decimal" autocomplete="off" />
            </div>
        </div>
        <div class="col-md-2 mb-3">
            <label class="form-label">Maliyet</label>
            <div class="input-group">
                <span class="input-group-text">₺</span>
                <input type="text" name="DefaultValues.CostPrice" value="@Model.DefaultValues.CostPrice.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)"
                       class="form-control currency-input" inputmode="decimal" autocomplete="off" />
            </div>
        </div>
        <div class="col-md-2 mb-3">
            <label class="form-label">KDV %</label>
            <input type="number" name="DefaultValues.VatRate" value="@(Model.DefaultValues.VatRate == 0 ? 20 : Model.DefaultValues.VatRate)"
                   class="form-control" step="1" min="0" max="100" />
        </div>
        <div class="col-md-2 mb-3 d-flex align-items-end">
            <button type="button" id="btn-generate-variants" class="btn btn-primary w-100">
                Varyantları Oluştur
            </button>
        </div>
    </div>
</div>
```

- [ ] **Step 4: Build ve doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Unit testleri çalıştır**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Tüm testler geçer

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml
git commit -m "fix(wizard): varsayılan stok alanını kaldır — stok yönetimi şube bazlı"
```

---

### Task 2: Fix 3 — ListPrice Opsiyonel, SalePrice Zorunlu (Test-First)

**Files:**
- Modify: `Test/Entegrasyon.Test/ValidationRules/AddProductVariantValidatorTests.cs`
- Modify: `Application/Entegrasyon.Business/Validation/FluentValidation/AddProductVariantValidator.cs`
- Modify: `Application/Entegrasyon.Business/Validation/FluentValidation/EditProductVariantValidator.cs`
- Modify: `Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml`

- [ ] **Step 1: AddProductVariantValidatorTests — mevcut testi güncelle + yeni testler yaz**

`Test/Entegrasyon.Test/ValidationRules/AddProductVariantValidatorTests.cs` — Mevcut `Should_Fail_WhenListPriceIsZero` testini kaldır (artık ListPrice 0 veya null valid). Yerine yeni testler ekle:

```csharp
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.UnitTest.ValidationRules;

public class AddProductVariantValidatorTests
{
    private readonly AddProductVariantValidator _validator = new();

    private static AddProductVariantDto ValidVariant() => new()
    {
        ListPrice = 100,
        SalePrice = 90,
        BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 5 }]
    };

    [Fact]
    public async Task Should_Pass_WhenListPriceIsNull()
    {
        var variant = ValidVariant();
        variant.ListPrice = null;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_WhenListPriceIsZero()
    {
        var variant = ValidVariant();
        variant.ListPrice = 0;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_WhenListPriceIsNegative()
    {
        var variant = ValidVariant();
        variant.ListPrice = -1;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Liste fiyatı negatif olamaz.");
    }

    [Fact]
    public async Task Should_Fail_WhenSalePriceIsZero()
    {
        var variant = ValidVariant();
        variant.SalePrice = 0;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Satış Fiyatı boş geçilemez");
    }

    [Fact]
    public async Task Should_Fail_WhenSalePriceIsNull()
    {
        var variant = ValidVariant();
        variant.SalePrice = null;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Satış Fiyatı boş geçilemez");
    }

    [Fact]
    public async Task Should_Fail_WhenBranchOfficeStocksIsEmpty()
    {
        var variant = ValidVariant();
        variant.BranchOfficeStocks = [];

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Lütfen stok değerlerini girin.");
    }

    [Fact]
    public async Task Should_Pass_WhenAllFieldsAreValid()
    {
        var result = await _validator.ValidateAsync(ValidVariant());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Pass_WhenListPriceIsNull_AndSalePriceIsSet()
    {
        var variant = ValidVariant();
        variant.ListPrice = null;
        variant.SalePrice = 50;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_WhenSalePriceExceedsListPrice()
    {
        var variant = ValidVariant();
        variant.ListPrice = 50;
        variant.SalePrice = 100;

        var result = await _validator.ValidateAsync(variant);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Satış fiyatı liste fiyatından büyük olamaz.");
    }
}
```

- [ ] **Step 2: Testleri çalıştır — FAIL bekleniyor**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AddProductVariantValidatorTests"`
Expected: `Should_Pass_WhenListPriceIsNull` ve `Should_Pass_WhenListPriceIsZero` FAIL (mevcut validator reddettiği için), `Should_Fail_WhenListPriceIsNegative` de FAIL (hata mesajı farklı)

- [ ] **Step 3: AddProductVariantValidator — ListPrice kuralını opsiyonel yap**

`Application/Entegrasyon.Business/Validation/FluentValidation/AddProductVariantValidator.cs`:

```csharp
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductVariantValidator : AbstractValidator<AddProductVariantDto>
{
    public AddProductVariantValidator()
    {
        RuleFor(x => x.ListPrice)
            .Must(v => !v.HasValue || v.Value >= 0)
            .WithMessage("Liste fiyatı negatif olamaz.");

        RuleFor(x => x.SalePrice).Must(v => v.HasValue && v.Value > 0).WithMessage("Satış Fiyatı boş geçilemez");
        RuleFor(x => x.BranchOfficeStocks).NotEmpty().WithMessage("Lütfen stok değerlerini girin.");
        RuleForEach(x => x.BranchOfficeStocks).SetValidator(new AddBranchOfficeStockValidator());

        RuleFor(x => x.VatRate)
            .Must(v => !v.HasValue || (v.Value >= 0 && v.Value <= 100))
            .WithMessage("KDV oranı 0-100 arasında olmalıdır.");

        RuleFor(x => x)
            .Must(x => !x.SalePrice.HasValue || !x.ListPrice.HasValue || x.SalePrice <= x.ListPrice)
            .WithMessage("Satış fiyatı liste fiyatından büyük olamaz.");
    }
}
```

- [ ] **Step 4: Testleri çalıştır — PASS bekleniyor**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AddProductVariantValidatorTests"`
Expected: Tüm testler PASS

- [ ] **Step 5: EditProductVariantValidator — ListPrice kuralını opsiyonel yap**

`Application/Entegrasyon.Business/Validation/FluentValidation/EditProductVariantValidator.cs`:

```csharp
using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditProductVariantValidator : AbstractValidator<EditProductVariantDto>
{
    public EditProductVariantValidator()
    {
        RuleFor(x => x.ListPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Liste fiyatı negatif olamaz.");

        RuleFor(x => x.SalePrice).GreaterThan(0).WithMessage("Satış fiyatı 0'dan büyük olmalıdır.");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("KDV oranı 0-100 arasında olmalıdır.");

        RuleFor(x => x.SalePrice)
            .LessThanOrEqualTo(x => x.ListPrice)
            .When(x => x.ListPrice > 0)
            .WithMessage("Satış fiyatı liste fiyatından büyük olamaz.");

        RuleFor(x => x.DimensionalWeight)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Desi 0 veya daha büyük olmalıdır.");
    }
}
```

**Not:** `EditProductVariantDto` record'unda `ListPrice` `decimal` (not nullable). `GreaterThanOrEqualTo(0)` 0 kabul eder. Karşılaştırma kuralına `.When(x => x.ListPrice > 0)` ekleniyor — ListPrice 0 ise SalePrice kısıtlaması uygulanmaz.

- [ ] **Step 6: EditProductValidatorTests güncelle**

`Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs` — `EditProductVariantDto` kullanan testlerde ListPrice=0 olan case'ler varsa pass etmeli. Dosyayı oku, mevcut `ListPrice` testlerini bul ve güncelle. Eğer `ListPrice=-1` testi varsa hata mesajını "Liste fiyatı negatif olamaz." olarak güncelle. Eğer `ListPrice=0` fail testi varsa kaldır veya pass beklentisine çevir.

- [ ] **Step 7: CreateVariantVm.ListPrice nullable yap**

`Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs` — `CreateVariantVm.ListPrice` ve `DefaultVariantValuesVm.ListPrice` alanlarını `decimal?` yap:

```csharp
public class CreateVariantVm
{
    public string Barcode { get; set; } = "";
    public decimal? ListPrice { get; set; }  // nullable — opsiyonel
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
    public decimal DimensionalWeight { get; set; }
    public decimal ECommercePrice { get; set; }
    public List<BranchOfficeStockVm> BranchOfficeStocks { get; set; } = [];
    public List<VariantAttributeValueVm> VariantAttributes { get; set; } = [];
}

public class DefaultVariantValuesVm
{
    public decimal? ListPrice { get; set; }  // nullable — opsiyonel
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
}
```

- [ ] **Step 8: _CreateStep3Variants.cshtml — ListPrice label güncelle**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml` — ListPrice label'ını güncelle (Task 1'den sonra bu `col-md-3` olmuş olacak):

```html
<div class="col-md-3 mb-3">
    <label class="form-label">Liste Fiyat <span class="text-muted fw-normal">(opsiyonel)</span></label>
    <div class="input-group">
        <span class="input-group-text">₺</span>
        <input type="text" name="DefaultValues.ListPrice" value="@(Model.DefaultValues.ListPrice?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))"
               class="form-control currency-input" inputmode="decimal" autocomplete="off" />
    </div>
    <small class="text-muted">Boş bırakılırsa yalnızca satış fiyatı kullanılır</small>
</div>
```

**Not:** `ListPrice?.ToString(...)` — nullable olduğu için `?.` ile çağrılmalı, null ise boş string render edilir.

- [ ] **Step 9: _CreateStep6Publish.cshtml — hidden field'da ListPrice nullable**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Publish.cshtml` satır 107:

```html
<input type="hidden" name="Variants[@i].ListPrice" value="@v.ListPrice" />
```

Bu zaten çalışır — `decimal?` null ise boş string olur, `CreateVariantVm.ListPrice` nullable olduğu için sorun yok.

- [ ] **Step 10: _VariantTable.cshtml — ListPrice label güncelle**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_VariantTable.cshtml` — Liste Fiyat sütun başlığına veya input'a "(opsiyonel)" ekle. Dosyayı oku ve güncelle.

- [ ] **Step 11: Build ve tüm testler**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: BUILD SUCCEEDED, tüm testler PASS

- [ ] **Step 12: Commit**

```bash
git add Application/Entegrasyon.Business/Validation/FluentValidation/AddProductVariantValidator.cs \
      Application/Entegrasyon.Business/Validation/FluentValidation/EditProductVariantValidator.cs \
      Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Publish.cshtml \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_VariantTable.cshtml \
      Test/Entegrasyon.Test/ValidationRules/AddProductVariantValidatorTests.cs \
      Test/Entegrasyon.Test/ValidationRules/EditProductValidatorTests.cs
git commit -m "fix(validation): ListPrice opsiyonel, SalePrice zorunlu kalacak"
```

---

### Task 3: Fix 4 — "Atla, Kaydet" Exception Fix

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs:371-393`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Publish.cshtml:104-129`

- [ ] **Step 1: _CreateStep6Publish.cshtml — Barcode hidden field ekle**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Publish.cshtml` satır 104-129'daki varyant hidden field loop'una `Barcode` ekle. Mevcut loop (satır 104):

```html
@for (int i = 0; i < Model.Variants.Count; i++)
{
    var v = Model.Variants[i];
    <input type="hidden" name="Variants[@i].Barcode" value="@v.Barcode" />
    <input type="hidden" name="Variants[@i].ListPrice" value="@v.ListPrice" />
    <input type="hidden" name="Variants[@i].SalePrice" value="@v.SalePrice" />
    <input type="hidden" name="Variants[@i].CostPrice" value="@v.CostPrice" />
    <input type="hidden" name="Variants[@i].VatRate" value="@v.VatRate" />
    <input type="hidden" name="Variants[@i].DimensionalWeight" value="@v.DimensionalWeight" />
    @for (int j = 0; j < v.VariantAttributes.Count; j++)
    {
        <!-- ... mevcut attribute hidden fields ... -->
    }
    @for (int b = 0; b < v.BranchOfficeStocks.Count; b++)
    {
        <!-- ... mevcut stock hidden fields ... -->
    }
}
```

Eklenecek tek satır: `<input type="hidden" name="Variants[@i].Barcode" value="@v.Barcode" />` — mevcut `ListPrice` hidden field'ından önce.

- [ ] **Step 2: CreateStep6 — Session fallback mekanizması ekle**

`Application/Entegrasyon.MVC/Features/Products/ProductController.cs` — `CreateStep6` metodunda (satır 373-393) Session'dan okunan varyant fiyatları 0 ise hidden field'lardan gelen değerleri fallback olarak kullan:

Mevcut kod (satır 388-390):
```csharp
// Merge ECommercePrice per variant
for (int i = 0; i < vm.Variants.Count && i < formVm.Variants.Count; i++)
    vm.Variants[i].ECommercePrice = formVm.Variants[i].ECommercePrice;
```

Yeni kod:
```csharp
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
```

- [ ] **Step 3: Build ve test**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: BUILD SUCCEEDED, tüm testler PASS

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ProductController.cs \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep6Publish.cshtml
git commit -m "fix(wizard): Atla Kaydet exception — Session fallback + Barcode hidden field"
```

---

### Task 4: Fix 1 — BackToStep2 State Koruma

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml`

- [ ] **Step 1: _CreateStep2Attributes.cshtml — model'den gelen değerleri form elementlerine yansıt**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml` — View'da `attrs` listesi (DB'den ViewBag) ile `Model.CategoryAttributes` (Session'dan) farklı sıralama/index'te olabilir. Eşleştirme `CategoryAttributeId` üzerinden yapılmalı.

Dosyanın tamamını şu şekilde güncelle:

```html
@model Entegrasyon.MVC.Features.Products.ViewModels.CreateProductVm
@using Entegrasyon.Entity.Dtos.Category

@{
    var attrs = (ViewBag.NonVariantAttributes as List<CategoryAttributeDto> ?? [])
        .OrderByDescending(a => a.IsRequired).ToList();
}

<div data-wizard-step="2">
<form hx-post="/products/add/step2" hx-target="#wizard-content" hx-swap="innerHTML">
    @Html.AntiForgeryToken()
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
            <h3 class="card-title">Kategori Özellikleri</h3>
            <span class="card-subtitle">@Model.CategoryName</span>
        </div>
        <div class="card-body">
            @if (attrs.Count == 0)
            {
                <div class="alert alert-info">Bu kategorinin ek özelliği bulunmamaktadır.</div>
            }
            else
            {
                <div class="row">
                    @for (int i = 0; i < attrs.Count; i++)
                    {
                        var attr = attrs[i];
                        var saved = Model.CategoryAttributes
                            .FirstOrDefault(a => a.CategoryAttributeId == attr.Id);
                        <div class="col-md-6 mb-3">
                            <label class="form-label @(attr.IsRequired ? "required fw-bold" : "")">
                                @attr.CategoriyAttributeHumanized
                            </label>
                            <input type="hidden" name="CategoryAttributes[@i].CategoryAttributeId" value="@attr.Id" />
                            <input type="hidden" name="CategoryAttributes[@i].AttributeName" value="@attr.CategoriyAttributeHumanized" />
                            <input type="hidden" name="CategoryAttributes[@i].IsRequired" value="@attr.IsRequired" />
                            <input type="hidden" name="CategoryAttributes[@i].AllowCustom" value="@attr.AllowCustom" />

                            @if (attr.AllowCustom)
                            {
                                <input type="text" name="CategoryAttributes[@i].CustomValue"
                                       class="form-control" placeholder="Değer girin..."
                                       value="@(saved?.CustomValue)"
                                       @(attr.IsRequired ? "required" : "") />
                            }
                            else
                            {
                                <select name="CategoryAttributes[@i].ValueId" class="form-select"
                                        @(attr.IsRequired ? "required" : "")>
                                    <option value="">Seçin...</option>
                                    @foreach (var val in attr.CategoryAttributeValues)
                                    {
                                        if (saved?.ValueId == val.Id)
                                        {
                                            <option value="@val.Id" selected>@val.Name</option>
                                        }
                                        else
                                        {
                                            <option value="@val.Id">@val.Name</option>
                                        }
                                    }
                                </select>
                            }
                        </div>
                    }
                </div>
            }
        </div>
        <div class="card-footer d-flex justify-content-between">
            <a href="/products/add/back-to-step1" hx-get="/products/add/back-to-step1" hx-target="#wizard-content" hx-swap="innerHTML" class="btn btn-ghost-secondary">&larr; Geri</a>
            <button type="submit" class="btn btn-primary">Devam: Varyantlar &rarr;</button>
        </div>
    </div>
</form>
</div>
```

**Kritik değişiklikler:**
1. `var saved = Model.CategoryAttributes.FirstOrDefault(a => a.CategoryAttributeId == attr.Id);` — DB attrs ile Session state'i `CategoryAttributeId` üzerinden eşleştirir
2. AllowCustom input'a `value="@(saved?.CustomValue)"` eklendi
3. Select option'lara `selected="@isSelected"` eklendi (`saved?.ValueId == val.Id` karşılaştırması)

**Not:** Razor'da `selected="@isSelected"` kullanmak doğru çalışmaz — `false` olduğunda `selected="False"` render eder ki HTML'de bu yine "selected" sayılır. Bu yüzden conditional `if/else` ile `selected` attribute'u yalnızca eşleşen option'a eklenir.

- [ ] **Step 2: Build ve test**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: BUILD SUCCEEDED, tüm testler PASS

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml
git commit -m "fix(wizard): BackToStep2 state koruma — seçili özellikler korunuyor"
```

---

### Task 5: Fix 5A — Entity + Migration: IsPublished + ProductDetailDto güncelle

**Files:**
- Modify: `Application/Entegrasyon.Entity/Products/Product.cs`
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/ProductDetailDto.cs`
- Migration: yeni migration dosyası

- [ ] **Step 1: Product entity'ye IsPublished ekle**

`Application/Entegrasyon.Entity/Products/Product.cs` — SEO alanlarından sonra:

```csharp
// SEO
public string? SeoTitle { get; set; }
public string? SeoDescription { get; set; }
public string? SeoSlug { get; set; }
public string? SeoKeywords { get; set; }

// Storefront
public bool IsPublished { get; set; }
```

- [ ] **Step 2: ProductDetailDto'ya SEO + IsPublished alanları ekle**

`Application/Entegrasyon.Entity/Dtos/Product/ProductDetailDto.cs`:

```csharp
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductDetailDto(
        Guid Id,
        string Title,
        string Description,
        string StockCode,
        string Season,
        string Year,
        int? BrandId,
        string BrandName,
        int CategoryId,
        string CategoryName,
        int TotalQuantity,
        int TotalSoldQuantity,
        IEnumerable<ProductVariantDetailDto> ProductVariantsDetails,
        IEnumerable<AttributeKeyValueDetailDto> AttributeKeyValueDetails,
        DateTimeOffset UpdatedAt,
        string? SeoTitle = null,
        string? SeoDescription = null,
        string? SeoSlug = null,
        string? SeoKeywords = null,
        bool IsPublished = false
    );
```

**Not:** Optional parametreler sona ekleniyor — mevcut çağrı noktaları kırılmaz (default değerleri var).

- [ ] **Step 3: ProductManager.GetProductDetailById güncelle**

`Application/Entegrasyon.Business/Concrete/ProductManager.cs` — `GetProductDetailById` metodundaki (satır 310) `ProductDetailDto` constructor çağrısına yeni alanları ekle. Mevcut `p.UpdatedAt` parametresinden sonra:

```csharp
.Select(p => new ProductDetailDto(
    p.Id, p.Title, p.Description ?? "", p.StockCode ?? "", p.Season ?? "", p.Year ?? "", p.BrandId, p.Brand!.Name!, p.CategoryId, p.Category!.Name!,
    p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
    p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
    p.ProductVariants.Select(pv => new ProductVariantDetailDto(
        pv.Id, pv.Barcode!, pv.DimensionalWeight, pv.CurrencyType, pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.ECommercePrice, pv.VatRate,
        pv.Images.OrderBy(img => img.DisplayOrder)
            .Select(img => img.StorageKey != null ? img.StorageKey + "_original.webp" : img.Src ?? "")
            .ToArray(),
        pv.BranchOfficeStocks.Select(stck => new StockDetailDto(stck.BranchOffice.Name!, stck.CurrentStock, stck.SoldQuantity, stck.FirstTotalStock))
    )),
    p.AttributeKeyValues.Select(kv => new AttributeKeyValueDetailDto(
        kv.CategoryAttribute.CategoryAttributeKey!,
        kv.CategoryAttribute!.CategoryAttributeHumanized ?? kv.CategoryAttribute.CategoryAttributeKey ?? "",
        kv.AttributeValueId.HasValue ? kv.AttributeValue!.Name! : kv.CustomValue ?? "")),
    p.UpdatedAt,
    p.SeoTitle,
    p.SeoDescription,
    p.SeoSlug,
    p.SeoKeywords,
    p.IsPublished
))
```

- [ ] **Step 4: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Migration oluştur ve uygula**

```bash
dotnet ef migrations add AddIsPublishedToProduct \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext

dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Migration dosyasını incele — sadece `IsPublished` (bool, default false) kolonu eklenmeli, başka değişiklik olmamalı.

- [ ] **Step 6: Pending model changes kontrolü**

```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: "No pending model changes"

- [ ] **Step 7: Testler**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Tüm testler PASS

- [ ] **Step 8: Commit**

```bash
git add Application/Entegrasyon.Entity/Products/Product.cs \
      Application/Entegrasyon.Entity/Dtos/Product/ProductDetailDto.cs \
      Application/Entegrasyon.Business/Concrete/ProductManager.cs \
      Application/Entegrasyon.DataAccess/Migrations/
git commit -m "feat(product): IsPublished alanı + ProductDetailDto SEO alanları + migration"
```

---

### Task 6: Fix 5B — StoreSettingsVm + Business Layer

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Products/ViewModels/StoreSettingsVm.cs`
- Modify: `Application/Entegrasyon.Business/Abstract/IProductManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs`

- [ ] **Step 1: StoreSettingsVm oluştur**

`Application/Entegrasyon.MVC/Features/Products/ViewModels/StoreSettingsVm.cs`:

```csharp
namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class StoreSettingsVm
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public bool IsPublished { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public List<VariantStorePriceVm> VariantPrices { get; set; } = [];
}

public class VariantStorePriceVm
{
    public Guid VariantId { get; set; }
    public string VariantName { get; set; } = "";
    public decimal SalePrice { get; set; }
    public decimal ECommercePrice { get; set; }
}
```

- [ ] **Step 2: IProductService'e UpdateStoreSettings ekle**

`Application/Entegrasyon.Business/Abstract/IProductManager.cs` — interface'e ekle:

```csharp
// Store Settings
Task<IResult> UpdateStoreSettings(Guid productId, string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords, Dictionary<Guid, decimal> variantECommercePrices);
Task<IResult> PublishProduct(Guid productId, string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords, Dictionary<Guid, decimal> variantECommercePrices);
```

- [ ] **Step 3: ProductManager — UpdateStoreSettings implement et**

`Application/Entegrasyon.Business/Concrete/ProductManager.cs` — dosyanın sonuna (class kapanışından önce) ekle:

```csharp
public async Task<IResult> UpdateStoreSettings(
    Guid productId,
    string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords,
    Dictionary<Guid, decimal> variantECommercePrices)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var product = await dbContext.MainProducts
        .Include(p => p.ProductVariants)
        .FirstOrDefaultAsync(p => p.Id == productId);

    if (product is null)
        return new ErrorResult("Ürün bulunamadı.");

    // Slug unique kontrolü
    if (!string.IsNullOrWhiteSpace(seoSlug))
    {
        var slugExists = await dbContext.MainProducts
            .AnyAsync(p => p.SeoSlug == seoSlug && p.Id != productId && !p.IsDeleted);
        if (slugExists)
            return new ErrorResult("Bu SEO URL zaten başka bir ürün tarafından kullanılıyor.");
    }

    product.SeoTitle = seoTitle;
    product.SeoDescription = seoDescription;
    product.SeoSlug = seoSlug;
    product.SeoKeywords = seoKeywords;

    foreach (var variant in product.ProductVariants)
    {
        if (variantECommercePrices.TryGetValue(variant.Id, out var ecomPrice))
            variant.ECommercePrice = ecomPrice;
    }

    await dbContext.SaveChangesAsync();
    await applicationLogManager.AddLog("Mağaza ayarları güncellendi.", LogType.Product, LogAction.Update, "Product", productId.ToString());
    return new SuccessResult("Mağaza ayarları kaydedildi.");
}

public async Task<IResult> PublishProduct(
    Guid productId,
    string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords,
    Dictionary<Guid, decimal> variantECommercePrices)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var product = await dbContext.MainProducts
        .Include(p => p.ProductVariants)
        .FirstOrDefaultAsync(p => p.Id == productId);

    if (product is null)
        return new ErrorResult("Ürün bulunamadı.");

    // Slug unique kontrolü
    if (!string.IsNullOrWhiteSpace(seoSlug))
    {
        var slugExists = await dbContext.MainProducts
            .AnyAsync(p => p.SeoSlug == seoSlug && p.Id != productId && !p.IsDeleted);
        if (slugExists)
            return new ErrorResult("Bu SEO URL zaten başka bir ürün tarafından kullanılıyor.");
    }

    product.SeoTitle = seoTitle;
    product.SeoDescription = seoDescription;
    product.SeoSlug = seoSlug;
    product.SeoKeywords = seoKeywords;
    product.IsPublished = true;

    foreach (var variant in product.ProductVariants)
    {
        if (variantECommercePrices.TryGetValue(variant.Id, out var ecomPrice))
            variant.ECommercePrice = ecomPrice;
    }

    await dbContext.SaveChangesAsync();
    await applicationLogManager.AddLog("Ürün mağazada yayınlandı.", LogType.Product, LogAction.Update, "Product", productId.ToString());
    return new SuccessResult("Ürün mağazada yayınlandı.");
}
```

- [ ] **Step 4: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Unit testler**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Tüm testler PASS

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ViewModels/StoreSettingsVm.cs \
      Application/Entegrasyon.Business/Abstract/IProductManager.cs \
      Application/Entegrasyon.Business/Concrete/ProductManager.cs
git commit -m "feat(product): UpdateStoreSettings + PublishProduct business layer"
```

---

### Task 7: Fix 5C — Controller Actions + Partial View + Detail Entegrasyonu

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs`
- Create: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_StoreSettings.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Detail.cshtml`

- [ ] **Step 1: ProductController — Store Settings action'ları ekle**

`Application/Entegrasyon.MVC/Features/Products/ProductController.cs` — sınıfın sonuna (mevcut action'lardan sonra) ekle:

```csharp
// ── Store Settings ────────────────────────────────────────────────

[HttpGet("/products/{id:guid}/store-settings")]
public async Task<IActionResult> StoreSettings(Guid id)
{
    var result = await productService.GetProductDetailById(id);
    if (!result.Success || result.Data is null)
        return NotFound();

    var detail = result.Data;
    // ProductVariantDetailDto'da attribute bilgisi yok, DB'den çekelim
    var variantAttrNames = await GetVariantAttributeNames(detail.Id);

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
    {
        TempData.SetSuccess(result.Message!);
        Response.HtmxTrigger("storeSettingsUpdated");
    }
    else
    {
        TempData.SetError(result.Message!);
    }

    return await StoreSettings(id);
}

[HttpPost("/products/{id:guid}/store-settings/publish")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PublishToStore(Guid id, StoreSettingsVm vm)
{
    var prices = vm.VariantPrices.ToDictionary(v => v.VariantId, v => v.ECommercePrice);
    var result = await productService.PublishProduct(id, vm.SeoTitle, vm.SeoDescription, vm.SeoSlug, vm.SeoKeywords, prices);

    if (result.Success)
    {
        TempData.SetSuccess(result.Message!);
        Response.HtmxTrigger("storeSettingsUpdated");
    }
    else
    {
        TempData.SetError(result.Message!);
    }

    return await StoreSettings(id);
}
```

**Not:** `ProductVariantDetailDto`'da varyant attribute bilgisi yok (sadece Barcode, fiyatlar, stok). Varyant isimleri (örn: "Kırmızı - XL") için `GetVariantAttributeNames` helper'ı DB'den çekiyor. Bu helper için `IProductVariantManager` veya doğrudan `IDbContextFactory` kullanılmalı — implementasyonda mevcut servislere göre uyarla. Eğer mevcut servis yoksa Barcode'u fallback olarak kullan (kodda zaten mevcut).

- [ ] **Step 2: _StoreSettings.cshtml partial oluştur**

`Application/Entegrasyon.MVC/Features/Products/Views/Partials/_StoreSettings.cshtml`:

```html
@model Entegrasyon.MVC.Features.Products.ViewModels.StoreSettingsVm

<div id="store-settings-card">
    <div class="card">
        <div class="card-header">
            <h3 class="card-title">
                Mağaza Ayarları
                @if (Model.IsPublished)
                {
                    <span class="badge bg-green-lt ms-2">Yayında</span>
                }
                else
                {
                    <span class="badge bg-secondary-lt ms-2">Taslak</span>
                }
            </h3>
        </div>
        <div class="card-body">
            <h4 class="mb-3">SEO Bilgileri</h4>
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label class="form-label">SEO Başlık</label>
                    <input type="text" name="SeoTitle" value="@Model.SeoTitle" class="form-control"
                           placeholder="@Model.ProductTitle" maxlength="70" />
                    <small class="text-muted">Boş bırakılırsa ürün adı kullanılır (maks. 70 karakter)</small>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label">SEO URL (Slug)</label>
                    <input type="text" name="SeoSlug" value="@Model.SeoSlug" class="form-control"
                           placeholder="ornek-urun-adi" />
                    <small class="text-muted">Boş bırakılırsa otomatik oluşturulur</small>
                </div>
                <div class="col-12 mb-3">
                    <label class="form-label">SEO Açıklama</label>
                    <textarea name="SeoDescription" class="form-control" rows="2" maxlength="160"
                              placeholder="Arama motorlarında görünecek kısa açıklama...">@Model.SeoDescription</textarea>
                    <small class="text-muted">Maks. 160 karakter</small>
                </div>
                <div class="col-12 mb-3">
                    <label class="form-label">SEO Anahtar Kelimeler</label>
                    <input type="text" name="SeoKeywords" value="@Model.SeoKeywords" class="form-control"
                           placeholder="anahtar, kelime, virgül, ile" />
                </div>
            </div>

            @if (Model.VariantPrices.Count > 0)
            {
                <h4 class="mb-3 mt-4">Mağaza Fiyatları</h4>
                <p class="text-muted small mb-3">Her varyant için mağazada görünecek fiyatı belirleyin. Boş bırakılırsa satış fiyatı kullanılır.</p>
                <div class="table-responsive">
                    <table class="table table-vcenter table-sm">
                        <thead>
                            <tr>
                                <th>Varyant</th>
                                <th>Satış Fiyatı</th>
                                <th>Mağaza Fiyatı</th>
                            </tr>
                        </thead>
                        <tbody>
                            @for (int i = 0; i < Model.VariantPrices.Count; i++)
                            {
                                var v = Model.VariantPrices[i];
                                <tr>
                                    <td>
                                        <input type="hidden" name="VariantPrices[@i].VariantId" value="@v.VariantId" />
                                        <input type="hidden" name="VariantPrices[@i].VariantName" value="@v.VariantName" />
                                        <input type="hidden" name="VariantPrices[@i].SalePrice" value="@v.SalePrice" />
                                        <span class="fw-bold">@v.VariantName</span>
                                    </td>
                                    <td class="text-muted">@v.SalePrice.ToString("N2") TL</td>
                                    <td>
                                        <div class="input-group input-group-sm" style="width:160px">
                                            <span class="input-group-text">₺</span>
                                            <input type="text" name="VariantPrices[@i].ECommercePrice"
                                                   value="@v.ECommercePrice.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)"
                                                   class="form-control currency-input" inputmode="decimal" autocomplete="off" />
                                        </div>
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
        </div>
        <div class="card-footer d-flex justify-content-end gap-2">
            <button type="submit" class="btn btn-outline-primary"
                    hx-post="/products/@Model.ProductId/store-settings"
                    hx-target="#store-settings-card"
                    hx-swap="outerHTML"
                    hx-include="closest .card">
                Kaydet
            </button>
            <button type="submit" class="btn btn-primary"
                    hx-post="/products/@Model.ProductId/store-settings/publish"
                    hx-target="#store-settings-card"
                    hx-swap="outerHTML"
                    hx-include="closest .card">
                Kaydet ve Yayınla
            </button>
        </div>
    </div>
</div>
```

**Not:** `hx-include="closest .card"` ile form tag'ı olmadan card içindeki tüm input'ları toplar. Anti-forgery token için card body'ye `@Html.AntiForgeryToken()` eklenmeli — card-body'nin başına ekle.

Düzeltme — card-body'nin başına ekle:
```html
<div class="card-body">
    @Html.AntiForgeryToken()
    <h4 class="mb-3">SEO Bilgileri</h4>
```

- [ ] **Step 3: Detail.cshtml — Mağaza Ayarları card ekle**

`Application/Entegrasyon.MVC/Features/Products/Views/Detail.cshtml` — Variants card'ından sonra (satır 173'ten sonra) ekle:

```html
        <!-- Store Settings (lazy loaded) -->
        <div class="mt-3"
             hx-get="/products/@Model.Id/store-settings"
             hx-trigger="load"
             hx-swap="innerHTML">
            <div class="card">
                <div class="card-body">
                    <div class="placeholder-glow">
                        <div class="placeholder col-4 mb-2"></div>
                        <div class="placeholder col-6"></div>
                    </div>
                </div>
            </div>
        </div>
```

Ayrıca üst buton grubuna (satır 7-35 arası) yayın durumu badge'i ekle. `Detail.cshtml`'in model'i `ProductDetailDto` — artık `IsPublished` alanı var. Mevcut buton listesinin başına:

```html
<div class="d-flex justify-content-between align-items-center mb-3">
    <div>
        @if (Model.IsPublished)
        {
            <span class="badge bg-green-lt">Mağazada Yayında</span>
        }
        else
        {
            <span class="badge bg-secondary-lt">Mağazada Yayınlanmadı</span>
        }
    </div>
    <div class="btn-list">
        <!-- mevcut butonlar -->
    </div>
</div>
```

- [ ] **Step 4: Build ve test**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: BUILD SUCCEEDED, tüm testler PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ProductController.cs \
      Application/Entegrasyon.MVC/Features/Products/Views/Partials/_StoreSettings.cshtml \
      Application/Entegrasyon.MVC/Features/Products/Views/Detail.cshtml
git commit -m "feat(product): ürün detay sayfasına mağaza ayarları card'ı ekle"
```

---

### Task 8: Tüm Testleri Çalıştır + Final Doğrulama

**Files:** Hiçbir dosya değiştirilmez — sadece doğrulama.

- [ ] **Step 1: Full build**

Run: `dotnet build Entegrasyon.sln`
Expected: BUILD SUCCEEDED, 0 hata

- [ ] **Step 2: Unit testler**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Tüm testler PASS

- [ ] **Step 3: MVC testler**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj`
Expected: Tüm testler PASS

- [ ] **Step 4: Integration testler (Docker gerekli)**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: Tüm testler PASS

- [ ] **Step 5: Pending model changes kontrolü**

Run: `dotnet ef migrations has-pending-model-changes -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`
Expected: "No pending model changes"
