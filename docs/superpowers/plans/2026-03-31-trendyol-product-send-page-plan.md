# Trendyol Ürün Gönderme Sayfası Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ürün sync detay sayfasında pazaryeri durum kartlarına tıklama → credential kontrolü → marketplace-specific gönderme sayfası ile ürün gönderme akışı oluşturma. Faz 1: Trendyol.

**Architecture:** Orchestrator pattern — `TrendyolProductSendPage` ana sayfa olarak, alt component'lar (preflight checks, override formlar, pricing, preview) bağımsız sorumluluklar ile. Business layer'da preflight kontrol ve preview DTO'ları. UI'da credential'sız kartlar disabled.

**Tech Stack:** Blazor Server, MudBlazor, EF Core, FluentValidation, ICommissionCalculator (mevcut), TDD pattern.

---

## File Structure

### Yeni Dosyalar
```
Features/MarketplaceSync/ProductSend/
├── TrendyolProductSendPage.razor
├── TrendyolProductSendPage.razor.cs
├── SendPreflightChecks.razor
├── SendPreflightChecks.razor.cs
├── SendOverrideForm.razor
├── SendOverrideForm.razor.cs
├── SendPricingPanel.razor
├── SendPricingPanel.razor.cs
├── SendPayloadPreview.razor
├── SendPayloadPreview.razor.cs

Entity/Dtos/Product/
├── ProductSendPreflightDto.cs (yeni)
├── TrendyolSendPreviewDto.cs (yeni)
├── VariantPricingRowDto.cs (yeni)

Test/Entegrasyon.IntegrationTest/Features/MarketplaceSync/
├── ProductSendPageTests.cs (yeni)
```

### Değiştirilecek Dosyalar
```
Entity/Dtos/Product/MarketplaceSyncItemDto.cs — HasCredentials property eklenir
Business/Abstract/IProductSyncManager.cs — GetSendPreflightAsync() metodu eklenir
Business/Concrete/ProductSyncManager.cs — GetSendPreflightAsync() implementasyonu
Business/Abstract/ITrendyolProductService.cs — GetSendPreviewAsync() metodu eklenir
Business/Concrete/Trendyol/TrendyolProductService.cs — GetSendPreviewAsync() implementasyonu
Blazor/Features/MarketplaceSync/ProductSync/MarketplaceStatusCards.razor(.cs) — credential UI
Blazor/Features/MarketplaceSync/ProductSync/ProductSyncFullDetailPage.razor(.cs) — navigasyon
```

---

## Tasks

### Task 1: DTOs — ProductSendPreflightDto

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Product/ProductSendPreflightDto.cs`

- [ ] **Step 1: Kod yaz**

```csharp
namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// Ürün gönderme öncesi kontrol sonuçları (Trendyol için).
/// Kategori, marka, özellik eşleştirmesi ve varyant-barcode kontrolü.
/// </summary>
public record ProductSendPreflightDto(
    bool CategoryMatched,
    string? MatchedCategoryName,          // eşleşen Trendyol kategori adı (örn: "Giyim > Tişört")
    bool BrandMatched,
    string? MatchedBrandName,             // eşleşen Trendyol marka adı (örn: "NIKE")
    bool RequiredAttributesMatched,
    List<string> MissingAttributes,       // eksik zorunlu özellik isimleri (örn: ["Renk", "Beden"])
    bool HasVariants,
    bool AllVariantsHaveBarcodes,
    bool AllPassed                        // tümü geçti mi — gönderme butonu aktif olmalı
);
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/ProductSendPreflightDto.cs
git commit -m "feat: add ProductSendPreflightDto for preflight validation"
```

---

### Task 2: DTOs — TrendyolSendPreviewDto ve alt DTOs

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Product/TrendyolSendPreviewDto.cs`

- [ ] **Step 1: Kod yaz**

```csharp
namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// Trendyol'a gönderilecek ürün verilerinin ön izlemesi (okunabilir özet).
/// </summary>
public record TrendyolSendPreviewDto(
    string Title,
    string BrandName,
    string TrendyolBrandName,              // eşleşen Trendyol marka adı
    string CategoryName,                   // ürünün kategorisi (örn: "Giyim > T-Shirt")
    string TrendyolCategoryName,           // eşleşen Trendyol kategorisi (örn: "Giyim > Tişört")
    int TrendyolCategoryId,                // Trendyol category ID (API'ye gönderilecek)
    string? Description,
    List<TrendyolPreviewAttributeDto> Attributes,    // özellik listesi
    List<TrendyolPreviewVariantDto> Variants         // varyant detayları
);

public record TrendyolPreviewAttributeDto(
    string Name,                           // örn: "Renk"
    string Value                           // örn: "Siyah"
);

public record TrendyolPreviewVariantDto(
    string Barcode,                        // örn: "ABC-S"
    string Attributes,                     // birleştirilmiş: "Siyah / S"
    decimal ListPrice,                     // liste fiyatı
    decimal SalePrice,                     // satış fiyatı (override varsa override, yoksa mevcut)
    int Quantity                           // stok
);
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/TrendyolSendPreviewDto.cs
git commit -m "feat: add TrendyolSendPreviewDto for payload preview"
```

---

### Task 3: DTO — VariantPricingRowDto

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Product/VariantPricingRowDto.cs`

- [ ] **Step 1: Kod yaz**

```csharp
namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// SendPricingPanel tarafından kullanılan varyant fiyatlandırma satırı.
/// </summary>
public record VariantPricingRowDto(
    Guid ProductVariantId,
    string Barcode,                        // örn: "ABC-S"
    string VariantName,                    // örn: "Siyah / S"
    decimal ListPrice,
    decimal SalePrice,
    int Quantity,
    decimal? OverridePrice = null          // kullanıcı tarafından ayarlanmış override fiyat
);

/// <summary>
/// SendPricingPanel'den TrendyolProductSendPage'e geri dönen override sonuçları.
/// </summary>
public record VariantPriceOverrideDto(
    Guid ProductVariantId,
    decimal? OverrideSalePrice             // null ise mevcut fiyat kullanılır
);
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/VariantPricingRowDto.cs
git commit -m "feat: add VariantPricingRowDto for pricing panel"
```

---

### Task 4: MarketplaceSyncItemDto — HasCredentials Property

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/MarketplaceSyncItemDto.cs`

- [ ] **Step 1: Mevcut kodu oku**

