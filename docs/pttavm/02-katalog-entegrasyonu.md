# PttAVM API — Katalog Entegrasyonu

Base URL: `https://integration-api.pttavm.com`

## 1. Ana Kategori Listesi

**GET** `/api/v1/categories/main`

Istek parametresi yok.

### Response

```json
{
  "success": true,
  "main_category": [
    {
      "id": "string",
      "name": "string",
      "updated_at": "2023-06-13T15:24:53"
    }
  ],
  "error": {
    "error_code": null,
    "error_message": null
  }
}
```

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| success | boolean | Islem basari durumu |
| id | string | Ana kategori ID |
| name | string | Ana kategori adi |
| updated_at | DateTime | Son guncelleme tarihi |

---

## 2. Kategori Agaci

**GET** `/api/v1/categories/category-tree`

### Request Parametreleri (opsiyonel)

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| parent_id | string | Belirli kategorinin alt agaci (0 = tum agac) |
| last_update | DateTime | Bu tarihten sonra guncellenenleri filtrele |

### Response

```json
{
  "success": true,
  "category_tree": [
    {
      "id": "string",
      "name": "string",
      "parent_id": "string",
      "updated_at": "DateTime",
      "children": []
    }
  ],
  "error": { "error_code": null, "error_message": null }
}
```

---

## 3. Kategori Bilgisi

**GET** `/api/v1/categories/{id}`

Path parametresi `id` zorunludur.

### Response

```json
{
  "success": true,
  "category": {
    "id": "string",
    "name": "string",
    "parent_id": "string",
    "updated_at": "DateTime",
    "children": [
      {
        "id": "string",
        "name": "string",
        "parent_id": "string",
        "updated_at": "DateTime"
      }
    ]
  },
  "error": { "error_code": null, "error_message": null }
}
```

---

## 4. Barkod Kontrol (Tekli)

**GET** `/api/v1/products`

Barkod numarasiyla urun bilgisi sorgular.

### Response Parametreleri

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| urunId | integer | Urun ID |
| barkod | string | Barkod |
| urunAdi | string | Urun adi |
| miktar | integer | Stok |
| kdVsiz | decimal | KDV'siz fiyat |
| kdVli | decimal | KDV'li fiyat |
| aktif | boolean | Aktiflik durumu |
| mevcut | boolean | Stokta var mi |
| agirlik | decimal | Agirlik |
| garantiSuresi | integer | Garanti suresi (ay) |
| gtin | string | GTIN numarasi |
| kargoProfilId | integer | Kargo profil ID |

---

## 5. Barkod Kontrol (Toplu)

**POST** `/api/v1/products/get-by-barcodes`

### Request

```json
{
  "barcodes": ["barcode1", "barcode2"]
}
```

### Response Parametreleri

Tekli barkod kontrolundeki alanlara ek olarak:

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| aciklama | string | Kisa aciklama |
| uzunAciklama | string | Uzun aciklama |
| anaKategoriId | integer | Ana kategori ID |
| altKategoriId | integer | Alt kategori ID |
| yeniKategoriId | integer | Yeni kategori ID |
| kdvOran | integer | KDV orani |
| iskonto | decimal | Indirim |
| boyX, boyY, boyZ | decimal | Boyutlar |
| desi | double | Desi |
| shopId | integer | Mağaza ID |
| durum | string | Urun durumu |
| resimListesi | array | Gorseller |
| variantListesi | array | Varyantlar |

---

## 6. Urun Aktif/Pasif Yap

**PUT** `/api/v1/products/{productId}/status`

### Request

```json
{
  "isActive": true
}
```

### Response

```json
{
  "success": true,
  "errorMessage": null,
  "errorCode": null
}
```

---

## 7. Urun Ekleme / Guncelleme

**POST** `/api/v1/products/upsert`

### Request Parametreleri

| Parametre | Tur | Durum | Aciklama |
|-----------|-----|-------|----------|
| categoryId | integer | Opsiyonel | Kategori ID |
| barcode | string | Opsiyonel | Barkod |
| name | string | Yeni urunlerde zorunlu | Urun adi |
| priceWithoutVat | decimal | Yeni urunlerde zorunlu | KDV'siz fiyat |
| vatRate | integer | Yeni urunlerde zorunlu | KDV orani (0, 1, 10, 20) |
| priceWithVat | decimal | Yeni urunlerde zorunlu | KDV'li fiyat |
| quantity | integer | Yeni urunlerde zorunlu | Stok miktari |
| desi | double | Opsiyonel | Desi |
| variants | array | Varyantli urunlerde zorunlu | Varyant listesi |
| images | array | Yeni urunlerde en az 1 | Gorsel listesi |
| noShippingProduct | boolean | Opsiyonel | Kargosuz urun |
| warrantyDuration | integer | Opsiyonel | Garanti suresi (0-24 ay) |
| basketMaxQuantity | integer | Opsiyonel | Sepet max adet (0-1000) |

### Response

```json
{
  "countOfProductsToBeProcessed": 1,
  "trackingId": "guid-string",
  "success": true,
  "message": null
}
```

### Onemli Kurallar

- Maks 1000 urun/istek
- Ayni istek 5 dakika icinde tekrarlanamaz
- Varyantlar gonderilirse guncellenir, gonderilmezse **silinir**
- Yeni urunler icin kategori ve gorsel zorunlu
- Stok negatif olamaz

---

## 8. Urun Guncelleme Kontrol (Tracking)

**POST** `/api/v1/products/tracking-result/{trackingId}`

### Response

```json
{
  "trackingId": "string",
  "status": "Completed",
  "progress": 1,
  "createdAt": "DateTime",
  "updatedAt": "DateTime",
  "productsSubTrackingResult": {
    "countOfTotalProducts": 1,
    "countOfWaitingProducts": 0,
    "countOfInProgressProducts": 0,
    "countOfCompletedProducts": 1,
    "countOfCancelledProducts": 0,
    "productBasedInfos": [
      {
        "status": "string",
        "message": "string",
        "failureReasons": ["string"],
        "barcode": "string",
        "productId": 1
      }
    ]
  }
}
```

### Durum Degerleri

| Durum | Aciklama |
|-------|----------|
| Waiting | Islem baslanmamis |
| InProgress | Guncelleme asamasinda |
| Completed | Basariyla tamamlandi |
| Cancelled | Iptal edildi (dogrulama/DB hatalari) |

---

## 9. Hatalı Urun Gorselleri

**POST** `/api/v1/products/get-faulty-images`

### Request

```json
{
  "productBarcodes": ["string"],
  "paginationParameters": {
    "pageNumber": 1,
    "pageSize": 100
  }
}
```

En az sayfalandirma VEYA barkod parametresi gonderilmeli. Maks 10.000 barkod/istek, maks 10.000 pageSize.

### Response

```json
{
  "productImagesWithErrorList": [
    {
      "productBarcode": "string",
      "infos": [
        {
          "order": 1,
          "url": "string",
          "errorReason": "string",
          "occurredAt": "DateTime"
        }
      ]
    }
  ],
  "success": true,
  "message": "string"
}
```
