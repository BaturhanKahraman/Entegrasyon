# Variant İsimlendirme — Tasarım Dokümanı

**Tarih:** 2026-04-19
**Durum:** Design (onaylandı, implementation bekleniyor)
**Kapsam:** ProductVariant için otomatik, deterministik isim üretimi ve gösterim katmanına yansıtılması.

---

## 1. Amaç ve Motivasyon

Şu anda `ProductVariant` entity'sinin `Name` alanı yoktur. Varyant gösterimi her sayfada farklı pattern'lerle run-time'da hesaplanıyor:

- `ProductManager.cs:417, 473` → `AttributeSummary = string.Join(" / ", ...)`
- `ProductController.cs:1129` → `VariantName = attrLabel ?? Barcode ?? "Varyant"`
- `POSController.cs:202` → `VariantAttributeSummary = variantSummary?.Trim() ?? ""`
- `POS/_POSSearchResults.cshtml:118` → label fallback = `AttributeSummary || Barcode`

Bu dağınıklığın pratik sonuçları:

1. **Tutarsızlık:** Aynı varyant POS'ta `"Sarı / XL"`, ürün detayda hiç gözükmüyor (sadece barkod), Pricing'de `VariantName` farklı.
2. **Boşluk:** `Products/Views/Variants.cshtml` tablosu varyant özelliklerini hiç göstermiyor — sadece barkod + fiyat. Kullanıcı "hangi varyant?" sorusunu cevaplayamıyor.
3. **Eksiklik:** Returns, Sales, Orders, Picking, Reports, Stock Movements, Branch Offices gibi 10+ sayfa varyantı sadece barkodla gösteriyor.

**Hedef:** Varyant oluşturulurken slicer ve varianter özelliklerine göre deterministik bir isim (`"Sarı XL"`, `"Kırmızı M"`) üret ve bu ismi varyant gösteren tüm sayfalarda tutarlı göster. Slicer/varianter yoksa ürün başlığına düş.

---

## 2. Temel Tasarım Kararları

