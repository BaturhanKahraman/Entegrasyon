# Pazarama — Stok & Fiyat API

## 1. Fiyat Güncelleme

Tüm ürün bilgilerini göndermeden sadece fiyat bilgisi ile hızlı güncelleme yapar. İşlem **asenkron** çalışır.

> **Önemli:** %70 oranındaki indirimlerde ürünler önce onay ekibine iletilir. Onay sonrası yeni fiyatla satışa açılır.

**Servis Tipi:** POST

```
POST https://{baseurl}/product/updatePrice-v2
```

> **Rate Limit:** Her istek arasında 10 sn bekleme. Bir istekte max 3000 ürün.

### Request Parametreleri

| Parametre | Açıklama | Veri Tipi | Karakter Uzunluğu | Zorunlu |
|-----------|----------|-----------|--------------------|---------|
| `items[].code` | Ürün barkodu | string | nvarchar(100) | Evet |
| `items[].listPrice` | Liste fiyatı | decimal | decimal(18,2) | Evet |
| `items[].salePrice` | Satış fiyatı | decimal | decimal(18,2) | Evet |

### Örnek Request

```json
{
  "items": [
    { "code": "ESSN218", "listPrice": 15000, "salePrice": 15000 },
    { "code": "8683670274616", "listPrice": 1037.61, "salePrice": 1037.61 }
  ]
}
```

### Örnek Response

```json
{
  "data": "b14a42a6-9311-4d79-bcf6-346df5f39294",
  "success": true,
  "messageCode": null,
  "message": "Fiyat/Stok güncelleme işleminiz sıraya alındı.",
  "userMessage": "Fiyat/Stok güncelleme işleminiz sıraya alındı.",
  "fromCache": false
}
```

> **NOT:** Response'daki `data` alanı `dataId` olarak batch sorgulama servisinde kullanılır.

---

## 2. Stok Güncelleme

Tüm ürün bilgilerini göndermeden sadece stok bilgisi ile hızlı güncelleme yapar. İşlem **asenkron** çalışır.

**Servis Tipi:** POST

```
POST https://{baseurl}/product/updateStock-v2
```

### Request Parametreleri

| Parametre | Açıklama | Veri Tipi | Karakter Uzunluğu | Zorunlu |
|-----------|----------|-----------|--------------------|---------|
| `items[].code` | Ürün barkodu | string | nvarchar(100) | Evet |
| `items[].stockCount` | Stok sayısı | int | int | Evet |

### Örnek Request

```json
{
  "items": [
    { "code": "5707055046711", "stockCount": 1 },
    { "code": "8683670279192", "stockCount": 1 }
  ]
}
```

### Örnek Response

```json
{
  "data": "7e618393-2fe4-459d-acf7-eb38bb1d72ce",
  "success": true,
  "messageCode": null,
  "message": "Fiyat/Stok güncelleme işleminiz sıraya alındı.",
  "userMessage": "Fiyat/Stok güncelleme işleminiz sıraya alındı.",
  "fromCache": false
}
```

---

## 3. Fiyat ve Stok Batch Sorgulama (DataId ile)

Fiyat veya stok güncelleme sonrası dönen `dataId` ile işlem sonucunu kontrol eder.

**Servis Tipi:** GET

```
GET https://{baseurl}/listing-state/batch-id/{dataId}/lake-projections?page=1&pageSize=3000
```

### Response Alanları

| Alan | Açıklama |
|------|----------|
| `operationStatusText` | İşlem durumu (`Başarılı`, `Tamamlanamadı`, `İşleniyor`) |
| `operationDetail` | Hata detayı (hata varsa) |
| `operationSourceText` | İşlem kaynağı (`MerchantApi`) |
| `price.status` / `stock.status` | 0=Başarılı, 1=Tamamlanamadı, 3=İşleniyor |
| `successCount` | Başarılı işlem sayısı |
| `notCompletedCount` | Tamamlanamayan sayısı |
| `failedCount` | Başarısız sayısı |
| `processingCount` | İşlenmekte olan sayısı |

### Örnek Response

```json
{
  "pageIndex": 1,
  "pageSize": 3000,
  "totalPages": 1,
  "totalCount": 4,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "data": [
    {
      "id": "be2f4b41-049c-4386-a8db-a37affaf0d33",
      "sellerId": "ef9bf337-83bb-4617-ef46-08d97204cc6d",
      "batchId": "c9db0524-3302-4c54-82b9-01a4442faadb",
      "refId": "6f1f7782-991e-4bdc-91ad-7ebaf45698a1",
      "code": "ESSN218",
      "price": {
        "status": 1,
        "operationDetail": "Ürün bulunamadı",
        "salePrice": 15000,
        "listPrice": 15000
      },
      "stock": null,
      "operationSourceText": "MerchantApi",
      "operationStatusText": "Tamamlanamadı"
    },
    {
      "id": "0018f3ae-244f-4aef-be21-da3378093868",
      "code": "5707055046711",
      "price": null,
      "stock": {
        "status": 3,
        "operationDetail": "",
        "count": 1
      },
      "operationSourceText": "MerchantApi",
      "operationStatusText": "İşleniyor"
    },
    {
      "id": "000c3970-2981-4107-b5c9-ae681927a73d",
      "code": "8683670279192",
      "price": null,
      "stock": {
        "status": 0,
        "operationDetail": "Başarılı",
        "count": 1
      },
      "operationSourceText": "MerchantApi",
      "operationStatusText": "Başarılı"
    }
  ],
  "success": true,
  "messageCode": "MSS0",
  "message": "İşlem Başarıyla Gerçekleştirildi",
  "successCount": 1,
  "notCompletedCount": 2,
  "failedCount": 0,
  "processingCount": 1
}
```
