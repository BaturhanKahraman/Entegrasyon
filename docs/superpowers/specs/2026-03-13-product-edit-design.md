# Product Edit — Design Spec
**Date:** 2026-03-13
**Status:** Approved

---

## Overview

Mevcut ürünleri düzenlemek için `/products/edit/{Id:guid}` rotasında tek sayfalık bir edit deneyimi. Sekme tabanlı layout (MudTabs), tek Kaydet butonu. Stok yönetimi ve pazaryeri senkronizasyonu bu sayfanın sorumluluğunda değil — ayrı sayfalar üstlenir.

---

## Kapsam: Düzenlenebilir vs. Düzenlenemez

### Düzenlenebilir
| Alan | Yer | Not |
|---|---|---|
| Title, Description, StockCode, Season, Year | Genel sekmesi | |
| BrandId | Genel sekmesi | |
| CategoryId | Genel sekmesi | Değişince uyarı + attribute temizleme |
| ListPrice, SalePrice, CostPrice, ECommercePrice | Varyantlar sekmesi | |
| DimensionalWeight, VatRate, CurrencyType | Varyantlar sekmesi | |
| Görseller (ekleme, soft-delete, cover seçimi) | Görseller sekmesi | |
| AttributeKeyValues | Özellikler sekmesi | Kategori değişince sıfırlanır |

### Düzenlenemez (Read-only)
| Alan | Neden |
|---|---|
| Barcode | Kritik tanımlayıcı; satış geçmişine bağlı. UI'da read-only MudChip + tooltip. |
| Varyant yapısı (IsVarianter/IsSlicer kombinasyonları) | Satışları bozar; Varyantlar sekmesinde sadece görüntülenir. |
| Stok miktarları | Ayrı Stok Yönetimi sayfasının sorumluluğunda. |
| Pazaryeri sync ayarları | Ayrı Marketplace Sync sayfasının sorumluluğunda. |

---

## Ayrı Sayfalara Bırakılan Sorumluluklar

- **Stok Yönetimi:** `BranchOfficeStock` düzenleme/ayarlama. Edit sayfasında stok read-only gösterilir, yanında "Stok Yönetimi →" link butonu.
- **Pazaryeri Senkronizasyonu:** Ürün kaydedildikten sonra marketplace'e gönderme manuel olarak Sync sayfasından yapılır.

---

## Entity & DTO Katmanı

### Mevcut DTO'larda eksik alanlar
- `ProductEditDetailDto` → `Season` ve `Year` eklenmeli (Product entity'sinde mevcut, DTO'da yok).
- `ProductVariantEditDetailDto` → `ECommercePrice` eklenmeli (ProductVariant entity'sinde mevcut, DTO'da yok).

### Mevcut DTO'larda yapılacak breaking değişiklikler

`ProductEditDetailDto` ve `ProductVariantEditDetailDto` pozisyonel record — yeni alan eklemek breaking change. Her iki DTO'nun LINQ projection'ı (`ProductManager.GetProductEditDetailById` satır 141–163) **aynı anda** güncellenmelidir:

```csharp
// ProductEditDetailDto → Season ve Year eklenir
public sealed record ProductEditDetailDto(
    Guid Id, string Title, string Description, string StockCode,
    string Season, string Year,   // ← YENİ
    int BrandId, int CategoryId,
    List<ProductVariantEditDetailDto> ProductVariants,
    List<AttributeKeyValueDto> AttributeKeyValues
);

// ProductVariantEditDetailDto → ECommercePrice eklenir
public record ProductVariantEditDetailDto(
    Guid Id, decimal DimensionalWeight, string CurrencyType, string Barcode,
    decimal ListPrice, decimal SalePrice, decimal CostPrice,
    decimal ECommercePrice,   // ← YENİ
    decimal VatRate,
    List<EditBranchOfficeStockDto> BranchOfficeStocks,
    List<EditableImageDto> UploadedImages,
    List<VariantAttributeDto> VariantAttributes
);
```

Mapster profilleri (`MappingConfig.cs`) bu alanları da kapsayacak şekilde güncellenmeli.

### Yeni DTOlar

