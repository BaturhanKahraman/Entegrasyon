# Trendyol WireMock — Tam Mock Tasarımı

**Tarih:** 2026-06-12  
**Kapsam:** Trendyol entegrasyonunun dev ortamında uçtan uca çalışması  
**Dışarıda:** e-Fatura (TrendyolEFaturaApiClient) — ayrı sprint

---

## 1. Problem

Dev ortamında (`IntegrationDb_Dev`) Trendyol çağrıları WireMock'a düşüyor fakat 4 mapping'in URL pattern'ı stale (eski `/suppliers/{id}/` prefix). Gerçek kod `integration/product/sellers/{id}/...` kullanıyor. Ayrıca `SellerId` DB'de null → URL'ler `sellers//v2/products` üretiyor, hiçbir pattern'a düşmüyor. Sonuç: ürün yayınlama, sipariş çekme, stok güncelleme tümü 404.

---

## 2. Çözüm Mimarisi

### 2.1 DevWireMockSeeder Değişikliği

`Application/Entegrasyon.MVC/Infrastructure/DevMode/DevWireMockSeeder.cs`

Mevcut: sadece `BaseUrl` güncellenir.  
Yeni: `WireMockUrl` set edilirken `SellerId` de `DevMode:TrendyolSellerId` config'inden okunarak set edilir.

```csharp
var testSellerId = configuration["DevMode:TrendyolSellerId"];
if (!string.IsNullOrEmpty(testSellerId) && mp.Name == "Trendyol")
    mp.SellerId = testSellerId;
```

`compose.yaml` (server `/opt/stacks/entegrasyon-dev/`) env bölümüne eklenir:
```yaml
- DevMode__TrendyolSellerId=123456
```

`appsettings.Development.json` `DevMode` bölümüne eklenir (lokal geliştirici için):
```json
"TrendyolSellerId": "123456"
```

### 2.2 Mapping Klasör Yapısı (yeniden organize)

```
wiremock/
  mappings/trendyol/
    product/
      create.json
      batch-inprogress.json
      batch-completed.json
      update-unapproved.json
      update-content.json
      delete.json
      _400-missing-items.json
    order/
      get-orders-created.json
      get-orders-invoiced.json
      get-orders-shipped.json
      update-unsupplied.json
      update-status.json
      get-shipping-label.json
    invoice/
      create-link.json
      delete-link.json
      upload-pdf.json
    stock/
      update.json
      _400-invalid.json
    catalog/
      brands-by-name.json        (mevcut, taşınır)
      category-attributes.json
      brands-page-first.json
      brands-page-empty.json
    seller/
      addresses.json
    _global-401.json
    _global-429.json
  __files/trendyol/
    product/
      batch-response.json
      batch-inprogress.json
      batch-completed.json
    order/
      orders-created.json
      orders-invoiced.json
      orders-shipped.json
      shipping-label.json
    catalog/
      brands-by-name.json
      category-attributes.json
      brands-page-first.json
      brands-page-empty.json
    seller/
      addresses.json
```

---

## 3. URL Pattern'ları (Düzeltme + Yeni)

Tüm seller-specific endpoint'ler `[0-9]+` ile sellerId matchler.

| Mapping | Method | urlPathPattern |
|---|---|---|
| product/create | POST | `/integration/product/sellers/[0-9]+/v2/products` |
| product/batch | GET | `/integration/product/sellers/[0-9]+/products/batch-requests/[^/]+` |
| product/update-unapproved | PUT | `/integration/product/sellers/[0-9]+/v2/products/unapproved-bulk-update` |
| product/update-content | PUT | `/integration/product/sellers/[0-9]+/v2/products/content-bulk-update` |
| product/delete | DELETE | `/integration/product/sellers/[0-9]+/products` |
| order/get-orders | GET | `/integration/order/sellers/[0-9]+/orders` |
| order/update-unsupplied | PUT | `/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+/unsupplied` |
| order/update-status | PUT | `/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+` |
| order/shipping-label | GET | `/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+/shipping-label` |
| invoice/create-link | POST | `/integration/order/sellers/[0-9]+/seller-invoice-links` |
| invoice/delete-link | DELETE | `/integration/order/sellers/[0-9]+/seller-invoice-links/[^/]+/customers/[0-9]+` |
| invoice/upload-pdf | POST | `/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+/invoice` |
| stock/update | POST | `/integration/inventory/sellers/[0-9]+/products/price-and-inventory` |
| seller/addresses | GET | `/integration/sellers/[0-9]+/addresses` |
| catalog/brands-by-name | GET | `/product/brands/by-name` |
| catalog/category-attrs | GET | `/product/product-categories/[0-9]+/attributes` |
| catalog/brands-first | GET | `/product/brands` (queryParam: page=-1) |
| catalog/brands-empty | GET | `/product/brands` (catch-all, priority düşük) |

