# Hepsiburada Product API — Endpoint Reference

Base URL: `https://mpop-sit.hepsiburada.com/product`

---

## Ürün Bilgisi Gönderme (uploadProductViaFile)

**POST** `/api/products/import`

Ürün bilgileri JSON dosyası olarak `multipart/form-data` formatında gönderilir.

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| version | integer | Hayır | 1 | Servis versiyonu |

### Request — Form Data

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| file | binary | Evet | .json uzantılı dosya |

### JSON Dosyası Formatı

```json
[
  {
    "categoryId": 18021982,
    "merchant": "6fc6d90d-ee1d-4372-b3a6-264b1275e9ff",
    "attributes": {
      "merchantSku": "SAMPLE-SKU-INT-0",
      "VaryantGroupID": "Hepsiburada0",
      "Barcode": "1234567891234",
      "UrunAdi": "Roth Tyler",
      "UrunAciklamasi": "Ürün açıklama metni",
      "Marka": "Nike",
      "GarantiSuresi": 24,
      "kg": "1",
      "tax_vat_rate": "5",
      "price": "130,50",
      "stock": "13",
      "Image1": "https://example.com/image1.jpg",
      "Image2": "https://example.com/image2.jpg",
      "Image3": "https://example.com/image3.jpg",
      "Image4": "https://example.com/image4.jpg",
      "Image5": "https://example.com/image5.jpg",
      "Video1": "https://example.com/video.mp4",
      "renk_variant_property": "Siyah",
      "ebatlar_variant_property": "Büyük Ebat"
    }
  }
]
```

### İstek Dosyası Alan Açıklamaları

| Alan | Tip | Açıklama |
|------|-----|----------|
| categoryId | integer | Ürünün kategori ID'si |
| merchant | string | Merchant ID (UUID) |
| merchantSku | string | Satıcı stok kodu (**büyük harf**, boşluksuz) |
| VaryantGroupID | string | Varyant grubu ID (aynı = varyant, unique = tekil) |
| Barcode | string | EAN13 barkod |
| UrunAdi | string | Ürün adı |
| UrunAciklamasi | string | Ürün açıklaması |
| Marka | string | Ürün markası |
| GarantiSuresi | integer | Garanti süresi (ay) |
| kg | string | Ağırlık (desi) |
| tax_vat_rate | string | KDV oranı |
| price | string | Fiyat (virgülle ayrılır: "130,50") |
| stock | string | Stok adedi |
| Image1–Image5 | string | Ürün görselleri URL (PNG/JPG) |
| Video1 | string | Video URL (sadece MP4) |
| {attribute_id} | string | Kategori özellik değerleri |

### Response

```json
{
  "success": true,
  "code": 0,
  "version": 1,
  "message": "",
  "data": {
    "trackingId": "ed4723e3-4b3f-4afe-bdf8-c57a2fdde58b"
  }
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| trackingId | string | Ürün durumunu takip etmek için kullanılan kod |

### Hata Kodları

| Kod | Mesaj |
|-----|-------|
| 3001 | Dosya içeriği geçersiz |
| 3002 | Dosya türü geçersiz |
| 3003 | Dosya bulunamadı |

---

## Hızlı Ürün Yükleme (uploadFastListingProduct)

**POST** `/api/products/fastlisting`

HB katalogunda kayıtlı ve global barkod içeren ürünleri hızlıca yükleme. Katalogda olmayan ürünler işleme alınmaz.

### Request Body

```json
[
  {
    "merchant": "6fc6d90d-ee1d-4372-b3a6-264b1275e9ff",
    "merchantSku": "SAMPLE-SKU",
    "productName": "Ürün Adı",
    "barcode": "1234567891234",
    "hbSku": "HBCV00001XXXXX",
    "stock": "10",
    "price": "99,90"
  }
]
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| merchant | string | Evet | Merchant ID |
| merchantSku | string | Evet | Satıcı stok kodu |
| productName | string | Evet | Ürün adı |
| barcode | string | Hayır | Ürün barkodu |
| hbSku | string | Hayır | HB SKU kodu |
| stock | string | Hayır | Stok |
| price | string | Hayır | Fiyat |

---

## Ürün Durumu Sorgulama (getProductStatusByTraceId)

**GET** `/api/products/status/{trackingId}`

