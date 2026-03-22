# Pazarama — Ürün API

## 1. Ürün Ekleme (Create Product)

Sisteme ürün ekleme işlemi için kullanılır. İşlem **asenkron** çalışır — başarılı yanıtta `batchRequestId` döner.

**Servis Tipi:** POST

```
POST https://{baseurl}/product/create
```

> **NOT:** Bir istekte en fazla **500 adet** ürün gönderilebilir. Her istek arasında **10 sn** rate limit vardır.
> Her istekte gönderilen listedeki barkodlar **unique** olmalıdır.

### Request Parametreleri

| Parametre | Açıklama | Veri Tipi | Karakter Uzunluğu | Zorunlu |
|-----------|----------|-----------|--------------------|---------|
| `name` | Ürün adı | string | nvarchar(100) | Evet |
| `displayName` | Gösterilecek isim | string | nvarchar(250) | Evet |
| `description` | Ürün açıklaması (HTML destekler) | string | nvarchar | Evet |
| `brandId` | Marka ID | guid | uniqueidentifier | Evet |
| `desi` | Kargo ebatı (desi) | int | int | Evet |
| `code` | Barkod bilgisi | string | nvarchar(500) | Evet |
| `groupCode` | Grup kodu (varyant ilişkilendirme) | string | nvarchar(10) | Evet |
| `stockCount` | Stok miktarı | int | int | Evet |
| `stockCode` | Stok kodu | string | nvarchar(100) | Evet |
| `vatRate` | KDV oranı | int | int | Evet |
| `listPrice` | Liste fiyatı | decimal | decimal(18,2) | Evet |
| `salePrice` | Satış fiyatı | decimal | decimal(18,2) | Evet |
| `categoryId` | Kategori ID (leaf olmalı) | guid | uniqueidentifier | Evet |
| `attributes[].attributeId` | Özellik ID | guid | uniqueidentifier | Evet |
| `attributes[].attributeValueId` | Özellik değer ID | guid | uniqueidentifier | Evet |
| `images[].imageUrl` | Görsel URL | string | nvarchar(500) | Evet |
| `productSaleLimitQuantity` | Satış adet limiti | int | — | Hayır |
| `currencyType` | Para birimi (default: `TRY`) | string | — | Hayır |

### Opsiyonel Alanlar (Temin Bilgileri & Güvenlik)

Ürün ekleme isteğine ek olarak aşağıdaki alanlar da gönderilebilir:

```json
{
  "productCommercialAdditionalInfo": {
    "securityDescription": "string"
  },
  "productCommercials": {
    "productCommercialId": "guid"
  },
  "securityDescriptionIdList": ["guid", "guid"],
  "productBatchInfo": {
    "batchNumber": "string",
    "serialNumber": "string",
    "expirationDate": "2026-02-13T13:49:09.048Z"
  },
  "securityDocuments": [
    { "url": "string", "type": 1 }
  ]
}
```

### Örnek Request

```json
{
  "products": [
    {
      "name": "TEN PELUS TAYT PT580-2",
      "displayName": "TEN PELUS TAYT PT580-2",
      "description": "<p>Açıklama metni...</p>",
      "brandId": "0a097abf-3c2e-42c9-fd40-08dbf9534e72",
      "desi": 1,
      "code": "IST0123456789DNM",
      "groupCode": "MST0123456789DNM1",
      "stockCode": "PT580-2 TEN PELUŞ-TAYT-SMTNS",
      "stockCount": 0,
      "listPrice": 10425.99,
      "salePrice": 10420.88,
      "productSaleLimitQuantity": 0,
      "currencyType": "TRY",
      "vatRate": 10,
      "images": [
        { "imageurl": "https://cdn.pazarama.com/gallery/.../image1.jpg" },
        { "imageurl": "https://cdn.pazarama.com/gallery/.../image2.jpeg" }
      ],
      "categoryId": "8ddbf6c0-d95b-42ed-ab42-a75e5ba03a12",
      "attributes": [
        {
          "attributeId": "08b2020b-e519-405f-85e2-1fd712104097",
          "attributeValueId": "5097b54e-0be9-49de-bef2-abe721c1c90b"
        },
        {
          "attributeId": "f2a74f71-5cc2-4ed7-a9d9-3874a9e94cb6",
          "attributeValueId": "e25fbf44-bb65-452f-8b5f-819ad019076d"
        }
      ]
    }
  ]
}
```

