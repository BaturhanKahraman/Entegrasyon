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
| Barcode | Kritik tanımlayıcı; satış geçmişine bağlı. UI'da read-only chip. |
| Varyant yapısı (IsVarianter/IsSlicer kombinasyonları) | Satışları bozar; Varyantlar sekmesinde sadece görüntülenir. |
| Stok miktarları | Ayrı Stok Yönetimi sayfasının sorumluluğunda. |
| Pazaryeri sync ayarları | Ayrı Marketplace Sync sayfasının sorumluluğunda. |

---

## Ayrı Sayfalara Bırakılan Sorumluluklar

- **Stok Yönetimi:** `BranchOfficeStock` düzenleme/ayarlama. Edit sayfasında stok read-only gösterilir, yanında link.
- **Pazaryeri Senkronizasyonu:** Ürün kaydedildikten sonra marketplace'e gönderme manuel olarak Sync sayfasından yapılır.

---

## Entity & DTO Katmanı

### Mevcut DTO'larda eksik alanlar
- `ProductEditDetailDto` → `Season` ve `Year` eklenmeli (Product entity'sinde mevcut, DTO'da yok).
- `ProductVariantEditDetailDto` → `ECommercePrice` eklenmeli (ProductVariant entity'sinde mevcut, DTO'da yok).

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
    List<AttributeKeyValueDto> AttributeKeyValues
);
```

`ProductEditDetailDto` read model olarak kalır. `UpdateProduct` artık `EditProductDto` alır.

**`EditProductVariantDto`** (write model — variant güncelleme):
```csharp
public sealed record EditProductVariantDto(
    Guid Id,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal DimensionalWeight,
    decimal VatRate,
    string CurrencyType,
    List<EditableImageDto> Images  // IsDeleted=true olanlar soft-delete
);
```

**`ProductEditPageDto`** (tek sorguda tüm sayfa verisi — CategoryEditPageDto pattern):
```csharp
public sealed record ProductEditPageDto(
    ProductEditDetailDto Product,
    List<BrandListDetailDto> Brands,
    List<CategoryDetailDto> LeafCategories,
    List<BranchOffice> BranchOffices,
    MarketplaceSyncStatusDto SyncStatus
);
```

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
    NeverSynced,       // ProductMarketplace kaydı yok
    Waiting,           // Status=Pending, BatchRequestId=null
    Processing,        // Status=Pending, BatchRequestId!=null (Gönderildi — İşlemde)
    OutOfSync,         // Status=Published, UpdatedAt > LastSyncedAt
    Synced,            // Status=Published, UpdatedAt <= LastSyncedAt
    Failed,
    Rejected
}
```

---

## Business Layer

### `IProductService` değişiklikleri
```csharp
Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id);
Task<IResult> UpdateProduct(EditProductDto dto);  // imza değişiyor
```

### `ProductManager.UpdateProduct` — 3 adımlı pipeline

**1. Validation** (`EditProductValidator` — FluentValidation):
- Title boş olamaz
- BrandId > 0, CategoryId > 0
- Her variant için ListPrice >= 0, SalePrice >= 0, CostPrice >= 0

**2. Business Rules** (LogicRunner):
- StockCode benzersizliği (kendi Id hariç)

**3. Execution**:
```
- Scalar alanlar: Title, Description, StockCode, Season, Year, BrandId, CategoryId
- Kategori değişikliği tespiti:
    if (existingProduct.CategoryId != dto.CategoryId)
        → Tüm AttributeKeyValues temizlenir
        → Varyant ProductVariantAttributes korunur (yapısal değişiklik yok)
- Variant güncellemesi (ekle/silme yok — sadece scalar alanlar):
    foreach variant: fiyat, ağırlık, kur bilgileri
- AttributeKeyValues: mevcut tümü silinir, dto'daki yeniden eklenir
- Images: EditableImageDto.IsDeleted=true olanlar soft-delete (IsDeleted=true, DeletedAt=now)
- SaveChangesAsync()
- ProductUpdatedEvent yayınlanır
```

### `ProductUpdatedEvent` (yeni channel event)
```csharp
public class ProductUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public bool CategoryChanged { get; set; }
}
```

Marketplace sync sayfası bu event'i dinleyerek "güncelleme gerekiyor" durumunu anlayabilir.

---

## Marketplace Sync State Matrisi