TrackingId ile gönderilen ürünlerin durumlarını sorgular.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| trackingId | string | Evet | Ürün gönderiminden alınan takip kodu |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| version | integer | Hayır | 1 | Servis versiyonu |
| page | integer | Hayır | 0 | Sayfa numarası |
| size | integer | Hayır | 1000 | Sayfa boyutu |

### Response — ImportStatusDTO

```json
{
  "success": true,
  "code": 0,
  "data": [
    {
      "itemOrderID": 1,
      "merchant": "6fc6d90d-...",
      "merchantSku": "SAMPLE-SKU",
      "hbSku": "HBCV00001XXXXX",
      "barcode": "1234567891234",
      "productStatus": "Incelenecek",
      "productName": "Ürün Adı",
      "variantGroupId": "VG001",
      "taskDetails": [],
      "validationResults": [],
      "rejectReasonsMessages": [],
      "importStatus": "SUCCESS",
      "importMessages": [],
      "matchedHbProductInfo": [],
      "videoStatus": "",
      "qualityScore": 0.0,
      "qualityStatus": ""
    }
  ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| itemOrderID | integer | JSON dosyasındaki ürün sırası |
| merchant | string | Merchant ID |
| merchantSku | string | Satıcı stok kodu |
| hbSku | string | Hepsiburada SKU |
| productStatus | string | Ürünün mevcut durumu |
| taskDetails | object[] | Görev detayları (reason, url) |
| taskDetails.reason | string | Hata nedeni |
| taskDetails.url | string | MPOP Task URL |
| taskDetails.commentList | object[] | Yorumlar (message, user) |
| validationResults | object[] | Validasyon hataları |
| validationResults.attributeName | string | Hata alan özellik adı |
| validationResults.message | string | Hata açıklaması |
| importStatus | string | PROCESSING, SUCCESS, FAILED |
| importMessages | object[] | Hata detayları |
| importMessages.severity | string | INFORMATION, WARNING, ERROR |
| importMessages.message | string | Detaylı durum bilgisi |
| matchedHbProductInfo | object[] | Eşleşen ürün bilgileri (hbSku, productName, brand, images) |

### Hata Kodları

| Kod | Mesaj |
|-----|-------|
| 4000 | TrackingId bulunamadı |

---

## TrackingId Geçmişini Sorgulama (getTrackingList)

**GET** `/api/products/trackingId-history`

Daha önce alınmış trackingId'leri listeler.

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| version | integer | Hayır | 2 | API versiyonu |
| page | integer | Hayır | 0 | Sayfa numarası |
| size | integer | Hayır | 1000 | Sayfa boyutu |

### Response

```json
{
  "success": true,
  "code": 0,
  "data": [
    {
      "trackingId": "ed4723e3-...",
      "createdDate": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

## Ürüne Ait Statü Bilgisi Çekme (checkProductStatus)

**POST** `/api/products/check-product-status`

Merchant'a ait ürünlerin tekil statü bilgilerini sorgular.

**Rate Limit:** 500 istek / 1 saniye (IP başına)

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| version | integer | Hayır | 1 | API versiyonu |

### Request Body

```json
[
  {
    "merchant": "6fc6d90d-...",
    "merchantSkuList": ["SKU-001", "SKU-002"]
  }
]
```

### Response — ProductStatusResponseDTO

```json
{
  "success": true,
  "code": 0,
  "data": [
    {
      "merchant": "6fc6d90d-...",
      "skuStatusList": [
        { "merchantSku": "SKU-001", "status": "MATCHED" },
        { "merchantSku": "SKU-002", "status": "WAITING" }
      ]
    }
  ]
}
```

---

## Statü Bazlı Ürün Bilgisi Çekme (getProductByMerchantIdAndStatus)

**GET** `/api/products/products-by-merchant-and-status`

Merchant'a ait ürünleri statüye göre toplu olarak listeler.

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| merchantId | string | Evet | — | Satıcı ID |
| productStatus | string | Evet | — | Ürün statüsü (aşağıdaki tablo) |
| taskStatus | boolean | Hayır | false | true: açık görevleri göster |
| version | integer | Hayır | 1 | API versiyonu |
| page | integer | Hayır | 0 | Sayfa numarası (0'dan başlar) |
| size | integer | Hayır | 1000 | Sayfa boyutu (max 100) |

### productStatus Değerleri

| Değer | Açıklama |
|-------|----------|
| WAITING | İncelenecek |
| MISSING_INFO | Eksik Bilgi |
| MATCHED | Satışa Hazır |
| PRE_MATCHED | Eşleşen |
| REJECTED | Reddedilen Eşleşme |
| MATCHED_WITH_STAGED | Ön Katalog Eşleşen |
| CREATED | Yaratıldı |
| IN_EXTERNAL_PROGRESS | Katalog Sürecinde |
| BLOCKED | Engelli |

### Response — ImportProductInformationDTO

```json
{
  "data": [
    {
      "merchantSku": "SKU-001",
      "barcode": "1234567891234",
      "hbSku": "HBCV00001XXXXX",
      "variantGroupId": "VG001",
      "productName": "Ürün Adı",
      "productStatus": "PRE_MATCHED",
      "taskDetails": [],
      "validationResults": [],
      "matchedHbProductInfo": [
        {
          "hbSku": "HBCV00001YYYYY",
          "productName": "Eşleşen Ürün",
          "brand": "Nike",
          "images": ["https://..."],
          "variantTypeAttributes": [
            { "name": "Renk", "value": "Siyah" }
          ]
        }
      ],
      "rejectReasonsMessages": [],
      "videoStatus": "",
      "qualityScore": 85.0,
      "qualityStatus": "GOOD"
    }
  ]
}
```

---

## Eşleşen Statü Onay (integratorApprovePreMatch)

**POST** `/api/products/approve-prematch`

Eşleşen statüdeki ürünleri onaylama.

### Request Body

```json
[
  {
    "merchant": "6fc6d90d-...",
    "merchantSkuList": ["SKU-001", "SKU-002"]
  }
]
```

---

## Eşleşen Statü Red (integratorRejectPreMatch)

**POST** `/api/products/reject-prematch`

Eşleşen statüdeki ürünleri reddetme.

### Request Body

```json
[
  {
    "merchant": "6fc6d90d-...",
    "merchantSkuList": ["SKU-001", "SKU-002"]
  }
]
```

---

## Aksiyon Bekleyen Ürün Silme (deleteByMerchantAndMerchantSkuList)

**POST** `/api/products/delete-process`

Onaylanmamış, envantere yansımayan ürünleri siler. Katalog sürecinde, yaratıldı, satışa hazır durumdaki ürünler **silinemez**.

### Request Body

```json
[
  {
    "merchant": "6fc6d90d-...",
    "merchantSku": "SKU-001"
  }
]
```

---

## Mağaza Bazlı Ürün Bilgisi Listeleme (getAllProductsByMerchantId)

**GET** `/api/products/all-products-of-merchant/{merchantId}`

Mağazanın tüm ürünlerini özellik değerleriyle birlikte listeler.

> **Not:** Bu endpoint sadece katalog ürün entegrasyonundan yüklenen ürünleri listeler. HB katalogundan envanterde açılanların bilgileri dönülmez.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| barcode | string | Hayır | — | Barkod filtresi |
| merchantSku | string | Hayır | — | MerchantSku filtresi |
| hbSku | string | Hayır | — | HB SKU filtresi |
| page | integer | Hayır | 0 | Sayfa numarası |
| size | integer | Hayır | 1000 | Sayfa boyutu (max 100) |

### Response — IntegratorProductInformation

```json
{
  "data": [
    {
      "merchantSku": "SKU-001",
      "barcode": "1234567891234",
      "hbSku": "HBCV00001XXXXX",
      "variantGroupId": "VG001",
      "productName": "Ürün Adı",
      "brand": "Nike",
      "images": ["https://..."],
      "categoryId": 18021982,
      "categoryName": "Spor Ayakkabı",
      "tax": "18",
      "price": "130,50",
      "description": "Ürün açıklaması",
      "status": "MATCHED",
      "baseAttributes": [
        { "name": "Marka", "value": "Nike", "mandatory": true }
      ],
      "variantTypeAttributes": [
        { "name": "Renk", "value": "Siyah", "mandatory": true }
      ],
      "productAttributes": [
        { "name": "Malzeme", "value": "Deri", "mandatory": false }
      ],
      "validationResults": [],
      "rejectReasons": [],
      "qualityScore": 85.0,
      "qualityStatus": "GOOD"
    }
  ]
}
```