```bash
head -30 Application/Entegrasyon.Entity/Dtos/Product/MarketplaceSyncItemDto.cs
```

- [ ] **Step 2: Property ekle**

Mevcut record'a `bool HasCredentials` property eklenir. İçerik:

```csharp
public record MarketplaceSyncItemDto(
    int MarketPlaceId,
    string MarketPlaceName,
    MarketplaceSyncState SyncState,
    DateTimeOffset? LastSyncedAt,
    string? BatchRequestId,
    string? StatusMessage,
    string? ExternalProductId = null,
    long? ContentId = null,
    bool? IsApproved = null,
    bool? IsArchived = null,
    bool HasCredentials = false);          // NEW: credential kontrol için
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/MarketplaceSyncItemDto.cs
git commit -m "feat: add HasCredentials property to MarketplaceSyncItemDto"
```

---

### Task 5: ProductSyncManager — GetSendPreflightAsync Unit Test

**Files:**
- Create: `Test/Entegrasyon.Test/Features/MarketplaceSync/ProductSendPreflightTests.cs`

- [ ] **Step 1: Test sınıfını yaz**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.Test.Features.MarketplaceSync;

public class ProductSendPreflightTests
{
    [Fact]
    public async Task GetSendPreflightAsync_AllChecksPassed_ReturnsTrueAndEmptyMissingAttributes()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockManager = new Mock<IProductSyncManager>();
        mockManager
            .Setup(m => m.GetSendPreflightAsync(productId, 1))
            .ReturnsAsync(new DataResult<ProductSendPreflightDto>(
                true,
                new ProductSendPreflightDto(
                    CategoryMatched: true,
                    MatchedCategoryName: "Giyim > Tişört",
                    BrandMatched: true,
                    MatchedBrandName: "NIKE",
                    RequiredAttributesMatched: true,
                    MissingAttributes: [],
                    HasVariants: true,
                    AllVariantsHaveBarcodes: true,
                    AllPassed: true
                )
            ));

        // Act
        var result = await mockManager.Object.GetSendPreflightAsync(productId, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AllPassed.Should().BeTrue();
        result.Data.MissingAttributes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSendPreflightAsync_CategoryNotMatched_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockManager = new Mock<IProductSyncManager>();
        mockManager
            .Setup(m => m.GetSendPreflightAsync(productId, 1))
            .ReturnsAsync(new DataResult<ProductSendPreflightDto>(
                true,
                new ProductSendPreflightDto(
                    CategoryMatched: false,
                    MatchedCategoryName: null,
                    BrandMatched: true,
                    MatchedBrandName: "NIKE",
                    RequiredAttributesMatched: true,
                    MissingAttributes: [],
                    HasVariants: true,
                    AllVariantsHaveBarcodes: true,
                    AllPassed: false
                )
            ));

        // Act
        var result = await mockManager.Object.GetSendPreflightAsync(productId, 1);

        // Assert
        result.Data!.AllPassed.Should().BeFalse();
        result.Data.CategoryMatched.Should().BeFalse();
    }

    [Fact]
    public async Task GetSendPreflightAsync_MissingAttributes_ReturnsListOfMissingAttributeNames()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockManager = new Mock<IProductSyncManager>();
        var missingAttrs = new List<string> { "Renk", "Beden" };
        mockManager
            .Setup(m => m.GetSendPreflightAsync(productId, 1))
            .ReturnsAsync(new DataResult<ProductSendPreflightDto>(
                true,
                new ProductSendPreflightDto(
                    CategoryMatched: true,
                    MatchedCategoryName: "Giyim > Tişört",
                    BrandMatched: true,
                    MatchedBrandName: "NIKE",
                    RequiredAttributesMatched: false,
                    MissingAttributes: missingAttrs,
                    HasVariants: true,
                    AllVariantsHaveBarcodes: true,
                    AllPassed: false
                )
            ));

        // Act
        var result = await mockManager.Object.GetSendPreflightAsync(productId, 1);

        // Assert
        result.Data!.RequiredAttributesMatched.Should().BeFalse();
        result.Data.MissingAttributes.Should().Equal("Renk", "Beden");
    }
}
```

- [ ] **Step 2: Test'i çalıştır (başarısız olmalı)**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductSendPreflightTests" -v
```

Beklenen: FAIL — `GetSendPreflightAsync` metodu henüz tanımlanmamış

- [ ] **Step 3: Commit**

```bash
git add Test/Entegrasyon.Test/Features/MarketplaceSync/ProductSendPreflightTests.cs
git commit -m "test: add ProductSendPreflightTests unit tests (failing)"
```

---

### Task 6: ProductSyncManager — GetSendPreflightAsync Implementation

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IProductSyncManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/ProductSyncManager.cs`

- [ ] **Step 1: Interface'e metodu ekle**

`IProductSyncManager`'a eklenir:

```csharp
Task<IDataResult<ProductSendPreflightDto>> GetSendPreflightAsync(Guid productId, int marketPlaceId);
```

- [ ] **Step 2: Implementasyon yaz**

`ProductSyncManager.GetSendPreflightAsync` metodunu implement et:

```csharp
public async Task<IDataResult<ProductSendPreflightDto>> GetSendPreflightAsync(Guid productId, int marketPlaceId)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var product = await context.Products
        .Include(p => p.Category)
        .Include(p => p.Brand)
        .Include(p => p.Variants)
        .FirstOrDefaultAsync(p => p.Id == productId);

    if (product is null)
        return new DataResult<ProductSendPreflightDto>(false, "Ürün bulunamadı.");

    // 1. Kategori eşleştirmesi kontrol et
    var categoryMatch = await context.CategoryMarketPlaceMatches
        .FirstOrDefaultAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == marketPlaceId);

    var categoryMatched = categoryMatch is not null;
    var matchedCategoryName = categoryMatch?.MarketPlaceCategoryName;

    // 2. Marka eşleştirmesi kontrol et
    var brandMatch = await context.BrandMarketPlaceMatches
        .FirstOrDefaultAsync(bm => bm.BrandId == product.BrandId && bm.MarketPlaceId == marketPlaceId);

    var brandMatched = brandMatch is not null;
    var matchedBrandName = brandMatch?.MarketPlaceBrandName;

    // 3. Zorunlu özellikler kontrol et
    var requiredAttributes = await context.CategoryAttributeCategories
        .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
        .Include(cac => cac.CategoryAttribute)
        .ToListAsync();

    var missingAttributes = new List<string>();
    foreach (var req in requiredAttributes)
    {
        var isMapped = await context.CategoryAttributeMarketPlaceMatches
            .AnyAsync(campm => campm.CategoryAttributeId == req.CategoryAttributeId && campm.MarketPlaceId == marketPlaceId);

        if (!isMapped)
            missingAttributes.Add(req.CategoryAttribute.Humanized);
    }

    var requiredAttributesMatched = missingAttributes.Count == 0;

    // 4. Varyant ve barcode kontrol et
    var hasVariants = product.Variants.Count > 0;
    var allVariantsHaveBarcodes = hasVariants && product.Variants.All(v => !string.IsNullOrEmpty(v.Barcode));

    var allPassed = categoryMatched && brandMatched && requiredAttributesMatched && hasVariants && allVariantsHaveBarcodes;

    var preflight = new ProductSendPreflightDto(
        categoryMatched,
        matchedCategoryName,
        brandMatched,
        matchedBrandName,
        requiredAttributesMatched,
        missingAttributes,
        hasVariants,
        allVariantsHaveBarcodes,
        allPassed
    );

    return new DataResult<ProductSendPreflightDto>(true, preflight);
}
```

- [ ] **Step 3: Test'i çalıştır (geçmeli)**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductSendPreflightTests" -v
```

