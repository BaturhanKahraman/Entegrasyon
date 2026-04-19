# Variant İsimlendirme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `ProductVariant.Name` alanını ekleyip varyant gösteren 16 view'da tutarlı bir `DisplayName` göster.

**Architecture:** Nullable `Name` kolonu DB'ye materialize edilir; `VariantNamingService.Compute` her Add/Update akışında çağrılır; read path `VariantNameExtensions.ResolveDisplayName` ile 3 katmanlı fallback (`Name ?? Attributes ?? ProductTitle`).

**Tech Stack:** .NET 10, EF Core 10, PostgreSQL (Npgsql), xUnit + Moq + FluentAssertions, Testcontainers, Razor + HTMX.

**Spec:** `docs/superpowers/specs/2026-04-19-variant-naming-design.md`

---

## Yol Haritası — 6 Faz

| Faz | Görevler | Commit Konusu |
|---|---|---|
| 1 | Task 1–4 | Migration + entity + service + unit tests |
| 2 | Task 5–8 | Write path (AddVariant, UpdateVariant, AddProduct, integration tests) |
| 3 | Task 9–11 | Shared display helper + partial + VM |
| 4 | Task 12–14 | POS + Products view güncellemeleri |
| 5 | Task 15–18 | Sales/Orders/Returns |
| 6 | Task 19–23 | Operational views (Reports, Stock, BranchOffice, Picking, Pricing) |

Her task sonunda `dotnet build` yeşil, ilgili test suite geçerli.

---

## Task 1: Entity alanı + Configuration

**Files:**
- Modify: `Application/Entegrasyon.Entity/Products/ProductVariant.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/ProductVariantEntityConfiguration.cs`

- [ ] **Step 1: Entity'ye Name alanı ekle**

`Application/Entegrasyon.Entity/Products/ProductVariant.cs` — `BranchOfficeStocks` satırından önce:

```csharp
public string? Name { get; set; }
```

- [ ] **Step 2: Configuration'a max length constraint ekle**

`ProductVariantEntityConfiguration.cs` — `builder.HasIndex(x => x.Barcode).IsUnique();` satırından sonra:

```csharp
builder.Property(x => x.Name).HasMaxLength(256);
```

- [ ] **Step 3: Build**

```bash
dotnet build Entegrasyon.sln
```

Expected: `Build succeeded`.

---

## Task 2: Migration — AddProductVariantName

**Files:**
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/<timestamp>_AddProductVariantName.cs` (generated)
- Create: `<timestamp>_AddProductVariantName.Designer.cs` (generated)
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/IntegrationDbContextModelSnapshot.cs` (generated)

- [ ] **Step 1: Migration oluştur**

```bash
dotnet ef migrations add AddProductVariantName \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: 3 dosya üretilir, `Name` kolonu `product_variants` (veya `ProductVariants`) tablosuna eklenir, `nullable: true`.

- [ ] **Step 2: Migration dosyasını gözden geçir**

Oluşan `Up()` sadece şunu içermeli:

```csharp
migrationBuilder.AddColumn<string>(
    name: "Name",
    table: "ProductVariants",
    type: "character varying(256)",
    maxLength: 256,
    nullable: true);
```

`Down()` karşıt `DropColumn`. Başka değişiklik varsa (snapshot drift), durdur ve raporla.

- [ ] **Step 3: DB'ye uygula**

```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- [ ] **Step 4: Pending model drift kontrolü**

```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 5: Commit (Task 1 + Task 2 birlikte)**

```bash
git add Application/Entegrasyon.Entity/Products/ProductVariant.cs \
  Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/ProductVariantEntityConfiguration.cs \
  Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(variant): ProductVariant.Name nullable kolonu ve migration"
```

---

## Task 3: `IVariantNamingService` interface + `VariantAttributeLite` struct

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IVariantNamingService.cs`

- [ ] **Step 1: Interface ve helper struct'ı yaz**

```csharp
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Abstract;

public interface IVariantNamingService
{
    /// <summary>
    /// Variant için Name hesaplar. Varianter ve slicer attribute'larını kullanır;
    /// hiçbir varianter/slicer değer yoksa product.Title döner; product.Title da boşsa null.
    /// Sonuç DB'ye yazılır (nullable).
    /// </summary>
    string? Compute(ProductVariant variant, Product product);
}

public readonly record struct VariantAttributeLite(
    string? Value,
    string? CustomValue,
    bool IsVarianter,
    bool IsSlicer,
    int Order);
```

- [ ] **Step 2: Build**

```bash
dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj
```

Expected: `Build succeeded`.

---

## Task 4: `VariantNamingService` — TDD

**Files:**
- Create: `Test/Entegrasyon.Test/Business/VariantNamingServiceTests.cs`
- Create: `Application/Entegrasyon.Business/Concrete/VariantNamingService.cs`

- [ ] **Step 1: Failing test yaz**