**`EditProductDto`** (write model — kaydetme komutu):
```csharp
public sealed record EditProductDto(
    Guid Id,
    string Title,
    string Description,
    string StockCode,
    string Season,
    string Year,
    int BrandId,
    int CategoryId,
    List<EditProductVariantDto> Variants,
    List<AttributeKeyValueDto> AttributeKeyValues,
    List<int> DeletedImageIds   // soft-delete edilecek görsel Id listesi
);
```

`ProductEditDetailDto` read model olarak kalır. `UpdateProduct` artık `EditProductDto` alır.

**`EditProductVariantDto`** (write model — variant güncelleme, yapısal değişiklik yok):
```csharp
public sealed record EditProductVariantDto(
    Guid Id,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal DimensionalWeight,
    decimal VatRate,
    string CurrencyType
    // Images buraya dahil DEĞİL — DeletedImageIds üst EditProductDto'da
);
```

> **Neden `Images` ayrı?** `EditableImageDto` içinde `IsDeleted` flag'ini taşıyıp buraya koymak, kaydet sırasında iki ayrı `SaveChangesAsync` çağrısına yol açar (image silme + ürün güncelleme). İkincisi başarısız olursa ürün güncellendi ama görseller silinmedi inconsistency'si oluşur. `DeletedImageIds` flat listesi her şeyi tek `SaveChangesAsync` ile çözüyor.

**`ProductEditPageDto`** (tek sorguda tüm sayfa verisi — CategoryEditPageDto pattern):
```csharp
public sealed record ProductEditPageDto(
    ProductEditDetailDto Product,
    List<BrandListDetailDto> Brands,
    List<CategorySelectDto> LeafCategories,   // minimal: sadece Id + Name
    List<BranchSelectDto> BranchOffices,      // minimal: sadece Id + Name
    MarketplaceSyncStatusDto SyncStatus
);
```