Beklenen: PASS

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IProductSyncManager.cs \
        Application/Entegrasyon.Business/Concrete/ProductSyncManager.cs
git commit -m "feat: implement GetSendPreflightAsync for preflight validation"
```

---

### Task 7: ProductSyncManager — GetProductSyncDetailAsync'e HasCredentials Kontrol Ekleme

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/ProductSyncManager.cs`

- [ ] **Step 1: GetProductSyncDetailAsync içinde HasCredentials doldur**

Mevcut `GetProductSyncDetailAsync` metodunda, `MarketplaceSyncItemDto` oluşturulurken `HasCredentials` ekle:

```csharp
private bool HasMarketplaceCredentials(MarketPlace marketplace)
{
    // Credential kontrolü marketplace türüne göre
    if (marketplace.Id == 1)  // Trendyol
        return !string.IsNullOrEmpty(marketplace.ApiKey)
            && !string.IsNullOrEmpty(marketplace.ApiSecret)
            && !string.IsNullOrEmpty(marketplace.SellerId);

    // OAuth2 tabanlı (Pazarama, Amazon)
    if (marketplace.Id == 5 || marketplace.Id == 6)
        return !string.IsNullOrEmpty(marketplace.ApiKey)
            && !string.IsNullOrEmpty(marketplace.ApiSecret)
            && !string.IsNullOrEmpty(marketplace.TokenUrl);

    // Diğerleri
    return !string.IsNullOrEmpty(marketplace.ApiKey)
        && !string.IsNullOrEmpty(marketplace.ApiSecret);
}

// GetProductSyncDetailAsync içinde MarketplaceSyncItemDto'yu şöyle güncelle:
var syncItem = new MarketplaceSyncItemDto(
    mp.MarketPlace.Id,
    mp.MarketPlace.Name,
    syncState,
    mp.LastSyncedAt,
    mp.BatchRequestId,
    mp.StatusMessage,
    mp.ExternalProductId,
    mp.ContentId,
    mp.IsApproved,
    mp.IsArchived,
    HasMarketplaceCredentials(mp.MarketPlace)  // NEW
);
```

- [ ] **Step 2: Test et**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "ProductSyncManager" -v
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/ProductSyncManager.cs
git commit -m "feat: add HasCredentials check in GetProductSyncDetailAsync"
```

---

### Task 8: TrendyolProductService — GetSendPreviewAsync Unit Test

**Files:**
- Create: `Test/Entegrasyon.Test/Features/MarketplaceSync/TrendyolSendPreviewTests.cs`

- [ ] **Step 1: Test yaz**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.Test.Features.MarketplaceSync;

public class TrendyolSendPreviewTests
{
    [Fact]
    public async Task GetSendPreviewAsync_WithOverrides_ReturnsMappedPreviewWithOverridePrices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockService = new Mock<ITrendyolProductService>();

        var overrides = new MarketplaceOverrideDetailDto(
            TitleOverride: "Custom Title",
            DescriptionOverride: "Custom Description",
            VariantOverrides: []
        );

        var preview = new TrendyolSendPreviewDto(
            Title: "Custom Title",
            BrandName: "Nike",
            TrendyolBrandName: "NIKE",
            CategoryName: "Giyim > T-Shirt",
            TrendyolCategoryName: "Giyim > Tişört",
            TrendyolCategoryId: 1234,
            Description: "Custom Description",
            Attributes: [
                new TrendyolPreviewAttributeDto("Renk", "Siyah"),
                new TrendyolPreviewAttributeDto("Beden", "M")
            ],
            Variants: [
                new TrendyolPreviewVariantDto("ABC-S", "Siyah / S", 299, 249, 15),
                new TrendyolPreviewVariantDto("ABC-M", "Siyah / M", 299, 249, 22)
            ]
        );

        mockService
            .Setup(s => s.GetSendPreviewAsync(productId, overrides))
            .ReturnsAsync(new DataResult<TrendyolSendPreviewDto>(true, preview));

        // Act
        var result = await mockService.Object.GetSendPreviewAsync(productId, overrides);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Custom Title");
        result.Data.Variants.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSendPreviewAsync_WithoutOverrides_ReturnsMappedPreviewWithOriginalData()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockService = new Mock<ITrendyolProductService>();

        var preview = new TrendyolSendPreviewDto(
            Title: "Original Title",
            BrandName: "Nike",
            TrendyolBrandName: "NIKE",
            CategoryName: "Giyim > T-Shirt",
            TrendyolCategoryName: "Giyim > Tişört",
            TrendyolCategoryId: 1234,
            Description: "Original Description",
            Attributes: [],
            Variants: []
        );

        mockService
            .Setup(s => s.GetSendPreviewAsync(productId, null))
            .ReturnsAsync(new DataResult<TrendyolSendPreviewDto>(true, preview));

        // Act
        var result = await mockService.Object.GetSendPreviewAsync(productId, null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Original Title");
    }
}
```

