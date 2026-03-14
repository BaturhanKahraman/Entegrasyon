# Trendyol Product API V2 — Endpoint Reference

V2, V1'den ayrı endpoint'ler kullanır. Onaysız ve onaylı ürünler için ayrı güncelleme/filtreleme endpoint'leri vardır. Kategori özellik değerleri ayrı bir endpoint'ten sayfalanarak çekilir.

## Akış Diyagramı (Özet)

```
getBrands → brandId kontrol
getCategoryTree → categoryId al
getCategoryAttributes → attribute listesi
getCategoryAttributeValues → attributeValueId'ler
  ↓
postProductCreate (V2) → batchRequestId
  ↓
getBatchRequestResult → items[].status == "SUCCESS"?
  EVET → filterProducts (Base) → approved == true?
    EVET → filterProducts (Approved) → onSale == true?
      EVET → Ürün yayında ✓
      HAYIR → stok/fiyat sıfır mı? → updatePriceAndInventory
    HAYIR → rejectReasonDetails boş mu?
      HAYIR → updateProducts (Unapproved) ile düzelt
      EVET → Onay süreci devam ediyor
  HAYIR → failureReasons kontrol et → düzelt → tekrar gönder
```

---

## Referans Endpoint'ler (V1 ile aynı, değişmemiş)

| Endpoint | URL |
|----------|-----|
| getBrands | `GET /integration/product/brands` |
| getCategoryTree | `GET /integration/product/product-categories` |
| getProviders | `GET /integration/product/cargo-companies` |

---

## Kategori Özellik Listesi (V2)

**GET** `/integration/product/categories/{categoryId}/attributes`

```json
{
  "id": 14609,
  "name": "Muay Thai Kaskı",
  "categoryAttributes": [
    {
      "allowCustom": true,
      "attribute": {"id": 338, "name": "Renk"},
      "categoryId": 14609,
      "required": true,
      "varianter": false,
      "slicer": true,
      "allowMultipleAttributeValues": false
    }
  ]
}
```

**Yeni alan:** `allowMultipleAttributeValues` — true ise attribute birden fazla değer kabul eder.

---

## Kategori Özellik Değerleri (V2)

**GET** `/integration/product/categories/{categoryId}/attributes/{attributeId}/values`

| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| size | int | Max 1000 |
| page | int | Max 1000 |
| attributeValueId | int | ID ile filtrele |
| attributeValue | string | İsim ile filtrele |

```json
{
  "totalElements": 150,
  "totalPages": 15,
  "page": 0,
  "size": 10,
  "content": [
    {"attributeValueId": 4872, "attributeValue": "Tek Ebat"},
    {"attributeValueId": 4873, "attributeValue": "S"}
  ]
}
```

---

## Ürün Yaratma (V2)

**POST** `/integration/product/sellers/{sellerId}/v2/products`

V1 ile aynı request body. Farklar:
- `cargoCompanyId` **kaldırıldı** — V2'de gerekli değil
- `currencyType` **kaldırıldı** — TRY varsayılan
- Kategori özellik değerleri V2 endpoint'lerinden alınmalı

Max 1000 item/request. Response: `batchRequestId`.

---

## Ürün Güncelleme — Onaysız Ürünler (V2)

**POST** `/integration/product/sellers/{sellerId}/products/unapproved-bulk-update`

Tüm alanlar güncellenebilir (barcode, brandId, categoryId dahil — henüz onaylanmadı).

```json
{
  "items": [
    {
      "barcode": "string",
      "title": "string",
      "description": "string",
      "productMainId": "string",
      "brandId": 1,
      "categoryId": 1,
      "stockCode": "string",
      "dimensionalWeight": 0,
      "vatRate": 0,
      "images": [{"url": "string"}],
      "attributes": [{"attributeId": 1, "attributeValueId": 1}],
      "shipmentAddressId": 0,
      "returningAddressId": 0
    }
  ]
}
```

---

## Ürün Güncelleme — Onaylı Ürünler (V2)

**POST** `/integration/product/sellers/{sellerId}/products/content-bulk-update`