| Karar | Seçim | Gerekçe |
|---|---|---|
| Saklama stratejisi | `ProductVariant.Name` — `string?` (nullable), DB'ye materialize | Query/projection basit kalır, 10+ view aynı alanı okur. Nullable olduğu için migration backfill gerekmez. |
| Format | `"{Varianter values} {Slicer values}"` — boşluk ayırıcı | Kullanıcının örneği (`"Sarı XL"`) ile birebir uyum. |
| Attribute sırası | `IsVarianter==true` önce, sonra `IsSlicer==true`; her grupta insertion order | Varianter görsel ayrımı belirler, slicer boyut/beden — doğal okuma sırası. |
| Fallback (render) | `Name ?? ComputeFromAttributes ?? ProductTitle` | Stale, null ve eksik attribute edge case'leri her zaman bir değer döner. |
| Write trigger | Her `Add`/`Update` variant akışında explicit `Compute` çağrısı | Domain event abstraction'ı YAGNI; explicit çağrı okunabilir ve test edilebilir. |
| Attribute değişiminde davranış | Otomatik recompute (`UpdateVariant` Name'i yeniden set eder) | Tutarlılık. Override ihtiyacı ortaya çıkarsa ileride `NameOverride` alanı eklenir. |
| Display stratejisi | Eager resolve: Business katmanı DTO'ya `DisplayName` alanını dolu gönderir | View tarafında unutulma riski yok, attribute array'i taşımaya gerek yok. |
| Gösterim kapsamı | Tüm 13 varyant gösteren view tek seferde | Tutarlılık tek atımda gelir. |
| Migration stratejisi | Sadece nullable kolon ekle, backfill yok | Fallback runtime'da hallediyor; zero-risk deploy. |
| Marketplace sync payload'ları | Kapsam dışı | Her marketplace'in title kuralları farklı, ayrı iş. |

---

## 3. Veri Modeli

### 3.1 Entity değişikliği

Dosya: `Application/Entegrasyon.Entity/Products/ProductVariant.cs`

```csharp
public sealed class ProductVariant : BaseEntity
{
    // ... mevcut alanlar
    public string? Name { get; set; }   // YENİ, max 256
}
```

### 3.2 Configuration

Dosya: `Application/Entegrasyon.DataAccess/.../Configurations/ProductVariantConfiguration.cs`

```csharp
builder.Property(v => v.Name).HasMaxLength(256).IsRequired(false);
```

### 3.3 Migration

```bash
dotnet ef migrations add AddProductVariantName \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

- Kolon: `product_variants.name varchar(256) NULL`
- **Backfill yok** — nullable olduğu için mevcut satırlar `NULL` kalır, render fallback devreye girer.
- **Index yok** — şu an arama `Title`+`Barcode` üzerinden yapılıyor; Name üzerinde arama gerekirse ileride eklenir.

---

## 4. İş Katmanı — VariantNamingService

### 4.1 Arayüz

Dosya: `Application/Entegrasyon.Business/Abstract/IVariantNamingService.cs`

```csharp
public interface IVariantNamingService
{
    // Yazma akışı: variant ve product üzerinden Name hesaplar.
    // Hiçbir varianter/slicer değer yoksa product.Title döner; attribute tamamen boşsa null döner (DB'ye null yazılır, fallback render eder).
    string? Compute(ProductVariant variant, Product product);
}

public readonly record struct VariantAttributeLite(
    string? Value,
    string? CustomValue,
    bool IsVarianter,
    bool IsSlicer,
    int Order);
```

**Not:** Okuma (display) akışı `VariantNameExtensions.ResolveDisplayName` static metoduna delegate edilir (Bölüm 6.1). İki ayrı API tutup aynı logic'i dublike etmiyoruz — servis sadece yazma akışında kullanılıyor, read path'lerde (projection + in-memory map) static extension yeterli.
```

### 4.2 Compute akışı

1. `variant.ProductVariantAttributes`'ı al.
2. `IsVarianter == true` olanları `Order` ASC (list insertion order) dizer.
3. Sonra `IsSlicer == true` olanları aynı sırayla ekler.
4. Her attribute için değer: `CategoryAttributeValue ?? CustomValue ?? ""`.
5. Boş olanları atla.
6. `string.Join(" ", values).Trim()`.
7. Boşsa → `product.Title`.
8. `product.Title` da boşsa (teoride olamaz) → `null`.

### 4.3 Resolve akışı (extension'da)

Fiili implementasyon `VariantNameExtensions.ResolveDisplayName` içinde — Bölüm 6.1. Pseudokod:

```csharp
if (!string.IsNullOrWhiteSpace(storedName)) return storedName;
var computed = ComputeFromAttributesInMemory(attributes);
if (!string.IsNullOrWhiteSpace(computed)) return computed;
return productTitle;
```

### 4.4 Edge case matrisi

| Durum | Sonuç |
|---|---|
| Varianter yok, slicer yok, attribute yok | `product.Title` |
| Varianter yok, slicer="XL" | `"XL"` |
| Varianter="Sarı", slicer="XL" | `"Sarı XL"` |
| Varianter=["Sarı","Çiçekli"], slicer=["XL","Regular"] | `"Sarı Çiçekli XL Regular"` |
| Attribute'ta CategoryAttributeValue=null, CustomValue="Lacivert" | `"Lacivert"` |
| Attribute değer alanları boş string | o attribute atlanır |
| Attribute koleksiyonu hiç yüklenmemiş (projection unutulmuş) | fallback zinciri → `product.Title` |

### 4.5 DI kayıt

`Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` → `AddApplicationDependencies()` içinde:

```csharp
services.AddScoped<IVariantNamingService, VariantNamingService>();
```

---

## 5. Write Path

### 5.1 ProductVariantManager.AddVariant

Dosya: `Application/Entegrasyon.Business/Concrete/ProductVariantManager.cs:126`

- `ProductVariantAttributes` mapping'i DTO'dan yapılır (şu an yok — bu gap doldurulacak).
- `variant.Name = namingService.Compute(variant, product);`

### 5.2 ProductVariantManager.UpdateVariant

Aynı dosya, satır 176.

- Idempotent safety olarak `variant.Name = namingService.Compute(variant, variant.Product);` çağrılır.
- Mevcut update attribute'u değiştirmese bile bu çağrı zararsız (aynı sonuç).

### 5.3 Product Wizard — toplu variant oluşturma

Dosya: `Application/Entegrasyon.MVC/Features/Products/ProductController.cs` — wizard finalize akışında (step6 Publish veya ProductManager'ın bir action'ı).

```csharp
foreach (var v in createdVariants)
    v.Name = namingService.Compute(v, product);
```

### 5.4 Marketplace import akışları

- `MatchedEntityImportManager`, `ProductMatchingService`, marketplace-specific importer'lar — variant DB'ye yazıldığı noktada `Compute` çağrılır.
- `IsVarianter`/`IsSlicer` bayrakları doğru set edilememişse Compute boş sonuç üretir ve `product.Title` fallback'e düşer; beklenen davranış.

### 5.5 Merkezi SaveChanges override alternatifi (reddedildi)

`DbContext.SaveChangesAsync` override'ında `ChangeTracker`'dan variant'ları bulup doldurmak:

- ❌ Attribute ve Product.Title o anda yüklü olmayabilir → ek query
- ❌ Transaction süresini uzatır
- ❌ Test edilmesi zor

Explicit çağrı tercih edildi.

---

## 6. Read Path

### 6.1 Ortak extension — `VariantNameExtensions`

Dosya: `Application/Entegrasyon.Business/Extensions/VariantNameExtensions.cs`

```csharp
public static class VariantNameExtensions
{
    public static string ResolveDisplayName(
        string? storedName,
        IEnumerable<VariantAttributeLite>? attributes,
        string productTitle)
    {
        if (!string.IsNullOrWhiteSpace(storedName)) return storedName;

        if (attributes is not null)
        {
            var ordered = attributes
                .Where(a => a.IsVarianter || a.IsSlicer)
                .OrderByDescending(a => a.IsVarianter)
                .ThenBy(a => a.Order);

            var computed = string.Join(" ",
                ordered
                    .Select(a => a.Value ?? a.CustomValue ?? "")
                    .Where(v => !string.IsNullOrWhiteSpace(v)))
                .Trim();

            if (!string.IsNullOrWhiteSpace(computed)) return computed;
        }

        return productTitle;
    }
}
```

### 6.2 Ortak Razor partial — `_VariantLabel.cshtml`

Dosya: `Application/Entegrasyon.MVC/Views/Shared/_VariantLabel.cshtml`

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

VM:

```csharp
public record VariantLabelModel(string DisplayName, string? Barcode, bool ShowBarcode = false);
```

### 6.3 DTO/VM değişiklikleri

Her variant-gösteren DTO'ya `DisplayName` (dolu, non-null string) alanı eklenir. Business katmanı projection + in-memory resolve ile doldurur:

```csharp
var rows = await dbContext.ProductVariants
    .Select(v => new
    {
        v.Name,
        // Collection navigation'dan projection: EF Core sırayı garanti etmez; bu yüzden
        // indeks-bazlı Order ClientEval aşamasında set edilir (.ToListAsync() sonrası).
        RawAttributes = v.ProductVariantAttributes
            .Select(a => new
            {
                a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer,
                // Sıralama anahtarı: kalıcı bir alan yok, bu yüzden şimdilik sabit 0.
                // Plan aşamasında "entity'ye SortOrder ekle" kararı verilirse burası güncellenir.
            })
            .ToList(),
        ProductTitle = v.Product.Title,
        // ... diğer alanlar
    })
    .ToListAsync();

var result = rows.Select(r =>
{
    var attrs = r.RawAttributes
        .Select((a, idx) => new VariantAttributeLite(
            a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, idx));

    return new FooDto(
        DisplayName: VariantNameExtensions.ResolveDisplayName(r.Name, attrs, r.ProductTitle),
        // ... diğer alanlar
    );
}).ToList();
```

**Sıralama stratejisi (açık nokta):** EF Core collection projection'da kayıt sırasını garanti etmez (özellikle PostgreSQL'de). Üç olası çözüm — plan aşamasında karar:

- **A)** `ProductVariantAttribute`'a `SortOrder` kolonu ekle, projection'da `.OrderBy(a => a.SortOrder)` kullan.
- **B)** `CategoryAttributeId` alanını entity'ye ekleyip onunla sırala (zaten `CategoryAttributeCategory` tablosu yoluyla join'lenebilir olduğu için anlamlı).
- **C)** Şimdilik stable değil kabul et — `IsVarianter`/`IsSlicer` gruplaması zaten tutuyor, grup içinde sıra DB'den geldiği gibi (genelde insertion order, PostgreSQL sequential scan'de çoğu zaman insertion).

İlk implementasyonda **C** ile başlanır (düşük riskli, test edilebilir). Eğer UI'da tutarsızlık görülürse **B**'ye geçilir.

### 6.4 Değişecek DTO'lar

| DTO | Konum | Değişiklik |
|---|---|---|
| `POSProductSearchResultDto` | `Entity/Dtos/POS/POSProductSearchDto.cs` | `AttributeSummary` → `DisplayName` |
| `POSCartItem` VM | `MVC/Features/POS/ViewModels/POSTerminalVm.cs:72` | `VariantAttributeSummary` → `DisplayName` |
| `ProductVariantSaleSearchDto` | `Entity/Dtos/Product/ProductVariant/` | yeni `DisplayName` |
| `VariantDetailPageDto` | `Entity/Dtos/Product/ProductVariant/VariantDetailPageDto.cs` | yeni `DisplayName` |
| `ProductDetailDto.ProductVariantsDetails` satırı | `Entity/Dtos/Product/` | yeni `DisplayName` |
| Sales/Order line item DTO'ları | `Entity/Dtos/Sale/`, `Entity/Dtos/Order/` | yeni `DisplayName` |
| Returns item DTO | `Features/Returns/ViewModels/` | yeni `DisplayName` |
| Picking order item DTO | `Features/Picking/` | yeni `DisplayName` |
| Stock alert row / inventory row DTO | `Features/Reports/` | yeni `DisplayName` |
| Stock transfer row | `Features/Stock/Transfers/` | yeni `DisplayName` |
| Stock movement table row | `Features/StockMovements/` | yeni `DisplayName` |
| Branch office stock row | `Features/BranchOffices/` | yeni `DisplayName` |
| `VariantPricingRowDto` | `Entity/Dtos/Product/VariantPricingRowDto.cs` | Mevcut `VariantName` → Compute'a bağla |

