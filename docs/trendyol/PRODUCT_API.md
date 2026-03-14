# Trendyol Product API — Endpoint Reference

## Ürün Aktarma (createProducts)

**POST** `/integration/product/sellers/{sellerId}/products`

Max 1000 item/request.

### Request Body
```json
{
  "items": [
    {
      "barcode": "barkod-1234",
      "title": "Bebek Takımı Pamuk",
      "productMainId": "1234BT",
      "brandId": 1791,
      "categoryId": 411,
      "quantity": 100,
      "stockCode": "STK-345",
      "dimensionalWeight": 2,
      "description": "Ürün açıklama bilgisi",
      "currencyType": "TRY",
      "listPrice": 250.99,
      "salePrice": 120.99,
      "vatRate": 18,
      "cargoCompanyId": 10,
      "images": [{"url": "https://example.com/image.jpg"}],
      "attributes": [
        {"attributeId": 338, "attributeValueId": 6980},
        {"attributeId": 47, "customAttributeValue": "PUDRA"}
      ],
      "shipmentAddressId": 0,
      "returningAddressId": 0,
      "deliveryDuration": 2,
      "deliveryOption": {
        "deliveryDuration": 1,
        "fastDeliveryType": "SAME_DAY_SHIPPING"
      }
    }
  ]
}
```

### Zorunlu Alanlar
| Alan | Tip | Max | Not |
|------|-----|-----|-----|
| barcode | string | 40 | Sadece `.` `-` `_` özel karakter |
| title | string | 100 | |
| productMainId | string | 40 | Varyantlar için aynı |
| brandId | int | — | getBrands'den |
| categoryId | int | — | getCategoryTree'den |
| quantity | int | — | Stok adedi |
| stockCode | string | 100 | Benzersiz |
| dimensionalWeight | number | — | Desi |
| description | string | 30000 | HTML destekli |
| currencyType | string | — | "TRY" |
| listPrice | number | — | ≥ salePrice |
| salePrice | number | — | |
| vatRate | int | — | 0, 1, 10, 20 |
| cargoCompanyId | int | — | getProviders'dan |
| images | List | 8 | HTTPS zorunlu |
| attributes | List | — | Kategori özellikleri |

### Opsiyonel Alanlar
- `deliveryDuration` (int) — teslimat süresi gün
- `deliveryOption` — hızlı teslimat (SAME_DAY_SHIPPING, FAST_DELIVERY)
- `shipmentAddressId` (int)
- `returningAddressId` (int)
- `lotNumber` (string, max 100)

### Response
```json
{ "batchRequestId": "c0bd29e1-003d-455a-9d74-..." }
```

---

## Ürün Güncelleme (updateProduct)

**PUT** `/integration/product/sellers/{sellerId}/products`

Aynı request body yapısı. **Onaylanmış ürünlerde güncellenemez:** barcode, productMainId, brandId, categoryId, slicer/varianter özellikler.

Max 1000 item/request.

---

## Stok ve Fiyat Güncelleme (updatePriceAndInventory)

**POST** `/integration/inventory/sellers/{sellerId}/products/price-and-inventory`

**Limitsiz** (rate limit yok). Max 1000 SKU/request.

```json
{
  "items": [
    {
      "barcode": "8680000000",
      "quantity": 100,
      "salePrice": 112.85,
      "listPrice": 113.85
    }
  ]
}
```

Kısıtlamalar:
- Max 20.000 adet/ürün
- Aynı istek 15 dakika içinde tekrarlanamaz

---

## Ürün Silme

**DELETE** `/integration/product/sellers/{sellerId}/products`

```json
{ "items": [{"barcode": "test123"}, {"barcode": "test456"}] }
```

Silinebilir: onay bekleyenler + 1 günden fazla arşivlenmiş ürünler.

---

## Batch Request Sorgulama (getBatchRequestResult)

**GET** `/integration/product/sellers/{sellerId}/products/batch-requests/{batchRequestId}`

### Response
```json
{
  "batchRequestId": "string",
  "status": "COMPLETED",
  "items": [
    {
      "requestItem": {},
      "status": "SUCCESS",
      "failureReasons": []
    }
  ],
  "creationDate": 1736154265337,
  "lastModification": 1736154266924,
  "sourceType": "API",
  "itemCount": 1,
  "failedItemCount": 0,
  "batchRequestType": "ProductV2OnBoarding"
}
```

**Batch status:** `COMPLETED`, `IN_PROGRESS`
**Item status:** `SUCCESS`, `FAILURE` (+ `failureReasons` array)
**Retention:** 4 saat sorgulanabilir.
**batchRequestType:** `ProductV2OnBoarding`, `ProductV2Update`, `ProductInventoryUpdate`, `ProductArchiveUpdate`, `ProductDeletion`

---

## Ürün Filtreleme (filterProducts)

**GET** `/integration/product/sellers/{sellerId}/products`

### Query Parametreleri
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| approved | bool | Onay durumu |
| barcode | string | Barkod ile ara |
| startDate | long | Timestamp sonrası |
| endDate | long | Timestamp öncesi |
| page | int | Sayfa (max 2500) |
| size | int | Sayfa boyutu |
| dateQueryType | string | `CREATED_DATE` veya `LAST_MODIFIED_DATE` |
| stockCode | string | Stok kodu |
| archived | bool | Arşivlenmiş |
| productMainId | string | Ana ürün kodu |
| onSale | bool | Satışta mı |
| rejected | bool | Reddedilmiş |
| brandIds | array | Marka ID'leri |

### Response
```json
{
  "totalElements": 42078,
  "totalPages": 4208,
  "page": 0,
  "size": 10,
  "content": [
    {
      "id": "string",
      "approved": true,
      "barcode": "string",
      "title": "string",
      "productMainId": "string",
      "brandId": 1791,
      "categoryName": "string",
      "quantity": 100,
      "listPrice": 250.99,
      "salePrice": 120.99,
      "vatRate": 18,
      "stockCode": "string",
      "images": [{"url": "string"}],
      "attributes": [
        {"attributeId": 1, "attributeName": "Renk", "attributeValueId": 2, "attributeValue": "Mavi"}
      ],
      "onsale": true,
      "locked": false,
      "productUrl": "string"
    }
  ]
}
```
