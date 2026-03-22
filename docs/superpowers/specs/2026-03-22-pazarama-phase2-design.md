# Pazarama Faz 2: Ürün Publish + Stok/Fiyat Sync — Tasarım Dokümanı

## Amaç

Pazarama pazaryerine ürün ekleme, batch durum kontrolü, stok güncelleme ve fiyat güncelleme yeteneklerini eklemek. Event-driven background service'ler ile otomatik senkronizasyon sağlamak.

## Kapsam

- PazaramaMappingValidator: pre-publish doğrulama (kategori, marka, attribute match kontrolü)
- PazaramaProductMapper: dahili ürün → Pazarama create request dönüşümü
- IPazaramaProductService: ürün publish + batch status kontrolü
- IPazaramaStockPriceService: stok/fiyat güncelleme (ayrı endpoint'ler)
- PazaramaProductPublishBackgroundService: ProductCreatedForMarketplaceEvent tüketimi
- PazaramaBatchStatusPollingService: batch durum polling (60sn interval)
- PazaramaStockPriceSyncService: StockPriceChangedEvent tüketimi + 10sn event batching
- Request/response DTO'ları
- Mock servisleri + unit testler

## Kapsam Dışı

- Ürün filtreleme / detay çekme (Faz 2.5 veya sonraki faz)
- Ürün durum senkronizasyonu (ProductStatusSync — onay durumu takibi)
- Sipariş, iade, fatura (Faz 3-4)
- Satışa açma/kapatma (external-status endpoint)

---

## Mimari Kararlar

### 1. Trendyol Pattern Mirror
Trendyol'un kanıtlanmış event-driven akışı birebir uygulanır: Validator → Mapper → Service → Background Service → Batch Polling. Aynı `EventChannel`, `ProductMarketplace`, `IProductActivityLogger` altyapısı kullanılır.

### 2. Varyant = Ayrı Ürün + groupCode
Pazarama her varyantı **ayrı ürün** olarak bekler. Aynı ürünün varyantları `groupCode` (max 10 karakter) ile gruplandırılır. Trendyol'un tek ürün altında varyant toplama yaklaşımından farklı. Mapper her `ProductVariant`'ı ayrı `PazaramaProductItem`'a dönüştürür.

### 3. Ayrı Stok ve Fiyat Endpoint'leri
Trendyol stok ve fiyatı tek endpoint'te günceller. Pazarama'da stok (`POST /product/updateStock-v2`) ve fiyat (`POST /product/updatePrice-v2`) **ayrı endpoint'lerdir**. `StockPriceChangedEvent` geldiğinde her iki endpoint'e de istek yapılır.

### 4. 10sn Rate Limit + Event Batching
Pazarama her istek arasında 10sn rate limit uygular. Background service `StockPriceChangedEvent`'leri 10 saniye boyunca buffer'lar, ardından toplu göderir (max 3000 item/istek). Bu, yüksek hacimli güncellemelerde rate limit'e takılmayı önler.

### 5. Batch Status Endpoint Farkı
- Ürün ekleme: `GET /product/getProductBatchResult?BatchRequestId={id}` → status 1=InProgress, 2=Done, 3=Error
- Stok/fiyat: `GET /listing-state/batch-id/{dataId}/lake-projections` → status 0=Başarılı, 1=Tamamlanamadı, 3=İşleniyor

### 6. GUID ID Eşleme
Tüm Pazarama ID'leri GUID. Match tablolarındaki mevcut `*ExternalId` (string) alanları kullanılır:
- Kategori: `CategoryMarketplace.ExternalCategoryId`
- Attribute: `CategoryAttributeMarketPlaceMatch.MarketPlaceCategoryAttributeExternalId`
- Attribute Value: `CategoryAttributeValueMarketPlaceMatch.MarketPlaceCategoryAttributeValueExternalId`
- Brand: `BrandMarketPlaceMatch.MarketPlaceBrandExternalId`

---

## Bileşenler

### A. PazaramaMappingValidator

Pre-publish doğrulama. `ValidateProductMappingsAsync(productId)` → detaylı hata mesajları.

Kontroller:
1. Product var mı, CategoryId set mi
2. `CategoryMarketplace` eşlemesi var mı (MarketPlaceId=5, ExternalCategoryId != null)
3. `BrandMarketPlaceMatch` eşlemesi var mı (MarketPlaceBrandExternalId != null)
4. Zorunlu attribute'lar (IsRequired=true) eşlenmiş mi (`MarketPlaceCategoryAttributeExternalId` != null)

**Reuse:** `TrendyolMappingValidator` / `N11MappingValidator` pattern

### B. PazaramaProductMapper

`MapProductAsync(productId)` → `IDataResult<PazaramaCreateProductRequest>`

Mapping akışı:
1. Product + Variants + Images + Attributes yükle
2. ProductMarketplace overrides (TitleOverride, DescriptionOverride, VariantOverrides) uygula
3. CategoryMarketplace → `categoryId` (GUID from ExternalCategoryId)
4. BrandMarketPlaceMatch → `brandId` (GUID from MarketPlaceBrandExternalId)
5. Her variant → ayrı `PazaramaProductItem`:
   - `code` = variant barcode
   - `groupCode` = Product.StockCode'un ilk 10 karakteri; StockCode null/boş ise Product.Id.ToString("N").Substring(0, 10)
   - `stockCount` = MarketPlaceWarehouse'lardaki toplam stok
   - `listPrice`, `salePrice` = variant fiyatları (override varsa override)
   - `attributes` = attribute match'lerden GUID'ler
   - `images` = ürün görselleri

**Reuse:** `TrendyolProductMapper` pattern (warehouse stock aggregation, override logic)

**Interface:** `IPazaramaProductMapper` — `MapProductAsync(Guid productId)`

### C. IPazaramaProductService

```csharp
interface IPazaramaProductService
{
    Task<IDataResult<string>> PublishProductAsync(Guid productId);
    Task<IDataResult<PazaramaBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId);
}
```

`PublishProductAsync` akışı:
1. `PazaramaMappingValidator.ValidateProductMappingsAsync()` → hata varsa ErrorResult
2. `IPazaramaProductMapper.MapProductAsync()` → request body oluştur
3. `IPazaramaApiClient.PostAsync("product/create", request)` → batchRequestId al
4. `ProductMarketplace.BatchRequestId` güncelle, Status=Pending
5. Activity log: `IProductActivityLogger`

`CheckBatchStatusAsync`:
- `GET /product/getProductBatchResult?BatchRequestId={id}`
- Status: InProgress=1, Done=2, Error=3
- failedProducts listesinden hata detayları

### D. IPazaramaStockPriceService

```csharp
interface IPazaramaStockPriceService
{
    Task<IDataResult<string>> UpdateStockAsync(List<PazaramaStockUpdateItem> items);
    Task<IDataResult<string>> UpdatePriceAsync(List<PazaramaPriceUpdateItem> items);
}
```

- Stok: `POST /product/updateStock-v2` → `{ items: [{ code, stockCount }] }`
- Fiyat: `POST /product/updatePrice-v2` → `{ items: [{ code, listPrice, salePrice }] }`
- Her iki endpoint de dataId döner (asenkron)

### E. Background Services

> **ÖNEMLİ:** `EventChannel<T>` (System.Threading.Channels) single-reader'dır. Aynı channel'dan birden fazla background service okuyamaz — event kaybına yol açar. Bu nedenle Pazarama **ayrı background service oluşturmaz**, mevcut multi-marketplace service'lere "Pazarama" case'i eklenir.

**E1. Mevcut ProductPublishBackgroundService'e Pazarama Case Ekleme**
- Mevcut `TrendyolProductPublishBackgroundService` zaten marketplace-agnostic çalışır (Trendyol + Hepsiburada switch/case)
- `Marketplaces` listesinde "Pazarama" geldiğinde → `IPazaramaProductService.PublishProductAsync()` çağır
- **Yeni ayrı BG service OLUŞTURULMAZ** — mevcut service'e case eklenir

**E2. PazaramaBatchStatusPollingService (AYRI SERVICE)**
- Batch polling event-driven değil, timer-based (60sn interval)
- EventChannel kullanmaz → ayrı service olabilir
- `ProductMarketplace` tablosunda `MarketPlaceId=5 AND Status=Pending AND BatchRequestId!=null` ara
- `CheckBatchStatusAsync()` ile kontrol et
- Done + 0 fail → Published, Error veya fail > 0 → Failed
- 24 saat timeout → Failed
- Activity logging
- **NOT:** HB pattern'ı takip et (MarketPlaceId filtresi zorunlu), Trendyol pattern'ını DEĞİL (Trendyol filtre yapmıyor — bug)

**E3. Mevcut StockPriceSyncService'e Pazarama Case Ekleme**
- Mevcut `TrendyolStockPriceSyncService` `StockPriceChangedEvent` dinler
- **Yeni ayrı BG service OLUŞTURULMAZ** — mevcut service'e Pazarama case'i eklenir
- Pazarama'ya özgü: **10sn event batching** gerekir (rate limit)
- Implementasyon: service içinde Pazarama için internal `Channel<T>` buffer + Timer(10sn) ile batch gönderim
- Published ürünler için:
  - Warehouse'lardan stok topla → `UpdateStockAsync()`
  - Fiyat bilgisi → `UpdatePriceAsync()`
- Max 3000 item/istek (gerekirse bölünür)

### F. Mock Servisleri

- `MockPazaramaProductService : IPazaramaProductService` — mock batchRequestId döner
- `MockPazaramaStockPriceService : IPazaramaStockPriceService` — mock dataId döner

DI toggle: `Pazarama:UseMock` config key (Faz 1'de zaten var)

### G. Request/Response DTO'ları

`PazaramaProductRequestModels.cs`:
- `PazaramaCreateProductRequest` — `{ products: [...] }`
- `PazaramaProductItem` — name, displayName, description, brandId, desi, code, groupCode, stockCode, stockCount, vatRate, listPrice, salePrice, categoryId, attributes, images
- `PazaramaProductAttribute` — attributeId, attributeValueId
- `PazaramaProductImage` — imageUrl
- `PazaramaStockUpdateRequest` — `{ items: [{ code, stockCount }] }`
- `PazaramaPriceUpdateRequest` — `{ items: [{ code, listPrice, salePrice }] }`
- `PazaramaBatchStatusResponse` — status, batchRequestId, totalCount, successfulCount, failedCount, failedProducts
- `PazaramaStockPriceBatchResponse` — pageIndex, data, successCount, failedCount, processingCount

---

## Sprint Yapısı

| Sprint | Kapsam | Tahmini Test |
|--------|--------|-------------|
| 1 | Request/Response DTO'ları + PazaramaMappingValidator | ~8 |
| 2 | IPazaramaProductMapper + PazaramaProductMapper | ~8 |
| 3 | IPazaramaProductService + Mock + ProductPublish BG + BatchPolling BG | ~10 |
| 4 | IPazaramaStockPriceService + Mock + StockPriceSync BG (event batching) + DI | ~8 |

---

## Dosya Envanteri

### Yeni Dosyalar (~14)
1. `Business/Concrete/Pazarama/PazaramaMappingValidator.cs`
2. `Business/Abstract/IPazaramaProductMapper.cs`
3. `Business/Concrete/Pazarama/PazaramaProductMapper.cs`
4. `Business/Abstract/IPazaramaProductService.cs`
5. `Business/Concrete/Pazarama/PazaramaProductService.cs`
6. `Business/Concrete/Pazarama/MockPazaramaProductService.cs`
7. `Business/Abstract/IPazaramaStockPriceService.cs`
8. `Business/Concrete/Pazarama/PazaramaStockPriceService.cs`
9. `Business/Concrete/Pazarama/MockPazaramaStockPriceService.cs`
10. `Business/Concrete/Pazarama/PazaramaProductRequestModels.cs`
11. `Business/BackgroundServices/PazaramaBatchStatusPollingService.cs` (timer-based, ayrı olabilir)
12. `Test/Entegrasyon.Test/Pazarama/PazaramaMappingValidatorTests.cs`
13. `Test/Entegrasyon.Test/Pazarama/PazaramaProductMapperTests.cs`
14. `Test/Entegrasyon.Test/Pazarama/PazaramaProductServiceTests.cs`
15. `Test/Entegrasyon.Test/Pazarama/PazaramaStockPriceServiceTests.cs`

### Değişecek Dosyalar
1. `Business/Concrete/Pazarama/PazaramaResponseModels.cs` — batch status response DTO'ları ekle
2. `ApplicationBootstrap/ApplicationDependencyExtension.cs` — yeni servisler + BG service DI
3. Mevcut ProductPublishBackgroundService — "Pazarama" case ekle (EventChannel paylaşımı nedeniyle ayrı BG service oluşturulamaz)
4. Mevcut StockPriceSyncService — Pazarama case + 10sn batching logic ekle
5. `Business/Abstract/IPazaramaApiClient.cs` — comment fix (Id=4 → Id=5)

---

## Doğrulama

1. `dotnet build Entegrasyon.sln` — hatasız build
2. `dotnet test --filter "Pazarama"` — tüm yeni testler yeşil
3. Mevcut testler kırılmamış
4. Mock modda: ürün publish isteği mock batchRequestId döner
5. Mock modda: stok/fiyat güncelleme mock dataId döner
