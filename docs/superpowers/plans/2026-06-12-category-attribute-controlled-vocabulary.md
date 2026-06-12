# Category Attribute Kontrollü Kelime Dağarcığı — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Category attribute'larda serbest düz yazıyı (`AllowCustom`/`CustomValue`) tamamen kaldır; her attribute'u TR-kanonik normalize + DB-unique ile tekilleşen, büyüyen kapalı bir değer listesine dönüştür.

**Architecture:** `CategoryAttributeValue`'ya `NormalizedName` kanonik anahtar + filtreli unique index eklenir. Kullanıcının yazdığı her değer `GetOrCreate` ile normalize edilip tekilleştirilerek gerçek değere terfi eder; `AttributeKeyValue.AttributeValueId` daima dolu olur. `CategoryAttribute.AllowCustom` ve her iki entity'deki `CustomValue` kolonları + ~30 tüketici noktası sökülür. Pazaryeri gönderiminde değer-match varsa mapped id, yoksa değer adı string olarak basılır.

**Tech Stack:** .NET 10, EF Core (PostgreSQL, no-tracking, `IDbContextFactory`), FluentValidation + LogicRunner, Mapperly, ASP.NET Core MVC + HTMX + Tabler + Tom Select, xUnit + Testcontainers (Integration), Playwright (E2E).

**Önemli kurallar (CLAUDE.md):**
- **EF Mutasyon Persist:** mutasyon metodunda LINQ ile yüklenen entity mutate ediliyorsa `.AsTracking()` ŞART (yoksa sessiz no-op). Her mutasyona RED-first persist testi.
- **Migration strict-rule:** `migrations add` → gözden geçir → `database update` → `has-pending-model-changes` doğrula.
- **TDD-First:** önce RED test, sonra GREEN.
- **Container politikası:** Testcontainers için server Docker'ı socket-tünelle (yerel container kurma). Migration/integration testi öncesi:
  ```bash
  ssh -nNT -L /tmp/docker-server.sock:/var/run/docker.sock server &
  export DOCKER_HOST=unix:///tmp/docker-server.sock TESTCONTAINERS_HOST_OVERRIDE=192.168.1.78 TESTCONTAINERS_RYUK_DISABLED=true
  ```

**Komut kısaltmaları:**
- Build: `dotnet build Entegrasyon.sln`
- Unit: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
- Integration: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
- Migration add: `dotnet ef migrations add <Name> -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`
- Migration update: `dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`

---

## Dosya Haritası