### Örnek Response

```json
{
  "data": {
    "batchRequestId": "8c3e7e0b-9239-4af1-a921-db94371a9db1",
    "creationDate": "2026-02-27T14:53:40.776+03:00",
    "error": {
      "errors": [],
      "message": null
    }
  },
  "success": true,
  "messageCode": null,
  "message": null,
  "userMessage": "Ürün ekleme isteği iletildi.",
  "fromCache": false
}
```

---

## 2. Batch İşlem Kontrolü (getProductBatchResult)

Ürün ekleme sonrası dönen `batchRequestId` ile işlem sonucunu kontrol eder.

> **NOT:** Batch request **4 saat** boyunca sorgulanabilir.

**Servis Tipi:** GET

```
GET https://{baseurl}/product/getProductBatchResult?BatchRequestId={batchRequestId}
```

### Status Değerleri

| Status | Değer | Açıklama |
|--------|-------|----------|
| InProgress | 1 | İşleniyor |
| Done | 2 | Tamamlandı |
| Error | 3 | Hata |

### Örnek Response

```json
{
  "data": {
    "status": 2,
    "batchRequestId": "8c3e7e0b-9239-4af1-a921-db94371a9db1",
    "batchResult": [],
    "totalCount": 1,
    "successfulCount": 0,
    "isExcel": false,
    "failedCount": 1,
    "failedProducts": [
      {
        "productName": "DENEMEE123 ÜRÜN45",
        "productCode": "895102324UE467",
        "errorReason": "895102324UE467 İndirimli satış fiyatı boş olmamalıdır!"
      },
      {
        "productName": "DENEMEE123 ÜRÜN45",
        "productCode": "895102324UE467",
        "errorReason": "Product'a ait attribute bulunamadı!"
      }
    ],
    "creationDate": "2026-02-27T14:53:40.733+03:00"
  },
  "success": true,
  "messageCode": "BSK0",
  "message": null,
  "userMessage": null,
  "fromCache": false
}
```

---

## 3. Ürün Filtreleme (Toplu)

Sisteme aktarılmış ürünleri `Approved` değerine göre listeler.

**Servis Tipi:** GET

```
GET https://{baseurl}/product/products?Approved={true|false}&Code={barcode}&Size={size}&Page={page}
```

### Sorgu Parametreleri

| Parametre | Açıklama | Veri Tipi |
|-----------|----------|-----------|
| `Approved` | `true`: onaylanmış, `false`: onaylanmamış | boolean |
| `Code` | Barkod ile filtreleme (opsiyonel) | string |
| `Page` | Sayfa numarası | int |
| `Size` | Sayfa başına kayıt | int |

### Ürün Durumları (State)

| Açıklama | State |
|----------|-------|
| Onay Bekliyor İlk Onay | 1 |
| Onay Bekliyor Güncelleme | 2 |
| Onaylandı | 3 |
| Reddedildi Değişiklik Bekleyen | 6 |
| Redden Dönüp Güncellenenler | 7 |

### Örnek Response (Approved=true)

```json
{
  "data": [
    {
      "name": "TEN PELUS TAYT PT580-2",
      "displayName": "TEN PELUS TAYT PT580-2",
      "description": null,
      "brandName": "TANLA BUTİK",
      "code": "MST9082147828410TNS",
      "groupCode": "",
      "stockCount": 0,
      "stockCode": "PT580-2 TEN PELUŞ-TAYT-SMTNS",
      "priorityRank": 0,
      "listPrice": 420,
      "salePrice": 420,
      "vatRate": 10,
      "categoryName": "Kadın Tayt",
      "state": 0,
      "status": null,
      "waitingApproveExp": null,
      "attributes": null,
      "images": null,
      "deliveryTypes": null
    }
  ],
  "success": true
}
```

### Örnek Response (Approved=false)

