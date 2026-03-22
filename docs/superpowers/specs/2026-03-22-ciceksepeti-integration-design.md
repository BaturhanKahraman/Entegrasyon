# Çiçeksepeti Marketplace Entegrasyonu — Design Spec

**Tarih:** 2026-03-22
**MarketPlaceId:** 7
**Yaklaşım:** Pazarama pattern klonu (multi-tenant ready)

---

## 1. Genel Bakış

Entegrasyon projesine 6. pazaryeri olarak Çiçeksepeti ekleniyor. REST API + API Key (`x-api-key` header) auth kullanır. Yazma işlemleri asenkron (batch) çalışır — `batchId` döner, poll ile takip edilir.

**Base URL'ler:**
- Production: `https://apis.ciceksepeti.com`
- Sandbox: `https://sandbox-apis.ciceksepeti.com`

**Kapsam:** Ürün yönetimi (CRUD + batch), stok/fiyat güncelleme, sipariş listeleme, kargo süreçleri (CS + kendi), fatura gönderimi, iade yönetimi, soru-cevap yönetimi, işçilik bedeli, dijital kod gönderimi.

---

## 2. Mimari & Dosya Yapısı

### Business Layer

```
Business/Concrete/Ciceksepeti/
├── CiceksepetiApiClient.cs            # HTTP client, x-api-key auth, multi-tenant cache
├── MockCiceksepetiApiClient.cs        # Dev/test mock
├── CiceksepetiCategoryService.cs      # Kategori & özellik API
├── CiceksepetiProductService.cs       # Ürün CRUD + batch status
├── CiceksepetiProductMapper.cs        # Product entity → CS DTO
├── CiceksepetiMappingValidator.cs     # Marketplace mapping kontrolü
├── CiceksepetiStockPriceService.cs    # Stok/fiyat batch güncelleme
├── CiceksepetiOrderService.cs         # Sipariş + tüm kargo işlemleri
├── CiceksepetiInvoiceService.cs       # Fatura PDF gönderimi
├── CiceksepetiReturnService.cs        # İade listeleme/onay/red
├── CiceksepetiQnAService.cs           # Soru-cevap yönetimi
├── CiceksepetiResponseModels.cs       # API response DTO'ları
└── CiceksepetiRequestModels.cs        # API request DTO'ları
```

### Abstract Interfaces

```
Business/Abstract/
├── ICiceksepetiApiClient.cs
├── ICiceksepetiCategoryService.cs
├── ICiceksepetiProductService.cs
├── ICiceksepetiStockPriceService.cs
├── ICiceksepetiOrderService.cs
├── ICiceksepetiInvoiceService.cs
├── ICiceksepetiReturnService.cs
└── ICiceksepetiQnAService.cs
```

### Background Services

```
Business/BackgroundServices/
├── CiceksepetiBatchStatusPollingService.cs
├── CiceksepetiOrderPollingService.cs
└── CiceksepetiStockPriceSyncService.cs
```

### Category Importer

```
Business/Concrete/Import/
└── CiceksepetiCategoryImporter.cs     # extends BaseCategoryImporterService
```

---

## 3. CiceksepetiApiClient

**Şablon:** `PazaramaApiClient.cs`

**Auth:** `x-api-key` HTTP header (MarketPlace.ApiKey alanından okunur)

**Multi-tenant:**
- `ConcurrentDictionary<int, (string ApiKey, string BaseUrl)>` credential cache
- Per-marketplace `SemaphoreSlim` rate limit izolasyonu
- `IDbContextFactory<IntegrationDbContext>` ile credential refresh

**Rate limit stratejisi:** Per-endpoint minimum interval kontrolü. Çiçeksepeti "aynı body" ve "farklı body" için ayrı rate limit uyguluyor — client tarafında sadece "farklı body" limiti uygulanacak (aynı body kontrolü scope dışı).

**Özel case:** Fatura endpoint'i (`/Branch/SendInvoiceMail`) farklı path prefix kullanır — ApiClient'ta `SendRawAsync` veya path override desteği.

**Metodlar:**
- `GetAsync<T>(string path, CancellationToken ct)`
- `PostAsync<T>(string path, object body, CancellationToken ct)`
- `PutAsync<T>(string path, object body, CancellationToken ct)`
- `SendRawAsync<T>(string fullPath, HttpMethod method, object? body, CancellationToken ct)` — fatura için

---

## 4. Ürün Yönetimi