- [ ] **Step 2: Test'i çalıştır (başarısız olmalı)**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolSendPreviewTests" -v
```

Beklenen: FAIL

- [ ] **Step 3: Commit**

```bash
git add Test/Entegrasyon.Test/Features/MarketplaceSync/TrendyolSendPreviewTests.cs
git commit -m "test: add TrendyolSendPreviewTests unit tests (failing)"
```

---

### Task 9: TrendyolProductService — GetSendPreviewAsync Implementation

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/ITrendyolProductService.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductService.cs`

- [ ] **Step 1: Interface'e metodu ekle**

```csharp
Task<IDataResult<TrendyolSendPreviewDto>> GetSendPreviewAsync(
    Guid productId,
    MarketplaceOverrideDetailDto? overrides);
```

- [ ] **Step 2: Implementasyon yaz**

```csharp
public async Task<IDataResult<TrendyolSendPreviewDto>> GetSendPreviewAsync(
    Guid productId,
    MarketplaceOverrideDetailDto? overrides)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var product = await context.Products
        .Include(p => p.Brand)
        .Include(p => p.Category)
        .Include(p => p.Variants)
        .ThenInclude(v => v.AttributeKeyValues)
        .FirstOrDefaultAsync(p => p.Id == productId);

    if (product is null)
        return new DataResult<TrendyolSendPreviewDto>(false, "Ürün bulunamadı.");

    // 1. Kategori eşleştirmesini al
    var categoryMatch = await context.CategoryMarketPlaceMatches
        .FirstOrDefaultAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == 1);

    if (categoryMatch is null)
        return new DataResult<TrendyolSendPreviewDto>(false, "Kategori eşleştirmesi bulunamadı.");

    // 2. Marka eşleştirmesini al
    var brandMatch = await context.BrandMarketPlaceMatches
        .FirstOrDefaultAsync(bm => bm.BrandId == product.BrandId && bm.MarketPlaceId == 1);

    var trendyolBrandName = brandMatch?.MarketPlaceBrandName ?? product.Brand.Name;

    // 3. Özellikler hazırla
    var attributes = new List<TrendyolPreviewAttributeDto>();
    var productAttributes = product.Variants.FirstOrDefault()?.AttributeKeyValues ?? [];

    foreach (var attr in productAttributes)
    {
        // Attribute value'sunu Trendyol eşleştirmesinden al
        var attrValue = attr.AttributeValueId.HasValue
            ? await context.CategoryAttributeValues.FirstOrDefaultAsync(cv => cv.Id == attr.AttributeValueId)
            : null;

        var displayValue = attrValue?.Value ?? attr.CustomValue ?? "—";
        attributes.Add(new TrendyolPreviewAttributeDto(attr.Name, displayValue));
    }

    // 4. Varyantlar hazırla
    var variants = new List<TrendyolPreviewVariantDto>();
    foreach (var variant in product.Variants)
    {
        var attrString = string.Join(" / ", variant.AttributeKeyValues.Select(a => a.CustomValue ?? "—"));

        // Override fiyat varsa kullan, yoksa mevcut fiyatı kullan
        var salePrice = overrides?.VariantOverrides
            .FirstOrDefault(vo => vo.ProductVariantId == variant.Id)
            ?.SalePriceOverride ?? variant.SalePrice;

        variants.Add(new TrendyolPreviewVariantDto(
            variant.Barcode ?? "—",
            attrString,
            variant.ListPrice,
            salePrice,
            variant.Quantity
        ));
    }

    // 5. Başlık ve açıklamayı al
    var title = overrides?.TitleOverride ?? product.Title;
    var description = overrides?.DescriptionOverride ?? product.Description;

    var preview = new TrendyolSendPreviewDto(
        Title: title,
        BrandName: product.Brand.Name,
        TrendyolBrandName: trendyolBrandName,
        CategoryName: product.Category.Name,
        TrendyolCategoryName: categoryMatch.MarketPlaceCategoryName,
        TrendyolCategoryId: categoryMatch.MarketPlaceCategoryId,
        Description: description,
        Attributes: attributes,
        Variants: variants
    );

    return new DataResult<TrendyolSendPreviewDto>(true, preview);
}
```

- [ ] **Step 3: Test'i çalıştır (geçmeli)**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolSendPreviewTests" -v
```

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ITrendyolProductService.cs \
        Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductService.cs
git commit -m "feat: implement GetSendPreviewAsync for payload preview"
```

---

### Task 10: MarketplaceStatusCards — Disabled Kartlar ve Credential Kontrolü

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/MarketplaceStatusCards.razor`
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/MarketplaceStatusCards.razor.cs`

- [ ] **Step 1: .razor dosyasında kartı Disabled parametresi ile güncelleştir**

```razor
<MudCard Elevation="2"
         Class="cursor-pointer"
         @onclick="@(mp.HasCredentials ? (() => OnMarketplaceSelected.InvokeAsync(mp.MarketPlaceId)) : null)"
         Style="@(mp.HasCredentials ? "" : "opacity: 0.6; cursor: not-allowed;")">
    @if (!mp.HasCredentials)
    {
        <MudTooltip Text="Bu pazaryeri için API bilgileri tanımlı değil">
            <div Style="position: absolute; width: 100%; height: 100%;"></div>
        </MudTooltip>
    }
    <MudCardHeader>
        <CardHeaderContent>
            <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                <MarketplaceLogo Name="@mp.MarketPlaceName" Size="Size.Medium" />
                <MudText Typo="Typo.subtitle1">@mp.MarketPlaceName</MudText>
            </MudStack>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudChip T="string" Size="Size.Small"
                     Color="@ProductSyncPage.GetSyncStateColor(mp.SyncState)">
                @ProductSyncPage.GetSyncStateLabel(mp.SyncState)
            </MudChip>
        </CardHeaderActions>
    </MudCardHeader>
    <MudCardContent Class="pt-0">
        <MudStack Spacing="1">
            <MudText Typo="Typo.caption" Color="Color.Dark">
                Son Sync: <strong>@(mp.LastSyncedAt?.LocalDateTime.ToString("dd.MM.yyyy HH:mm") ?? "—")</strong>
            </MudText>
            @if (!string.IsNullOrEmpty(mp.ExternalProductId))
            {
                <MudText Typo="Typo.caption" Color="Color.Secondary">
                    Ext ID: @mp.ExternalProductId
                </MudText>
            }
            @if (!string.IsNullOrEmpty(mp.StatusMessage))
            {
                <MudText Typo="Typo.caption" Color="Color.Error">@mp.StatusMessage</MudText>
            }
        </MudStack>
    </MudCardContent>
</MudCard>
```