| State | Koşul | UI Etiketi | Renk |
|---|---|---|---|
| NeverSynced | ProductMarketplace kaydı yok | Senkronize Edilmedi | Gri |
| Waiting | Status=Pending, BatchRequestId=null | Bekliyor | Sarı |
| Processing | Status=Pending, BatchRequestId!=null | Gönderildi — İşlemde | Mavi |
| OutOfSync | Status=Published, UpdatedAt > LastSyncedAt | Güncelleme Gerekiyor | Turuncu |
| Synced | Status=Published, UpdatedAt <= LastSyncedAt | Yayında | Yeşil |
| Failed | Status=Failed | Başarısız | Kırmızı |
| Rejected | Status=Rejected | Reddedildi | Koyu Kırmızı |

Dirty flag için yeni sütuna gerek yok — timestamp karşılaştırması yeterli.

---

## Blazor UI Katmanı

### Route
`/products/edit/{Id:guid}` — `Products.razor.cs`'teki `EditProduct` metodu zaten bu rotaya yönlendiriyor.

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
- Loading state, ürün bulunamadı guard (→ `/products` yönlendirme)
- `MudTabs`: Genel / Varyantlar / Görseller / Özellikler
- Header: ürün başlığı + `MarketplaceSyncState` badge (`MudChip`)
- Alt bileşenlere `@bind` ile form state geçirir
- Altta tek "Kaydet" + "İptal" butonu

### Sekme sorumlulukları

**Genel sekmesi** (`ProductEditGeneralTab`):
- Title, Description, StockCode, Season, Year (MudTextField)
- Brand: MudSelect — değiştirilebilir
- Category: MudSelect — değişince `ShowMessageBox` uyarı ("Kategori değiştirilirse mevcut özellikler temizlenecek. Devam?"), onaylanırsa AttributeKeyValues sıfırlanır

**Varyantlar sekmesi** (`ProductEditVariantsTab`):
- Her varyant için MudExpansionPanel, başlık = varyant etiket (renk/beden/custom)
- Barcode: read-only MudChip
- Düzenlenebilir: ListPrice, SalePrice, CostPrice, ECommercePrice, DimensionalWeight, VatRate, CurrencyType
- Stok: read-only gösterim + "Stok Yönetimi →" link butonu

**Görseller sekmesi** (`ProductEditImagesTab`):
- Mevcut görseller: preview grid, silme ikonu (IsDeleted=true), cover radio
- "Görsel Ekle" → `ImageUploadDialog` (mevcut bileşen)

**Özellikler sekmesi** (`ProductEditAttributesTab`):
- Kategori değişmediğinde: mevcut AttributeKeyValues MudSelect/MudTextField ile
- Kategori değişince: sıfırlanır, yeni kategorinin attribute listesi yüklenir
- AddProduct'taki `_regularAttributes` mantığı buraya taşınır

---

## Gözden Kaçabilecek Diğer Noktalar

1. **`SearchVector` güncellemesi:** PostgreSQL full-text search vektörü muhtemelen DB trigger'ı ile otomatik güncellenir — EF Core tarafında yapılacak bir şey yok. Ancak migration/trigger varlığı doğrulanmalı.
2. **`EditProductValidator`:** `AddProductValidator`'dan türetilmez, ayrı yazılır — çünkü edit'te zorunlu alanlar farklı (örn. stok kontrolü yok).
3. **EF Core change tracking:** `UpdateProduct`'taki mevcut `_dbContext.MainProducts.Update(product)` tüm graph'ı connected state'e almaya çalışır — bu tehlikeli. Doğru yol: `FirstOrDefaultAsync` ile entity'yi context'e yükle, sonra scalar değerleri ata, child collection'ları manuel yönet.
4. **Görsel yönetimi — yeni görseller:** Edit sayfasında eklenen yeni görseller `IBrowserFile` olarak tutulur; kaydet sırasında `IImageManager.AddProductImages` çağrılır (AddProduct'taki akış).
5. **Barcode alanı:** UI'da `MudChip` veya `MudTextField ReadOnly=true` + tooltip "Barkod düzenlenemez" ile gösterilmeli.
6. **`ProductEditPageDto.CategoryEditPageDto` benzeri:** İleride kategori değişince yeni kategorinin attribute listesi lazım — bu `ICategoryAttributeManager.GetCategoryAttributesByCategory` ile çekilir (AddProduct'ta zaten yapılıyor).
7. **`ECommercePrice` vs `SalePrice`:** İkisi farklı kavramlar (e-ticaret fiyatı ≠ mağaza satış fiyatı). UI'da ayrı etiketlerle gösterilmeli, karışıklık yaşanmamalı.