**`CategorySelectDto`** (yeni minimal DTO — kategori dropdown'ı için):
```csharp
public sealed record CategorySelectDto(int Id, string Name);
```

> `CategoryDetailDto` full fetch (attribute count, product count vs.) dropdown için aşırı. `AddProduct` gibi minimal tutuluyor.

**`BranchSelectDto`** (yeni minimal DTO — şube gösterimi için):
```csharp
public sealed record BranchSelectDto(int Id, string Name);
```

> `BranchListDetailDto` `UserCount` ve `CreatedAt` taşıyor — edit sayfası için gereksiz. `CategorySelectDto` ile tutarlı pattern.

**`MarketplaceSyncStatusDto`**:
```csharp
public sealed record MarketplaceSyncStatusDto(
    MarketplaceSyncState State,
    DateTimeOffset? LastSyncedAt,
    string? BatchRequestId,
    string? StatusMessage
);

public enum MarketplaceSyncState
{
    NeverSynced,   // ProductMarketplace kaydı yok
    Waiting,       // Status=Pending, BatchRequestId=null
    Processing,    // Status=Pending, BatchRequestId!=null  →  "Gönderildi — İşlemde"
    OutOfSync,     // Status=Published, UpdatedAt > LastSyncedAt
    Synced,        // Status=Published, UpdatedAt <= LastSyncedAt
    Failed,
    Rejected
}
```

---

## Business Layer

### `IProductService` değişiklikleri

**Eklenen:**
```csharp
Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id);
Task<IResult> UpdateProduct(EditProductDto dto);
```

**Kaldırılan:**
```csharp
// Task<IDataResult<ProductEditDetailDto>> GetProductEditDetailById(Guid id);  ← KALDIRILIR
// Task<IResult> UpdateProduct(ProductEditDetailDto dto);                        ← İMZA DEĞİŞİR
```

`GetProductEditDetailById` kaldırılır — `GetProductEditPageData` onun yerini alır. Bu metodu çağıran başka consumer yoksa (test dosyaları dahil grep ile doğrulanmalı) silinebilir; varsa NRE patched version korunur.

> **NRE patch zorunlu:** `GetProductEditDetailById` interface'den kaldırılsa bile `ProductManager.cs` satır ~159'daki şu ifade **her durumda** düzeltilmeli:
> ```csharp
> // ÖNCE (crash riski):
> akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId).IsRequired
> // SONRA:
> akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId)?.IsRequired ?? false
> ```
> Bu projection `GetProductEditPageData`'ya taşınırken doğrudan düzeltilir.

### `ProductManager.GetProductEditPageData`

Not-found durumunda `ErrorDataResult` döner ("Ürün bulunamadı") — `SuccessDataResult(null)` anti-pattern kullanılmaz.

> **Dikkat:** Mevcut `GetProductEditDetailById` içindeki bu LINQ parçası NRE riski taşır:
> ```csharp
> akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId).IsRequired
> ```
> Junction row yoksa null deref olur. `GetProductEditPageData` implementasyonunda bu projection düzeltilmeli: `?.IsRequired ?? false` kullanılmalı.

### `ProductManager.UpdateProduct` — 3 adımlı pipeline

**1. Validation** (`EditProductValidator` — FluentValidation, ayrı yazılır, `AddProductValidator`'dan türetilmez):
- Title boş olamaz
- BrandId > 0, CategoryId > 0
- Her variant için ListPrice >= 0, SalePrice >= 0, CostPrice >= 0

> **DI Kaydı:** `ApplicationDependencyExtension.cs` içindeki `AddValidators()` metoduna şu satır eklenmeli:
> ```csharp
> services.AddScoped<IValidator<EditProductDto>, EditProductValidator>();
> ```
> Eksik kalırsa `IFluentValidator.ValidateAndThrowAsync<EditProductDto>` çağrısı runtime'da "Validator not found!" ile patlar.

**2. Business Rules** (LogicRunner):
- StockCode benzersizliği (kendi Id hariç)

**3. Execution** — EF Core change tracking ile (NoTracking default'u aşılır):

```csharp
// 1. Tracked load — AsTracking() zorunlu (context globally no-tracking)
var product = await _dbContext.MainProducts
    .AsTracking()
    .Include(p => p.ProductVariants)
    .Include(p => p.AttributeKeyValues)
    .Include(p => p.ProductVariants).ThenInclude(pv => pv.Images)
    .FirstOrDefaultAsync(p => p.Id == dto.Id);

if (product is null) return new ErrorResult("Ürün bulunamadı.");

// 2. Scalar alanlar
product.Title = dto.Title;
product.Description = dto.Description;
product.StockCode = dto.StockCode;
product.Season = dto.Season;
product.Year = dto.Year;
product.BrandId = dto.BrandId;

// 3. UpdatedAt garantisi — OutOfSync detection için Product entity'si
//    her zaman modified sayılmalı (sadece child değişse bile)
//    BaseEntity interceptor bunu zaten halleder, ama explicit olarak dokunulması önerilir.

// 4. Kategori değişikliği
bool categoryChanged = product.CategoryId != dto.CategoryId;
if (categoryChanged)
{
    product.CategoryId = dto.CategoryId;
    _dbContext.AttributeKeyValues.RemoveRange(product.AttributeKeyValues);
    product.AttributeKeyValues.Clear();
}

// 5. AttributeKeyValues — delete all, re-add (composite PK: CategoryAttributeId + ProductId)
if (!categoryChanged)
    _dbContext.AttributeKeyValues.RemoveRange(product.AttributeKeyValues);
foreach (var akv in dto.AttributeKeyValues)
    product.AttributeKeyValues.Add(/* map */);

// 6. Variant scalar güncellemesi (ekle/silme yok)
foreach (var variantDto in dto.Variants)
{
    var variant = product.ProductVariants.First(pv => pv.Id == variantDto.Id);
    variant.ListPrice = variantDto.ListPrice;
    // ... diğer scalar alanlar
}

// 7. Image soft-delete
foreach (var imageId in dto.DeletedImageIds)
{
    var image = product.ProductVariants.SelectMany(pv => pv.Images)
                       .FirstOrDefault(img => img.Id == imageId);
    if (image is not null) { image.IsDeleted = true; image.DeletedAt = DateTimeOffset.UtcNow; }
}

// 8. Yeni görseller Blazor katmanında IImageManager.AddProductImages ile eklenir
//    (ayrı çağrı — yeni IBrowserFile'lar server'a DTO üzerinden geçirilemez)

await _dbContext.SaveChangesAsync();

// 9. Event
_productUpdatedChannel.TryPublish(new ProductUpdatedEvent(product.Id, product.Title, categoryChanged));
```

### `ProductUpdatedEvent` (yeni channel event)
```csharp
public class ProductUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public bool CategoryChanged { get; set; }

    public ProductUpdatedEvent() { }
    public ProductUpdatedEvent(Guid productId, string productTitle, bool categoryChanged)
    {
        ProductId = productId;
        ProductTitle = productTitle;
        CategoryChanged = categoryChanged;
    }
}
```

`EventChannel<ProductUpdatedEvent>` `ApplicationDependencyExtension.cs`'e kaydedilmeli.

Marketplace sync sayfası bu event'i dinleyerek "güncelleme gerekiyor" durumunu anlayabilir.

---

## Marketplace Sync State Matrisi

| State | Koşul | UI Etiketi | MudChip Renk |
|---|---|---|---|
| NeverSynced | ProductMarketplace kaydı yok | Senkronize Edilmedi | Default (Gri) |
| Waiting | Status=Pending, BatchRequestId=null | Bekliyor | Warning (Sarı) |
| Processing | Status=Pending, BatchRequestId!=null | Gönderildi — İşlemde | Info (Mavi) |
| OutOfSync | Status=Published, UpdatedAt > LastSyncedAt | Güncelleme Gerekiyor | Warning (Turuncu) |
| Synced | Status=Published, UpdatedAt <= LastSyncedAt | Yayında | Success (Yeşil) |
| Failed | Status=Failed | Başarısız | Error (Kırmızı) |
| Rejected | Status=Rejected | Reddedildi | Error (Koyu Kırmızı) |

Dirty flag için yeni sütuna gerek yok — `Product.UpdatedAt > ProductMarketplace.LastSyncedAt` karşılaştırması yeterli.

> **Önemli:** `UpdatedAt` yalnızca EF tarafından `EntityState.Modified` olan entity'lerde güncellenir. Sadece child değişikliklerinde (örn. sadece AttributeKeyValues) parent `Product` entity'si modified sayılmayabilir. Bu durumda `OutOfSync` detection yanlış çalışır. Execution adımında `Product` entity'sine mutlaka dokunulmalı (scalar field ataması veya explicit `_dbContext.Entry(product).State = EntityState.Modified`).

---

## Blazor UI Katmanı

### Route
`/products/edit/{Id:guid}` — `Products.razor.cs`'teki `EditProduct` zaten bu rotaya yönlendiriyor, route tanımı eklenmesi yeterli.

### Dosya yapısı
```
Features/Products/
  ProductEdit.razor
  ProductEdit.razor.cs
  ProductEditGeneralTab.razor
  ProductEditGeneralTab.razor.cs
  ProductEditVariantsTab.razor
  ProductEditVariantsTab.razor.cs
  ProductEditImagesTab.razor
  ProductEditImagesTab.razor.cs
  ProductEditAttributesTab.razor
  ProductEditAttributesTab.razor.cs
```

`ImageUploadDialog` mevcut — yeniden kullanılır, kopyalanmaz.

### `ProductEdit` — ana bileşen sorumlulukları
- `OnInitializedAsync`: `GetProductEditPageData(Id)` — tek sorguda tüm veriler
- Loading state; `ErrorDataResult` → Snackbar + `/products` yönlendirme
- `MudTabs`: Genel / Varyantlar / Görseller / Özellikler
- Header: ürün başlığı + `MarketplaceSyncState` badge (`MudChip`)
- Alt bileşenlere `@bind` ile form state geçirir
- Altta tek "Kaydet" + "İptal" butonu
- Kaydet akışı:
  1. `UpdateProduct(editDto)` — scalar + attribute + image soft-delete tek `SaveChangesAsync`
  2. Yeni görsel varsa `IImageManager.AddProductImages` çağrısı

### Sekme sorumlulukları

**Genel sekmesi** (`ProductEditGeneralTab`):
- Title, Description, StockCode, Season, Year (MudTextField)
- Brand: MudSelect — değiştirilebilir
- Category: MudSelect — değişince `ShowMessageBox` uyarı ("Kategori değiştirilirse mevcut özellikler temizlenecek. Devam?"), onaylanırsa AttributeKeyValues sıfırlanır; yeni kategorinin attribute listesi `ICategoryAttributeManager.GetCategoryAttributesByCategory` ile Blazor katmanında çekilir (business layer değil)

**Varyantlar sekmesi** (`ProductEditVariantsTab`):
- Her varyant için MudExpansionPanel, başlık = varyant etiketi (renk/beden/custom)
- Barcode: read-only MudChip + tooltip "Barkod düzenlenemez"
- Düzenlenebilir: ListPrice, SalePrice, CostPrice, ECommercePrice, DimensionalWeight, VatRate, CurrencyType
- ECommercePrice ve SalePrice ayrı etiketlerle gösterilmeli (e-ticaret fiyatı ≠ mağaza satış fiyatı)
- Stok: read-only gösterim + "Stok Yönetimi →" link butonu

**Görseller sekmesi** (`ProductEditImagesTab`):
- Mevcut görseller: `EditableImageDto` listesi preview grid olarak gösterilir
- Silme: UI'da `IsDeleted` toggle — kaydet anında `DeletedImageIds` listesine eklenir
- Cover seçimi: radio toggle — kaydet anında ilgili variant'ın `Images` listesinde `IsCoverImage` güncellenir
- `EditableImageDto` pozisyonel record (immutable) — UI state için wrapper mutable class kullanılmalı (`ImageEditState { int Id, string Src, bool IsDeleted, bool IsCoverImage }`)
- "Görsel Ekle" → `ImageUploadDialog` (mevcut bileşen, yeni IBrowserFile'lar ayrı tutulur)

**Özellikler sekmesi** (`ProductEditAttributesTab`):
- Kategori değişmediğinde: mevcut AttributeKeyValues MudSelect/MudTextField ile
- Kategori değişince (GeneralTab'dan event): sıfırlanır, yeni kategorinin attribute listesi yüklenir
- AddProduct'taki `_regularAttributes` + `_regularAttrValueIds` + `_regularAttrCustomValues` mantığı buraya taşınır

---

## Gözden Kaçabilecek Diğer Noktalar

1. **`SearchVector` güncellemesi:** PostgreSQL full-text search vektörü DB trigger'ı ile otomatik güncellenir — EF Core'da yapılacak ekstra şey yok. Migration'da trigger varlığı doğrulanmalı.
2. **EF Core tracked load zorunluluğu:** Context globally NoTracking — `AsTracking()` olmadan `Update()` çağrısı `SearchVector` (generated column) dahil tüm alanları overwrite eder. Her zaman tracked load + scalar assignment pattern'ı kullanılmalı.
3. **`AttributeKeyValue` composite PK:** `CategoryAttributeId + ProductId` — delete/re-add stratejisi için `RemoveRange` tracked collection üzerinde çalışmalı; list.Clear() yeterli değil. Ayrıca `HasQueryFilter(!IsDeleted)` mevcut olduğundan tracked load zaten sadece silinmemiş AKV'leri getirir — bu beklenen davranış.
4. **`ProductUpdatedEvent` DI kaydı:** `EventChannel<ProductUpdatedEvent>` singleton olarak `ApplicationDependencyExtension.cs`'e (`ChannelExtensions.cs` üzerinden) eklenmeli.
5. **`EditProductValidator` DI kaydı:** `AddValidators()` extension'ına eklenmeli; eksik kalırsa runtime exception.
6. **Yeni görsel akışı:** Edit sayfasında yeni eklenen görseller `IBrowserFile` olarak Blazor state'inde tutulur; kaydet sırasında `IImageManager.AddProductImages(product.Id, imageUploads)` ile MinIO'ya yüklenir — aynı AddProduct akışı.
7. **`ECommercePrice` vs `SalePrice`:** İki farklı kavram. UI'da net etiketler: "E-Ticaret Fiyatı" vs "Satış Fiyatı".
8. **`ImageEditState` wrapper:** `EditableImageDto` immutable record — UI'da `IsDeleted` / cover toggle için mutable wrapper class gerekli, doğrudan record property'si atanamaz.
9. **`IsMain` vs `IsCoverImage`:** `Image` entity'sinde `IsCoverImage` `[Obsolete]` olarak işaretlenmiş, aktif alan `IsMain`. `EditableImageDto` şu an `IsCoverImage`'ı taşıyor ancak yazma işlemlerinde `img.IsMain` kullanılmalı. Gerekirse DTO'da alan adı düzeltilebilir.
10. **`ProductManager` primary constructor migrasyonu:** Codebase'deki diğer tüm manager'lar C# 12 primary constructor kullanıyor (`CLAUDE.md`). `ProductManager` eski explicit constructor pattern'ını koruyor. `EventChannel<ProductUpdatedEvent>` enjeksiyonu eklenirken primary constructor'a geçiş yapılabilir.