`Test/Entegrasyon.Test/Business/VariantNamingServiceTests.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class VariantNamingServiceTests
{
    private readonly IVariantNamingService _sut = new VariantNamingService();

    private static Product MakeProduct(string title = "Basic Tshirt") => new() { Title = title };

    private static ProductVariant MakeVariant(params ProductVariantAttribute[] attrs)
        => new() { ProductVariantAttributes = attrs.ToList() };

    [Fact]
    public void Compute_ReturnsVarianterThenSlicer()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true },
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true }
        );

        var result = _sut.Compute(variant, MakeProduct());

        result.Should().Be("Sarı XL");
    }

    [Fact]
    public void Compute_MultipleVarianters_KeepsInsertionOrder()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true },
            new ProductVariantAttribute { CategoryAttributeValue = "Çiçekli", IsVarianter = true }
        );

        var result = _sut.Compute(variant, MakeProduct());

        result.Should().Be("Sarı Çiçekli");
    }

    [Fact]
    public void Compute_OnlySlicer_ReturnsSlicer()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("XL");
    }

    [Fact]
    public void Compute_OnlyVarianter_ReturnsVarianter()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("Sarı");
    }

    [Fact]
    public void Compute_NoVarianterNoSlicer_ReturnsProductTitle()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "Yuvarlak", IsVarianter = false, IsSlicer = false }
        );

        _sut.Compute(variant, MakeProduct("T-shirt")).Should().Be("T-shirt");
    }

    [Fact]
    public void Compute_EmptyAttributes_ReturnsProductTitle()
    {
        var variant = MakeVariant();

        _sut.Compute(variant, MakeProduct("Sade Ürün")).Should().Be("Sade Ürün");
    }

    [Fact]
    public void Compute_UsesCustomValueWhenCategoryValueNull()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = null, CustomValue = "Lacivert", IsVarianter = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("Lacivert");
    }

    [Fact]
    public void Compute_SkipsEmptyValues()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "", IsVarianter = true },
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("XL");
    }

    [Fact]
    public void Compute_MixedOrder_PutsVarianterFirst()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true },
            new ProductVariantAttribute { CategoryAttributeValue = "M", IsSlicer = true },
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("Sarı XL M");
    }

    [Fact]
    public void Compute_EmptyProductTitleAndNoAttributes_ReturnsNull()
    {
        var result = _sut.Compute(MakeVariant(), new Product { Title = "" });
        result.Should().BeNull();
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~VariantNamingServiceTests"
```

Expected: Build fails — `VariantNamingService` not defined.

- [ ] **Step 3: Implementation yaz**

`Application/Entegrasyon.Business/Concrete/VariantNamingService.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Concrete;

public sealed class VariantNamingService : IVariantNamingService
{
    public string? Compute(ProductVariant variant, Product product)
    {
        var values = variant.ProductVariantAttributes
            .Where(a => a.IsVarianter || a.IsSlicer)
            .OrderByDescending(a => a.IsVarianter)
            .Select(a => a.CategoryAttributeValue ?? a.CustomValue ?? string.Empty)
            .Where(v => !string.IsNullOrWhiteSpace(v));

        var computed = string.Join(" ", values).Trim();

        if (!string.IsNullOrWhiteSpace(computed)) return computed;
        if (!string.IsNullOrWhiteSpace(product.Title)) return product.Title;
        return null;
    }
}
```

- [ ] **Step 4: Test tekrar çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~VariantNamingServiceTests" -v n
```

Expected: 10 test passed.

- [ ] **Step 5: DI kayıt**

`Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` — `AddApplicationDependencies()` içinde, `services.AddScoped<IProductService, ProductManager>();` (satır 127) civarına:

```csharp
services.AddScoped<IVariantNamingService, VariantNamingService>();
```

- [ ] **Step 6: Tüm unit test suite'i çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

Expected: All passed (sadece yeni 10 test + mevcut ~1344).

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IVariantNamingService.cs \
  Application/Entegrasyon.Business/Concrete/VariantNamingService.cs \
  Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
  Test/Entegrasyon.Test/Business/VariantNamingServiceTests.cs
git commit -m "feat(variant): VariantNamingService + unit tests"
```

---

## Task 5: `ProductVariantManager.AddVariant` — Name set et

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/ProductVariantManager.cs`

- [ ] **Step 1: Constructor'a `IVariantNamingService` ekle**

Satır 17-24 primary constructor:

```csharp
public class ProductVariantManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IFluentValidator fluentValidator,
    IBarcodeService barcodeService,
    IImageManager imageManager,
    EventChannel<ProductUpdatedEvent> productUpdatedChannel,
    ITenantContext tenantContext,
    IVariantNamingService namingService) : IProductVariantManager
```

- [ ] **Step 2: `AddVariant` içinde Compute çağır**

`AddVariant` metodunda (satır 144-162 civarı), `dbContext.ProductVariants.Add(variant);` ÖNCESİNE:

```csharp
variant.Name = namingService.Compute(variant, product);
```

- [ ] **Step 3: `UpdateVariant` içinde safety recompute**

`UpdateVariant` metodunda (satır 176-207 civarı), `await dbContext.SaveChangesAsync();` ÖNCESİNE:

```csharp
variant.Name = namingService.Compute(variant, variant.Product);
```

- [ ] **Step 4: `ProductVariantManagerTests` constructor'ı düzelt**

`Test/Entegrasyon.Test/Business/ProductVariantManagerTests.cs` — `_sut = new ProductVariantManager(...)` çağrısına (satır 60-68) son parametre olarak:

```csharp
new VariantNamingService()
```

ekle (Mock değil, gerçek — pure logic).

- [ ] **Step 5: Build + test**

```bash
dotnet build Entegrasyon.sln
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductVariantManagerTests"
```

Expected: Build passed, testler yeşil.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/ProductVariantManager.cs \
  Test/Entegrasyon.Test/Business/ProductVariantManagerTests.cs
git commit -m "feat(variant): AddVariant/UpdateVariant Name compute entegrasyonu"
```

---

## Task 6: `ProductManager.AddProduct` — variant'lara Name ver

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs`

- [ ] **Step 1: Constructor'a `IVariantNamingService` ekle**

Satır 29-40:

```csharp
public class ProductManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    Entegrasyon.Business.Mappers.ProductMapper mapper,
    IFluentValidator validator,
    IOfficeStockManager officeStockManager,
    IAttributeKeyValueManager attributeKeyValueManager,
    IBarcodeService barcodeService,
    EventChannel<ProductAddedEvent> productAddedChannel,
    EventChannel<ProductUpdatedEvent> productUpdatedChannel,
    IMinioFileStorage minioFileStorage,
    ITenantContext tenantContext,
    IVariantNamingService namingService) : IProductService
