# PttAVM Entegrasyon Tasarim Dokumani

**Tarih:** 2026-03-22
**MarketPlaceId:** 7
**Durum:** Onaylandi

## Ozet

PttAVM pazaryeri entegrasyonu, 3 faz halinde gerceklestirilecektir. REST + JSON tabanli API, iki ayri base URL ve iki farkli auth mekanizmasi (ApiKey+Token ve BasicAuth) kullanir. Toplam ~22 endpoint, 4 modul (Katalog, Listeleme, Sipariş, Kargo) icerir.

## Kararlar

| Karar | Secim | Gerekce |
|-------|-------|---------|
| Faz sayisi | 3 | Granular ilerleme, Faz 1 ile satisa baslanir |
| API client yapisi | 2 ayri client | Farkli auth mekanizmalari, SRP, test kolayligi |
| Tracking mekanizmasi | Dedicated background service | Async API, Amazon pattern'i kanitlanmis |
| Kategori UI | Lazy-loading tree | Performansli, Trendyol pattern'iyle tutarli |
| MarketPlaceId | 7 | Sirayla devam, ID 4 kasitli olarak bos birakildi (Amazon gecisinden kalma orphan kayit riski) |
| Mimari yaklasim | Pazarama klonu | Kanitlanmis pattern, minimum risk |

## API Genel Bakis

| Modul | Endpoint Sayisi | Base URL | Auth |
|-------|----------------|----------|------|
| Katalog | 9 | integration-api.pttavm.com | ApiKey + AccessToken |
| Listeleme | 3 | integration-api.pttavm.com | ApiKey + AccessToken |
| Sipariş | 5 | integration-api.pttavm.com | ApiKey + AccessToken |
| Kargo | 5 | shipment.pttavm.com | Basic Auth |

Detayli API dokumantasyonu: `docs/pttavm/` (5 dosya)

## Faz 1 — Altyapi + Katalog

### Scope

- PttavmCatalogApiClient (ApiKey + Token auth)
- MockPttavmCatalogApiClient + `Pttavm:UseMock` toggle
- PttavmCategoryImporter (lazy-loading tree)
- PttavmCategoryTreeView component (CategoryImport sayfasina yeni tab)
- MarketPlaceConstants.PttavmMarketPlaceId = 7
- Kategori eslestirme (MarketPlaceId=7)
- docs/pttavm/ altinda API dokumantasyonu

### PttavmCatalogApiClient

**Auth mekanizmasi:** Her istekte header olarak gonderilir (OAuth2 token yenilemesi yok).

```
Header "Api-Key": {MarketPlace tablosundan}
Header "Access-Token": {MarketPlace tablosundan}
Header "Content-Type": "application/json"
Header "X-Correlation-Id": {Guid.NewGuid()}
```