### CiceksepetiCategoryService
- `GetCategoriesAsync()` → GET /api/v1/Categories (recursive tree)
- `GetCategoryAttributesAsync(int categoryId)` → GET /api/v1/Categories/{categoryId}/attributes

### CiceksepetiCategoryImporter (extends BaseCategoryImporterService)
- Tree → flat list dönüşümü, leaf kategorileri MarketPlaceId=7 ile kaydet
- Attribute import: `type="Variant Ozellik"` → IsVarianter=true, `type="Urun Ozellik"` → IsVarianter=false

### CiceksepetiProductMapper
- Product entity → `CiceksepetiCreateProductRequest` dönüşümü
- mainProductCode (ana ürün kodu), stockCode (varyant kodu)
- Image URL'leri, attribute mapping (id + ValueId + TextLength)
- deliveryType, deliveryMessageType mapping

### CiceksepetiMappingValidator
- Kategori mapped? (CategoryMarketPlaceMatches)
- Required attribute'lar mapped? (CategoryAttributeMarketPlaceMatches)
- Image format/boyut kontrolü (500x500–2000x2000, JPG/PNG)

### CiceksepetiProductService
- `PublishProductAsync(Guid productId)` → validate → map → POST /api/v1/Products → batchId
- `UpdateProductAsync(Guid productId)` → PUT /api/v1/Products (isActive zorunlu!)
- `CheckBatchStatusAsync(string batchId)` → GET batch-status/{batchId}
- `GetProductsAsync(filters)` → GET /api/v1/Products (pageSize max 60)

**Dikkat:** Update'te `operatorContacts`/`safetyInfo` gönderilmezse mevcut veri silinir!

### CiceksepetiStockPriceService
- `UpdateStockAndPriceAsync(items)` → PUT /api/v1/Products/price-and-stock
- Max 200 ürün/batch
- İş kuralları: fiyat %50 düşüş limiti, listPrice salesPrice ile birlikte

---

## 5. Sipariş & Kargo

### CiceksepetiOrderService
- `GetOrdersAsync(filters)` → POST GetOrders (date range max 2 hafta, page 0-based)
- `ReadyForCargoWithCsAsync(orderItemIds)` → PUT readyforcargowithcsintegration
- `UpdateStatusWithOwnCargoAsync(items)` → PUT statusupdatewithsupplierintegration
- `ChangeCargoCompanyAsync(items)` → PUT CargoCompany
- `SendCargoMeasurementAsync(items)` → POST CargoMeasurement
- `SendDigitalCodeAsync(items)` → PUT digital-order-status-update
- `UpdateLaborCostAsync(items)` → PUT UpdateLaborCost

### CiceksepetiInvoiceService
- `SendInvoiceAsync(orderItemId, pdfBase64OrUrl)` → POST /Branch/SendInvoiceMail
- Farklı path prefix — ApiClient.SendRawAsync kullanır

---

## 6. İade & Soru-Cevap

### CiceksepetiReturnService
- `GetReturnOrdersAsync(filters)` → POST getcanceledorders (max 1 ay)
- `ConfirmReturnReceivedAsync(orderItemIds)` → POST refundprocessstartreceivedprocess (status 20→22)
- `EvaluateReturnAsync(orderItemId, process)` → POST cancelevaluation (1=onayla, 3=reddet)

### CiceksepetiQnAService
- `GetQuestionsAsync(filters)` → GET /api/v1/sellerquestions (max 32 gün, page 1-based)
- `AnswerQuestionAsync(id, answer, branchActionId)` → PUT /api/v1/sellerquestions/{id}
- `GetActionsAsync()` → GET /api/v1/sellerquestions/actions

---

## 7. Background Services

### CiceksepetiBatchStatusPollingService
- Bekleyen batch'leri DB'den çek → poll → sonuçları güncelle
- Multi-tenant: tenant başına izole polling
- Poll interval: konfigüre edilebilir (default 30 sn)

### CiceksepetiOrderPollingService
- Periyodik yeni sipariş çekme → DB'ye kaydet
- `ConcurrentDictionary<int, DateTime>` last poll timestamp (multi-tenant)
- Date range: son 2 saatlik pencere ile poll

### CiceksepetiStockPriceSyncService
- EventChannel ile tetikleme veya periyodik sync
- Değişen stok/fiyatları tespit → batch güncelleme

---

## 8. DI & Configuration

### ApplicationDependencyExtension.cs

