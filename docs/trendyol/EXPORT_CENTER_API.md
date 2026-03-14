# Trendyol İhracat Merkezi (Export Center) API — Endpoint Reference

## Genel Bakış

İhracat Merkezi, satıcıların Trendyol'un uluslararası platformlarına ürün listelemesini sağlar. **Mikro ihracattan farklı bir model:**
- Ürünler Trendyol tarafından Türkiye sınırları içinde satın alınır
- Lojistik Trendyol tarafından yönetilir (depodan toplu toplama)
- Satıcıya iade dönmez, komisyon faturası kesilmez
- Onaylı satıcılara özel (başvuru gerekli)

**Base URL:** `/integration/ecgw/`

### Ek HTTP Header'lar (Zorunlu)

Standart Basic Auth'a ek olarak:
| Header | Tip | Açıklama |
|--------|-----|----------|
| `x-clientip` | string (IPv4) | İsteğin gönderildiği IP |
| `x-correlationid` | string (UUID) | Her istekte yeni UUID — log takibi için |
| `x-agentname` | string | Entegratör veya satıcı adı |

---

# Ürün Entegrasyonu

## Ürün Oluşturma (V2)

**POST** `/integration/ecgw/v2/{sellerId}/products`

Ürün **önce Satıcı Paneli'nde** oluşturulmuş olmalı. Export Center'da yeni ürün oluşturulamaz.

```json
{
  "products": [
    {
      "barcode": "string",
      "buyingPrice": 5.0,
      "rrp": 22.0,
      "gtip": "22313170",
      "stock": 100,
      "origin": "Türkiye",
      "categoryId": 123,
      "attributes": [
        {"attributeId": 1, "attributeValueId": 100},
        {"attributeId": 2, "customAttributeValue": "Kırmızı"},
        {"attributeId": 3, "customAttributeValues": {"100": "95", "200": "5"}}
      ]
    }
  ]
}
```

| Alan | Zorunlu | Açıklama |
|------|---------|----------|
| barcode | Evet | Panel'deki mevcut ürün barkodu |
| buyingPrice | Evet | Trendyol'un satın alma fiyatı (KDV dahil) |
| rrp | Hayır | Önerilen perakende fiyat (≥ buyingPrice) |
| gtip | Evet | Gümrük tarife kodu (4-12 hane) |
| stock | Evet | Stok adedi |
| origin | Evet | Menşei ülke (lookup'tan alınır) |
| categoryId | Evet | Kategori (barkod lookup veya kategori listesinden) |
| attributes | Evet | Zorunlu özellikler (kategoriye göre değişir) |

**Kısıtlama:** Fiyat güncellemesi **günde 1 kez** barkod başına.

Response: `{"batchId": "string"}`

---

## Ürün Listeleme

**GET** `/integration/ecgw/v2/{sellerId}/products`

| Parametre | Açıklama |
|-----------|----------|
| size | Max 100 |
| pageKey | İlk istekte boş, sonrakilerde response header `x-paging-key` değeri |
| barcodes | Barkod ile filtre (çoklu) |

Response: barcode, sellerBarcode, buyingPrice, rrpPrice, stock, origin, gtip, categoryId, attributes.

---

## Stok Güncelleme

**POST** `/integration/ecgw/v1/{sellerId}/stocks`

```json
{"stocks": [{"barcode": "brcd123", "stock": 223}]}
```

Max 5000 ürün/request. Response: `batchId`.

---

## Fiyat Güncelleme

**POST** `/integration/ecgw/v1/{sellerId}/prices`

```json
{"priceInfos": [{"barcode": "brcd123", "rrp": 22.00, "buyingPrice": 5.0}]}
```

`rrp` opsiyonel (verilirse ≥ buyingPrice). Response: `batchId`.

---

## Batch Sonuç Sorgulama

**GET** `/integration/ecgw/v1/{sellerId}/check-status?batchId={batchId}`

```json
{
  "batchId": "57a7229a-...",
  "batchType": "ProductCreate",
  "status": "COMPLETED",
  "itemCount": 1,
  "items": [
    {
      "requestItem": {"product": {"barcode": "BRCD001", "price": 12.99, "...": "..."}},
      "status": "SUCCESS",
      "failureReasons": []
    }
  ]
}
```

**Batch ID'ler 24 saat sonra silinir** (marketplace'de 4 saat).

---

# Lookup Servisleri

| Endpoint | URL | Açıklama |
|----------|-----|----------|
| Menşei ülkeler | `GET /ecgw/v1/{sellerId}/lookup/origins` | `{"items": [{"name": "Türkiye"}]}` |
| Materyal bileşenleri | `GET /ecgw/v1/{sellerId}/lookup/compositions` | ⚠️ Deprecated (31 Mart 2025) |
| Yıkama talimatları | `GET /ecgw/v1/{sellerId}/lookup/care-instructions` | ⚠️ Deprecated (31 Mart 2025) |
| Kategori özellikleri | `GET /ecgw/v1/{sellerId}/lookup/product-categories/{categoryId}/attributes` | Zorunlu özellikler |
| Barkod → Kategori | `POST /ecgw/v1/{sellerId}/lookup/product-categories/by-barcodes` | Barkoddan categoryId çöz |

### Barkod → Kategori Request/Response
```json
// Request
{"barcodes": ["barcode-1", "barcode-2"]}

// Response
{
  "barcodeCategories": {"barcode-1": {"id": 123, "displayName": "Etek"}},
  "notFound": ["barcode-3"]
}
```

---

# Sipariş/Paket Entegrasyonu

Export Center'da siparişler "paket" olarak yönetilir — Trendyol toplu sipariş oluşturur, satıcı toplu hazırlar.

## Paket Durumları
| Status | Açıklama |
|--------|----------|
| `new` | Yeni paket |
| `pending` | Hazırlanıyor |
| `completed` | Tamamlandı |
| `cancelled` | İptal edildi |

## Paket Listeleme (V2)

**GET** `/integration/ecgw/v2/{sellerId}/packages`

| Parametre | Açıklama |
|-----------|----------|
| status | new, pending, completed, cancelled |
| page | Min 1, default 1 |
| size | Max 100 |
| trackingNumber | Takip numarası |
| approvedStartDate / approvedEndDate | Onay tarihi (UTC+0, ms) |
| creationStartDate / creationEndDate | Oluşturma tarihi |
| boutiqueId | Butik ID |

Response: trackingNumber, packageId, totalQuantity, totalBuyingPrice, currency, status, cargos.

## Paket Detayı (V2)

**GET** `/integration/ecgw/v2/{sellerId}/packages/items`

| Parametre | Açıklama |
|-----------|----------|
| packageId | Paket ID |
| status | Durum filtresi |
| page, size | Sayfalama |

Response: itemId, barcode, sellerBarcode, name, newQuantity, pendingQuantity, completedQuantity, unSuppliedQuantity, cancelledQuantity, unitBuyingPrice.

## Paket Listeleme (V3 — Birleşik)

**GET** `/integration/ecgw/v3/{sellerId}/packages`

V2'deki listeleme + detay birleştirilmiş. Max 100 item/sayfa.

Response: V2 listeleme + detay alanları tek response'da. Her item'da barcode, quantities, price, status, cargos.

---

## V2 vs V3 Paket API

| Özellik | V2 | V3 |
|---------|-----|-----|
| Listeleme | Ayrı endpoint | Birleşik |
| Detay | Ayrı endpoint | Birleşik |
| Max size | 50 (V2 listeleme) | 100 |
| Kullanım | Geriye uyumluluk | **Yeni entegrasyonlar için önerilen** |