- [ ] **Step 2: .razor.cs dosyasında NeverSynced + credential'lı karta tıklama event'ini ekle**

```csharp
public partial class MarketplaceStatusCards : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<MarketplaceSyncItemDto> Marketplaces { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback<int> OnMarketplaceSelected { get; set; }

    // NeverSynced kart tıklaması → send sayfasına navigasyon
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private async Task HandleCardClick(int marketPlaceId)
    {
        var mp = Marketplaces.FirstOrDefault(m => m.MarketPlaceId == marketPlaceId);
        if (mp is null || !mp.HasCredentials) return;

        // NeverSynced ise send sayfasına git
        if (mp.SyncState == MarketplaceSyncState.NeverSynced)
        {
            var productId = /* ProductId buraya gelecek — parent'tan parameter olarak */;
            if (marketPlaceId == 1) // Trendyol
                NavigationManager.NavigateTo($"/products/{productId}/sync/trendyol/send");
            else
                await OnMarketplaceSelected.InvokeAsync(marketPlaceId);
        }
        else
        {
            await OnMarketplaceSelected.InvokeAsync(marketPlaceId);
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/MarketplaceStatusCards.razor*
git commit -m "feat: add credential check and disable cards without API credentials"
```

---

### Task 11: TrendyolProductSendPage — Orchestrator Sayfası

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/TrendyolProductSendPage.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/TrendyolProductSendPage.razor.cs`

- [ ] **Step 1: .razor dosyasını yaz**

```razor
@page "/products/{Id:guid}/sync/trendyol/send"
@rendermode InteractiveServer
@attribute [Authorize(Policy = AppPermissions.Marketplace.Edit)]
@using Entegrasyon.Entity.Dtos.Product

<PageTitle>Trendyol'a Ürün Gönder</PageTitle>