```json
{
  "data": [
    {
      "name": "Deneme ürün",
      "displayName": "Deneme ürün",
      "description": "...",
      "brandName": "Goodyear",
      "code": "MS55550",
      "groupCode": null,
      "stockCount": 0,
      "stockCode": null,
      "priorityRank": 99,
      "listPrice": 1000,
      "salePrice": 1000,
      "vatRate": 1,
      "categoryName": "Oto Lastikler (Diğer)",
      "state": 1,
      "status": "Onay Bekliyor-İlk Onay",
      "waitingApproveExp": "",
      "attributes": [
        { "attributeName": "Mevsim", "attributeValue": "4 Mevsim" },
        { "attributeName": "Araç Türü", "attributeValue": "4x4 - SUV" }
      ],
      "images": [
        { "imageUrl": "https://cdntest.pazarama.com/gallery/.../screenshot_11.png" }
      ],
      "deliveryTypes": null
    }
  ],
  "success": true
}
```

---

## 4. Tekli Ürün Detayı (getProductDetail)

Tek bir ürünün tüm detay bilgisini getirir.

**Servis Tipi:** POST

```
POST https://{baseurl}/product/getProductDetail
```

### Örnek Request

```json
{
  "Code": "Testt-Tisort-9"
}
```

### Örnek Response

```json
{
  "data": {
    "name": "deneme",
    "displayName": "deneme",
    "description": "Deneme Test Tişört",
    "brandId": "a2b7954f-b06b-48d6-8993-3b7eacd54b94",
    "brandName": "Mavi",
    "code": "Testt-Tisort-9",
    "stockCount": 1,
    "stockCode": null,
    "priorityRank": 0,
    "vatRate": 18,
    "listPrice": 1999,
    "salePrice": 1750,
    "installmentCount": 0,
    "categoryId": "67d7c7c9-4d8b-4aeb-b6bf-880eafbc2b27",
    "state": 3,
    "stateDescription": "Onaylandı",
    "attributes": [
      {
        "attributeId": "f2a74f71-5cc2-4ed7-a9d9-3874a9e94cb6",
        "attributeValueId": "d77c1888-9959-490f-84e3-8a350c85211d"
      }
    ],
    "images": [
      { "imageUrl": "https://cdn.pazarama.com/asset/Testt-Tisort-9/images/deneme-1.jpg" }
    ],
    "groupCode": "MaviTesttTisort",
    "badges": [],
    "isCatalogProduct": true
  },
  "success": true
}
```

---

## 5. Katalog Ürün Sorgusu

Pazarama kataloğundaki ürünleri arayarak hızlı yükleme yapma imkanı sağlar.

**Servis Tipi:** POST

```
POST https://{baseurl}/product/getProductTitleCodeSearch
```

### Örnek Request

```json
{
  "code": "9786257220897",
  "name": "",
  "size": 10,
  "page": 1,
  "sellerId": "xy9bf337-83yx-4617-xy46-08d97204yx6d"
}
```

### Örnek Response

```json
{
  "data": {
    "products": [
      {
        "productId": "37c5e1c1-2da0-481f-f0e0-08dac7c0bbb6",
        "name": "Mutlu Çocuklar",
        "displayName": "Mutlu Çocuklar",
        "code": "9786257220897",
        "categoryName": "Psikoloji, Kişisel Gelişim",
        "brandName": "Mona Kitap",
        "vatRate": 0,
        "imageUrl": "https://cdn.pazarama.com/asset/9786257220897/images/mutluocuklar-1.jpg",
        "isVariantable": "Varyantsız"
      }
    ],
    "pageResponse": {
      "pageIndex": 1,
      "pageSize": 10,
      "totalCount": 1,
      "totalPages": 1
    }
  },
  "success": true
}
```

---

## 6. Hızlı Ürün Ekleme (Katalog Üzerinden)

Pazarama kataloğunda mevcut bir ürün için hızlı yükleme yapar.

**Servis Tipi:** POST

```
POST https://{baseurl}/product/addProductsWithBarcodes
```

### Örnek Request

```json
{
  "product": {
    "productId": "ee2e2559-25f2-4872-595b-08d9cb9100ab",
    "code": "string",
    "stockCode": "string",
    "listPrice": 999.5,
    "stockCount": 0,
    "salePrice": 888.45,
    "installmentCount": 0,
    "brandId": "fecf7511-513d-47d5-a3a5-5064e5e1ca5c"
  }
}
```