---

## 4. WireMock Scenario'ları

### 4.1 `batch-lifecycle`

Ürün yayınlama polling döngüsünü simüle eder. `TrendyolBatchStatusPollingService` 60 saniyede bir poll eder.

```
Started (initial)
  GET .../batch-requests/{id}
    → status: "IN_PROGRESS", itemCount: 1, failedItemCount: 0
    → newScenarioState: "batch-polled"

batch-polled
  GET .../batch-requests/{id}
    → status: "COMPLETED", items: [{status:"SUCCESS"}], failedItemCount: 0
    → newScenarioState: "Started"   ← otomatik reset
```

### 4.2 `order-lifecycle`

Sipariş akışının stateful simülasyonu. Başlangıç state: `order-created`.

```
order-created (initial)
  GET .../orders → [{status:"Created", shipmentPackageId:99001, ...}]
  POST .../seller-invoice-links → 200 → newState: "order-invoiced"

order-invoiced
  GET .../orders → [{status:"Picking"}]
  GET .../shipment-packages/99001/shipping-label → 200 (PDF placeholder)
  PUT .../shipment-packages/99001 → 200 → newState: "order-shipped"

order-shipped
  GET .../orders → [{status:"Shipped", cargoTrackingNumber:"TY-TRACK-001"}]
```

GET orders için 3 ayrı mapping dosyası, her biri `requiredScenarioState` ile ayrışır.  
DELETE invoice-link ve PUT unsupplied scenario-bağımsız (her zaman 200).

---

## 5. Response Body Tasarımı

### 5.1 Batch Response (POST product create)
```json
{"batchRequestId": "batch-test-001"}
```

### 5.2 Batch Status — IN_PROGRESS
```json
{
  "batchRequestId": "batch-test-001",
  "status": "IN_PROGRESS",
  "items": [],
  "itemCount": 1,
  "failedItemCount": 0,
  "batchRequestType": "ITEM",
  "creationDate": 1749729600000,
  "lastModification": 1749729601000
}
```

### 5.3 Batch Status — COMPLETED
```json
{
  "batchRequestId": "batch-test-001",
  "status": "COMPLETED",
  "items": [{"requestItem": null, "status": "SUCCESS", "failureReasons": []}],
  "itemCount": 1,
  "failedItemCount": 0,
  "batchRequestType": "ITEM",
  "creationDate": 1749729600000,
  "lastModification": 1749729660000
}
```

### 5.4 Orders — Created State
```json
{
  "page": 0, "size": 50, "totalPages": 1, "totalElements": 1,
  "content": [{
    "shipmentPackageId": 99001,
    "orderNumber": "TY-TEST-001",
    "orderDate": "2026-06-12T10:00:00+03:00",
    "status": "Created",
    "grossAmount": 299.90, "totalDiscount": 0.0, "totalPrice": 299.90,
    "micro": false, "fastDelivery": false,
    "estimatedDeliveryEndDate": "2026-06-15T23:59:59+03:00",
    "cargoProviderInfo": {"cargoProviderName": "Yurtiçi Kargo", "cargoTrackingNumber": null, "cargoTrackingLink": null},
    "customerInfo": {"firstName": "Test", "lastName": "Müşteri", "email": "test@example.com"},
    "shipmentAddress": {"city": "İstanbul", "district": "Kadıköy", "fullAddress": "Test Mah. Test Sk. No:1", "postalCode": "34710", "countryCode": "TR"},
    "invoiceAddress": {"city": "İstanbul", "district": "Kadıköy", "fullAddress": "Test Mah. Test Sk. No:1", "postalCode": "34710", "countryCode": "TR"},
    "lines": [{
      "lineId": 1001, "quantity": 1, "price": 299.90, "discount": 0.0,
      "barcode": "TEST-BARCODE-001", "merchantSku": "TEST-SKU-001",
      "productName": "Test Ürün", "productColor": "Mavi", "productSize": "M",
      "merchantId": 123456, "vatRate": 10
    }]
  }]
}
```
`orders-invoiced.json`: `status` → `"Picking"`, `orders-shipped.json`: `status` → `"Shipped"` + `cargoTrackingNumber` dolu.