### 6.5 Değişecek view'lar

| View | Değişiklik |
|---|---|
| `Products/Views/Variants.cshtml` | Tabloda ilk kolon `DisplayName` |
| `Products/Views/VariantDetail.cshtml` | Başlık `"ProductTitle — DisplayName"` |
| `POS/Views/Partials/_POSSearchResults.cshtml` | `AttributeSummary` → `DisplayName` |
| `POS/Views/Partials/_POSCart.cshtml` | `VariantAttributeSummary` → `DisplayName` |
| `Returns/Views/Detail.cshtml` | Variant satırına `DisplayName` |
| `Sales/Views/SaleDetail.cshtml` | Ürün adı yanına variant label |
| `Sales/Views/OrderDetail.cshtml` | Aynı |
| `Sales/Views/OrderPrint.cshtml` | Aynı |
| `Picking/Views/Partials/_OrderItems.cshtml` | Aynı |
| `Reports/Views/Inventory.cshtml` | Aynı |
| `Reports/Views/Partials/_StockAlertTable.cshtml` | Aynı |
| `Stock/Transfers/Views/Detail.cshtml` | Aynı |
| `StockMovements/Views/Partials/_MovementTable.cshtml` | Aynı |
| `BranchOffices/Views/Detail.cshtml` | Aynı |
| `BranchOffices/Views/_StockTransferDialog.cshtml` | Aynı |
| `Pricing/Views/Partials/_PriceVariants.cshtml` | `VariantName` → `DisplayName` |

---

## 7. Test Stratejisi

### 7.1 Unit testler — `VariantNamingServiceTests`

Dosya: `Test/Entegrasyon.Test/Business/VariantNamingServiceTests.cs`

- `Compute_ReturnsVarianterThenSlicer`
- `Compute_MultipleVarianters_KeepsInsertionOrder`
- `Compute_OnlySlicer_ReturnsSlicer`
- `Compute_OnlyVarianter_ReturnsVarianter`
- `Compute_NoVarianterNoSlicer_ReturnsProductTitle`
- `Compute_EmptyAttributes_ReturnsProductTitle`
- `Compute_UsesCustomValue_WhenCategoryValueNull`
- `Compute_SkipsEmptyValues`
- `Compute_MixedOrder_PutsVarianterFirst`
- `Resolve_PrefersStoredName`
- `Resolve_FallsBackToComputeWhenNameNull`
- `Resolve_FallsBackToProductTitleWhenAllEmpty`
- `Resolve_TreatsWhitespaceNameAsNull`