Sadece sınırlı alanlar güncellenebilir. `contentId` zorunlu (filterProducts Approved'dan alınır).

```json
{
  "items": [
    {
      "contentId": 9510902,
      "title": "string",
      "description": "string",
      "images": [{"url": "string"}],
      "attributes": [{"attributeId": 1, "attributeValueId": 1}]
    }
  ]
}
```

**Güncellenemez:** barcode, productMainId, brandId, categoryId, slicer/varianter özellikler.
**Önemli:** Attribute güncellerken TÜM attribute'ları gönderin (partial update yok).

---

## Stok ve Fiyat Güncelleme (V1 ile aynı)

**POST** `/integration/inventory/sellers/{sellerId}/products/price-and-inventory`

```json
{"items": [{"barcode": "string", "quantity": 100, "salePrice": 112.85, "listPrice": 113.85}]}
```

---

## Batch Request Sorgulama (V1 ile aynı)

**GET** `/integration/product/sellers/{sellerId}/products/batch-requests/{batchRequestId}`

---

## Ürün Silme (V1 ile aynı)

**DELETE** `/integration/product/sellers/{sellerId}/products`

---

## Ürün Filtreleme — Temel Bilgiler (V2)

**GET** `/integration/product/sellers/{sellerId}/product/{barcode}`

Tek barkod ile ürün durumu sorgu:
```json
{
  "barcode": "smoketest-250049",
  "approved": true,
  "approvedDate": 1763622556000,
  "archived": false,
  "listingId": "a089a30ed1632032913b28099e49d948",
  "contentId": 9511264
}
```

---

## Ürün Filtreleme — Onaysız Ürünler (V2)

**GET** `/integration/product/sellers/{sellerId}/products/unapproved`

| Parametre | Tip | Not |
|-----------|-----|-----|
| barcode | string | Tek barkod |
| startDate/endDate | long | Timestamp |
| dateQueryType | string | `CREATED_DATE`, `LAST_MODIFIED_DATE` |
| size | int | Max 1000 |
| status | string | `rejected` veya `pendingApproval` |
| nextPageToken | string | 10.000+ ürün için |
| stockCode, productMainId, brandIds | — | Filtreler |

Response: `rejectReasonDetails` ile red sebepleri görüntülenir.

---

## Ürün Filtreleme — Onaylı Ürünler (V2)

**GET** `/integration/product/sellers/{sellerId}/products/approved`

| Parametre | Tip | Not |
|-----------|-----|-----|
| barcode | string | Tek barkod |
| startDate/endDate | long | Timestamp |
| dateQueryType | string | `VARIANT_CREATED_DATE`, `VARIANT_MODIFIED_DATE`, `CONTENT_MODIFIED_DATE` |
| size | int | Max 100 |
| status | string | `archived`, `blacklisted`, `locked`, `onSale` |
| nextPageToken | string | 10.000+ ürün için |

Response: `contentId`, `variants[]` (her varyant ayrı: barcode, stockCode, price, stock, onSale, locked, archived, blacklisted).

---

## Ürün Arşivleme (archiveProducts)

**PUT** `/integration/product/sellers/{sellerId}/products/archive-state`

```json
{
  "items": [
    {"barcode": "barkod-1234", "archived": true},
    {"barcode": "barkod-5678", "archived": false}
  ]
}
```

`archived: true` → arşive al, `false` → arşivden çıkar.

---

## Buybox Kontrol

**POST** `/integration/product/sellers/{sellerId}/products/buybox-information`

```json
{"barcodes": ["1111111111111", "2222222222"]}
```

Max 10 barkod/request, 1000 req/min.

```json
{
  "buyboxInfo": [
    {"barcode": "1111111111111", "buyboxOrder": 1, "buyboxPrice": 600, "hasMultipleSeller": false}
  ]
}
```

`buyboxOrder` = 1 → birinci sıradasın (en iyi pozisyon).

---

## Ürün Kilit Kaldırma (unlock)

**PUT** `/integration/product/sellers/{sellerId}/products/unlock`

```json
{"items": [{"barcode": "barcode1"}, {"barcode": "barcode2"}]}
```

Kilitli (`locked: true`) ürünlerin kilidini kaldırır. Kilit nedeni genellikle stok/fiyat tutarsızlığı.
