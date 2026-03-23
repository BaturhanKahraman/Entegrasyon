# Temu Marketplace Entegrasyonu — Design Spec

**Tarih:** 2026-03-23
**MarketPlaceId:** 9
**Yaklasim:** Pazarama/Ciceksepeti pattern klonu (multi-tenant ready)

---

## 1. Genel Bakis

Entegrasyon projesine 7. pazaryeri olarak Temu ekleniyor. REST-like API + App Key/Secret + MD5 Sign auth mekanizmasi kullanir. Tek router endpoint uzerinden method-based routing yapar (`type` parametresi ile).

**Base URL'ler:**

| Bolge | URL |
|-------|-----|
| EU (Turkiye dahil) | `https://openapi-b-eu.temu.com` |
| US | `https://openapi-b-us.temu.com` |
| Global (Meksika, Japonya) | `https://openapi-b-global.temu.com` |

**Router Path:** `/openapi/router`

**Auth:** App Key + App Secret + Access Token + MD5 Sign (detay: `docs/temu/BASE_KNOWLEDGE.md`)

**Kapsam:** Urun yonetimi (CRUD, kategori), stok/fiyat guncelleme, siparis listeleme, kargo surecleri, iade yonetimi.

> **UYARI:** Temu API dokumantasyonu diger pazaryerlerine gore sinirlidir. Bircok endpoint detayi "TBD" olarak isaretlenmistir ve resmi Partner Platform dokumantasyonundan dogrulanmalidir.

---

## 2. Karar Tablosu

| Karar | Secim | Gerekce |
|-------|-------|---------|
| MarketPlaceId | 9 | Siradaki uygun ID (8 = Ciceksepeti) |
| Auth mekanizmasi | App Key + MD5 Sign + Access Token | Temu Partner Platform standardi |
| API Base URL | EU endpoint | Turkiye operasyonlari EU bolgesinde |
| ApiClient pattern | Scoped + static credential cache | Pazarama/Ciceksepeti pattern'i ile tutarli |
| Token refresh | 3 ayda bir | Access token suresi 3 ay |
| Rate limit | 20 QPS per app_key | Temu varsayilan limit |
| Request format | JSON POST (tek router endpoint) | Tum method'lar ayni URL'e gider |
| Mock client | Evet | Dev/test ortami icin |

---

## 3. Mimari & Dosya Yapisi

### Business Layer

```
Business/Concrete/Temu/
├── TemuApiClient.cs                # HTTP client, MD5 sign, multi-tenant cache
├── MockTemuApiClient.cs            # Dev/test mock
├── TemuCategoryService.cs          # Kategori & ozellik API
├── TemuProductService.cs           # Urun CRUD
├── TemuProductMapper.cs            # Product entity → Temu DTO
├── TemuMappingValidator.cs         # Marketplace mapping kontrolu
├── TemuStockPriceService.cs        # Stok/fiyat guncelleme
├── TemuOrderService.cs             # Siparis yonetimi
├── TemuShippingService.cs          # Kargo islemleri
├── TemuReturnService.cs            # Iade/iptal yonetimi
├── TemuResponseModels.cs           # API response DTO'lari
└── TemuRequestModels.cs            # API request DTO'lari
```

### Abstract Interfaces

```
Business/Abstract/
├── ITemuApiClient.cs
├── ITemuCategoryService.cs
├── ITemuCategoryImporter.cs
├── ITemuProductService.cs
├── ITemuProductMapper.cs
├── ITemuStockPriceService.cs
├── ITemuOrderService.cs
├── ITemuShippingService.cs
└── ITemuReturnService.cs
```

> **Not:** `TemuMappingValidator` interface'siz concrete olarak inject edilir (Pazarama/Ciceksepeti pattern'i ile tutarli).

### Background Services

```
Business/BackgroundServices/
├── TemuOrderPollingService.cs
└── TemuStockPriceSyncService.cs
```

### Category Importer

```
Business/Concrete/Import/
└── TemuCategoryImporter.cs          # extends BaseCategoryImporterService
```

---

## 4. TemuApiClient

**Sablon:** `PazaramaApiClient.cs` / `CiceksepetiApiClient.cs`

