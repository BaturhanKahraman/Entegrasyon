# Çiçeksepeti Product API

## 1. Ürün Oluşturma (Create)

**Endpoint:** `POST /api/v1/Products`
**Rate Limit:** 1 req / 5 sn (farklı body)
**Max Items:** 1000 ürün/istek
**Asenkron:** Evet — `batchId` döner, max 24 saat

**Request Body:**
```json
{
  "products": [
    {
      "productName": "Kırmızı Gül Buketi",
      "mainProductCode": "MAIN-001",
      "stockCode": "SKU-001-RED",
      "categoryId": 456,
      "description": "El yapımı kırmızı gül buketi, 25 adet...",
      "deliveryType": 2,
      "deliveryMessageType": 1,
      "stockQuantity": 100,
      "salesPrice": 299.99,
      "listPrice": 399.99,
      "barcode": "8680001234567",
      "images": [
        "https://example.com/image1.jpg",
        "https://example.com/image2.jpg"
      ],
      "Attributes": [
        {
          "id": 101,
          "ValueId": 1001,
          "TextLength": 0
        }
      ],
      "operatorContacts": [
        {
          "contactTypeId": 1,
          "value": "+905551234567"
        }
      ],
      "safetyInfo": {
        "text": "Güvenlik bilgisi metni"
      }
    }
  ]
}
```

### Zorunlu Alanlar

| Alan | Tip | Açıklama |
|------|-----|----------|
| `productName` | string | Ürün adı (max 255 karakter) |
| `mainProductCode` | string | Ana ürün kodu (varyantları gruplar) |
| `stockCode` | string | Stok kodu (benzersiz) |
| `categoryId` | int | Leaf kategori ID |
| `description` | string | Açıklama (min 30, max 20000 karakter) |
| `deliveryType` | int | Teslimat tipi |
| `deliveryMessageType` | int | Teslimat mesaj tipi |
| `stockQuantity` | int | Stok adedi |
| `salesPrice` | decimal | Satış fiyatı |
| `images` | string[] | Görsel URL'leri |

### Opsiyonel Alanlar

| Alan | Tip | Açıklama |
|------|-----|----------|
| `listPrice` | decimal | Liste fiyatı (üzeri çizili fiyat) |
| `barcode` | string | Barkod (bir kez set edilince değiştirilemez) |
| `Attributes` | array | Kategori özellikleri |
| `operatorContacts` | array | İletişim bilgileri |
| `safetyInfo` | object | Güvenlik bilgisi |

### deliveryType Değerleri

| Değer | Açıklama |
|-------|----------|
| 1 | Servis Aracı |
| 2 | Kargo |
| 3 | Her İkisi |

### deliveryMessageType Değerleri

Teslimat süresini belirler (aynı gün, 1-3 gün, vb.)

### Attribute Yapısı

| Alan | Tip | Açıklama |
|------|-----|----------|
| `id` | int | Özellik ID (attributeId) |
| `ValueId` | int | Seçilen değer ID (attributeValues'tan) |
| `TextLength` | int | Serbest metin karakter limiti (kişiselleştirme için) |

**Response:**
```json
{
  "batchId": "abc-123-def-456"
}
```

---

## 2. Ürün Listeleme (List)

**Endpoint:** `GET /api/v1/Products`
**Rate Limit:** 1 req / 5 sn (farklı body), 1 req / 10 dk (aynı body)
**Max PageSize:** 60

**Query Parameters:**

| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| `ProductStatus` | int | Ürün durumu filtresi |
| `PageSize` | int | Sayfa başına kayıt (max 60) |
| `Page` | int | Sayfa numarası (1-based) |
| `SortMethod` | int | Sıralama yöntemi (1-8) |
| `StockCode` | string | Stok kodu filtresi |
| `variantName` | string | Varyant adı filtresi |

### ProductStatus Değerleri

| Değer | Açıklama |
|-------|----------|
| 2 | Onay Bekliyor |
| 3 | Aktif |
| 4 | Pasif |
| 5 | Reddedildi |
| 7 | Stokta Yok |
| 8 | Kaldırıldı |

**Response:**
```json
{
  "totalCount": 150,
  "products": [
    {
      "productName": "Kırmızı Gül Buketi",
      "productCode": "MAIN-001",
      "categoryId": 456,
      "categoryName": "Buket Çiçekler",
      "stockCode": "SKU-001-RED",
      "mainProductCode": "MAIN-001",
      "productStatusType": 3,
      "description": "...",
      "link": "https://www.ciceksepeti.com/...",
      "salesPrice": 299.99,
      "StockQuantity": 100,
      "barcode": "8680001234567",
      "isActive": true,
      "images": ["..."],
      "attributes": [...],
      "operatorContacts": [...],
      "safetyInfo": {...}
    }
  ]
}
```

---

## 3. Ürün Güncelleme (Update)

**Endpoint:** `PUT /api/v1/Products`
**Rate Limit:** 1 req / 1 sn
**Max Items:** 200 ürün/istek
**Asenkron:** Evet — `batchId` döner, max 24 saat

Request body, ürün oluşturma ile aynı yapıdadır. Ek zorunlu alan:

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `isActive` | bool | Evet | Ürünün aktif/pasif durumu |

### Önemli Kısıtlamalar

- **Barkod:** Bir kez set edildikten sonra değiştirilemez
- **Varyant özellikleri:** Mevcut varyant özellikleri güncellenemez (yeni varyant oluşturulmalı)
- **operatorContacts / safetyInfo:** Bu alanlar gönderilmezse mevcut veri **SİLİNİR** — boş göndermek "sil" anlamına gelir!

**Response:**
```json
{
  "batchId": "abc-123-def-456"
}
```

---

## 4. Batch Status Kontrolü

**Endpoint:** `GET /api/v1/Products/batch-status/{batchId}`
**Rate Limit:** 5 req / 1 sn (farklı batchId), 1 req / 1 dk (aynı batchId)

**Response:**
```json
{
  "batchId": "abc-123-def-456",
  "itemCount": 2,
  "items": [
    {
      "data": {
        "siteCode": "CS-12345",
        "stockCode": "SKU-001-RED",
        "stockQuantity": 100,
        "listPrice": 399.99,
        "salesPrice": 299.99
      },
      "itemId": "item-1",
      "status": "Success",
      "failureReasons": [],
      "lastModificationDate": "2026-03-22T14:00:00+03:00"
    },
    {
      "data": {...},
      "itemId": "item-2",
      "status": "Failed",
      "failureReasons": [
        { "message": "Geçersiz kategori ID", "code": "INVALID_CATEGORY" }
      ],
      "lastModificationDate": "2026-03-22T14:00:05+03:00"
    }
  ]
}
```

### Batch Item Status Değerleri

| Status | Açıklama |
|--------|----------|
| `Pending` | Beklemede |
| `Processing` | İşleniyor |
| `Success` | Başarılı |
| `Failed` | Hata |
| `Warning` | Uyarı (kısmi başarı) |