### 7.2 Integration testler

Dosya: `Test/Entegrasyon.IntegrationTest/Business/ProductVariantNamingIntegrationTests.cs`

- `AddVariant_PersistsComputedName` — wizard benzeri akış, DB'den okuma, Name assertion.
- `UpdateVariant_RecomputesName` — fiyat update'inde Name idempotent.
- `Migration_AllowsNullName` — eski fixture variant'lar `NULL` kalır, sorgular patlamaz.
- `RenderedVariantListUsesDisplayName` — product detay endpoint'i response'unda DisplayName gözükür.

### 7.3 E2E testler (opsiyonel, düşük öncelik)

- Wizard ile Tshirt oluştur (Renk=Sarı, Beden=XL), Variants sayfasında `"Sarı XL"` satırını gör.
- POS'ta ara, sonuçta label doğru.

### 7.4 Regresyon güvencesi

Etkilenen test suite'leri:

- `MapperConversionTests` — DTO mapping kontrol
- `ProductWizardViewModelTests` — wizard variant oluşumu
- `MatchedEntityImportIntegrationTests` — import akışı
- `ProductSendIntegrationTests`, `ProductSyncPageTests`

---

## 8. Rollout Planı

### 8.1 Önerilen commit sırası

1. **Foundation:** Migration + entity alan + `IVariantNamingService` + unit testler.
2. **Write path:** `AddVariant`/`UpdateVariant`/wizard bulk create → Compute çağrıları + integration testler.
3. **Shared display helper:** `VariantNameExtensions` + `_VariantLabel.cshtml` partial + VM.
4. **POS + Product views:** POS search/cart + Products Variants/VariantDetail view güncellemeleri.
5. **Sales/Orders/Returns:** Bu 3 feature'ın DTO + view güncellemeleri.
6. **Operational views:** Reports, Stock Movements, Stock Transfers, Branch Offices, Picking, Pricing.

Her commit sonrası: `dotnet build`, unit + integration test suite çalıştırılır.

### 8.2 Post-deploy doğrulama

- Dev DB'de migration uygula → eski variant'lar `Name = NULL` kalır.
- Ürün detay sayfasında eski variant'lar `ProductTitle` fallback ile gösterilir.
- Wizard ile yeni ürün oluştur → yeni variant'lar `"Sarı XL"` Name'e sahip.
- POS search ve cart'ta label doğru.
- `dotnet ef migrations has-pending-model-changes` temiz.

---

## 9. Risk ve Açık Noktalar

- **`ProductVariantAttribute` entity'sinde kalıcı sıralama alanı yok.** Bölüm 6.3'te anlatılan **C** seçeneği ile başlanır (DB read order = insertion order, PostgreSQL'de genelde stable); kullanıcı UI'da tutarsız sıralama görürse entity'ye `SortOrder` (A) veya `CategoryAttributeId` (B) eklenir.
- **Marketplace import'tan gelen variant'larda `IsVarianter`/`IsSlicer` bayrakları** doğru set edilemezse Compute `product.Title`'a fallback eder. Kullanıcı manuel düzeltebilir (ileride `NameOverride` gerekirse).
- **Çoklu kelimeli değerlerde boşluk ayırıcı belirsizleşebilir:** `"Koyu Mavi Ekstra Geniş"` — renk mi beden mi belirsiz. Kullanıcının örneğini takip ettik; UX'te sorun olursa slash ayırıcıya geçilebilir (tek satır değişiklik).
- **Wizard step3 `_VariantTable.cshtml` preview badge'leri** şimdilik olduğu gibi kalır — preview'de Name basmak UX improvement olarak ayrı tutulur.

---

## 10. Kapsam Dışı

- Marketplace sync payload'larında variant title (her marketplace farklı kurallarda).
- `Name` üzerinde full-text search / trigram index (şu an POS search Title+Barcode yeterli).
- Kullanıcı Name override UI'ı (şu an ihtiyaç yok; nullable alan zaten izin veriyor, UI eklemek iş ileride).
- Admin panel master catalog variant'ları için ayrı naming rule (ileride, multi-tenant fazında).