### 5.5 Supplier Addresses
```json
{
  "supplierAddresses": [{
    "id": 1, "fullAddress": "Test Depo Mah. Sanayi Sk. No:5",
    "city": "İstanbul", "district": "Pendik", "postalCode": "34890",
    "isDefault": true, "isShipmentAddress": true,
    "isInvoiceAddress": true, "isReturningAddress": false
  }]
}
```

### 5.6 Brand Importer
`brands-page-first.json` (page=-1): `{"brands": [{"id":1,"name":"Nike"},{"id":2,"name":"Adidas"},{"id":3,"name":"Zara"}]}`  
`brands-page-empty.json` (page≠-1): `{"brands": []}`

### 5.7 Category Attributes
`TrendyolCategory` DTO: `id`, `name`, `displayName`, `categoryAttributes[]` (her biri: `attribute.Id`, `attribute.Name`, `attributeValues[]`, `required`, `varianter`, `slicer`, `allowCustom`).

```json
{
  "id": 388, "name": "Çocuk Giyim", "displayName": "Çocuk Giyim",
  "categoryAttributes": [
    {
      "allowCustom": false, "required": true, "varianter": false, "slicer": true,
      "categoryId": 388,
      "attribute": {"id": 348, "name": "Renk"},
      "attributeValues": [{"id": 1001, "name": "Mavi"}, {"id": 1002, "name": "Kırmızı"}]
    },
    {
      "allowCustom": false, "required": true, "varianter": true, "slicer": false,
      "categoryId": 388,
      "attribute": {"id": 338, "name": "Beden"},
      "attributeValues": [{"id": 2001, "name": "2-3 Yaş"}, {"id": 2002, "name": "4-5 Yaş"}]
    }
  ]
}
```

---

## 6. Validation Error Mapping'leri (CLAUDE.md kuralı)

```
POST .../v2/products — body'de items boş/eksik:
  bodyPatterns: [{"matchesJsonPath": "$.items[0]", "absent": true}]
  → 400 {"errors": [{"code":"VALIDATION_ERROR","message":"items is required"}]}

POST .../price-and-inventory — items boş:
  → 400 {"errors": [{"code":"VALIDATION_ERROR","message":"items cannot be empty"}]}

Global 401:
  _global-401.json — priority: 1 (en düşük), tüm /integration/... path'leri yakalar
  Authorization header ABSENT ise 401 döner (bodyPatterns + absent: true).
  RİSK: gelecekte yeni eklenen bir mapping bu catch-all'dan önce 401 yiyebilir.
  Önlem: yeni mapping'e priority: 5+ ver, global mapping priority: 1 kalır.

Global 429:
  _global-429.json — priority: 1, Retry-After: 1 header ekler
```

---

## 7. Değişmeyenler

- WireMock Docker image: `wiremock/wiremock:3.9.2` (aynı kalır)
- `--global-response-templating` flag zaten aktif
- `compose.yaml` network, healthcheck, volume mount yapısı değişmez
- e-Fatura endpoint'leri bu scope dışında

---

## 8. Uygulama Sırası

1. DevWireMockSeeder + `appsettings.Development.json` + `compose.yaml` — SellerId seed
2. Mevcut 4 kırık mapping'i düzelt (URL prefix)
3. `catalog/` klasörünü oluştur, brands-by-name'i taşı, category-attributes + brand importer ekle
4. `seller/addresses.json` ekle
5. `stock/` ekle (update + 400)
6. `product/` klasörünü oluştur — batch scenario dahil
7. `order/` + `invoice/` — order-lifecycle scenario
8. Global 401/429 ekle
9. Server'a deploy (`git push develop` → Gitea runner otomatik deploy eder)
10. Dev container loglarından WireMock hit'lerini doğrula
