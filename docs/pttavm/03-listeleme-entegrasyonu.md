# PttAVM API — Listeleme Entegrasyonu

Base URL: `https://integration-api.pttavm.com`

## 1. Stok Kontrol Listesi (Urun Arama)

**GET** `/api/v1/products/search`

Arama filtreleriyle eslesen urun bilgilerini sayfalanmis sekilde dondurur.

### Query Parametreleri

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| categoryId | integer | Ana kategori filtresi |
| subCategoryId | integer | Alt kategori filtresi |
| isActive | boolean | Aktiflik filtresi |
| isInStock | boolean | Stok filtresi |
| merchantCategoryId | integer | Mağaza kategori filtresi |
| searchPage | integer | Sayfa numarasi |

### Response

Dizi olarak doner. Her oge:

- Urun temel bilgileri: `urunId`, `urunAdi`, `aciklama`
- Fiyat: `kdVsiz`, `kdVli`, `kdvOran`, `iskonto`
- Stok: `miktar`
- Gorseller: `resimListesi` (url, sira)
- Kategori: `anaKategoriId`, `altKategoriId`
- Varyantlar: `variantListesi` (barkod, fiyat, stok)
- Kargo: `kargoProfilId`, `tahminiKargoSuresi`
- Garanti: `garantiSuresi`, `garantiVerenFirma`

---

## 2. Fiyat Stok Guncelle

**POST** `/api/v1/products/stock-prices`

### Request

```json
[
  {
    "barcode": "string",
    "active": true,
    "quantity": 100,
    "priceWithoutVAT": 80.00,
    "priceWithVAT": 96.00,
    "vatRate": 20,
    "discount": 0,
    "isCargoFromSupplier": false,
    "variants": [
      {
        "quantity": 10,
        "price": 5.00,
        "attributes": [
          {
            "definition": "Renk",
            "value": "Kirmizi"
          }
        ]
      }
    ]
  }
]
```

### Request Parametreleri

| Parametre | Tur | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| barcode | string | Evet | Urun barkodu |
| active | boolean | Hayir | Aktiflik durumu |
| quantity | integer | Hayir | Stok (0-9999) |
| priceWithoutVAT | decimal | Hayir | KDV'siz fiyat |
| priceWithVAT | decimal | Hayir | KDV'li fiyat |
| vatRate | integer | Hayir | KDV orani (0, 1, 10, 20) |
| discount | decimal | Hayir | Indirim (0-70) |
| isCargoFromSupplier | boolean | Hayir | Kargo tedarikciden |
| variants | array | Hayir | Varyant bilgileri |

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
- Barkodsuz urunler isleme alinmaz
- Stok: 0-9999
- Varyant basina maks 100 adet
- Ayni istek 5 dakika icinde tekrar gonderilemez

---

## 3. Fiyat / Stok Guncelleme Kontrolu

**POST** `/api/v1/products/tracking-result/{trackingId}`

Katalog entegrasyonundaki "Urun Guncelleme Kontrol" endpoint'i ile aynidir. Ayni response yapisi ve durum degerleri gecerlidir.

Detay icin bkz: [02-katalog-entegrasyonu.md — Bolum 8](02-katalog-entegrasyonu.md)