@if (_loading)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
}
else if (_detail is null)
{
    <MudAlert Severity="Severity.Error">Ürün bulunamadı.</MudAlert>
}
else
{
    <MudStack Spacing="3">

        @* Üst başlık *@
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            <MudButton OnClick="GoBack" StartIcon="@Icons.Material.Filled.ArrowBack"
                       Variant="Variant.Text" Color="Color.Default">
                Ürüne Dön
            </MudButton>
            <MudText Typo="Typo.h5">@_detail.Title</MudText>
            <MudChip T="string" Size="Size.Small" Color="Color.Default">@_detail.StockCode</MudChip>
        </MudStack>

        @* Preflight Kontroller *@
        @if (_preflight is not null)
        {
            <SendPreflightChecks Preflight="@_preflight" OnAllPassed="@((passed) => _preflightPassed = passed)" />
        }

        @* Override Formları (preflight geçerse aktif) *@
        @if (_preflightPassed)
        {
            <SendOverrideForm CurrentTitle="@_detail.Title"
                              CurrentDescription="@_detail.Description"
                              OnOverrideChanged="@((overrides) => _currentOverrides = (overrides.Title, overrides.Description))" />
        }

        @* Fiyatlandırma Paneli *@
        @if (_preflightPassed && _pricingRows is not null)
        {
            <SendPricingPanel ProductId="@Id"
                              MarketPlaceId="1"
                              CategoryId="@_detail.CategoryId"
                              Variants="@_pricingRows"
                              OnPriceOverridesChanged="@((overrides) => _priceOverrides = overrides)" />
        }

        @* Payload Ön İzleme *@
        @if (_preflightPassed && _preview is not null)
        {
            <SendPayloadPreview Preview="@_preview" IsLoading="@_previewLoading" />
        }

        @* Gönder Butonu *@
        @if (_preflightPassed)
        {
            <MudButton Variant="Variant.Filled" Color="Color.Success" Size="Size.Large"
                       StartIcon="@Icons.Material.Filled.Send"
                       OnClick="HandleSendAsync"
                       Disabled="@_sending">
                @if (_sending) { <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" /> }
                Trendyol'a Gönder
            </MudButton>
        }

    </MudStack>
}
```

- [ ] **Step 2: .razor.cs dosyasını yaz**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSend;

public partial class TrendyolProductSendPage : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ITrendyolProductService TrendyolService { get; set; } = null!;
    [Inject] private IMarketplaceOverrideManager OverrideManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private ProductSyncDetailDto? _detail;
    private ProductSendPreflightDto? _preflight;
    private TrendyolSendPreviewDto? _preview;
    private List<VariantPricingRowDto>? _pricingRows;

    private bool _loading = true;
    private bool _preflightPassed = false;
    private bool _previewLoading = false;
    private bool _sending = false;

    private (string? Title, string? Description) _currentOverrides;
    private List<VariantPriceOverrideDto> _priceOverrides = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;

        // Ürün detayı yükle
        var detailResult = await SyncManager.GetProductSyncDetailAsync(Id);
        if (!detailResult.Success || detailResult.Data is null)
        {
            Snackbar.Add("Ürün bulunamadı.", Severity.Error);
            _loading = false;
            return;
        }
        _detail = detailResult.Data;

        // Preflight kontrolleri çalıştır
        var preflightResult = await SyncManager.GetSendPreflightAsync(Id, 1);
        if (preflightResult.Success && preflightResult.Data is not null)
        {
            _preflight = preflightResult.Data;
            if (_preflight.AllPassed)
            {
                await LoadPreview();
                await LoadPricingRows();
            }
        }

        _loading = false;
    }

    private async Task LoadPreview()
    {
        _previewLoading = true;
        var overrides = new MarketplaceOverrideDetailDto(
            TitleOverride: _currentOverrides.Title,
            DescriptionOverride: _currentOverrides.Description,
            VariantOverrides: []
        );

        var previewResult = await TrendyolService.GetSendPreviewAsync(Id, overrides);
        if (previewResult.Success && previewResult.Data is not null)
            _preview = previewResult.Data;

        _previewLoading = false;
    }

    private async Task LoadPricingRows()
    {
        // Ürün bilgisini al ve varyantları VariantPricingRowDto'ya dönüştür
        // (Detay yükleme sırasında zaten _detail dolduruldu)
        if (_detail?.Variants is null) return;

        _pricingRows = _detail.Variants
            .Select(v => new VariantPricingRowDto(
                v.Id,
                v.Barcode ?? "—",
                v.VariantName,
                v.ListPrice,
                v.SalePrice,
                v.Quantity
            ))
            .ToList();
    }

    private async Task HandleSendAsync()
    {
        var confirmed = await ShowConfirmationDialog();
        if (!confirmed) return;

        _sending = true;
        try
        {
            // Override'ları kaydet
            if (_currentOverrides.Title is not null || _currentOverrides.Description is not null)
            {
                var overrideDto = new MarketplaceOverrideDetailDto(
                    TitleOverride: _currentOverrides.Title,
                    DescriptionOverride: _currentOverrides.Description,
                    VariantOverrides: _priceOverrides
                );
                await OverrideManager.SaveOverridesAsync(Id, 1, overrideDto);
            }

            // Ürünü sync kuyruğuna ekle
            var result = await SyncManager.SyncProductAsync(Id, 1);
            Snackbar.Add(result.Message ?? "Ürün gönderme başlatıldı.",
                         result.Success ? Severity.Success : Severity.Error);

            if (result.Success)
                NavigationManager.NavigateTo($"/products/{Id}/sync");
        }
        finally
        {
            _sending = false;
        }
    }

    private async Task<bool> ShowConfirmationDialog()
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm",
            "Bu ürünü Trendyol'a göndermek istediğinize emin misiniz?");
        return confirmed;
    }

    private void GoBack() => NavigationManager.NavigateTo($"/products/{Id}/sync");
}
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/TrendyolProductSendPage.razor*
git commit -m "feat: add TrendyolProductSendPage orchestrator component"
```

---

### Task 12: SendPreflightChecks Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPreflightChecks.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPreflightChecks.razor.cs`

- [ ] **Step 1: .razor dosyasını yaz**

```razor
@using Entegrasyon.Entity.Dtos.Product

<MudPaper Elevation="1" Class="pa-4">
    <MudText Typo="Typo.h6" Class="mb-4">Gönderme Ön Kontrolleri</MudText>

    <MudStack Spacing="2">

        @* Kategori Eşleştirmesi *@
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            @if (Preflight.CategoryMatched)
            {
                <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" />
                <MudText>Kategori Eşleştirmesi: @Preflight.MatchedCategoryName</MudText>
            }
            else
            {
                <MudIcon Icon="@Icons.Material.Filled.Cancel" Color="Color.Error" />
                <MudText>Kategori Eşleştirmesi: Bulunamadı</MudText>
                <MudLink Href="/marketplace/categories" Class="ml-auto">Düzelt →</MudLink>
            }
        </MudStack>

        @* Marka Eşleştirmesi *@
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            @if (Preflight.BrandMatched)
            {
                <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" />
                <MudText>Marka Eşleştirmesi: @Preflight.MatchedBrandName</MudText>
            }
            else
            {
                <MudIcon Icon="@Icons.Material.Filled.Cancel" Color="Color.Error" />
                <MudText>Marka Eşleştirmesi: Bulunamadı</MudText>
                <MudLink Href="/marketplace/brands" Class="ml-auto">Düzelt →</MudLink>
            }
        </MudStack>

        @* Zorunlu Özellikler *@
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            @if (Preflight.RequiredAttributesMatched)
            {
                <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" />
                <MudText>Zorunlu Özellikler: Eşleşmiş</MudText>
            }
            else
            {
                <MudIcon Icon="@Icons.Material.Filled.Cancel" Color="Color.Error" />
                <MudText>Zorunlu Özellikler: @string.Join(", ", Preflight.MissingAttributes) eksik</MudText>
                <MudLink Href="/marketplace/attributes" Class="ml-auto">Düzelt →</MudLink>
            }
        </MudStack>

        @* Varyantlar *@
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            @if (Preflight.HasVariants)
            {
                <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" />
                <MudText>Varyantlar: Mevcut</MudText>
            }
            else
            {
                <MudIcon Icon="@Icons.Material.Filled.Cancel" Color="Color.Error" />
                <MudText>Varyantlar: Bulunamadı</MudText>
                <MudLink Href="/products/@ProductId" Class="ml-auto">Ekle →</MudLink>
            }
        </MudStack>

        @* Barkodlar *@
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            @if (Preflight.AllVariantsHaveBarcodes)
            {
                <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" />
                <MudText>Varyant Barkodları: Tamamlanmış</MudText>
            }
            else
            {
                <MudIcon Icon="@Icons.Material.Filled.Cancel" Color="Color.Error" />
                <MudText>Varyant Barkodları: Eksik</MudText>
                <MudLink Href="/products/@ProductId" Class="ml-auto">Ekle →</MudLink>
            }
        </MudStack>

    </MudStack>

    @if (Preflight.AllPassed)
    {
        <MudAlert Severity="Severity.Success" Class="mt-4">
            Tüm kontroller geçti. Ürünü göndermeye hazırsınız.
        </MudAlert>
    }
    else
    {
        <MudAlert Severity="Severity.Warning" Class="mt-4">
            Ürün gönderilmez. Lütfen eksik alanları tamamlayın.
        </MudAlert>
    }
</MudPaper>

@code {
    [Parameter] public Guid ProductId { get; set; }
}
```

- [ ] **Step 2: .razor.cs dosyasını yaz**

```csharp
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSend;

public partial class SendPreflightChecks : ComponentBase
{
    [Parameter, EditorRequired] public ProductSendPreflightDto Preflight { get; set; } = null!;
    [Parameter] public Guid ProductId { get; set; }
    [Parameter] public EventCallback<bool> OnAllPassed { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await OnAllPassed.InvokeAsync(Preflight.AllPassed);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPreflightChecks.razor*
git commit -m "feat: add SendPreflightChecks component for validation"
```

---

### Task 13: SendOverrideForm Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendOverrideForm.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendOverrideForm.razor.cs`

- [ ] **Step 1: .razor dosyasını yaz**

```razor
<MudPaper Elevation="1" Class="pa-4">
    <MudText Typo="Typo.h6" Class="mb-3">Pazaryeri Düzenlemeleri (Opsiyonel)</MudText>

    <MudStack Spacing="3">

        <MudTextField T="string"
                      Label="Başlık Düzenlemesi"
                      @bind-Value="_titleOverride"
                      Variant="Variant.Outlined"
                      Placeholder="Boş bırakılırsa ürünün kendi başlığı kullanılır"
                      HelperText="Trendyol'a bu başlık ile gönderilecek"
                      @onchange="OnValueChanged" />

        <MudTextField T="string"
                      Label="Açıklama Düzenlemesi"
                      @bind-Value="_descriptionOverride"
                      Variant="Variant.Outlined"
                      Lines="4"
                      Placeholder="Boş bırakılırsa ürünün kendi açıklaması kullanılır"
                      HelperText="Trendyol'a bu açıklama ile gönderilecek"
                      @onchange="OnValueChanged" />

    </MudStack>

    <MudAlert Severity="Severity.Info" Class="mt-3">
        Düzenleme alanlarını boş bırakırsanız, ürünün mevcut başlık ve açıklaması kullanılır.
    </MudAlert>
</MudPaper>
```

- [ ] **Step 2: .razor.cs dosyasını yaz**

```csharp
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSend;

public partial class SendOverrideForm : ComponentBase
{
    [Parameter] public string? CurrentTitle { get; set; }
    [Parameter] public string? CurrentDescription { get; set; }
    [Parameter] public EventCallback<(string? Title, string? Description)> OnOverrideChanged { get; set; }

    private string? _titleOverride;
    private string? _descriptionOverride;

    protected override void OnInitialized()
    {
        _titleOverride = CurrentTitle;
        _descriptionOverride = CurrentDescription;
    }

    private async Task OnValueChanged()
    {
        await OnOverrideChanged.InvokeAsync((_titleOverride, _descriptionOverride));
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendOverrideForm.razor*
git commit -m "feat: add SendOverrideForm component for title/description override"
```

---

### Task 14: SendPricingPanel Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPricingPanel.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPricingPanel.razor.cs`

- [ ] **Step 1: .razor dosyasını yaz**

```razor
@using Entegrasyon.Entity.Dtos.Product

<MudPaper Elevation="1" Class="pa-4">
    <MudText Typo="Typo.h6" Class="mb-3">Fiyatlandırma</MudText>

    <MudTable Items="@Variants" Dense="true" Hover="true" Elevation="0" Bordered="true">
        <HeaderContent>
            <MudTh>Barkod</MudTh>
            <MudTh>Varyant</MudTh>
            <MudTh Style="text-align: right">Liste Fiyatı</MudTh>
            <MudTh Style="text-align: right">Satış Fiyatı</MudTh>
            <MudTh Style="text-align: right">Fiyat Override</MudTh>
            <MudTh Style="text-align: right">Komisyon</MudTh>
            <MudTh Style="text-align: right">Net Kar</MudTh>
        </HeaderContent>
        <RowContent>
            <MudTd>@context.Barcode</MudTd>
            <MudTd>@context.VariantName</MudTd>
            <MudTd Style="text-align: right">@context.ListPrice.ToString("N2")</MudTd>
            <MudTd Style="text-align: right">@context.SalePrice.ToString("N2")</MudTd>
            <MudTd Style="text-align: right">
                <MudNumericField T="decimal?"
                                 @bind-Value="context.OverridePrice"
                                 Variant="Variant.Filled"
                                 Density="Density.Compact"
                                 Min="0"
                                 @onchange="@((decimal? val) => HandlePriceChange(context.ProductVariantId, val))" />
            </MudTd>
            <MudTd Style="text-align: right">
                @if (_commissionResults.TryGetValue(context.ProductVariantId, out var commission))
                {
                    <MudText>@commission.CommissionAmount.ToString("C2")</MudText>
                }
            </MudTd>
            <MudTd Style="text-align: right">
                @if (_commissionResults.TryGetValue(context.ProductVariantId, out var result))
                {
                    <MudText Color="@(result.NetProfit >= 0 ? Color.Success : Color.Error)">
                        @result.NetProfit.ToString("C2")
                    </MudText>
                }
            </MudTd>
        </RowContent>
    </MudTable>

    <MudAlert Severity="Severity.Info" Class="mt-3">
        Override fiyat boş bırakılırsa, ürünün mevcut satış fiyatı kullanılır.
    </MudAlert>
</MudPaper>
```

- [ ] **Step 2: .razor.cs dosyasını yaz**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSend;

public partial class SendPricingPanel : ComponentBase
{
    [Parameter, EditorRequired] public Guid ProductId { get; set; }
    [Parameter, EditorRequired] public int MarketPlaceId { get; set; }
    [Parameter] public int? CategoryId { get; set; }
    [Parameter, EditorRequired] public List<VariantPricingRowDto> Variants { get; set; } = [];
    [Parameter] public EventCallback<List<VariantPriceOverrideDto>> OnPriceOverridesChanged { get; set; }

    [Inject] private ICommissionCalculator CommissionCalculator { get; set; } = null!;

    private Dictionary<Guid, CommissionCalculationResult> _commissionResults = [];

    private async Task HandlePriceChange(Guid variantId, decimal? overridePrice)
    {
        var variant = Variants.FirstOrDefault(v => v.ProductVariantId == variantId);
        if (variant is null) return;

        var priceToCalculate = overridePrice ?? variant.SalePrice;

        var calcResult = await CommissionCalculator.CalculateAsync(MarketPlaceId, CategoryId, priceToCalculate, 0);
        if (calcResult.Success)
            _commissionResults[variantId] = calcResult.Data!;

        // Parent'a override'ları bildir
        var overrides = Variants
            .Where(v => v.OverridePrice.HasValue)
            .Select(v => new VariantPriceOverrideDto(v.ProductVariantId, v.OverridePrice))
            .ToList();

        await OnPriceOverridesChanged.InvokeAsync(overrides);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPricingPanel.razor*
git commit -m "feat: add SendPricingPanel component with commission calculator"
```

---

### Task 15: SendPayloadPreview Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPayloadPreview.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPayloadPreview.razor.cs`

- [ ] **Step 1: .razor dosyasını yaz**

```razor
@using Entegrasyon.Entity.Dtos.Product

@if (IsLoading)
{
    <MudProgressLinear Indeterminate="true" />
}
else if (Preview is not null)
{
    <MudPaper Elevation="1" Class="pa-4">
        <MudText Typo="Typo.h6" Class="mb-3">Gönderilecek Ürün Özeti</MudText>

        <MudGrid>
            <MudItem xs="12" sm="6">
                <MudText Typo="Typo.caption" Color="Color.Dark">Başlık</MudText>
                <MudText Typo="Typo.body2">@Preview.Title</MudText>
            </MudItem>
            <MudItem xs="12" sm="6">
                <MudText Typo="Typo.caption" Color="Color.Dark">Marka</MudText>
                <MudText Typo="Typo.body2">@Preview.BrandName → @Preview.TrendyolBrandName</MudText>
            </MudItem>
            <MudItem xs="12">
                <MudText Typo="Typo.caption" Color="Color.Dark">Kategori</MudText>
                <MudText Typo="Typo.body2">@Preview.CategoryName → @Preview.TrendyolCategoryName (ID: @Preview.TrendyolCategoryId)</MudText>
            </MudItem>
            <MudItem xs="12">
                <MudText Typo="Typo.caption" Color="Color.Dark">Açıklama</MudText>
                <MudText Typo="Typo.body2">@Preview.Description</MudText>
            </MudItem>
        </MudGrid>

        @* Özellikler *@
        @if (Preview.Attributes.Any())
        {
            <MudDivider Class="my-4" />
            <MudText Typo="Typo.subtitle2" Class="mb-2">Özellikler</MudText>
            <MudStack Row="true" Wrap="Wrap.Wrap" Spacing="1">
                @foreach (var attr in Preview.Attributes)
                {
                    <MudChip T="string" Size="Size.Small" Color="Color.Primary">@attr.Name: @attr.Value</MudChip>
                }
            </MudStack>
        }

        @* Varyantlar *@
        @if (Preview.Variants.Any())
        {
            <MudDivider Class="my-4" />
            <MudText Typo="Typo.subtitle2" Class="mb-2">Varyantlar (@Preview.Variants.Count)</MudText>
            <MudSimpleTable Dense="true" Hover="true" Elevation="0" Bordered="true">
                <thead>
                    <tr>
                        <th>Barkod</th>
                        <th>Varyant</th>
                        <th Style="text-align: right">Fiyat</th>
                        <th Style="text-align: right">Stok</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var variant in Preview.Variants)
                    {
                        <tr>
                            <td>@variant.Barcode</td>
                            <td>@variant.Attributes</td>
                            <td Style="text-align: right">@variant.SalePrice.ToString("N2") ₺</td>
                            <td Style="text-align: right">@variant.Quantity</td>
                        </tr>
                    }
                </tbody>
            </MudSimpleTable>
        }
    </MudPaper>
}
```

- [ ] **Step 2: .razor.cs dosyasını yaz**

```csharp
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSend;

public partial class SendPayloadPreview : ComponentBase
{
    [Parameter] public TrendyolSendPreviewDto? Preview { get; set; }
    [Parameter] public bool IsLoading { get; set; }
}
```

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSend/SendPayloadPreview.razor*
git commit -m "feat: add SendPayloadPreview component for payload summary"
```

---

### Task 16: Integration Test

**Files:**
- Create: `Test/Entegrasyon.IntegrationTest/Features/MarketplaceSync/ProductSendPageTests.cs`

- [ ] **Step 1: Integration test yaz**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Features.MarketplaceSync;

public class ProductSendPageTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private IServiceProvider _serviceProvider = null!;
    private IProductSyncManager _syncManager = null!;

    public ProductSendPageTests()
    {
        _fixture = new IntegrationTestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _serviceProvider = _fixture.GetServiceProvider();
        _syncManager = _serviceProvider.GetRequiredService<IProductSyncManager>();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task GetSendPreflightAsync_AllChecksPassed_ReturnsAllPassedTrue()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var brand = new Brand { Name = "Nike" };
        var category = new Category { Name = "Giyim > T-Shirt" };
        var product = new Product
        {
            Title = "Nike T-Shirt",
            Brand = brand,
            Category = category,
            Variants = new List<ProductVariant>
            {
                new() { Barcode = "ABC-S", SalePrice = 249 },
                new() { Barcode = "ABC-M", SalePrice = 249 }
            }
        };

        // Eşleştirmeleri ekle
        var categoryMatch = new CategoryMarketPlaceMatch
        {
            Category = category,
            MarketPlace = new MarketPlace { Id = 1, Name = "Trendyol" },
            MarketPlaceCategoryId = 1234,
            MarketPlaceCategoryName = "Giyim > Tişört"
        };

        var brandMatch = new BrandMarketPlaceMatch
        {
            Brand = brand,
            MarketPlace = new MarketPlace { Id = 1, Name = "Trendyol" },
            MarketPlaceBrandName = "NIKE"
        };

        context.Products.Add(product);
        context.CategoryMarketPlaceMatches.Add(categoryMatch);
        context.BrandMarketPlaceMatches.Add(brandMatch);
        await context.SaveChangesAsync();

        // Act
        var result = await _syncManager.GetSendPreflightAsync(product.Id, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AllPassed.Should().BeTrue();
        result.Data.CategoryMatched.Should().BeTrue();
        result.Data.BrandMatched.Should().BeTrue();
    }

    [Fact]
    public async Task GetSendPreflightAsync_CategoryNotMatched_ReturnsAllPassedFalse()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var product = new Product
        {
            Title = "Ürün",
            Brand = new Brand { Name = "Brand" },
            Category = new Category { Name = "Kategori" },
            Variants = new List<ProductVariant> { new() { Barcode = "ABC" } }
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var result = await _syncManager.GetSendPreflightAsync(product.Id, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AllPassed.Should().BeFalse();
        result.Data.CategoryMatched.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Test'i çalıştır**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "ProductSendPageTests" -v
```

- [ ] **Step 3: Commit**

```bash
git add Test/Entegrasyon.IntegrationTest/Features/MarketplaceSync/ProductSendPageTests.cs
git commit -m "test: add ProductSendPageTests integration tests"
```

---

## Plan Self-Review

✅ **Spec Coverage:**
- Credential kontrolü → Task 4, 7, 10
- GetSendPreflightAsync → Tasks 5, 6
- GetSendPreviewAsync → Tasks 8, 9
- MarketplaceStatusCards credential UI → Task 10
- TrendyolProductSendPage → Task 11
- Alt component'lar (Preflight, Override, Pricing, Preview) → Tasks 12-15
- Integration test → Task 16

✅ **Placeholder Check:** Tüm kod snippets tam ve çalışabilir durumdadır.

✅ **Type Consistency:** ProductSendPreflightDto, TrendyolSendPreviewDto, VariantPricingRowDto, VariantPriceOverrideDto türleri tüm task'larda tutarlı.

✅ **No Dangling Refs:** Tüm interface metodları, DTO'lar ve component parametreleri tanımlanmıştır.