**Auth:** MD5 Sign mekanizmasi:
1. Tum parametreleri key'e gore alfabetik sirala
2. `{app_secret}key1value1key2value2...{app_secret}` birlestir
3. MD5 hash → UPPERCASE hex → `sign` parametresi

**Constructor Dependencies:**
- `IDbContextFactory<IntegrationDbContext>` — credential lookup
- `IHttpClientFactory` — HTTP client olusturma
- `ILogger<TemuApiClient>` — teknik loglama

**Multi-tenant:**
- `ConcurrentDictionary<int, TemuCredentials>` credential cache (`AppKey`, `AppSecret`, `AccessToken`, `BaseUrl`)
- Per-marketplace `SemaphoreSlim` rate limit izolasyonu (20 QPS)
- `IDbContextFactory<IntegrationDbContext>` ile credential refresh

**DI Lifetime:** Scoped (Pazarama/Ciceksepeti pattern'i ile tutarli). Credential cache `static` veya ayri singleton servis.

**Rate limit stratejisi:** 20 QPS per app_key. `SemaphoreSlim` + minimum interval kontrolu.

**Retry stratejisi:** `IHttpClientFactory` uzerinden Polly retry policy — 429 ve 5xx icin exponential backoff. Max 3 retry.

**Token refresh:** Access token suresi 3 ay. Token expire oldiginda otomatik refresh mekanizmasi.

**Ozel case:** Tum API cagrilari tek endpoint'e gider (`/openapi/router`), `type` parametresi method'u belirler.

**Metodlar:**
- `CallAsync<T>(string type, object parameters, CancellationToken ct)` — genel API cagrisi
- `RefreshAccessTokenAsync(int marketPlaceId, CancellationToken ct)` — token yenileme
- `CalculateSign(Dictionary<string, string> parameters, string appSecret)` — MD5 imza hesaplama

---

## 5. Urun Yonetimi

### TemuCategoryService
- `GetCategoriesAsync()` → type: `bg.goods.cats.get` (TBD - dogrulanacak)
- `GetCategoryAttributesAsync(long catId)` → type: `bg.goods.cat.template.get` (TBD - dogrulanacak)

### TemuCategoryImporter (extends BaseCategoryImporterService)
- Kategori agacini flat list'e donustur, leaf kategorileri MarketPlaceId=9 ile kaydet
- Attribute import: TBD - Temu'nun ozellik yapisi incelenecek

### TemuProductMapper
- Product entity → Temu DTO donusumu
- SKU mapping (outer_id ile esleme)
- Gorsel URL'leri, attribute mapping
- Fiyat formati: TBD (muhtemelen cent cinsinden integer)

### TemuMappingValidator
- Kategori mapped? (CategoryMarketPlaceMatches)
- Required attribute'lar mapped? (CategoryAttributeMarketPlaceMatches)
- Gorsel format/boyut kontrolu: TBD

### TemuProductService
**Dependencies:** `ITemuApiClient`, `ITemuProductMapper`, `TemuMappingValidator`, `IProductActivityLogger`, `IApplicationLogManager`, `ILogger<T>`

- `PublishProductAsync(Guid productId)` → validate → map → type: `bg.goods.add`
- `UpdateProductAsync(Guid productId)` → type: `bg.goods.edit` (veya v2)
- `GetProductsAsync(filters)` → type: `bg.goods.get`
- `UpdateSalesStatusAsync(long goodsId, bool isActive)` → type: `bg.goods.sales.status`

**Logging:** Dual-logging pattern — `IApplicationLogManager` (admin, Turkce) + `ILogger<T>` (developer, teknik)
**Activity logging:** `IProductActivityLogger` ile her publish/update sonucu kaydedilir

### TemuStockPriceService
- `UpdatePriceAsync(items)` → type: `bg.local.goods.priceorder.change.sku.price` (dogrulanmis)
- `UpdateStockAsync(items)` → TBD - stok guncelleme endpoint'i incelenecek
- Rate limit: 20 QPS icinde kalmali

---

## 6. Siparis & Kargo

### TemuOrderService
- `GetOrdersAsync(filters)` → type: `bg.order.list.v2.get` (dogrulanmis)
- `DecryptShippingInfoAsync(orderSn)` → type: `bg.order.decryptshippinginfo.get` (dogrulanmis)
- TBD - Siparis durumu guncelleme endpoint'i incelenecek

### TemuShippingService
- `UpdateTrackingAsync(orderSn, trackingNo, carrierCode)` → TBD - endpoint incelenecek
- TBD - Kargo firma listesi alma
- TBD - Kargo etiketi olusturma

---

## 7. Iade Yonetimi

### TemuReturnService
- `GetReturnsAsync(filters)` → type: `bg.aftersale.*` (TBD - tam endpoint incelenecek)
- `ApproveReturnAsync(returnId)` → TBD
- `RejectReturnAsync(returnId, reason)` → TBD

---

## 8. Background Services

### TemuOrderPollingService
- Periyodik yeni siparis cekme → DB'ye kaydet
- `ConcurrentDictionary<int, DateTime>` last poll timestamp (multi-tenant)
- Poll interval: konfigurasyon ile ayarlanabilir (default 60 sn)

### TemuStockPriceSyncService
- EventChannel ile tetikleme veya periyodik sync
- Degisen stok/fiyatlari tespit → guncelleme
- Rate limit gozetilerek batch isleme

---

## 9. DI & Configuration

### ApplicationDependencyExtension.cs

```csharp
public static IServiceCollection AddTemuServices(
    this IServiceCollection services, IConfiguration configuration)
{
    var useMock = configuration.GetValue<bool>("Temu:UseMock", true);

    // ApiClient — Scoped
    if (useMock)
        services.AddScoped<ITemuApiClient, MockTemuApiClient>();
    else
        services.AddScoped<ITemuApiClient, TemuApiClient>();

    // Services
    services.AddScoped<ITemuCategoryService, TemuCategoryService>();
    services.AddScoped<ITemuProductService, TemuProductService>();
    services.AddScoped<ITemuProductMapper, TemuProductMapper>();
    services.AddScoped<ITemuStockPriceService, TemuStockPriceService>();
    services.AddScoped<ITemuOrderService, TemuOrderService>();
    services.AddScoped<ITemuShippingService, TemuShippingService>();
    services.AddScoped<ITemuReturnService, TemuReturnService>();
    services.AddScoped<TemuMappingValidator>();
    services.AddScoped<ITemuCategoryImporter, TemuCategoryImporter>();

    return services;
}
```

### AddBackgroundServices (mevcut metoda ekleme)

```csharp
// Temu background services
services.AddHostedService<TemuOrderPollingService>();
services.AddHostedService<TemuStockPriceSyncService>();
```

### MarketPlaceConstants.cs
```csharp
public const int TemuMarketPlaceId = 9;
```

### Seed Data
MarketPlace tablosuna: `Id=9, Name="Temu", BaseUrl="https://openapi-b-eu.temu.com"`

> **Not:** MarketPlaceId=8 Ciceksepeti tarafindan kullaniliyor (bkz. `2026-03-22-ciceksepeti-integration-design.md`).

---

## 10. Multi-Tenant Tasarim

- **TemuApiClient:** Scoped, credential cache static `ConcurrentDictionary<int, TemuCredentials>`
- **MD5 Sign:** Credential'dan `AppSecret` alinir, per-tenant izole
- **Rate limit:** Per-marketplace `SemaphoreSlim` (20 QPS, tenant izolasyonu)
- **Token refresh:** Per-tenant access token yonetimi (3 ay expire)
- **Background services:** `IServiceScopeFactory` + `IDbContextFactory` ile scoped DB erisimi
- **Order polling:** `ConcurrentDictionary<int, DateTime>` last poll timestamp

---

## 11. Test Stratejisi (TDD-First)

### Unit Tests
- `TemuApiClientTests` — credential resolve, MD5 sign hesaplama, token refresh, error handling, rate limit
- `TemuProductMapperTests` — entity → DTO mapping, edge cases
- `TemuMappingValidatorTests` — mapping completeness, missing attribute detection
- `TemuProductServiceTests` — publish/update with mock ApiClient
- `TemuOrderServiceTests` — order fetch, decrypt shipping info
- `TemuStockPriceServiceTests` — fiyat/stok guncelleme
- `TemuReturnServiceTests` — iade listeleme/onaylama/reddetme
- `TemuShippingServiceTests` — kargo bilgisi guncelleme

### E2E Tests
- Sandbox API'ye gercek HTTP request'ler (sandbox ortami varsa)
- Kategori listeleme, urun CRUD, siparis listeleme akislari

---

## 12. Implementation Fazlari

| Faz | Kapsam | Bagimlilik |
|-----|--------|------------|
| 0 | API dokumanlari → develop commit | — |
| 1 | Altyapi: ApiClient (MD5 sign), Models, Constants, DI, Seed | Faz 0 |
| 2 | Kategori & Ozellik Import | Faz 1 |
| 3 | Urun: Mapper, Validator, ProductService, StockPriceService | Faz 2 |
| 4 | Siparis: OrderService, ShippingService | Faz 1 |
| 5 | Iade: ReturnService | Faz 1 |
| 6 | Background Services (Order Polling, StockPrice Sync) | Faz 3, 4 |
| 7 | UI Tab (Blazor) | Faz 1-6 |

> **Onemli:** Faz 1'e baslamadan once Temu Partner Platform dokumantasyonuna tam erisim saglanmali ve `docs/temu/` altindaki TBD maddeler doldurulmalidir.

---

## 13. API Dokumanlari

Detayli API dokumanlari: `docs/temu/`

| Dosya | Kapsam |
|-------|--------|
| BASE_KNOWLEDGE.md | Auth (MD5 sign), URL'ler, rate limits, genel mimari |
| PRODUCT_API.md | Urun CRUD, kategori, marka |
| ORDER_API.md | Siparis yonetimi |
| STOCK_PRICE_API.md | Stok/fiyat guncelleme |
| SHIPPING_API.md | Kargo/lojistik |
| RETURNS_API.md | Iade/iptal |

---

## 14. Temu'ya Ozel Teknik Hususlar

### Tek Router Endpoint
Diger marketplace'lerden farkli olarak Temu tum API cagrilarini tek bir URL'e (`/openapi/router`) yonlendirir. `type` parametresi hangi metodun cagrildigini belirler. Bu, `TemuApiClient`'in diger client'lerden farkli bir mimari ile yazilmasini gerektirir — path-based routing yerine method-based routing.

### MD5 Sign Mekanizmasi
Diger pazaryerlerindeki API Key header veya OAuth2 Bearer token yerine, Temu her request icin parametrelerden MD5 hash ile imza hesaplar. Bu hesaplama `TemuApiClient` icinde kapsullenmelidir.

### Bolge Bazli Token Izolasyonu
Her bolge (EU, US, Global) icin ayri access_token gerekir. Turkiye operasyonlari icin sadece EU token'i yeterlidir, ancak multi-tenant senaryoda farkli tenant'lar farkli bolgelerde olabilir.

### Pinduoduo Mirasi
Temu, PDD Holdings'in global markasi oldugu icin API yapisi Pinduoduo Open Platform ile buyuk benzerlikler tasir. Pinduoduo API dokumantasyonu referans olarak kullanilabilir, ancak endpoint isimleri ve parametrelerde farkliliklar olabilir.

---

## 15. Acik Konular ve Riskler

| # | Konu | Risk Seviyesi | Aciklama |
|---|------|---------------|----------|
| 1 | Sinirli dokumantasyon | Yuksek | Temu API dokumantasyonu diger pazaryerlerine gore sinirli ve SPA tabanli |
| 2 | Endpoint dogrulama | Yuksek | Bircok endpoint ismi Pinduoduo pattern'inden turetilmistir, dogrulanmasi gerekir |
| 3 | Sandbox ortami | Orta | Sandbox/test ortami mevcut mu, dogrulanmalidir |
| 4 | Turkiye pazari ozellikleri | Orta | Lokal pazar (TR) icin ozel gereksinimler incelenmeli |
| 5 | Fiyat formati | Dusuk | Cent vs birim fiyat dogrulanmali |
| 6 | Gorsel gereksinimleri | Dusuk | Format, boyut ve adet sinirlari dogrulanmali |

> **Sonraki Adim:** Temu Partner Platform'a developer erisimi saglanarak resmi dokumantasyondan TBD maddelerin doldurulmasi.