```csharp
public static IServiceCollection AddCiceksepetiServices(
    this IServiceCollection services, IConfiguration configuration)
{
    var useMock = configuration.GetValue<bool>("Ciceksepeti:UseMock", true);

    if (useMock)
        services.AddSingleton<ICiceksepetiApiClient, MockCiceksepetiApiClient>();
    else
        services.AddSingleton<ICiceksepetiApiClient, CiceksepetiApiClient>();

    services.AddScoped<ICiceksepetiCategoryService, CiceksepetiCategoryService>();
    services.AddScoped<ICiceksepetiProductService, CiceksepetiProductService>();
    services.AddScoped<ICiceksepetiStockPriceService, CiceksepetiStockPriceService>();
    services.AddScoped<ICiceksepetiOrderService, CiceksepetiOrderService>();
    services.AddScoped<ICiceksepetiInvoiceService, CiceksepetiInvoiceService>();
    services.AddScoped<ICiceksepetiReturnService, CiceksepetiReturnService>();
    services.AddScoped<ICiceksepetiQnAService, CiceksepetiQnAService>();
    services.AddScoped<CiceksepetiProductMapper>();
    services.AddScoped<CiceksepetiMappingValidator>();

    return services;
}
```

### MarketPlaceConstants.cs
```csharp
public const int CiceksepetiMarketPlaceId = 7;
```

### Seed Data
MarketPlace tablosuna: `Id=7, Name="Çiçeksepeti", BaseUrl="https://apis.ciceksepeti.com"`

---

## 9. Multi-Tenant Tasarım

- **CiceksepetiApiClient:** Singleton, `ConcurrentDictionary<int, (string, string)>` credential cache
- **Rate limit:** Per-marketplace `SemaphoreSlim` (tenant izolasyonu)
- **Background services:** `IServiceScopeFactory` + `IDbContextFactory` ile scoped DB erişimi
- **Order polling:** `ConcurrentDictionary<int, DateTime>` last poll timestamp
- **Batch polling:** Tenant bazlı izole batch tracking

---

## 10. Test Stratejisi (TDD-First)

### Unit Tests
- `CiceksepetiApiClientTests` — credential resolve, header injection, error handling, rate limit
- `CiceksepetiProductMapperTests` — entity → DTO mapping, edge cases
- `CiceksepetiMappingValidatorTests` — mapping completeness, missing attribute detection
- `CiceksepetiProductServiceTests` — publish/update/batch with mock ApiClient
- `CiceksepetiOrderServiceTests` — order fetch, all cargo operations
- `CiceksepetiStockPriceServiceTests` — batch update, business rules (price limits)
- `CiceksepetiReturnServiceTests` — return list/confirm/evaluate
- `CiceksepetiQnAServiceTests` — question fetch/answer

### E2E Tests
- Sandbox API'ye gerçek HTTP request'ler
- Kategori listeleme, ürün CRUD, sipariş listeleme akışları

---

## 11. Implementation Fazları

| Faz | Kapsam | Bağımlılık |
|-----|--------|------------|
| 0 | API dökümanları → develop commit | — |
| 1 | Altyapı: ApiClient, Models, Constants, DI, Seed | Faz 0 |
| 2 | Kategori & Özellik Import | Faz 1 |
| 3 | Ürün: Mapper, Validator, ProductService, StockPriceService | Faz 2 |
| 4 | Sipariş: OrderService, InvoiceService, Kargo | Faz 1 |
| 5 | İade & Soru-Cevap | Faz 1 |
| 6 | Background Services | Faz 3, 4 |
| 7 | UI Tab (Blazor) | Faz 1-6 |

---

## 12. API Dökümanları

Detaylı API dökümanları: `docs/ciceksepeti/`

| Dosya | Kapsam |
|-------|--------|
| BASE_KNOWLEDGE.md | Auth, URL'ler, rate limits, batch pattern |
| CATEGORY_API.md | Kategori & özellik listeleme |
| PRODUCT_API.md | Ürün CRUD + batch status |
| STOCK_PRICE_API.md | Stok/fiyat güncelleme |
| ORDER_API.md | Sipariş listeleme |
| CARGO_API.md | CS kargo + kendi kargo + ek işlemler |
| INVOICE_API.md | Fatura gönderimi |
| RETURN_API.md | İade yönetimi |
| QNA_API.md | Soru-cevap |
| LABOR_COST_API.md | İşçilik bedeli |