### Örnek Response

```json
{
  "data": {
    "code": "9786257220897",
    "stockCode": "9786257220897",
    "productId": "37c5e1c1-2da0-481f-f0e0-08dac7c0bbb6",
    "sellerId": "xy9bf337-83yx-4617-xy46-08d97204yx6d",
    "listPrice": 99,
    "stockCount": 0,
    "salePrice": 88,
    "installmentCount": 0
  },
  "success": true
}
```

---

## 7. KDV Oranı Güncelleme

Sadece ürünlerin KDV oranını toplu olarak günceller.

**Servis Tipi:** PUT

```
PUT https://{baseurl}/product/vatRate/bulk
```

### Request Parametreleri

| Parametre | Açıklama | Veri Tipi | Zorunlu |
|-----------|----------|-----------|---------|
| `productCode` | Ürün barkodu | string | Evet |
| `vatRate` | KDV oranı | int | Evet |

### Örnek Request

```json
{
  "listingVatRates": [
    { "productCode": "caak2940", "vatRate": 0 },
    { "productCode": "caak2772", "vatRate": 0 }
  ]
}
```

### Örnek Response

```json
{
  "data": true,
  "success": true,
  "messageCode": "MER_0",
  "message": "İşlem Başarıyla Gerçekleştirildi.",
  "userMessage": "İşlem Başarıyla Gerçekleştirildi.",
  "fromCache": false
}
```

---

## 8. Kataloglu Ürünleri Satışa Kapatma/Açma (Tekli)

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product/external-status
```

### productStatus Enum

| Değer | Açıklama |
|-------|----------|
| 1 | Satışa açılır |
| 10 | Satışa kapatılır |

> **NOT:** Cache kaynaklı anlık yansıma olmayabilir, ancak kapatılan ürün satışa çıkmaz.

### Örnek Request

```json
{
  "productItem": {
    "code": "test",
    "productStatus": 10
  }
}
```

### Örnek Response

```json
{
  "data": {
    "result": [
      {
        "success": true,
        "message": null,
        "code": "test",
        "productStatus": 10
      }
    ]
  },
  "success": true
}
```

---

## 9. Kataloglu Ürünleri Satışa Kapatma/Açma (Toplu)

Tek istekte birden fazla ürün için satış durumu güncellemesi yapar.

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product/bulkUpdateProductStatusFromApi
```

### Örnek Request

```json
{
  "productItems": [
    { "code": "1534", "productStatus": 1 },
    { "code": "stest1", "productStatus": 10 }
  ]
}
```

### Örnek Response

```json
{
  "data": "671bc9c2-a025-4c7f-b8d0-037055b30447",
  "success": true,
  "messageCode": "MER0"
}
```

---

## 10. Ürün Kapama/Açma İşlem Sonuç Kontrolü

Toplu satış durumu güncelleme sonrası dönen ID ile işlem sonucunu kontrol eder.

**Servis Tipi:** GET

```
GET https://isortagimapi.pazarama.com/listing-state/batch-id/{batchId}/lake-projections?page=1&pageSize=10&lakeType=1
```

### Örnek Response

```json
{
  "data": {
    "data": [
      {
        "id": "3e538759-73a1-4ca6-b51e-7f45358c1826",
        "sellerId": "b39125b7-4b6f-4704-d19e-08d9b38d66e2",
        "batchId": "4ef86e6c-98c6-4810-a0cd-26c4bd5c90e5",
        "refId": "3e538759-73a1-4ca6-b51e-7f45358c1826",
        "code": "test23525243",
        "price": null,
        "stock": null,
        "status": { "operationDetail": "" },
        "operationSourceText": "MerchantApi",
        "operationStatusText": "Başarılı"
      },
      {
        "id": "ae64a49f-6761-45ea-bf7a-0886acdc04b0",
        "code": "gc8733",
        "status": { "operationDetail": "Ürün bulunamadı" },
        "operationStatusText": "Hata oluştu"
      }
    ],
    "totalCount": 32,
    "pageIndex": 1,
    "pageSize": 10
  },
  "success": true
}
```