```

- [ ] **Step 2: `AddProduct` içinde her variant için Compute**

`AddProduct` metodunda (satır 42-94), `attributeKeyValueManager.ClearEmptyAttributes(product);` ÖNCESİNE:

```csharp
foreach (var variant in product.ProductVariants)
    variant.Name = namingService.Compute(variant, product);
```

- [ ] **Step 3: Build**

```bash
dotnet build Entegrasyon.sln
```

- [ ] **Step 4: `ProductManagerTests` constructor'ını düzelt**

`Test/Entegrasyon.Test/Business/ProductManagerTests.cs` içindeki `ProductManager(...)` çağrılarına son parametre olarak `new VariantNamingService()` ekle. Birden fazla test sınıfı olabilir — grep ile bul:

```bash
grep -rn "new ProductManager(" Test/
```

Her bulunan satırda son parametreyi ekle (build hatası yönlendirecek).

- [ ] **Step 5: Unit testler yeşil mi**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

Expected: Passed.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/ProductManager.cs Test/Entegrasyon.Test/
git commit -m "feat(variant): ProductManager.AddProduct variant Name compute"
```

---

## Task 7: Marketplace import akışlarında Compute

**Files:**
- Investigate: Marketplace importer'lar (TrendyolImporter, Hepsiburada, N11, Pazarama, Amazon) variant yazan satırlar

- [ ] **Step 1: Variant DB'ye ekleyen import noktalarını bul**

```bash
grep -rn "dbContext.ProductVariants.Add\|ProductVariants = new.*ProductVariant\|new ProductVariant" \
  Application/Entegrasyon.Business/Concrete/Import/ \
  Application/Entegrasyon.Business/Concrete/Trendyol/ \
  Application/Entegrasyon.Business/Concrete/Hepsiburada/ \
  Application/Entegrasyon.Business/Concrete/N11/ \
  Application/Entegrasyon.Business/Concrete/Pazarama/ \
  Application/Entegrasyon.Business/Concrete/Amazon/ \
  Application/Entegrasyon.Business/Concrete/MatchedEntityImportManager.cs 2>/dev/null
```

Bulunan her dosyada variant oluşturulup DB'ye yazıldığı yere yakın bir noktada `variant.Name = namingService.Compute(variant, product)` çağrısı gerekli.

- [ ] **Step 2: Her importer için değişiklik uygula**

Her import manager'ının primary constructor'ına `IVariantNamingService namingService` parametresi ekle; variant oluşturulduktan sonra Name set et.

Özel dikkat: `MatchedEntityImportManager.cs` — en kritik nokta.

Örnek pattern (import manager içinde):

```csharp
foreach (var variant in newVariants)
    variant.Name = namingService.Compute(variant, product);
```

- [ ] **Step 3: Build**

```bash
dotnet build Entegrasyon.sln
```

Derleme hatası varsa eksik constructor parametrelerini sırayla ekle.

- [ ] **Step 4: Integration test suite'i çalıştır (regresyon)**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~Matched|FullyQualifiedName~Import"
```

Expected: Passed.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/
git commit -m "feat(variant): marketplace import akislarinda Name compute"
```

---

## Task 8: Integration test — `ProductVariantNamingIntegrationTests`

**Files:**
- Create: `Test/Entegrasyon.IntegrationTest/Business/ProductVariantNamingIntegrationTests.cs`

- [ ] **Step 1: Testi yaz (Add + Update + Null backfill senaryoları)**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Business;

[Trait("Category", "Integration")]
public class ProductVariantNamingIntegrationTests : IntegrationTestBase
{
    public ProductVariantNamingIntegrationTests(PostgreSqlFixture pg, WireMockFixture wm) : base(pg, wm) { }