**Yeni:**
- `Application/Entegrasyon.Business/Helpers/AttributeValueNormalizer.cs` — TR-kanonik normalize (saf fonksiyon).
- `Test/Entegrasyon.Test/Business/AttributeValueNormalizerTests.cs` — normalize unit testleri.
- `Test/Entegrasyon.Test/Business/CategoryAttributeValueManagerGetOrCreateTests.cs` — GetOrCreate unit (mock yerine in-memory factory varsa integration'a taşınır; bkz Task 3).
- `Test/Entegrasyon.IntegrationTest/Business/CategoryAttributeValueDedupIntegrationTests.cs` — unique-index dedup + persist.
- `Application/Entegrasyon.MVC/Features/Attributes/Views/Values.cshtml` — değer yönetim sayfası.
- `Application/Entegrasyon.MVC/Features/Attributes/Views/Partials/_AttributeValueTable.cshtml` — değer tablosu partial.

**Değişen (entity/config):**
- `Application/Entegrasyon.Entity/Categories/CategoryAttribute.cs` — `AllowCustom` sil.
- `Application/Entegrasyon.Entity/Categories/CategoryAttributeValue.cs` — `NormalizedName` ekle.
- `Application/Entegrasyon.Entity/Categories/AttributeKeyValue.cs` — `CustomValue` sil, `AttributeValueId` non-null.
- `Application/Entegrasyon.Entity/Products/ProductVariantAttribute.cs` — `CustomValue` sil.
- `Application/Entegrasyon.Entity/Templates/TemplateCategoryAttributeData.cs` — `AllowCustom` sil.
- `Application/Entegrasyon.DataAccess/.../EntityConfigurations/CategoryAttributeValueEntityConfiguration.cs` (yoksa oluştur) — NormalizedName + filtreli unique index.

**Değişen (business — CustomValue/AllowCustom söküm, ~30 nokta):** `CategoryAttributeValueManager`, `ICategoryAttributeValueManager`, `AttributeKeyValueManager`, `IVariantNamingService`+`VariantAttributeLite`, `VariantNamingService`, `ProductVariantManager`, `ProductManager`, `SaleManager`, `LabelManager`, `DiscountManager`, `MarketplaceOverrideManager`, `OfficeStockManager`, `StockTransferRequestManager`, `BranchOfficeManager`, `CategoryAttributeManager`, `CategoryManager`, `CategoryAttributeCategoryManager`, `MatchedEntityImportManager`, `AttributeAutoMatchService`, importer'lar (`TrendyolCategoryImporter`, `TrendyolCategoryImporterService`, `CiceksepetiCategoryImporter`, `HepsiburadaCategoryImporter`, `N11CategoryImporter`, `N11RestCategoryImporter`, `PazaramaCategoryImporter`), validatorlar (`AddCategoryAttributeDtoValidator`, `EditCategoryAttributeDtoValidator`), `CategoryAttributeMapper`, Trendyol send `TrendyolProductMapper`.

**Değişen (DTO/VM):** `AddCategoryAttributeDto`, `EditCategoryAttributeDto`, `CategoryAttributeDto`, `AttributeKeyValueDto`, `CreateProductVm`, `CategoryCreateVm`, `EditAttributeVm`.

**Değişen (MVC view):** `Features/Products/Views/Partials/_CreateStep2Attributes.cshtml`, `_CreateStep3Variants.cshtml`, `_CreateStep5Review.cshtml`, `Features/Products/Views/Edit.cshtml`, `Features/Attributes/Views/Index.cshtml`, `Features/Attributes/Views/Partials/_AttributeDetail.cshtml`, `Features/Categories/Views/*` (AllowCustom kolon/checkbox).

**Değişen (controller):** `Features/Attributes/AttributeController.cs`, `Features/Categories/CategoryController.cs`, `Features/Products/.../ProductController` (step2 + inline değer-create endpoint).

---

## Phase 0 — Normalize Çekirdeği (additive, build yeşil kalır)

### Task 1: AttributeValueNormalizer (TR-kanonik)

**Files:**
- Create: `Application/Entegrasyon.Business/Helpers/AttributeValueNormalizer.cs`
- Test: `Test/Entegrasyon.Test/Business/AttributeValueNormalizerTests.cs`

- [ ] **Step 1: Failing test yaz**

`Test/Entegrasyon.Test/Business/AttributeValueNormalizerTests.cs`:
```csharp
using Entegrasyon.Business.Helpers;
using Xunit;

namespace Entegrasyon.Test.Business;

public class AttributeValueNormalizerTests
{
    [Theory]
    [InlineData("Sarı", "SARI")]
    [InlineData("sarı", "SARI")]
    [InlineData("SARI", "SARI")]
    [InlineData("  sarı  ", "SARI")]
    [InlineData("açık   sarı", "AÇIK SARI")]   // ic bosluk teke
    [InlineData("iğne", "İĞNE")]                // i -> İ (TR)
    [InlineData("ısı", "ISI")]                   // ı -> I (TR)
    public void Normalize_ProducesCanonicalKey(string raw, string expected)
    {
        Assert.Equal(expected, AttributeValueNormalizer.Normalize(raw));
    }

    [Fact]
    public void Normalize_DifferentWords_StayDifferent()
    {
        Assert.NotEqual(
            AttributeValueNormalizer.Normalize("Açık Sarı"),
            AttributeValueNormalizer.Normalize("Sarı"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrWhitespace_ReturnsEmpty(string? raw)
    {
        Assert.Equal(string.Empty, AttributeValueNormalizer.Normalize(raw));
    }
}
```

- [ ] **Step 2: Testi koş, FAIL doğrula**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AttributeValueNormalizerTests"`
Expected: FAIL — `AttributeValueNormalizer` tipi yok (derlenmez).

- [ ] **Step 3: Minimal implementasyon**

`Application/Entegrasyon.Business/Helpers/AttributeValueNormalizer.cs`:
```csharp
using System.Globalization;

namespace Entegrasyon.Business.Helpers;

/// <summary>
/// Category attribute degerlerini kanonik anahtara cevirir: trim + ic bosluk teke +
/// Turkce-duyarli buyuk harf katlama. "Sarı"/"sarı"/"SARI"/" sarı " -> "SARI".
/// Bu anahtar tekillestirmede (dedup) ve unique index'te kullanilir.
/// </summary>
public static class AttributeValueNormalizer
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var collapsed = string.Join(' ',
            raw.Split(' ', '\t', '\n', '\r', '\f', '\v',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return collapsed.ToUpper(Tr);
    }
}
```

- [ ] **Step 4: Testi koş, PASS doğrula**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AttributeValueNormalizerTests"`
Expected: PASS (tüm theory satırları).

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Helpers/AttributeValueNormalizer.cs Test/Entegrasyon.Test/Business/AttributeValueNormalizerTests.cs
git commit -m "feat(attributes): TR-kanonik AttributeValueNormalizer + unit testler"
```

---

## Phase 1 — Veri Modeli (NormalizedName ekle, additive)

### Task 2: CategoryAttributeValue.NormalizedName + EntityConfiguration

**Files:**
- Modify: `Application/Entegrasyon.Entity/Categories/CategoryAttributeValue.cs`
- Create/Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/CategoryAttributeValueEntityConfiguration.cs`

> **Not:** Unique index bu task'ta MODELE eklenir ama migration Task 5'te (backfill + dedup'tan SONRA) üretilir — mevcut seed değerlerinin `NormalizedName`'i önce doldurulmalı yoksa unique index NULL/çakışan değerlerde patlar.

- [ ] **Step 1: Entity'ye alan ekle**

`CategoryAttributeValue.cs`:
```csharp
namespace Entegrasyon.Entity.Categories;

public class CategoryAttributeValue : BaseEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string NormalizedName { get; set; } = string.Empty;
    public int CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; } = null!;
}
```

- [ ] **Step 2: EntityConfiguration ekle/güncelle**

Önce var mı bak: `ls Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/ | grep -i CategoryAttributeValue` (MarketPlaceMatch hariç saf `CategoryAttributeValueEntityConfiguration` yoksa oluştur).

`CategoryAttributeValueEntityConfiguration.cs`:
```csharp
using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class CategoryAttributeValueEntityConfiguration : IEntityTypeConfiguration<CategoryAttributeValue>
{
    public void Configure(EntityTypeBuilder<CategoryAttributeValue> builder)
    {
        builder.Property(x => x.NormalizedName)
            .HasMaxLength(256)
            .IsRequired();

        // Aktif (silinmemis) degerlerde attribute basina kanonik tekillik.
        builder.HasIndex(x => new { x.CategoryAttributeId, x.NormalizedName })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
```

> Eğer bu config sınıfı zaten varsa: yalnızca `NormalizedName` property + unique index satırlarını ekle. Config'in `ApplyConfigurationsFromAssembly` ile otomatik kaydedildiğini doğrula (IntegrationDbContext `OnModelCreating`'e bak); değilse elle `modelBuilder.ApplyConfiguration(new CategoryAttributeValueEntityConfiguration())` ekle.

- [ ] **Step 3: Build (additive, yeşil olmalı)**

Run: `dotnet build Entegrasyon.sln`
Expected: PASS. (Migration henüz yok; model snapshot drift'i Task 5'te giderilecek.)

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Entity/Categories/CategoryAttributeValue.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/CategoryAttributeValueEntityConfiguration.cs
git commit -m "feat(attributes): CategoryAttributeValue.NormalizedName + filtreli unique index (model)"
```

---

### Task 3: GetOrCreate terfi motoru (CategoryAttributeValueManager)

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/ICategoryAttributeValueManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryAttributeValueManager.cs`
- Test: `Test/Entegrasyon.IntegrationTest/Business/CategoryAttributeValueDedupIntegrationTests.cs`

> GetOrCreate gerçek DB davranışı (unique index + persist) gerektirir → **integration testi** (unit değil). Bu yüzden Task 2 migration'ı Task 5'te uygulanmadan bu testin GREEN'i çalışmaz; testi şimdi RED yaz, Task 5 sonrası GREEN'i doğrula. (Sıra: Task 3 kod + RED → Task 5 migration → Task 3 testi GREEN.)

- [ ] **Step 1: Failing integration test yaz**

`CategoryAttributeValueDedupIntegrationTests.cs`:
```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Business;

[Collection("IntegrationTest")]
public class CategoryAttributeValueDedupIntegrationTests
{
    private readonly IntegrationTestFactory _factory;
    public CategoryAttributeValueDedupIntegrationTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task GetOrCreate_SameCanonical_ReturnsSameId_NoDuplicate()
    {
        using var scope = _factory.Services.CreateScope();
        var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var manager = scope.ServiceProvider.GetRequiredService<ICategoryAttributeValueManager>();

        int attrId;
        await using (var ctx = await ctxFactory.CreateDbContextAsync())
        {
            var attr = new CategoryAttribute { CategoryAttributeKey = "renk", CategoryAttributeHumanized = "Renk" };
            ctx.CategoryAttributes.Add(attr);
            await ctx.SaveChangesAsync();
            attrId = attr.Id;
        }

        var id1 = await manager.GetOrCreate(attrId, "Sarı");
        var id2 = await manager.GetOrCreate(attrId, "  SARI ");   // ayni kanonik
        var id3 = await manager.GetOrCreate(attrId, "Kırmızı");   // farkli

        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, id3);

        await using var verify = await ctxFactory.CreateDbContextAsync();
        var count = await verify.CategoryAttributeValues
            .CountAsync(v => v.CategoryAttributeId == attrId && !v.IsDeleted);
        Assert.Equal(2, count);   // Sarı + Kırmızı (SARI dup degil)
    }
}
```

> **Not:** Test sınıf iskeleti (`IntegrationTestFactory`, `[Collection("IntegrationTest")]`) için mevcut bir integration testine bak (örn. `Test/Entegrasyon.IntegrationTest/Business/CategoryAttributeCategoryRemoveIntegrationTests.cs`) ve aynı fixture/collection adını kullan.

- [ ] **Step 2: Interface'e GetOrCreate ekle**

`ICategoryAttributeValueManager.cs`:
```csharp
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryAttributeValueManager
{
    Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeId(int id);
    Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeIds(IEnumerable<int> categoryAttributeIds);
    Task<int> GetOrCreate(int categoryAttributeId, string rawName);
}
```

- [ ] **Step 3: GetOrCreate implementasyonu (persist + race-safe)**

`CategoryAttributeValueManager.cs`'e ekle (`using Entegrasyon.Business.Helpers;` + `using Microsoft.EntityFrameworkCore;` mevcut):
```csharp
    public async Task<int> GetOrCreate(int categoryAttributeId, string rawName)
    {
        var normalized = AttributeValueNormalizer.Normalize(rawName);
        if (normalized.Length == 0)
            throw new ArgumentException("Değer boş olamaz.", nameof(rawName));

        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var existing = await dbContext.CategoryAttributeValues
            .Where(v => v.CategoryAttributeId == categoryAttributeId && v.NormalizedName == normalized && !v.IsDeleted)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();
        if (existing.HasValue)
            return existing.Value;

        var entity = new CategoryAttributeValue
        {
            CategoryAttributeId = categoryAttributeId,
            Name = rawName.Trim(),
            NormalizedName = normalized
        };
        dbContext.CategoryAttributeValues.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync();
            return entity.Id;
        }
        catch (DbUpdateException)
        {
            // Race: baska istek ayni kanonik degeri ekledi -> unique index ihlali. Mevcut id'yi don.
            await using var retry = await _contextFactory.CreateDbContextAsync();
            return await retry.CategoryAttributeValues
                .Where(v => v.CategoryAttributeId == categoryAttributeId && v.NormalizedName == normalized && !v.IsDeleted)
                .Select(v => v.Id)
                .FirstAsync();
        }
    }
```

- [ ] **Step 4: Build yeşil**

Run: `dotnet build Entegrasyon.sln`
Expected: PASS.

- [ ] **Step 5: Commit (test GREEN doğrulaması Task 5 sonrası)**

```bash
git add Application/Entegrasyon.Business/Abstract/ICategoryAttributeValueManager.cs Application/Entegrasyon.Business/Concrete/CategoryAttributeValueManager.cs Test/Entegrasyon.IntegrationTest/Business/CategoryAttributeValueDedupIntegrationTests.cs
git commit -m "feat(attributes): GetOrCreate terfi motoru (normalize+dedup, race-safe) + RED integration test"
```

---

## Phase 2 — AllowCustom Söküm (atomik, build kırılır→düzelir)

### Task 4: AllowCustom'ı entity + tüketicilerden çıkar

> Bu task atomik: `CategoryAttribute.AllowCustom` silinince tüm okuyucular derlenmez. Hepsi tek commit'te düzeltilir. Migration Task 5'te.

**Files (modify):**
- `Application/Entegrasyon.Entity/Categories/CategoryAttribute.cs`
- `Application/Entegrasyon.Entity/Templates/TemplateCategoryAttributeData.cs`
- `Application/Entegrasyon.Entity/Dtos/Category/AddCategoryAttributeDto.cs`, `EditCategoryAttributeDto.cs`, `CategoryAttributeDto.cs`
- `Application/Entegrasyon.Business/Mappers/CategoryAttributeMapper.cs`
- `Application/Entegrasyon.Business/Validation/FluentValidation/AddCategoryAttributeDtoValidator.cs`, `EditCategoryAttributeDtoValidator.cs`
- `Application/Entegrasyon.Business/Concrete/CategoryAttributeManager.cs` (89, 125), `CategoryManager.cs` (445), `CategoryAttributeCategoryManager.cs` (172, 182), `MatchedEntityImportManager.cs` (423, 634)
- İmporter'lar: `TrendyolCategoryImporter.cs` (185), `TrendyolCategoryImporterService.cs` (197), `CiceksepetiCategoryImporter.cs` (236 + yorumlar), `HepsiburadaCategoryImporter.cs` (243), `N11CategoryImporter.cs` (316), `N11RestCategoryImporter.cs` (186), `PazaramaCategoryImporter.cs` (237)
- `Application/Entegrasyon.Business/Concrete/TrendyolCategoryAttributeProvider.cs` (40), `MarketplaceAttributeDto` kullanan yerler
- MVC: `Features/Attributes/AttributeController.cs` (53, 85), `EditAttributeVm.cs` (8), `Features/Categories/CategoryController.cs` (126, 144, 248, 305), `CategoryCreateVm.cs` (21), `Features/Attributes/Views/Index.cshtml`, `Partials/_AttributeDetail.cshtml`

- [ ] **Step 1: RED — AllowCustom referansı kalmadığını doğrulayan grep testi (manuel kanıt)**

Bu mekanik söküm; "test" burada **derleyici**. Önce mevcut testleri çalıştırıp yeşil tabanı kaydet:
Run: `dotnet build Entegrasyon.sln` → PASS (söküm öncesi).

- [ ] **Step 2: Entity + DTO'lardan AllowCustom sil**

`CategoryAttribute.cs`: `public bool AllowCustom { get; set; }` satırını sil.
`TemplateCategoryAttributeData.cs`: AllowCustom satırını sil.
`AddCategoryAttributeDto.cs` / `EditCategoryAttributeDto.cs` / `CategoryAttributeDto.cs`: AllowCustom alanını record/sınıftan çıkar (constructor parametresi ise çağıranları da düzelt).

- [ ] **Step 3: Mapper + validator temizle**

`CategoryAttributeMapper.cs`: `AllowCustom = dto.AllowCustom` satırını sil.
`AddCategoryAttributeDtoValidator.cs` / `EditCategoryAttributeDtoValidator.cs`: `RuleFor(x => x.AllowCustom)...` bloklarını ve `.When(x => x.AllowCustom == ...)` koşullarını sil. (Koşul, "AllowCustom false ise değer zorunlu" gibi bir kuralı sarıyorsa: artık her zaman değer-listesi modeli olduğundan kuralı koşulsuz yap veya gereksizse kaldır — mevcut kuralın amacını oku, körlemesine silme.)

- [ ] **Step 4: Business + importer tüketicileri temizle**

Her dosyada `AllowCustom` atama/okuma satırını sil. İmporter'larda pazaryeri DTO'sundan map eden satırlar (`AllowCustom = attr.AllowCustom` / `= false` / `= attr.IsCustomValue` / `= attr.Type == "string"`) **tamamen kalkar** (artık entity'de alan yok). `AttributeAutoMatchService.cs:128` JSON serialize `allowCustom = a.AllowCustom` → `a` pazaryeri DTO'su ise (entity değil) **kalabilir**; entity ise kaldır. Dosyayı açıp `a`'nın tipini doğrula.

- [ ] **Step 5: MVC controller + VM + view temizle**

`AttributeController.cs`, `CategoryController.cs`: AllowCustom okuyan/atayan satırları sil.
`EditAttributeVm.cs`, `CategoryCreateVm.cs`: `AllowCustom` property sil.
`Features/Attributes/Views/Index.cshtml`, `_AttributeDetail.cshtml`: `@if (attr.AllowCustom)` / AllowCustom kolon-badge bloklarını sil (Tabler tablo kolonu ise başlığıyla birlikte).

- [ ] **Step 6: Build yeşil**

Run: `dotnet build Entegrasyon.sln`
Expected: PASS. Kalan referans varsa derleyici gösterir; her birini gider.
Doğrulama: `grep -rn "AllowCustom" Application --include="*.cs" --include="*.cshtml" | grep -vE "/obj/|/bin/|/Migrations/|MasterCatalog|MasterAttribute"` → master-catalog dışı çıktı **boş** olmalı (master kendi modelinde tutabilir).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(attributes): CategoryAttribute.AllowCustom + tüm tüketicilerini sök"
```

---

## Phase 3 — CustomValue Söküm (atomik, build kırılır→düzelir)

### Task 5: CustomValue'yu iki entity + ~30 tüketiciden çıkar, AttributeValueId zorunlu

> En geniş task. `AttributeKeyValue.CustomValue` ve `ProductVariantAttribute.CustomValue` silinince variant/sale/label/discount/stock yolları derlenmez. Her `X ?? CustomValue` → `X`. Tek commit.

**Files (modify):**
- `Application/Entegrasyon.Entity/Categories/AttributeKeyValue.cs` — `CustomValue` sil, `AttributeValueId`'yi `int` (non-null) yap.
- `Application/Entegrasyon.Entity/Products/ProductVariantAttribute.cs` — `CustomValue` sil.
- `Application/Entegrasyon.Entity/Dtos/Attributes/AttributeKeyValueDto.cs` — `CustomValue` parametresini sil.
- `Application/Entegrasyon.Business/Abstract/IVariantNamingService.cs` — `VariantAttributeLite.CustomValue` sil (record alanı + tüm `new VariantAttributeLite(...)` çağrıları).
- `Application/Entegrasyon.Business/Concrete/VariantNamingService.cs` (13)
- `ProductVariantManager.cs` (60, 68, 97, 105, 147, 350, 374-375), `ProductManager.cs` (159, 168, 261, 267, 359, 364, 384, 534, 555, 611, 639, 864, 883, 932, 951)
- `SaleManager.cs` (181), `LabelManager.cs` (222), `DiscountManager.cs` (39-40), `MarketplaceOverrideManager.cs` (54), `OfficeStockManager.cs` (325, 335), `StockTransferRequestManager.cs` (365, 374), `BranchOfficeManager.cs` (340, 349, 388, 398)
- `AttributeKeyValueManager.cs` — `ClearEmptyAttributes` CustomValue dalını çıkar.

- [ ] **Step 1: Yeşil taban**

Run: `dotnet build Entegrasyon.sln` → PASS.

- [ ] **Step 2: AttributeKeyValue + ProductVariantAttribute entity'leri**

`AttributeKeyValue.cs`:
```csharp
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Categories;

public sealed class AttributeKeyValue : BaseEntity
{
    public int CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; } = null!;
    public int AttributeValueId { get; set; }
    public CategoryAttributeValue AttributeValue { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
```

`ProductVariantAttribute.cs`:
```csharp
namespace Entegrasyon.Entity.Products;

public sealed class ProductVariantAttribute
{
    public int? CategoryAttributeValueId { get; set; }
    public string? CategoryAttributeValue { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
}
```

- [ ] **Step 3: VariantAttributeLite + IVariantNamingService**

`IVariantNamingService.cs` record:
```csharp
public readonly record struct VariantAttributeLite(
    string? Value,
    bool IsVarianter,
    bool IsSlicer,
    int Order);
```
Tüm `new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)` çağrılarından `a.CustomValue` parametresini çıkar → `new VariantAttributeLite(a.CategoryAttributeValue, a.IsVarianter, a.IsSlicer, i)`. (ProductVariantManager, SaleManager, OfficeStockManager, StockTransferRequestManager, BranchOfficeManager, ProductManager.)

`VariantNamingService.cs:13`: `.Select(a => a.CategoryAttributeValue ?? a.CustomValue ?? string.Empty)` → `.Select(a => a.Value ?? string.Empty)` (record alan adı `Value`).

- [ ] **Step 4: Geri kalan `?? CustomValue` okumalarını sadeleştir**

Her noktada CustomValue fallback'ini kaldır:
- `DiscountManager.cs:39-40`: `a.CategoryAttributeValue ?? a.CustomValue` → `a.CategoryAttributeValue`; `Where(... !string.IsNullOrEmpty(a.CategoryAttributeValue ?? a.CustomValue))` → `... !string.IsNullOrEmpty(a.CategoryAttributeValue)`.
- `LabelManager.cs:222`: `!string.IsNullOrWhiteSpace(a.CustomValue) ? a.CustomValue : a.CategoryAttributeValue` → `a.CategoryAttributeValue`.
- `MarketplaceOverrideManager.cs:54`: `a.CategoryAttributeValue ?? a.CustomValue ?? "?"` → `a.CategoryAttributeValue ?? "?"`.
- `ProductVariantManager.cs:147`: `a.CustomValue ?? ""` → `a.Value ?? ""` (VariantAttributeLite kullanımı; alan adı `Value`).
- `ProductVariantManager.cs:374-375`: `a.CategoryAttributeValue ?? a.CustomValue ?? ""` → `a.Value ?? ""`.
- `ProductManager.cs`:
  - `:159` `VariantAttributeDto(... pva.CustomValue ?? "" ...)` → CustomValue argümanını çıkar; `VariantAttributeDto` tanımında CustomValue alanı varsa onu da kaldır ve diğer çağrılarını düzelt (tipi aç, alanı sök).
  - `:168` `akv.CustomValue ?? string.Empty` → bu projeksiyon AttributeKeyValueDto kuruyorsa CustomValue parametresini komple çıkar.
  - `:261` `Where(a => a.AttributeValueId.HasValue || !string.IsNullOrWhiteSpace(a.CustomValue))` → `Where(a => a.AttributeValueId > 0)` (artık int non-null; `> 0` geçerli değer demek).
  - `:267` `CustomValue = akv.CustomValue` → bu atamayı sil (entity'de alan yok).
  - `:364` `kv.AttributeValueId.HasValue ? kv.AttributeValue!.Name! : kv.CustomValue ?? ""` → `kv.AttributeValue?.Name ?? ""` (AttributeValueId artık daima dolu, AttributeValue include'lu).
  - `:864`, `:883`, `:932`, `:951` `a.CustomValue ?? a.CategoryAttributeValue ?? ""` / `akv.CustomValue ?? ""` → `a.CategoryAttributeValue ?? ""` / `akv.AttributeValue?.Name ?? ""` (bağlama göre; satırı aç, `akv` mı `a` mı, entity mi lite mi belirle).
- `OfficeStockManager.cs`, `StockTransferRequestManager.cs`, `BranchOfficeManager.cs`: yalnızca `new VariantAttributeLite(...)` çağrılarından CustomValue parametresi (Step 3 kapsamında); ek `?? CustomValue` yoksa dokunma.

> **DİKKAT:** `AttributeValueId` artık `int` (non-null). `.HasValue` / `!.Value` / `== 0` kullanan TÜM noktalar derleme hatası verir. Derleyiciyi takip et: `akv.AttributeValueId.HasValue` → `akv.AttributeValueId > 0`; `akv.AttributeValueId!.Value` → `akv.AttributeValueId`. (TrendyolProductMapper Task 6'da ayrı ele alınır — şimdilik orada `CustomValue` ve `.HasValue` derleme hatası kalabilir; Task 6 onu kapatır. İstersen Task 5+6'yı tek build-yeşil bloğunda birleştir.)

- [ ] **Step 5: ClearEmptyAttributes sadeleştir**

`AttributeKeyValueManager.cs`:
```csharp
    public void ClearEmptyAttributes(Product product)
    {
        product.AttributeKeyValues = product.AttributeKeyValues
            .Where(x => x.AttributeValueId > 0)
            .ToList();
    }
```

- [ ] **Step 6: AttributeKeyValueDto**

`AttributeKeyValueDto.cs`: `string CustomValue` parametresini record'dan çıkar; tüm `new AttributeKeyValueDto(...)` çağrılarını (ProductManager projeksiyonları) güncelle.

- [ ] **Step 7: Build — Task 6 ile birlikte yeşil**

Run: `dotnet build Entegrasyon.sln`
Expected: TrendyolProductMapper hariç temiz; Task 6 tamamlanınca tam yeşil. (Pratik: Task 5 ve Task 6'yı ardışık yap, build-yeşil'i Task 6 sonunda doğrula, tek commit ya da iki commit.)

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(attributes): CustomValue'yu iki entity + ~30 tüketiciden sök, AttributeValueId zorunlu"
```

---

### Task 6: Trendyol send mapper — CustomValue dalını değer-adı string'ine çevir

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductMapper.cs` (31, 71-77, 138-189)

- [ ] **Step 1: Failing integration test yaz**

`Test/Entegrasyon.IntegrationTest/Features/MarketplaceSync/` altındaki mevcut `ProductSendIntegrationTests.cs` desenine uygun yeni test: eşleşmesi OLMAYAN bir attribute değeri → gönderim payload'ında `CustomAttributeValue == value.Name` (string) basılıyor; eşleşmesi OLAN → `AttributeValueId` dolu, `CustomAttributeValue == null`.
```csharp
[Fact]
public async Task Map_UnmatchedValue_SendsValueNameAsCustomAttribute()
{
    // Arrange: bir AttributeKeyValue, CategoryAttributeMarketPlaceMatch VAR ama
    // CategoryAttributeValueMarketPlaceMatch YOK; value.Name = "Limon Sarısı".
    // Act: mapper.MapAsync(product...)
    // Assert: ilgili TrendyolProductAttribute.AttributeValueId == null
    //         && CustomAttributeValue == "Limon Sarısı"
}
```
> Test gövdesini mevcut `ProductSendIntegrationTests` seed/fixture yardımcılarını kullanarak doldur (aynı dosyadaki helper'lara bak: product + variant + match seed). Mapper'ın public giriş metod adını (`MapAsync`/`Map`) `ITrendyolProductMapper`'dan oku.

- [ ] **Step 2: Testi koş, FAIL doğrula**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~Map_UnmatchedValue"`
Expected: FAIL (şu an eşleşmesiz değer `continue` ile atlanıyor → attribute hiç eklenmiyor).

- [ ] **Step 3: Value-name sözlüğü ekle + akv dalını güncelle**

`TrendyolProductMapper.cs` — value id→name lookup ekle (mevcut `valueMatches` bloğunun yanına, satır ~77 sonrası):
```csharp
var valueNames = await dbContext.CategoryAttributeValues.AsNoTracking()
    .Where(v => valueIds.Contains(v.Id))
    .ToDictionaryAsync(v => v.Id, v => v.Name);
```
Ürün seviyesi döngü (138-160) yeni hali:
```csharp
foreach (var akv in product.AttributeKeyValues)
{
    if (!attributeMatches.TryGetValue(akv.CategoryAttributeId, out var trendyolAttrId))
        continue;

    int? trendyolValueId = null;
    string? customValue = null;

    if (valueMatches.TryGetValue(akv.AttributeValueId, out var mappedValueId))
    {
        trendyolValueId = mappedValueId;
    }
    else if (valueNames.TryGetValue(akv.AttributeValueId, out var name) && !string.IsNullOrEmpty(name))
    {
        customValue = name;   // eslesme yok -> deger adini string bas (Trendyol allowCustom=false ise API reddeder; A karari)
    }
    else
    {
        continue;
    }

    attributes.Add(new TrendyolProductAttribute(trendyolAttrId, trendyolValueId, customValue));
}
```

- [ ] **Step 4: Varyant seviyesi dalını güncelle (165-188)**

`pva.CustomValue` artık yok; eşleşme yoksa snapshot string `pva.CategoryAttributeValue`'yu customAttributeValue bas:
```csharp
foreach (var pva in variant.ProductVariantAttributes)
{
    if (!pva.CategoryAttributeValueId.HasValue) continue;

    var attrValue = await dbContext.CategoryAttributeValues.AsNoTracking()
        .FirstOrDefaultAsync(v => v.Id == pva.CategoryAttributeValueId.Value);
    if (attrValue is null || !attributeMatches.TryGetValue(attrValue.CategoryAttributeId, out var tAttrId))
        continue;

    if (valueMatches.TryGetValue(pva.CategoryAttributeValueId.Value, out var trendyolValueId))
    {
        attributes.Add(new TrendyolProductAttribute(tAttrId, trendyolValueId, null));
    }
    else if (!string.IsNullOrEmpty(pva.CategoryAttributeValue))
    {
        attributes.Add(new TrendyolProductAttribute(tAttrId, null, pva.CategoryAttributeValue));
    }
}
```

- [ ] **Step 5: Build + test GREEN**

Run: `dotnet build Entegrasyon.sln` → PASS.
Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~Map_UnmatchedValue"` → PASS (migration uygulandıktan sonra; Task 7 ile sıralı).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(trendyol-send): eşleşmesiz attribute değerini değer-adı string olarak bas (CustomValue sökümü)"
```

---

## Phase 4 — Migration (backfill + dedup + drop + unique index)

### Task 7: Tek migration üret, gözden geçir, uygula

**Files:**
- Create: `Application/Entegrasyon.DataAccess/.../Migrations/<timestamp>_CategoryAttributeControlledVocabulary.cs`

- [ ] **Step 1: Migration üret**

Run:
```bash
dotnet ef migrations add CategoryAttributeControlledVocabulary -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```
Beklenen otomatik içerik: `DropColumn AllowCustom` (CategoryAttributes, TemplateCategoryAttributeData), `DropColumn CustomValue` (AttributeKeyValues, ProductVariantAttributes), `AddColumn NormalizedName` (CategoryAttributeValues), `AlterColumn AttributeValueId` non-null, `CreateIndex` filtreli unique.

- [ ] **Step 2: Migration'ı elle düzenle — backfill + dedup sırası**

Üretilen `Up(MigrationBuilder)` gövdesini şu sıraya getir (EF sırayı yanlış üretebilir → unique index NULL/çakışmada patlar):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1) AllowCustom + CustomValue kolonlarini dusur (EF urettigi DropColumn'lar burada kalsin)
    migrationBuilder.DropColumn(name: "AllowCustom", table: "CategoryAttributes");
    migrationBuilder.DropColumn(name: "AllowCustom", table: "TemplateCategoryAttributeData");
    migrationBuilder.DropColumn(name: "CustomValue", table: "AttributeKeyValues");
    migrationBuilder.DropColumn(name: "CustomValue", table: "ProductVariantAttributes");

    // 2) NormalizedName kolonu (once nullable/defaultsuz ekle ki backfill yapilabilsin)
    migrationBuilder.AddColumn<string>(
        name: "NormalizedName", table: "CategoryAttributeValues",
        type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: "");

    // 3) Backfill: mevcut Name -> kanonik (PostgreSQL: TR-duyarli upper yok; uygulama-eslestirmesi
    //    icin upper(trim) yeterli temel — tam TR katlama gerekiyorsa asagidaki not).
    migrationBuilder.Sql(@"
        UPDATE ""CategoryAttributeValues""
        SET ""NormalizedName"" = upper(regexp_replace(btrim(""Name""), '\s+', ' ', 'g'))
        WHERE ""Name"" IS NOT NULL;");

    // 4) Dedup: ayni (CategoryAttributeId, NormalizedName) icin en kucuk Id'yi koru,
    //    digerlerini soft-delete et (referans butunlugu icin fiziksel silme YOK).
    migrationBuilder.Sql(@"
        WITH ranked AS (
            SELECT ""Id"",
                   row_number() OVER (PARTITION BY ""CategoryAttributeId"", ""NormalizedName""
                                      ORDER BY ""Id"") AS rn
            FROM ""CategoryAttributeValues""
            WHERE ""IsDeleted"" = false
        )
        UPDATE ""CategoryAttributeValues"" v
        SET ""IsDeleted"" = true, ""DeletedAt"" = now() AT TIME ZONE 'UTC'
        FROM ranked r
        WHERE v.""Id"" = r.""Id"" AND r.rn > 1;");

    // 5) AttributeValueId non-null
    migrationBuilder.AlterColumn<int>(
        name: "AttributeValueId", table: "AttributeKeyValues",
        type: "integer", nullable: false, defaultValue: 0,
        oldClrType: typeof(int), oldType: "integer", oldNullable: true);

    // 6) Filtreli unique index (backfill+dedup'tan SONRA)
    migrationBuilder.CreateIndex(
        name: "IX_CategoryAttributeValues_CategoryAttributeId_NormalizedName",
        table: "CategoryAttributeValues",
        columns: new[] { "CategoryAttributeId", "NormalizedName" },
        unique: true,
        filter: "\"IsDeleted\" = false");
}
```
> **TR katlama notu:** PostgreSQL `upper()` `i→I` yapar (`İ` değil), uygulamadaki `ToUpper(tr-TR)` `i→İ` yapar. Bu fark, backfill ile uygulama-üretimi kanonik anahtarların UYUŞMAMASINA yol açar → dedup'tan sonra uygulama yeni "İ"li kanonikle eski "I"lı satırı eşleştiremez. **Çözüm:** backfill'i SQL yerine C# ile yap (migration içinde değil, `Up` sonrası bir data-seed/one-off servis veya migration'da raw değerleri çekip `AttributeValueNormalizer.Normalize` ile güncelleyen kod). **Karar:** Bu projede seed `CategoryAttributeValue` sayısı yönetilebilirse, migration'da raw SQL yerine `IntegrationDbContext` üzerinden C# backfill tercih et (Task 7b). Eğer canlı veri yoksa ve dev DB master'dan yeniden seed edilecekse, en temizi: **dev DB'yi master'dan taze seed et, backfillّi seed sırasında `Normalize` ile yap** — o zaman migration sadece şema (drop/add/index), dedup gerekmez. Hangi yolun geçerli olduğunu uygulamadan önce kullanıcıyla netleştir.

- [ ] **Step 2b: KARAR VERİLDİ — taze seed (i) + apply-güvenli migration**

Kullanıcı kararı (2026-06-12): **dev DB verisi önemsiz, tekrar seed'lenebilir → taze seed (i).** Doğruluk re-seed'den gelir (`NormalizedName = AttributeValueNormalizer.Normalize(Name)`).
**ANCAK** mevcut dev DB'de `NormalizedName=NULL` satırlar var; NOT NULL kolon `defaultValue=""` ile eklenince attribute başına tüm değerler `""` paylaşır → filtreli unique index apply'da **çakışır**. Bu yüzden Step 2'deki SQL blok 3 (backfill `upper(trim)`) + blok 4 (dedup) **KALSIN** — sadece migration'ın mevcut satırlarda çökmeden uygulanması için. TR `upper()` uyuşmazlığı **önemsiz**, çünkü re-seed `Normalize` ile üzerine yazacak. Yani: SQL blokları apply-güvenliği için tutulur, kanonik doğruluk re-seed'le sağlanır.

- [ ] **Step 3: Uygula**

Run: `dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`
Expected: hatasız.

- [ ] **Step 4: Snapshot drift doğrula**

Run: `dotnet ef migrations has-pending-model-changes -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`
Expected: "No changes" / pending yok.

- [ ] **Step 5: Phase 1-3 integration testlerini koş (artık GREEN)**

Docker tüneli aç (üstteki container politikası), sonra:
Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~CategoryAttributeValueDedup|FullyQualifiedName~Map_UnmatchedValue"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(db): CategoryAttribute kontrollü kelime dağarcığı migration (drop AllowCustom/CustomValue, NormalizedName + unique index)"
```

---

## Phase 5 — MVC: Sihirbaz + Değer Yönetim Sayfası

### Task 8: Ürün sihirbazı step2 — her zaman select + Tom Select inline ekleme

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep2Attributes.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Products/ViewModels/CreateProductVm.cs` (AllowCustom ×2 + CustomValue temizliği — Task 4/5'te entity tarafı bitti, VM tarafı burada)
- Modify: `Features/Products/.../ProductController` (step2 POST: ValueId işle; CustomValue dalı yok) + yeni inline değer-create endpoint
- Modify: `Application/Entegrasyon.MVC/Features/Products/Views/Partials/_CreateStep3Variants.cshtml` (varyant attribute AllowCustom dalı)

- [ ] **Step 1: CreateProductVm temizle**

`CreateProductVm.cs`: attribute alt-VM'lerinden `public bool AllowCustom` (×2) ve `public string? CustomValue` alanlarını sil. POST binding sadece `CategoryAttributeId`, `AttributeName`, `IsRequired`, `ValueId` tutsun.

- [ ] **Step 2: _CreateStep2Attributes.cshtml — tek dal (select)**

`AllowCustom` hidden input (47) ve `@if (attr.AllowCustom) { text input } else { select }` (49-73) bloğunu **tek select**'e indir:
```cshtml
<input type="hidden" name="CategoryAttributes[@i].CategoryAttributeId" value="@attr.Id" />
<input type="hidden" name="CategoryAttributes[@i].AttributeName" value="@attr.CategoriyAttributeHumanized" />
<input type="hidden" name="CategoryAttributes[@i].IsRequired" value="@attr.IsRequired" />

<select name="CategoryAttributes[@i].ValueId"
        class="form-select js-attr-value-select"
        data-attribute-id="@attr.Id"
        data-tom-create="true"
        @(attr.IsRequired ? "required" : "")>
    <option value="">Seçin...</option>
    @foreach (var val in attr.CategoryAttributeValues)
    {
        <option value="@val.Id" selected="@(saved?.ValueId == val.Id)">@val.Name</option>
    }
</select>
```

- [ ] **Step 3: Tom Select "create" davranışı (inline ekleme, marka tarzı)**

Sayfa script'ine (step2 partial sonu veya wizard JS) Tom Select init ekle; yeni değer yazılınca `attribute/values/create` POST'la id al, option'a yerleştir:
```html
<script>
document.querySelectorAll('.js-attr-value-select[data-tom-create]').forEach(function (el) {
    new TomSelect(el, {
        create: function (input, callback) {
            fetch('/attributes/values/create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ categoryAttributeId: parseInt(el.dataset.attributeId), name: input })
            })
            .then(r => r.ok ? r.json() : Promise.reject())
            .then(d => callback({ value: d.id, text: d.name }))
            .catch(() => callback(false));
        },
        createOnBlur: true,
        persist: false
    });
});
</script>
```
> Tom Select projede `wwwroot/lib`'de mevcut (CLAUDE.md). Wizard partial HTMX ile yükleniyorsa script'in swap sonrası çalıştığını doğrula (`htmx:afterSwap` içinde init veya inline `<script>` swap'ta çalışır).

- [ ] **Step 4: Inline değer-create endpoint**

ProductController veya AttributeController'a (HTMX/JSON):
```csharp
[HttpPost("/attributes/values/create")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateValue([FromBody] CreateAttributeValueRequest req)
{
    if (req is null || req.CategoryAttributeId <= 0 || string.IsNullOrWhiteSpace(req.Name))
        return BadRequest();
    var id = await categoryAttributeValueManager.GetOrCreate(req.CategoryAttributeId, req.Name);
    var name = req.Name.Trim();
    return Json(new { id, name });
}

public sealed record CreateAttributeValueRequest(int CategoryAttributeId, string Name);
```
> Yetki: bu endpoint ürün ekleyen kullanıcıya açık olmalı; mevcut ürün-create yetkisiyle aynı policy/role. Controller'ın authorize attribute'unu kontrol et. AntiForgery JSON POST için header gönderiliyor (Step 3).

- [ ] **Step 5: _CreateStep3Variants.cshtml AllowCustom dalı**

Varyant attribute girişinde de `AllowCustom` koşulu varsa (Task 4 grep'inde çıktı) aynı şekilde tek-select'e indir veya varyanter değerleri zaten select ise yalnız AllowCustom referansını sök. Dosyayı aç, `AllowCustom`/`CustomValue` referansını kaldır.

- [ ] **Step 6: Build + manuel doğrulama**

Run: `dotnet build Entegrasyon.sln` → PASS.
Run: `grep -rn "AllowCustom\|CustomValue" Application/Entegrasyon.MVC --include="*.cshtml" --include="*.cs" | grep -vE "/obj/|/bin/"` → boş.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(product-wizard): attribute girişi tek select + Tom Select inline değer ekleme"
```

---

### Task 9: Attribute Değer Yönetim Sayfası

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Attributes/Views/Values.cshtml`
- Create: `Application/Entegrasyon.MVC/Features/Attributes/Views/Partials/_AttributeValueTable.cshtml`
- Modify: `Features/Attributes/AttributeController.cs` (Values GET + add/edit/delete actions)
- Modify: `ICategoryAttributeValueManager` + `CategoryAttributeValueManager` (Update + SoftDelete)

- [ ] **Step 1: Failing integration test — değer düzenle persist (no-tracking footgun)**

`CategoryAttributeValueDedupIntegrationTests.cs`'e ekle:
```csharp
[Fact]
public async Task UpdateName_Persists_AndRecomputesNormalized()
{
    using var scope = _factory.Services.CreateScope();
    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
    var manager = scope.ServiceProvider.GetRequiredService<ICategoryAttributeValueManager>();

    int attrId, valId;
    await using (var ctx = await ctxFactory.CreateDbContextAsync())
    {
        var attr = new CategoryAttribute { CategoryAttributeKey = "renk", CategoryAttributeHumanized = "Renk" };
        ctx.CategoryAttributes.Add(attr);
        await ctx.SaveChangesAsync();
        attrId = attr.Id;
    }
    valId = await manager.GetOrCreate(attrId, "Sari");

    await manager.UpdateName(valId, "Sarı");

    await using var verify = await ctxFactory.CreateDbContextAsync();
    var v = await verify.CategoryAttributeValues.FirstAsync(x => x.Id == valId);
    Assert.Equal("Sarı", v.Name);
    Assert.Equal("SARI", v.NormalizedName);
}
```

- [ ] **Step 2: Testi koş, FAIL**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~UpdateName_Persists"`
Expected: FAIL — `UpdateName` yok.

- [ ] **Step 3: Manager Update + SoftDelete (AsTracking ŞART)**

`ICategoryAttributeValueManager.cs`:
```csharp
    Task UpdateName(int id, string newName);
    Task SoftDelete(int id);
```
`CategoryAttributeValueManager.cs`:
```csharp
    public async Task UpdateName(int id, string newName)
    {
        var normalized = AttributeValueNormalizer.Normalize(newName);
        if (normalized.Length == 0)
            throw new ArgumentException("Değer boş olamaz.", nameof(newName));

        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var entity = await dbContext.CategoryAttributeValues
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted)
            ?? throw new InvalidOperationException("Değer bulunamadı.");

        entity.Name = newName.Trim();
        entity.NormalizedName = normalized;
        await dbContext.SaveChangesAsync();
    }

    public async Task SoftDelete(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var entity = await dbContext.CategoryAttributeValues
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }
```

- [ ] **Step 4: Test GREEN**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~UpdateName_Persists"`
Expected: PASS.

- [ ] **Step 5: Controller actions + view**

`AttributeController.cs` (PRG + HTMX deseni, mevcut feature-folder stiline uy):
```csharp
[HttpGet("/attributes/{attributeId:int}/values")]
public async Task<IActionResult> Values(int attributeId)
{
    var values = await categoryAttributeValueManager.GetValuesByCategoryAttributeId(attributeId);
    var vm = new AttributeValuesVm(attributeId, values.Where(v => !v.IsDeleted)
        .Select(v => new AttributeValueRow(v.Id, v.Name ?? "")).ToList());
    return Request.IsHtmx() ? PartialView("Partials/_AttributeValueTable", vm) : View(vm);
}

[HttpPost("/attributes/{attributeId:int}/values")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddValue(int attributeId, string name)
{
    await categoryAttributeValueManager.GetOrCreate(attributeId, name);
    TempData.SetSuccess("Değer eklendi.");
    return RedirectToAction(nameof(Values), new { attributeId });
}

[HttpPost("/attributes/values/{id:int}/edit")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditValue(int id, int attributeId, string name)
{
    await categoryAttributeValueManager.UpdateName(id, name);
    TempData.SetSuccess("Değer güncellendi.");
    return RedirectToAction(nameof(Values), new { attributeId });
}

[HttpPost("/attributes/values/{id:int}/delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteValue(int id, int attributeId)
{
    await categoryAttributeValueManager.SoftDelete(id);
    TempData.SetSuccess("Değer silindi.");
    return RedirectToAction(nameof(Values), new { attributeId });
}
```
VM tipleri (aynı dosyada veya ViewModels altında):
```csharp
public sealed record AttributeValuesVm(int AttributeId, List<AttributeValueRow> Values);
public sealed record AttributeValueRow(int Id, string Name);
```
`Values.cshtml` + `_AttributeValueTable.cshtml`: Tabler datagrid/table — kolon başlıkları "Değer", "İşlem"; satırda düzenle (modal/inline) + sil butonu; üstte "Yeni Değer" formu. **Tabler strict-rule:** kullanılan her bileşen (`table`, `btn`, `badge`, `modal`) class kombinasyonu `https://tabler.io/docs/ui/<component>`'tan doğrula. Referans: `Features/Reports/Views/*` + `_StockAlertTable.cshtml` tasarım dili (KPI/empty-state/tablo partial).

- [ ] **Step 6: Build + commit**

Run: `dotnet build Entegrasyon.sln` → PASS.
```bash
git add -A
git commit -m "feat(attributes): değer yönetim sayfası (liste/ekle/düzenle/soft-delete)"
```

---

## Phase 6 — Tam Regresyon + E2E + Review

### Task 10: Tüm test paketleri + E2E + review gate

- [ ] **Step 1: Unit**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: tümü PASS (normalize, mevcut suite).

- [ ] **Step 2: Integration (Docker tüneli açık)**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: tümü PASS — özellikle variant naming / sale / label / stock-transfer / product-send yolları (CustomValue sökümü regresyon yüzeyi).

- [ ] **Step 3: E2E — ürün ekle akışı**

`Test/Entegrasyon.E2E/` altında yeni senaryo (mevcut ürün-ekleme E2E desenine uy): ürün ekle → step2'de attribute değeri **seç** → listede olmayan değeri **inline yaz/ekle** → kaydet → ürün detayında değer görünür → ikinci üründe aynı attribute'ta yeni değer listede hazır.
Run: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --filter "FullyQualifiedName~AttributeValue"`
Expected: PASS (uygulama debug ayakta).

- [ ] **Step 4: Bağımsız review gate**

Sırayla çağır:
- `ecc:csharp-reviewer` — tüm C# değişikliği (entity, manager, mapper, controller).
- `ecc:database-reviewer` — migration + filtreli unique index + dedup SQL + AttributeValueId non-null.
- `ecc:security-reviewer` — `/attributes/values/create` ve değer-yönetim endpoint'leri (kullanıcı girdisi, yetki, antiforgery, mutasyon).
Bulguları gider, testleri tekrar koş.

- [ ] **Step 5: graphify güncelle + final commit**

Run: `graphify update .`
```bash
git add -A
git commit -m "test(attributes): kontrollü kelime dağarcığı tam regresyon + E2E"
```

---

## Self-Review Notları (plan yazarından)

- **Spec kapsamı:** AllowCustom söküm (Task 4), CustomValue söküm (Task 5), NormalizedName+unique (Task 2,7), GetOrCreate dedup (Task 3), Trendyol send (Task 6), MVC sihirbaz+yönetim (Task 8,9), test (Task 1,3,6,9,10) — spec'in tüm bölümleri karşılandı.
- **Açık karar (Task 7 Step 2b):** backfill stratejisi — taze seed (i) mi yerinde backfill (ii) mi → uygulamadan önce kullanıcıyla netleş. TR `upper()` uyuşmazlığı riski bu yüzden işaretli.
- **Atomik build pencereleri:** Task 4 (AllowCustom) ve Task 5+6 (CustomValue) tek seferde derlenmeyi bozar; ara build-yeşil ancak tüm tüketiciler düzeltilince. Bu kabul edildi (entity property silmenin doğası).
- **EF Mutasyon Persist:** GetOrCreate/UpdateName/SoftDelete `.AsTracking()` veya Add ile persist; her birine integration testi (Task 3, 9).