**Multi-tenant tasarim:**
- Token yenileme olmadigi icin ConcurrentDictionary token cache gereksiz
- Credential cache: `ConcurrentDictionary<int, PttavmCredentials>` (TTL: 5dk, DB'ye her istekte gitmemek icin)
- Tenant basina izole SemaphoreSlim gerekli degil (token refresh yok)

**Interface:**

```csharp
public interface IPttavmCatalogApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body);
}
```

DeleteAsync yok — PttAVM API'sinde DELETE endpoint'i bulunmuyor.

### PttavmCategoryImporter

Lazy-loading tree stratejisi:

1. `LoadMainCategories()` → GET /api/v1/categories/main → kok dugumler (HasChildren=true)
2. `LoadChildren(parentId)` → GET /api/v1/categories/{parentId} → secilen dugumun dogrudan alt kategorilerini doner (kullanici expand ettiginde tetiklenir)
3. `ImportSelectedCategories(selectedNodes)` → internal Category tablosuna esle, CategoryAttributeMarketPlaceMatch (MarketPlaceId=7) olustur, CategoryImportRequestedEvent publish et

> **Not:** `GET /api/v1/categories/category-tree` endpoint'i tam agaci bir seferde doner. Lazy-loading stratejisinde kullanilmaz cunku agac buyuklugu bilinmiyor ve performans riski tasir. Ancak ileride incremental sync icin kullanilabilir (`last_update` parametresi ile sadece degisen kategorileri cekme). Bu endpoint PttavmCatalogApiClient uzerinden erisilebilir durumda tutulur ama Faz 1 scope'unda aktif olarak kullanilmaz.

### CategoryImport UI Degisiklikleri

**Yeni tab sirasi:** Trendyol → Hepsiburada → N11 → Pazarama → **PttAVM** → Amazon

**Yeni dosyalar:**
- `Features/CategoryImport/PttavmCategoryTreeView.razor`
- `Features/CategoryImport/PttavmCategoryTreeView.razor.cs`

**CategoryImport.razor.cs degisiklikleri:**
- Yeni state: `pttavmCategories`, `pttavmSelectedNodes`, `pttavmLoading`
- Yeni inject: `PttavmCategoryImporter`
- Yeni metodlar: `LoadPttavmCategories()`, `OnPttavmNodeExpanded()`, `ImportPttavmCategories()`

### Dosya Yapisi

```
Business/
├── Abstract/
│   └── IPttavmCatalogApiClient.cs
├── Concrete/
│   └── Pttavm/
│       ├── PttavmCatalogApiClient.cs
│       ├── MockPttavmCatalogApiClient.cs
│       ├── PttavmResponseModels.cs
│       └── PttavmCategoryImporter.cs
├── Utility/Constants/
│   └── MarketPlaceConstants.cs  (PttavmMarketPlaceId = 7 eklenir)

Blazor/Features/CategoryImport/
├── PttavmCategoryTreeView.razor         (yeni)
├── PttavmCategoryTreeView.razor.cs      (yeni)
├── CategoryImport.razor                 (PttAVM tab eklenir)
└── CategoryImport.razor.cs              (PttAVM state + metodlar eklenir)

ApplicationBootstrap/
└── ApplicationDependencyExtension.cs    (PttAVM DI kayitlari eklenir)
```

### DI Kayit

```csharp
// PttAVM
var usePttavmMock = configuration.GetValue<bool>("Pttavm:UseMock", true);
if (usePttavmMock)
    services.AddScoped<IPttavmCatalogApiClient, MockPttavmCatalogApiClient>();
else
    services.AddScoped<IPttavmCatalogApiClient, PttavmCatalogApiClient>();

services.AddScoped<PttavmCategoryImporter>();
```

## Faz 2 — Urun Yonetimi

### Scope

- IPttavmProductService — urun publish (POST /products/upsert)
- IPttavmStockPriceService — fiyat-stok guncelleme (POST /products/stock-prices)
- PttavmProductMapper — internal product → PttAVM request DTO mapping
- PttavmMappingValidator — publish oncesi dogrulama
- PttavmProductTrackingPollingService (background) — trackingId polling
- PttavmStockPriceSyncService (background) — periyodik fiyat-stok sync
- Barkod kontrol, urun aktif/pasif, hatali gorseller

### IPttavmProductService

```csharp
public interface IPttavmProductService
{
    Task<PttavmUpsertResult> UpsertProductsAsync(List<PttavmProductRequest> products);
    Task<PttavmTrackingResult> GetTrackingResultAsync(string trackingId);
    Task<PttavmProductInfo> GetProductByBarcodeAsync(string barcode);
    Task<List<PttavmProductInfo>> GetProductsByBarcodesAsync(List<string> barcodes);
    Task<PttavmBaseResult> SetProductStatusAsync(int productId, bool isActive);
    Task<PttavmFaultyImagesResult> GetFaultyImagesAsync(List<string>? barcodes, int page, int pageSize);
}
```

### IPttavmStockPriceService

```csharp
public interface IPttavmStockPriceService
{
    Task<PttavmUpsertResult> UpdateStockPricesAsync(List<PttavmStockPriceRequest> items);
    Task<List<PttavmProductInfo>> SearchProductsAsync(PttavmProductSearchFilter filter);
}
```

### Async Tracking Flow

```
UpsertProducts() → trackingId doner
    → DB'ye kaydet: PttavmTrackingRecord { TrackingId, Type, Status=Pending, CreatedAt }
    → PttavmProductTrackingPollingService (background, her 30sn)
        → Pending/InProgress kayitlari sorgula
        → GET /products/tracking-result/{trackingId}
        → Status guncelle (Completed/Cancelled)
        → Hatali urunler → ApplicationLog'a yaz
        → Multi-tenant: ConcurrentDictionary<int, List<TrackingRecord>>
```

### PttavmProductMapper

Internal Product entity → PttavmProductRequest DTO donusumu:
- Kategori eslestirmesi: CategoryAttributeMarketPlaceMatch (MarketPlaceId=7)
- Ozellik eslestirmesi: CategoryAttributeValueMarketPlaceMatch
- Varyant destegi: variants array'i
- Gorsel URL'leri: MinIO'dan public URL
- KDV hesaplama: vatRate (0, 1, 10, 20)

### PttavmMappingValidator

Publish oncesi kontroller:
- Kategori eslesmesi var mi?
- Zorunlu alanlar dolu mu? (name, barcode, price, quantity, images)
- KDV orani gecerli mi? (0, 1, 10, 20)
- Stok 0-9999 araliginda mi?
- Maks 1000 urun/batch kontrolu
- Indirim 0-70 araliginda mi?

### PttavmStockPriceSyncService (Background)

- Periyodik: configurable interval, default 15dk
- Degisen urunleri toplar (son sync'ten bu yana UpdatedAt degisim)
- Batch: maks 1000/batch
- trackingId ile sonuc takip
- Multi-tenant: tenant basina ayri sync cycle
- Duplicate guard: ayni istek 5dk icinde tekrar gonderilmez

### Dosya Yapisi

```
Business/
├── Abstract/
│   ├── IPttavmProductService.cs
│   ├── IPttavmStockPriceService.cs
│   └── IPttavmProductMapper.cs
├── Concrete/
│   └── Pttavm/
│       ├── PttavmProductService.cs + MockPttavmProductService.cs
│       ├── PttavmStockPriceService.cs + MockPttavmStockPriceService.cs
│       ├── PttavmProductMapper.cs
│       ├── PttavmMappingValidator.cs
│       └── PttavmProductRequestModels.cs
├── BackgroundServices/
│   ├── PttavmProductTrackingPollingService.cs
│   └── PttavmStockPriceSyncService.cs

Entity/
└── Pttavm/
    └── PttavmTrackingRecord.cs  (TrackingId, Type, Status, CreatedAt, UpdatedAt, TenantId)
```

> **Not:** `PttavmTrackingRecord` entity'si `BaseEntity`'den turetilir, EF Core configuration'i ve migration gerektirir.

### DI Kayit (Faz 2)

```csharp
// PttAVM — Urun Yonetimi
var usePttavmMock = configuration.GetValue<bool>("Pttavm:UseMock", true);
if (usePttavmMock)
{
    services.AddScoped<IPttavmProductService, MockPttavmProductService>();
    services.AddScoped<IPttavmStockPriceService, MockPttavmStockPriceService>();
}
else
{
    services.AddScoped<IPttavmProductService, PttavmProductService>();
    services.AddScoped<IPttavmStockPriceService, PttavmStockPriceService>();
}
services.AddScoped<IPttavmProductMapper, PttavmProductMapper>();
services.AddScoped<PttavmMappingValidator>();

// Background services (AddBackgroundServices icine)
services.AddHostedService<PttavmProductTrackingPollingService>();
services.AddHostedService<PttavmStockPriceSyncService>();
```

### Kritik Is Kurali: Varyant Silme Davranisi

PttAVM API'si urun guncelleme sirasinda varyant gonderilmezse mevcut varyantlari **siler**. Bu veri kaybina yol acabilir. `PttavmProductMapper` her zaman mevcut varyantlari dahil etmeli — varyantli urunlerde bos varyant dizisi gonderilmemelidir. `PttavmMappingValidator` bu durumu kontrol etmelidir.

## Faz 3 — Sipariş + Kargo

### Scope

- PttavmShipmentApiClient (BasicAuth, shipment.pttavm.com)
- MockPttavmShipmentApiClient
- IPttavmOrderService — Sipariş sorgulama
- PttavmOrderPollingService (background) — yeni Sipariş polling
- IPttavmShippingService — kargo barkod olusturma, etiket, durum guncelleme
- IPttavmInvoiceService — fatura gonderme

### PttavmShipmentApiClient

```
Base URL: https://shipment.pttavm.com
Auth: Basic Auth (username:password → Base64)
Content-Type: application/json
```

```csharp
public interface IPttavmShipmentApiClient
{
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
}
```

Sadece POST — kargo API'sinin tum endpointleri POST.

Credential'lar MarketPlace tablosunda: `ShipmentUsername` / `ShipmentPassword` alanlari.

Multi-tenant: ConcurrentDictionary<int, PttavmShipmentCredentials> ile credential cache.

### IPttavmOrderService

```csharp
public interface IPttavmOrderService
{
    Task<List<PttavmOrder>> SearchOrdersAsync(DateTime startDate, DateTime endDate, bool isActiveOrders);
    Task<PttavmOrderDetail> GetOrderDetailAsync(string orderId);
    Task<List<PttavmCargoInfo>> GetCargoInfosAsync(string orderId);
    Task<List<PttavmCargoProfile>> GetCargoProfilesAsync();
}
```

> **Not:** `SearchOrdersAsync`, `GetOrderDetailAsync`, `GetCargoInfosAsync` ve `GetCargoProfilesAsync` hepsi `integration-api.pttavm.com` uzerinde calisir, dolayisiyla **PttavmCatalogApiClient** kullanir (ShipmentApiClient degil). `IPttavmOrderService` constructor'inda `IPttavmCatalogApiClient` inject edilir.

API kisitlamasi: Sipariş arama tarih araligi maks 40 gun.

### IPttavmShippingService

```csharp
public interface IPttavmShippingService
{
    Task<List<PttavmWarehouse>> GetWarehousesAsync();
    Task<PttavmBarcodeCreateResult> CreateBarcodesAsync(List<PttavmBarcodeRequest> orders);
    Task<PttavmBarcodeStatusResult> CheckBarcodeStatusAsync(string trackingId);
    Task<string> GetBarcodeTagAsync(string barcode, string orderId, string? type = null);
    Task<PttavmBaseResult> UpdateNoShippingOrderAsync(string orderId);
}
```

### IPttavmInvoiceService

```csharp
public interface IPttavmInvoiceService
{
    Task<PttavmBaseResult> SendInvoiceAsync(string orderId, List<int> lineItemIds, string? pdfUrl, string? base64Content);
}
```

### PttavmOrderPollingService (Background)

- Her 5 dakika (configurable)
- SearchOrders(son 24 saat, isActiveOrders=false)
- Yeni Siparişleri DB'ye kaydet
- Durum degisikliklerini guncelle
- Multi-tenant: ConcurrentDictionary<int, DateTime> ile tenant basina lastPollTime
- Bildirim: yeni Sipariş → SignalR ile UI'a push

### Kargo Akisi

```
Sipariş geldi
  → GetWarehouses() → depo sec
  → CreateBarcodes(orderId, warehouseId) → trackingId
  → CheckBarcodeStatus(trackingId) → barkod numarasi al
  → GetBarcodeTag(barcode, orderId) → etiket yazdir (ZPL veya HTML)
  → (Dijital urun ise) UpdateNoShippingOrder(orderId)
```

### Sipariş Durumlari

| Durum | Aciklama |
|-------|----------|
| kargo_yapilmasi_bekleniyor | Hazirlanacak |
| havale_onayi_bekleniyor | Odeme bekleniyor |
| gondericisine_teslim_edildi | Kargoya verildi |
| gonderilmis | Gonderildi |
| tamamlandi | Teslim edildi |
| iptal | Iptal |
| iade | Iade |
| onay_surecinde | Onay bekleniyor |
| odeme_gecersiz | Odeme gecersiz |

### Dosya Yapisi

```
Business/
├── Abstract/
│   ├── IPttavmShipmentApiClient.cs
│   ├── IPttavmOrderService.cs
│   ├── IPttavmShippingService.cs
│   └── IPttavmInvoiceService.cs
├── Concrete/
│   └── Pttavm/
│       ├── PttavmShipmentApiClient.cs + MockPttavmShipmentApiClient.cs
│       ├── PttavmOrderService.cs + MockPttavmOrderService.cs
│       ├── PttavmShippingService.cs + MockPttavmShippingService.cs
│       ├── PttavmInvoiceService.cs + MockPttavmInvoiceService.cs
│       └── PttavmOrderResponseModels.cs
├── BackgroundServices/
│   └── PttavmOrderPollingService.cs
```

### DI Kayit (Faz 3)

```csharp
// PttAVM — Sipariş + Kargo
var usePttavmMock = configuration.GetValue<bool>("Pttavm:UseMock", true);
if (usePttavmMock)
{
    services.AddScoped<IPttavmShipmentApiClient, MockPttavmShipmentApiClient>();
    services.AddScoped<IPttavmOrderService, MockPttavmOrderService>();
    services.AddScoped<IPttavmShippingService, MockPttavmShippingService>();
    services.AddScoped<IPttavmInvoiceService, MockPttavmInvoiceService>();
}
else
{
    services.AddScoped<IPttavmShipmentApiClient, PttavmShipmentApiClient>();
    services.AddScoped<IPttavmOrderService, PttavmOrderService>();
    services.AddScoped<IPttavmShippingService, PttavmShippingService>();
    services.AddScoped<IPttavmInvoiceService, PttavmInvoiceService>();
}

// Background service (AddBackgroundServices icine)
services.AddHostedService<PttavmOrderPollingService>();
```

## Test Stratejisi

TDD-first, her faz icin:

### Unit Testler (Moq + FluentAssertions)

**Faz 1:**
- PttavmConstantsTests — MarketPlaceId=7, diger ID'lerle cakismama
- PttavmCatalogApiClientTests — header dogrulama, auth, error handling, X-Correlation-Id
- PttavmCategoryImporterTests — tree parsing, lazy-load, mapping, empty response

**Faz 2:**
- PttavmProductServiceTests — upsert, tracking, barcode kontrol, status degistirme
- PttavmStockPriceServiceTests — fiyat-stok guncelleme, batch limitleri
- PttavmProductMapperTests — entity → DTO donusumu, varyant, gorsel
- PttavmMappingValidatorTests — zorunlu alan, KDV, stok araligi, batch limiti

**Faz 3:**
- PttavmOrderServiceTests — Sipariş arama, detay, tarih araligi kontrolu
- PttavmShippingServiceTests — barkod olusturma akisi, depo listeleme
- PttavmInvoiceServiceTests — fatura gonderme, PDF format

### E2E Testler (Playwright)

- CategoryImport sayfasinda PttAVM tab'inin gorunmesi
- PttAVM tree load (ana kategoriler)
- Kategori secimi ve import akisi

## Multi-Tenant Hususlar

- Credential cache: `ConcurrentDictionary<int, PttavmCredentials>` (Catalog) ve `ConcurrentDictionary<int, PttavmShipmentCredentials>` (Shipment). Her cache entry `CachedAt` timestamp tutar, 5dk TTL ile dogrulanir (`DateTime.UtcNow - CachedAt > TimeSpan.FromMinutes(5)` ise DB'den yeniden cekilir)
- Background service state: tenant basina izole (`ConcurrentDictionary<int, T>`)
- DB sorgulari: tenant filtresi uygulanabilir
- Configuration: tenant-specific credential'lar DB'den (MarketPlace tablosu), appsettings'e hardcode degil
- Polling service'ler: tenant basina ayri poll cycle ve lastPollTime

## API Kisitlamalar Ozeti

| Kisitlama | Deger |
|-----------|-------|
| Maks urun/istek (upsert) | 1000 |
| Maks urun/istek (stock-prices) | 1000 |
| Duplicate istek suresi | 5 dakika |
| Stok araligi | 0-9999 |
| Indirim araligi | 0-70 |
| Sipariş arama tarih araligi | Maks 40 gun |
| Hatali gorsel barkod/istek | Maks 10.000 |
| Hatali gorsel pageSize | Maks 10.000 |
| Garanti suresi | 0-24 ay |
| Sepet max adet | 0-1000 |