    protected override async Task OnInitializeAsync()
    {
        using var db = CreateDbContext();
        if (!await db.BranchOffices.AnyAsync(b => b.Id == 1))
            db.BranchOffices.Add(new BranchOffice { Id = 1, Name = "Ana Depo", IsDefaultMarketPlaceStock = true, CreatedAt = DateTimeOffset.UtcNow });
        if (!await db.Brands.AnyAsync(b => b.Name == "Naming Marka"))
            db.Brands.Add(new Brand { Name = "Naming Marka", CreatedAt = DateTimeOffset.UtcNow });
        if (!await db.Categories.AnyAsync(c => c.Name == "Naming Kategori"))
            db.Categories.Add(new Category { Name = "Naming Kategori", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AddProduct_ComputesVariantName_WithVarianterAndSlicer()
    {
        using var db = CreateDbContext();
        var brandId = (await db.Brands.FirstAsync(b => b.Name == "Naming Marka")).Id;
        var categoryId = (await db.Categories.FirstAsync(c => c.Name == "Naming Kategori")).Id;

        var dto = new AddProductDto
        {
            Title = "Naming Test Tshirt",
            StockCode = "NAME-TST-001",
            BrandId = brandId,
            CategoryId = categoryId,
            ProductVariants = new List<AddProductVariantDto>
            {
                new()
                {
                    Barcode = "NAME0001",
                    SalePrice = 100m,
                    CostPrice = 50m,
                    VatRate = 20m,
                    ProductVariantAttributes = new List<Entegrasyon.Entity.Dtos.Attributes.VariantAttributeDto>
                    {
                        new(null, "Sarı", "", true, false),
                        new(null, "XL", "", false, true)
                    },
                    BranchOfficeStocks = new List<AddBranchOfficeStockDto>
                    {
                        new() { BranchOfficeId = 1, FirstTotalStock = 10 }
                    }
                }
            }
        };

        var productService = Scope.ServiceProvider.GetRequiredService<IProductService>();
        var result = await productService.AddProduct(dto);
        result.Success.Should().BeTrue();

        using var verifyDb = CreateDbContext();
        var saved = await verifyDb.ProductVariants.AsNoTracking()
            .Include(v => v.ProductVariantAttributes)
            .FirstAsync(v => v.Barcode == "NAME0001");

        saved.Name.Should().Be("Sarı XL");
    }

    [Fact]
    public async Task Migration_AllowsNullName_OnLegacyVariant()
    {
        using var db = CreateDbContext();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Legacy Urun",
            StockCode = "LEG-001",
            BrandId = (await db.Brands.FirstAsync()).Id,
            CategoryId = (await db.Categories.FirstAsync()).Id,
            CreatedAt = DateTimeOffset.UtcNow,
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Barcode = "LEG001",
                    SalePrice = 1m,
                    CostPrice = 1m,
                    ListPrice = 1m,
                    ECommercePrice = 1m,
                    Name = null,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        };
        db.MainProducts.Add(product);
        await db.SaveChangesAsync();

        using var verifyDb = CreateDbContext();
        var saved = await verifyDb.ProductVariants.AsNoTracking()
            .FirstAsync(v => v.Barcode == "LEG001");

        saved.Name.Should().BeNull();
    }
}
```

- [ ] **Step 2: Integration testleri çalıştır (Docker ayakta olmalı)**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~ProductVariantNamingIntegrationTests"
```

Expected: 2 passed.

- [ ] **Step 3: Commit**

```bash
git add Test/Entegrasyon.IntegrationTest/Business/ProductVariantNamingIntegrationTests.cs
git commit -m "test(variant): integration tests for naming on Add + null legacy"
```

---

## Task 9: `VariantNameExtensions` static class

**Files:**
- Create: `Application/Entegrasyon.Business/Extensions/VariantNameExtensions.cs`
- Create: `Test/Entegrasyon.Test/Business/VariantNameExtensionsTests.cs`

- [ ] **Step 1: Failing test yaz**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Extensions;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class VariantNameExtensionsTests
{
    [Fact]
    public void Resolve_PrefersStoredName()
    {
        var result = VariantNameExtensions.ResolveDisplayName(
            "Override",
            new[] { new VariantAttributeLite("Sarı", null, true, false, 0) },
            "Tshirt");
        result.Should().Be("Override");
    }

    [Fact]
    public void Resolve_FallsBackToComputeWhenNameNull()
    {
        var result = VariantNameExtensions.ResolveDisplayName(
            null,
            new[]
            {
                new VariantAttributeLite("Sarı", null, true, false, 0),
                new VariantAttributeLite("XL", null, false, true, 1)
            },
            "Tshirt");
        result.Should().Be("Sarı XL");
    }

    [Fact]
    public void Resolve_FallsBackToProductTitleWhenAllEmpty()
    {
        var result = VariantNameExtensions.ResolveDisplayName(null, Array.Empty<VariantAttributeLite>(), "Tshirt");
        result.Should().Be("Tshirt");
    }

    [Fact]
    public void Resolve_TreatsWhitespaceNameAsNull()
    {
        var result = VariantNameExtensions.ResolveDisplayName(
            "   ",
            new[] { new VariantAttributeLite("Sarı", null, true, false, 0) },
            "Tshirt");
        result.Should().Be("Sarı");
    }

    [Fact]
    public void Resolve_NullAttributes_FallsBackToTitle()
    {
        var result = VariantNameExtensions.ResolveDisplayName(null, null, "Tshirt");
        result.Should().Be("Tshirt");
    }
}
```

- [ ] **Step 2: Testi çalıştır (fail)**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~VariantNameExtensionsTests"
```

Expected: Build fails.

- [ ] **Step 3: Implementation**

`Application/Entegrasyon.Business/Extensions/VariantNameExtensions.cs`:

```csharp
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Extensions;

public static class VariantNameExtensions
{
    public static string ResolveDisplayName(
        string? storedName,
        IEnumerable<VariantAttributeLite>? attributes,
        string productTitle)
    {
        if (!string.IsNullOrWhiteSpace(storedName)) return storedName.Trim();

        if (attributes is not null)
        {
            var values = attributes
                .Where(a => a.IsVarianter || a.IsSlicer)
                .OrderByDescending(a => a.IsVarianter)
                .ThenBy(a => a.Order)
                .Select(a => a.Value ?? a.CustomValue ?? string.Empty)
                .Where(v => !string.IsNullOrWhiteSpace(v));

            var computed = string.Join(" ", values).Trim();
            if (!string.IsNullOrWhiteSpace(computed)) return computed;
        }

        return productTitle;
    }
}
```

- [ ] **Step 4: Testleri çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~VariantNameExtensionsTests"
```

Expected: 5 passed.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Extensions/VariantNameExtensions.cs \
  Test/Entegrasyon.Test/Business/VariantNameExtensionsTests.cs
git commit -m "feat(variant): VariantNameExtensions.ResolveDisplayName + tests"
```

---

## Task 10: `_VariantLabel.cshtml` + `VariantLabelModel`

**Files:**
- Create: `Application/Entegrasyon.MVC/ViewModels/Shared/VariantLabelModel.cs`
- Create: `Application/Entegrasyon.MVC/Views/Shared/_VariantLabel.cshtml`

- [ ] **Step 1: VM**

```csharp
namespace Entegrasyon.MVC.ViewModels.Shared;

public sealed record VariantLabelModel(string DisplayName, string? Barcode = null, bool ShowBarcode = false);
```

- [ ] **Step 2: Partial view**

```razor
@model Entegrasyon.MVC.ViewModels.Shared.VariantLabelModel

<span class="variant-label" title="@Model.Barcode">
    <span class="fw-semibold">@Model.DisplayName</span>
    @if (Model.ShowBarcode && !string.IsNullOrEmpty(Model.Barcode))
    {
        <code class="text-secondary small ms-1">@Model.Barcode</code>
    }
</span>
```

- [ ] **Step 3: Build**

```bash
dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj
```

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/ViewModels/Shared/ \
  Application/Entegrasyon.MVC/Views/Shared/_VariantLabel.cshtml
git commit -m "feat(mvc): shared _VariantLabel partial + VariantLabelModel"
```

---

## Task 11: Read-path konvansiyonu — referans

**Uygulanma notu (kod yazılmaz, sadece standart):** Task 12-22'de tekrarlanan pattern şudur:

### DTO alan adı konvansiyonu

- **Variant-tek DTO'lar** (variant'ın kendisini temsil eden): alan adı `DisplayName`. Örn: `POSProductSearchVariantDto`, `VariantDetailPageDto`, `ProductVariantSaleSearchDto`.
- **Composite/parent DTO'lar** (içinde variant referansı olan satır): alan adı `VariantDisplayName` (product ya da order alanlarıyla karışmasın diye). Örn: `SaleLineItemDto`, `OrderItemDto`, `StockMovementRowDto`, `BranchOfficeStockRowDto`.

### Projection pattern (her manager'da)

```csharp
var raw = await dbContext.ProductVariants  // veya SaleItems, OrderItems vs.
    .Select(x => new
    {
        // ... mevcut alanlar
        x.Name,                              // veya x.ProductVariant.Name
        ProductTitle = x.Product.Title,      // veya x.ProductVariant.Product.Title
        RawAttrs = x.ProductVariantAttributes
            .Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer })
            .ToList()
    })
    .ToListAsync();

