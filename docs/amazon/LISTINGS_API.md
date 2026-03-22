# Amazon Listings Items API (v2021-08-01)

## Endpoint'ler

### putListingsItem — Ürün Oluşturma/Güncelleme
**PUT** `/listings/2021-08-01/items/{sellerId}/{sku}`

Yeni listing oluşturur veya mevcut listing'i tamamen günceller.

**Query:** `marketplaceIds` (zorunlu)

**Body:**
```json
{
  "productType": "SHOES",
  "requirements": "LISTING",
  "attributes": {
    "item_name": [{"value": "Nike Air Max", "marketplace_id": "A33AVAJ2PDY3EV"}],
    "brand": [{"value": "Nike"}],
    "externally_assigned_product_identifier": [{"type": "ean", "value": "1234567890123"}],
    "main_product_image_locator": [{"media_location": "https://..."}],
    "purchasable_offer": [{"currency": "TRY", "our_price": [{"schedule": [{"value_with_tax": 299.99}]}]}],
    "fulfillment_availability": [{"fulfillment_channel_code": "DEFAULT", "quantity": 50}]
  }
}
```

**Yanıt:** `ACCEPTED` veya `INVALID`
```json
{
  "sku": "MY-SKU-001",
  "status": "ACCEPTED",
  "submissionId": "sub-123",
  "issues": []
}
```

### patchListingsItem — Kısmi Güncelleme
**PATCH** `/listings/2021-08-01/items/{sellerId}/{sku}`

### getListingsItem — Listing Sorgulama
**GET** `/listings/2021-08-01/items/{sellerId}/{sku}`

### deleteListingsItem — Listing Silme
**DELETE** `/listings/2021-08-01/items/{sellerId}/{sku}`

## Product Type Definitions API (v2020-09-01)

### searchDefinitionsProductTypes
**GET** `/definitions/2020-09-01/productTypes`
- `keywords` — Arama
- `marketplaceIds` — Zorunlu

### getDefinitionsProductType
**GET** `/definitions/2020-09-01/productTypes/{productType}`
- `requirements` — `LISTING`, `LISTING_PRODUCT_ONLY`, `LISTING_OFFER_ONLY`

JSON Schema döndürür — hangi attribute'ların zorunlu/opsiyonel olduğunu belirler.

## Ürün Oluşturma Workflow

1. `searchDefinitionsProductTypes` → Doğru product type'ı bul
2. `getDefinitionsProductType` → JSON Schema al (zorunlu alanlar)
3. `putListingsItem` → Listing oluştur
4. Status: `ACCEPTED` → Sonraki adım | `INVALID` → Hataları düzelt
5. `getListingsItem` ile durumu kontrol et

## Toplu İşlem — JSON_LISTINGS_FEED
Feeds API ile `JSON_LISTINGS_FEED` feed type'ı kullanarak toplu listing oluşturma/güncelleme yapılabilir.