var result = raw.Select(r => new FooDto(
    // ... mevcut alanlar
    DisplayName: VariantNameExtensions.ResolveDisplayName(
        r.Name,
        r.RawAttrs.Select((a, i) =>
            new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
        r.ProductTitle)
)).ToList();
```

### View pattern

Ürün ana adı/ID kolonu yanında ikincil satır olarak:

```razor
<div class="text-secondary small">@item.VariantDisplayName</div>
```

Veya shared partial ile:

```razor
<partial name="_VariantLabel" model='new VariantLabelModel(item.VariantDisplayName, item.Barcode, true)' />
```

### Zorunlu using'ler

Projection yapan her manager dosyasının üst kısmına:

```csharp
using Entegrasyon.Business.Abstract;    // VariantAttributeLite
using Entegrasyon.Business.Extensions;  // VariantNameExtensions
```

---

## Task 12: POS DTO + controller + view güncellemeleri

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/POS/POSProductSearchDto.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs` (POS search projection, satır 399-485)
- Modify: `Application/Entegrasyon.MVC/Features/POS/ViewModels/POSTerminalVm.cs`
- Modify: `Application/Entegrasyon.MVC/Features/POS/POSController.cs`
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSSearchResults.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCart.cshtml`

- [ ] **Step 1: `POSProductSearchVariantDto` — `AttributeSummary` → `DisplayName`**

```csharp
public sealed class POSProductSearchVariantDto
{
    // ... mevcut alanlar
    public string DisplayName { get; set; } = "";  // AttributeSummary yerine
}
```

Eski `AttributeSummary` alanını kaldır.

- [ ] **Step 2: `ProductManager.cs` — POS search projection (2 yer: satır 399-425, 441-485)**

Mevcut `AttributeSummary = string.Join(" / ", ...)` satırlarını bul. Projection'ı şu şekilde değiştir:

Örnek (satır 399-425 civarı — `ExactBarcodeMatch`):

```csharp
var exactBarcodeProjection = await dbContext.ProductVariants
    .Where(pv => pv.Barcode == query.SearchQuery)
    .Select(pv => new
    {
        pv.ProductId,
        VariantId = pv.Id,
        ProductTitle = pv.Product.Title,
        pv.Barcode,
        ImageUrl = pv.Images.Where(i => i.IsMain).Select(i => i.Src).FirstOrDefault(),
        pv.SalePrice,
        pv.ListPrice,
        pv.VatRate,
        CurrentStock = pv.BranchOfficeStocks.Sum(s => s.CurrentStock),
        Name = pv.Name,
        RawAttributes = pv.ProductVariantAttributes.Select(a => new
        {
            a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer
        }).ToList()
    })
    .FirstOrDefaultAsync();

var exactBarcode = exactBarcodeProjection == null ? null : new POSProductSearchVariantDto
{
    ProductId = exactBarcodeProjection.ProductId,
    VariantId = exactBarcodeProjection.VariantId,
    ProductTitle = exactBarcodeProjection.ProductTitle,
    Barcode = exactBarcodeProjection.Barcode ?? "",
    ImageUrl = exactBarcodeProjection.ImageUrl,
    SalePrice = exactBarcodeProjection.SalePrice,
    ListPrice = exactBarcodeProjection.ListPrice,
    VatRate = exactBarcodeProjection.VatRate,
    CurrentStock = exactBarcodeProjection.CurrentStock,
    DisplayName = VariantNameExtensions.ResolveDisplayName(
        exactBarcodeProjection.Name,
        exactBarcodeProjection.RawAttributes.Select((a, i) =>
            new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
        exactBarcodeProjection.ProductTitle)
};
```

`using Entegrasyon.Business.Abstract;` (`VariantAttributeLite`) + `using Entegrasyon.Business.Extensions;` ekle.

İkinci yer (`Variants = ...` projection'ı, satır 459-485) için benzer mapping: raw anonymous → DTO dönüşümünde `DisplayName` set et.

- [ ] **Step 3: `POSTerminalVm.cs` — `VariantAttributeSummary` → `DisplayName`**

```csharp
public class POSCartItem  // veya ilgili record
{
    // ... mevcut alanlar
    public string DisplayName { get; set; } = "";  // VariantAttributeSummary yerine
}
```

- [ ] **Step 4: `POSController.cs` satır 202**

`VariantAttributeSummary = variantSummary?.Trim() ?? ""` →
`DisplayName = variantSummary?.Trim() ?? ""`

Ayrıca `variantSummary` değişkeninin nasıl hesaplandığına bak; eğer `AttributeSummary`'den geliyorsa artık `DisplayName`'den gelmeli.

- [ ] **Step 5: `_POSSearchResults.cshtml` güncelle**

Satır 16, 24-27, 72, 118, 123:
- `v.AttributeSummary` → `v.DisplayName`
- Label fallback: `var label = string.IsNullOrEmpty(v.DisplayName) ? v.Barcode : v.DisplayName;` (ek değişiklik: `DisplayName` zaten non-null, fallback gereksiz — ama backward safety korunsun)

Önerilen partial kullanımı: `<partial name="_VariantLabel" model='new VariantLabelModel(v.DisplayName, v.Barcode, false)' />`

- [ ] **Step 6: `_POSCart.cshtml` güncelle**

Satır 48-51:
`item.VariantAttributeSummary` → `item.DisplayName`

- [ ] **Step 7: Build + POS test suite**

```bash
dotnet build Entegrasyon.sln
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~POS"
dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --filter "FullyQualifiedName~POS"
```

Expected: Passed.

- [ ] **Step 8: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/POS/ \
  Application/Entegrasyon.Business/Concrete/ProductManager.cs \
  Application/Entegrasyon.MVC/Features/POS/
git commit -m "feat(pos): variant DisplayName replaces AttributeSummary"
```

---

## Task 13: Products detay — `Variants.cshtml` + `VariantDetail.cshtml`

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/ProductVariantDetailDto.cs` (veya `ProductDetailDto.cs` içinde)
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs` (satır 313-370 civarı — `GetProducts`/`GetProductDetail`)
- Modify: `Application/Entegrasyon.Business/Concrete/ProductVariantManager.cs` (VariantDetailPageDto oluştuğu yer — satır 267-338)
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/ProductVariant/VariantDetailPageDto.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Variants.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/VariantDetail.cshtml`

- [ ] **Step 1: `VariantDetailPageDto.cs` — `DisplayName` ekle**

```csharp
public record VariantDetailPageDto(
    Guid ProductId,
    string ProductTitle,
    string DisplayName,          // YENİ
    Guid VariantId,
    // ... mevcut alanlar
);
```

Çağıranları (`VariantDetail.cshtml`, controller) buna uygun güncelle.

- [ ] **Step 2: `ProductVariantManager.GetVariantDetailPage` — DisplayName doldur**

Satır 267-338:

```csharp
var variantName = variant.Name;  // zaten dolu olabilir
var displayName = VariantNameExtensions.ResolveDisplayName(
    variantName,
    variant.ProductVariantAttributes.Select((a, i) =>
        new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
    variant.Product.Title);

var dto = new VariantDetailPageDto(
    variant.ProductId,
    variant.Product.Title,
    displayName,
    variant.Id,
    // ... mevcut alanlar
);
```

- [ ] **Step 3: `ProductVariantDetailDto` için DisplayName ekle**

`Application/Entegrasyon.Entity/Dtos/Product/` altındaki `ProductVariantDetailDto`'ya yeni alan:

```csharp
public record ProductVariantDetailDto(
    Guid Id,
    string Barcode,
    string DisplayName,          // YENİ
    // ... mevcut
);
```

- [ ] **Step 4: `ProductManager.cs` `GetProductDetail` projection'ında DisplayName ekle**

Satır 313-338 civarı — `ProductVariants.Select(pv => new ProductVariantDetailDto(...))`. Raw anonim + map pattern'iyle Name + RawAttributes çek, in-memory ResolveDisplayName kullan.

- [ ] **Step 5: `Variants.cshtml` — tabloda ilk kolon DisplayName**

Satır 62-94 arası tablo thead'a yeni kolon ekle ve tbody'de gösterime al:

```razor
<thead>
    <tr>
        <th>Varyant</th>           @* YENİ *@
        <th>Barkod</th>
        <th>Stok Kodu</th>
        ...
    </tr>
</thead>
```

`<tr>` içinde:
```razor
<td><span class="fw-semibold">@variant.DisplayName</span></td>
```

Barkod kolonu korunur.

- [ ] **Step 6: `VariantDetail.cshtml` — başlıkta DisplayName**

Üst kısmında sayfa başlığı (muhtemelen bir `<h1>` veya breadcrumb) — `Model.ProductTitle` yanına veya yerine `@Model.DisplayName` ekle.

- [ ] **Step 7: Build + test**

```bash
dotnet build Entegrasyon.sln
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Product"
```

- [ ] **Step 8: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Product/ \
  Application/Entegrasyon.Business/Concrete/ProductManager.cs \
  Application/Entegrasyon.Business/Concrete/ProductVariantManager.cs \
  Application/Entegrasyon.MVC/Features/Products/Views/
git commit -m "feat(products): variant DisplayName on detail + variants list"
```

---

## Task 14: `ProductVariantSaleSearchDto` + sale search projection

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/ProductVariant/` (`ProductVariantSaleSearchDto.cs` veya benzeri)
- Modify: `Application/Entegrasyon.Business/Concrete/ProductVariantManager.cs` (satır 46-81)

- [ ] **Step 1: DTO'ya `DisplayName` ekle**

`ProductVariantSaleSearchDto`'ya `string DisplayName` parametresi ekle.

- [ ] **Step 2: `GetProductVariantByBarcode` ve `GetProductVariantsBySearchText` projection'ına DisplayName ekle**

Pattern: anonim çek, in-memory resolve.

```csharp
var raw = await dbContext.ProductVariants
    .Where(x => x.Barcode == barcode)
    .Select(x => new
    {
        x.Id,
        ProductTitle = x.Product.Title,
        Image = x.Images.FirstOrDefault(img => img.IsMain)!.Src ?? x.Images.FirstOrDefault()!.Src ?? "",
        x.VatRate, x.ListPrice, x.SalePrice, x.CostPrice,
        Stock = x.BranchOfficeStocks.Sum(z => z.CurrentStock),
        CategoryName = x.Product.Category.Name,
        x.Name,
        Attrs = x.ProductVariantAttributes.Select(a =>
            new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList()
    })
    .FirstOrDefaultAsync();

if (raw == null) return new ErrorDataResult<...>(...);

var displayName = VariantNameExtensions.ResolveDisplayName(
    raw.Name,
    raw.Attrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
    raw.ProductTitle);

var dto = new ProductVariantSaleSearchDto(raw.Id, raw.ProductTitle, displayName, raw.Image, raw.VatRate, raw.ListPrice, raw.SalePrice, raw.CostPrice, raw.Stock, raw.CategoryName);
```

- [ ] **Step 3: Build + test + commit**

```bash
dotnet build Entegrasyon.sln
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductVariantManager"
git add Application/Entegrasyon.Entity/Dtos/Product/ \
  Application/Entegrasyon.Business/Concrete/ProductVariantManager.cs
git commit -m "feat(variant): ProductVariantSaleSearchDto DisplayName"
```

---

## Task 15: Sales — SaleDetail + OrderDetail + OrderPrint

**Files:**
- Identify: Sales DTO'ları `Application/Entegrasyon.Entity/Dtos/Sale/` veya `Features/Sales/`
- Modify: ilgili DTO'lar + SaleManager projection'ı
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/OrderDetail.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/OrderPrint.cshtml`

- [ ] **Step 1: Sales satır DTO'sunu keşfet**

```bash
grep -rn "ProductVariant\|variant" Application/Entegrasyon.MVC/Features/Sales/ViewModels/ \
  Application/Entegrasyon.Entity/Dtos/Sale/ 2>/dev/null
```

Tipik yapı: `SaleLineItemDto` veya benzeri.

- [ ] **Step 2: DTO'ya `VariantDisplayName string` ekle**

Her sales line item DTO'suna.

- [ ] **Step 3: `SaleManager` projection'ı güncelle**

`Application/Entegrasyon.Business/Concrete/SaleManager.cs` veya benzeri — sale item projection'ında anonim + resolve pattern'i uygula.

- [ ] **Step 4: 3 view'da variant label ekle**

Ürün adı yanına veya ayrı kolonda:

```razor
<div class="text-secondary small">@item.VariantDisplayName</div>
```

veya:

```razor
<partial name="_VariantLabel" model='new VariantLabelModel(item.VariantDisplayName, item.Barcode, true)' />
```

- [ ] **Step 5: Build + test + commit**

```bash
dotnet build Entegrasyon.sln
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Sale"
git add Application/Entegrasyon.Entity/Dtos/Sale/ Application/Entegrasyon.Business/Concrete/SaleManager.cs \
  Application/Entegrasyon.MVC/Features/Sales/
git commit -m "feat(sales): variant DisplayName on SaleDetail/OrderDetail/OrderPrint"
```

---

## Task 16: Returns — `Detail.cshtml`

**Files:**
- Identify: Returns DTO
- Modify: `Application/Entegrasyon.Business/Concrete/SaleReturnManager.cs` (muhtemel)
- Modify: `Application/Entegrasyon.MVC/Features/Returns/Views/Detail.cshtml`

- [ ] **Step 1: Returns DTO'sunu keşfet**

```bash
grep -rn "return.*variant\|ReturnItemDto\|SaleReturnDetailDto" Application/Entegrasyon.Entity/Dtos/ \
  Application/Entegrasyon.MVC/Features/Returns/ 2>/dev/null
```

- [ ] **Step 2-4: Aynı pattern — DTO alanı, projection, view**

DTO'ya `VariantDisplayName` ekle, ilgili manager projection'ına resolve, view'da göster.

- [ ] **Step 5: Build + test + commit**

```bash
dotnet build Entegrasyon.sln
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Return"
git add Application/Entegrasyon.Business/Concrete/SaleReturnManager.cs \
  Application/Entegrasyon.MVC/Features/Returns/ Application/Entegrasyon.Entity/Dtos/
git commit -m "feat(returns): variant DisplayName on return detail"
```

---

## Task 17: Orders — `Detail.cshtml` (Orders feature)

**Files:**
- Identify: `Application/Entegrasyon.Business/Concrete/OrderManager.cs` (veya OrderService)
- Modify: Order line item DTO
- Modify: `Application/Entegrasyon.MVC/Features/Orders/Views/Detail.cshtml`

- [ ] **Step 1: Mevcut akışı keşfet**

```bash
grep -rn "OrderLineItem\|OrderItemDto\|order.*variant" Application/Entegrasyon.Entity/Dtos/Order/ 2>/dev/null
```

- [ ] **Step 2-4: DTO alanı + projection + view güncellemeleri**

Aynı pattern.

- [ ] **Step 5: Commit**

```bash
dotnet build Entegrasyon.sln
git add Application/Entegrasyon.Business/Concrete/OrderManager.cs \
  Application/Entegrasyon.MVC/Features/Orders/ Application/Entegrasyon.Entity/Dtos/Order/
git commit -m "feat(orders): variant DisplayName on order detail"
```

---

## Task 18: Picking — `_OrderItems.cshtml`

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/PickingManager.cs` (muhtemel)
- Modify: `Application/Entegrasyon.MVC/Features/Picking/Views/Partials/_OrderItems.cshtml`

- [ ] **Step 1-4: Standart pattern — DTO + projection + view**

Picking bir sub-feature, OrderDetail benzeri yapıda. Aynı DTO/projection değişikliğini uygula.

- [ ] **Step 5: Commit**

```bash
dotnet build Entegrasyon.sln
git add Application/Entegrasyon.Business/Concrete/PickingManager.cs Application/Entegrasyon.MVC/Features/Picking/
git commit -m "feat(picking): variant DisplayName on order items"
```

---

## Task 19: Reports — Inventory + StockAlert

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/ReportManager.cs` (veya ilgili)
- Modify: `Application/Entegrasyon.MVC/Features/Reports/Views/Inventory.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Reports/Views/Partials/_StockAlertTable.cshtml`

- [ ] **Step 1-4: Standart pattern**

Her iki view'da variant gösterilen satırlarda `VariantDisplayName` ekle. Inventory muhtemelen ayrı DTO kullanıyor; stock alert ayrı.

- [ ] **Step 5: Commit**

```bash
dotnet build Entegrasyon.sln
git add Application/Entegrasyon.Business/Concrete/ Application/Entegrasyon.MVC/Features/Reports/
git commit -m "feat(reports): variant DisplayName on inventory + stock alerts"
```

---

## Task 20: Stock — Transfers + Movements

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/StockTransferManager.cs` ve `StockMovementManager.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Stock/Transfers/Views/Detail.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/StockMovements/Views/Partials/_MovementTable.cshtml`

- [ ] **Step 1-4: Standart pattern**

İki farklı feature, her biri kendi DTO + projection + view pattern'ini izleyecek.

- [ ] **Step 5: Commit**

```bash
dotnet build Entegrasyon.sln
git add Application/Entegrasyon.Business/Concrete/ Application/Entegrasyon.MVC/Features/Stock/ Application/Entegrasyon.MVC/Features/StockMovements/
git commit -m "feat(stock): variant DisplayName on transfers + movements"
```

---

## Task 21: BranchOffices — Detail + StockTransferDialog

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/BranchOfficeManager.cs` (muhtemel)
- Modify: `Application/Entegrasyon.MVC/Features/BranchOffices/Views/Detail.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/BranchOffices/Views/_StockTransferDialog.cshtml`

- [ ] **Step 1-4: Standart pattern**

- [ ] **Step 5: Commit**

```bash
dotnet build Entegrasyon.sln
git add Application/Entegrasyon.Business/Concrete/BranchOfficeManager.cs Application/Entegrasyon.MVC/Features/BranchOffices/
git commit -m "feat(branches): variant DisplayName on detail + transfer dialog"
```

---

## Task 22: Pricing — `_PriceVariants.cshtml`

**Files:**
- Modify: `Application/Entegrasyon.Entity/Dtos/Product/VariantPricingRowDto.cs` (`VariantName` zaten var, Compute'a bağla)
- Modify: İlgili manager projection'ı (muhtemel `PricingManager.cs`)
- Modify: `Application/Entegrasyon.MVC/Features/Pricing/Views/Partials/_PriceVariants.cshtml`

- [ ] **Step 1: DTO'yu güncelle (`VariantName` → `DisplayName` rename veya aynı bırak + logic değiştir)**

DTO'da mevcut `VariantName` alanının nasıl doldurulduğunu kontrol et. Eğer şu an barkod veya ilk varianter değeri ile dolduruluyorsa, resolve extension'a geçir. Alan adı aynı kalabilir (`VariantName`) — sadece değer kaynağı değişir.

- [ ] **Step 2: Manager projection'ında resolve pattern'i**

- [ ] **Step 3: View'da kullanım zaten var, test et**

- [ ] **Step 4: Commit**

```bash
dotnet build Entegrasyon.sln
git add Application/Entegrasyon.Entity/Dtos/Product/VariantPricingRowDto.cs \
  Application/Entegrasyon.Business/Concrete/ \
  Application/Entegrasyon.MVC/Features/Pricing/
git commit -m "feat(pricing): VariantName sourced from ResolveDisplayName"
```

---

## Task 23: E2E smoke test + full suite

**Files:**
- Create (opsiyonel): `Test/Entegrasyon.E2E/Tests/VariantNamingE2ETests.cs`

- [ ] **Step 1: Full unit suite**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

Expected: All passed.

- [ ] **Step 2: Full integration suite**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```

Expected: All passed.

- [ ] **Step 3: MVC test suite**

```bash
dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj
```

Expected: All passed.

- [ ] **Step 4: Dev server'ı ayağa kaldır ve manuel doğrula**

```bash
cd Application/Entegrasyon.MVC && dotnet run
```

Kontrol et (tarayıcıda http://localhost:5100):
- Bir ürün detayına git → variant tablosunda `DisplayName` kolonu görünüyor
- Yeni ürün oluştur (wizard, Renk=Sarı, Beden=XL) → variant list'te `"Sarı XL"` gözüküyor
- POS sayfası → arama + sepette variant DisplayName doğru
- Bir satış detayına git → variant label görünüyor

- [ ] **Step 5: Migration drift kontrolü**

```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: `No changes`.

- [ ] **Step 6: Son commit (varsa kalan)**

```bash
git status
git add -A
git commit -m "chore(variant): final smoke test + cleanup" || echo "Temiz"
```

---

## Self-Review Checklist (implementation sırasında kullanılır)

- Her DTO değişikliği için: hem `Entity/Dtos/` hem `Business/Mappers/` hem view → güncellendi mi?
- Her manager değişikliği için: DI + constructor + primary test suite'i güncellendi mi?
- `VariantNameExtensions.ResolveDisplayName` kullanılan her yerde `using Entegrasyon.Business.Extensions;` import'u var mı?
- Her commit sonrası `dotnet build` yeşil mi?
- Migration snapshot drift yok mu?

---

## Potansiyel Bloklar

- **Sipariş/Sales feature'ında DTO'lar Mapper ile üretiliyor olabilir** — `Business/Mappers/` içindeki Mapperly kaynak generator'ları güncellemek gerekebilir.
- **Test dbContext mock'ları** — `ReturnsDbSet` ile kurulu testlerde `ProductVariantAttributes` navigation'ı eksikse Compute bazı testlerde `product.Title` döner; assertion'ları buna göre ayarla.
- **İzole stage DB** — migration stage'de uygulanmadan prod'a geçilirse eski variant'lar Name null kalır; run-time fallback halleder.
