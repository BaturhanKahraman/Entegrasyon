# Hepsiburada Claim API — Endpoint Reference

Claim API Base URL: `https://oms-external-sit.hepsiburada.com`
Test Claim Base URL: `https://claim-stub-external-sit.hepsiburada.com`

> Tüm endpoint'ler HTTP Basic Auth ve `User-Agent` header gerektirir.

---

## Bolum A — REST API Endpoint'leri

---

## 1. Test Claim Olusturma

**POST** `/claims/merchant/{merchantid}/create`

> **Sadece SIT ortaminda** kullanilabilir. `claim-stub-external-sit.hepsiburada.com` base URL kullanilir.

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| merchantid | UUID | Evet | Satici ID |

### Request Body

```json
{
  "newSKU": "HBCV00001XXXXX",
  "orderNumber": "ORD-123456789",
  "type": "return",
  "reason": "ProductIsBroken"
}
```

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| newSKU | string | Evet | Urun SKU |
| orderNumber | string | Evet | Sipariş numarasi |
| type | enum | Evet | `change`, `missingitem`, `missingpart`, `missinginvoice`, `extraproduct`, `return`, `renewproduct` |
| reason | enum | Evet | `ProductIsBroken`, `ProductIsDamaged`, `WrongProductSentByMerchant` |

### Response — `200 OK`

```json
{
  "ClaimList": [
    {
      "claimNumber": "CLM-987654321",
      "explanation": "Urun hasarli geldi",
      "type": "return"
    }
  ]
}
```

---

## 2. Tum Talepleri Listeleme

**GET** `/claims/merchantId/{merchantId}`

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satici ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Aciklama |
|-----------|-----|---------|---------|----------|
| beginDate | string | Hayir | — | Baslangic tarihi (`yyyy-MM-dd HH:mm`) |
| endDate | string | Hayir | — | Bitis tarihi (`yyyy-MM-dd HH:mm`) |
| offset | integer | Hayir | 0 | Baslangic noktasi |
| limit | integer | Hayir | — | Sayfa boyutu (1–100) |

### Response Headers

| Header | Aciklama |
|--------|----------|
| X-Limit | Sayfa boyutu |
| X-Offset | Baslangic noktasi |
| X-Page | Mevcut sayfa |
| X-Total-Count | Toplam kayit sayisi |
| X-Total-Pages | Toplam sayfa sayisi |

### Response — `200 OK`

```json
[
  {
    "number": "CLM-987654321",
    "status": "NewRequest",
    "claimType": "Return",
    "claimDate": "2025-06-15T10:30:00Z",
    "quantity": 1,
    "explanation": "Urun hasarli geldi",
    "refundAmount": 150.00,
    "refundCurrency": "TRY",
    "refundDate": null,
    "orderNumber": "ORD-123456789",
    "orderDate": "2025-06-10T08:00:00Z",
    "customerName": "Ahmet Yilmaz",
    "merchantRejectionStatement": null,
    "lineItemId": "uuid",
    "sku": "HBCV00001XXXXX",
    "merchantSku": "MERCHANT-SKU-001",
    "priceAmount": 150.00,
    "totalPriceAmount": 150.00,
    "priceCurrency": "TRY",
    "finalizedWith": null,
    "awaitingActionExpireDate": "2025-06-17T10:30:00Z",
    "reason": "ProductIsBroken",
    "requestedProduct": null
  }
]
```

---

## 3. Statu Bazli Talep Listeleme

**GET** `/claims/merchantId/{merchantId}/status/{status}`

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satici ID |
| status | enum | Evet | Talep statusu (asagiya bakiniz) |

### Status Degerleri

`NewRequest` · `Accepted` · `AwaitingAction` · `InDispute` · `Rejected` · `Refunded` · `Cancelled`

### Query Parameters

Bolum 2'deki parametrelere ek olarak:

| Parametre | Tip | Zorunlu | Default | Aciklama |
|-----------|-----|---------|---------|----------|
| statusBeginDate | string | Hayir | — | Statu baslangic tarihi (`yyyy-MM-dd HH:mm`) |
| statusEndDate | string | Hayir | — | Statu bitis tarihi (`yyyy-MM-dd HH:mm`) |

### Response

Bolum 2 ile ayni format ve pagination header'lari.

---

## 4. Talep Kabul Etme

**POST** `/claims/number/{claimNumber}/accept`

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| claimNumber | string | Evet | Talep numarasi |

### Request Body

```json
{
  "FinalizedWith": "Refund",
  "InvoiceLink": "https://example.com/invoice.pdf",
  "AcceptionReason": "StockProblem"
}
```

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| FinalizedWith | enum | Hayir | `Refund` veya `Change` |
| InvoiceLink | string | Hayir | Fatura linki |
| AcceptionReason | enum | Hayir | Yeni urun talebini iade ile onaylama sebepleri (asagiya bakiniz) |

**Yeni urun talebi iade onaylama sebepleri:** `StockProblem`, `ProductNotDefective`, `Other`

> **Not:** Body gonderilmezse; degisim (`change`) talebi degisim olarak, yeni urun (`renewproduct`) talebi ise iade olarak onaylanir.

### Response — `204 No Content`

---

## 5. Talep Reddetme

**POST** `/claims/number/{claimNumber}/reject`

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| claimNumber | string | Evet | Talep numarasi |

### Request Body

```json
{
  "ClaimRejectionReason": "ProductIsDamaged",
  "MerchantStatement": "Urun iade surecinde hasar gormus.",
  "Reports": [],
  "UploadedReportsUrls": [
    "https://example.com/report1.pdf"
  ]
}
```

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| ClaimRejectionReason | enum | Evet | Red sebebi (asagiya bakiniz) |
| MerchantStatement | string | Hayir | Satici aciklamasi |
| Reports | array | Hayir | Rapor dosyalari |
| UploadedReportsUrls | array | Hayir | Yuklu rapor URL'leri |

### Return & RenewProduct Red Sebepleri

`CustomerReturnedWrongItem` · `ProductIsDamaged` · `MissingQuantity` · `NoSuchAccessory` · `BoxIsEmptyWithReport` · `BoxIsEmptyWithoutReport` · `SomePartsOrSomeAccessoriesOrSomePapersAreMissing` · `ReturnedProductIsNotDelivered` · `NewProductWillBeSent` · `ExtraProductHasBeenReturned` · `ProductNotWrong` · `ProductNotDefective` · `StockProblem` · `ReturnedProductHasAccountOrPassword` · `MarkedAsServiceProcess`

### MissingItem & MissingPart Red Sebepleri

`ProductSentComplete` · `MissingItemOrPartCannotBeSupplied` · `ClaimedComponentIsNotPartOfTheProduct` · `InvoiceReplacesWarranty` · `PartialShipmentMissingPackageWillBeDelivered` · `CustomerProblemSolved` · `Other`

### Response — `204 No Content`

---

## Claim Model

| Alan | Tip | Aciklama |
|------|-----|----------|
| number | string | Talep numarasi |
| status | enum | `NewRequest`, `AwaitingAction`, `InDispute`, `Accepted`, `Rejected`, `Refunded`, `Cancelled` |
| claimType | enum | `Return`, `Missingpart`, `MissingItem`, `DamagedWithReport`, `WrongProduct`, `UndeliveredProduct`, `MissingInvoice`, `MissingWaranty`, `ExtraProduct`, `RenewProduct` |
| claimDate | datetime | Talep tarihi |
| quantity | integer | Adet |
| explanation | string | Musteri aciklamasi |
| refundAmount | decimal | Iade tutari |
| refundCurrency | string | Para birimi |
| refundDate | datetime | Iade tarihi |
| orderNumber | string | Sipariş numarasi |
| orderDate | datetime | Sipariş tarihi |
| customerName | string | Musteri adi |
| merchantRejectionStatement | string | Satici red aciklamasi |
| lineItemId | string | Sipariş kalemi ID |
| sku | string | Hepsiburada SKU |
| merchantSku | string | Satici SKU |
| priceAmount | decimal | Birim fiyat |
| totalPriceAmount | decimal | Toplam fiyat |
| priceCurrency | string | Para birimi |
| finalizedWith | enum | `Refund`, `Change` |
| awaitingActionExpireDate | datetime | Aksiyon bekleme suresi |
| reason | string | Talep sebebi |
| requestedProduct | object | Degisim icin talep edilen urun |

---

## Is Kurallari: Yeni Urun & Urun Degisimi

### Yeni Urun Talebi (RenewProduct)

- Kusurlu veya yanlis gonderim durumlarinda acilir
- Satici minimum stok: 5 adet
- Satici 48 saat icinde yanit vermek zorundadir
- 3 opsiyon: yeni urun onayla, iade olarak onayla, reddet

### Urun Degisimi Talebi (Change)

- Sadece Fashion kategorilerdeki urunler icin gecerlidir
- Renk ve beden degisimi yapilabilir
- %5'e kadar fiyat farki saticiya aittir
- Satici 48 saat icinde yanit vermek zorundadir

---

## Bolum B — Webhook Event'leri

> Asagidaki endpoint'ler saticinin kendi sisteminde olusturmasi gereken callback URL'leridir.
> Hepsiburada bu endpoint'lere ilgili olay gerceklestiginde istek gonderir.

---

## 6. Aksiyon Bekleyen Talep Bildirimi

**PUT** `{baseUrl}/claims/awaitingaction`

Talep durumu `AwaitingAction` olarak guncellendiginde Hepsiburada tarafindan cagirilir.

### Request Body

```json
{
  "claimNumber": "CLM-987654321",
  "type": "Return",
  "quantity": 1,
  "status": "AwaitingAction",
  "customerId": "uuid",
  "customerName": "Ahmet Yilmaz",
  "orderNumber": "ORD-123456789",
  "explanation": "Urun hasarli geldi",
  "claimDate": "2025-06-15T10:30:00Z",
  "orderDate": "2025-06-10T08:00:00Z",
  "line": {
    "lineItemId": "uuid",
    "productName": "Ornek Urun",
    "listingId": "listing-001",
    "merchantId": "uuid",
    "hbSku": "HBCV00001XXXXX",
    "merchantSku": "MERCHANT-SKU-001",
    "price": 150.00,
    "totalPrice": 150.00,
    "productImageUrlFormat": "https://images.hepsiburada.net/{sku}/{size}.jpg"
  },
  "reason": "ProductIsBroken",
  "reports": [],
  "delivery": {
    "code": "DEL-001",
    "status": "InTransit",
    "direction": "CustomerToMerchant",
    "createdDate": "2025-06-16T12:00:00Z"
  },
  "requestedProduct": {
    "sku": "HBCV00002XXXXX",
    "variantProperties": "Renk: Kirmizi, Beden: M",
    "changeOption": "Change"
  }
}
```

### Beklenen Response — `204 No Content`

---

## 7. Ihtilafli Talep Kabul Bildirimi

**PUT** `{baseUrl}/claims/accept`

Ihtilafli bir talep Hepsiburada tarafindan kabul edildiginde cagirilir.

### Request Body

Bolum 6'daki alanlara ek olarak:

```json
{
  "claimNumber": "CLM-987654321",
  "type": "Return",
  "quantity": 1,
  "status": "Accepted",
  "customerId": "uuid",
  "customerName": "Ahmet Yilmaz",
  "orderNumber": "ORD-123456789",
  "explanation": "Urun hasarli geldi",
  "claimDate": "2025-06-15T10:30:00Z",
  "orderDate": "2025-06-10T08:00:00Z",
  "line": {
    "lineItemId": "uuid",
    "productName": "Ornek Urun",
    "listingId": "listing-001",
    "merchantId": "uuid",
    "hbSku": "HBCV00001XXXXX",
    "merchantSku": "MERCHANT-SKU-001",
    "price": 150.00,
    "totalPrice": 150.00,
    "productImageUrlFormat": "https://images.hepsiburada.net/{sku}/{size}.jpg"
  },
  "reason": "ProductIsBroken",
  "reports": [],
  "delivery": {
    "code": "DEL-001",
    "status": "Delivered",
    "direction": "CustomerToMerchant",
    "createdDate": "2025-06-16T12:00:00Z"
  },
  "requestedProduct": null,
  "acceptedBy": "Hepsiburada",
  "acceptedDate": "2025-06-18T14:00:00Z"
}
```

| Ek Alan | Tip | Aciklama |
|---------|-----|----------|
| acceptedBy | string | Kabul eden taraf |
| acceptedDate | datetime | Kabul tarihi |

### Beklenen Response — `204 No Content`

---

## 8. Ihtilafli Talep Red Bildirimi

**PUT** `{baseUrl}/claims/reject`

Ihtilafli bir talep Hepsiburada tarafindan reddedildiginde cagirilir.

### Request Body

Bolum 6'daki alanlara ek olarak:

```json
{
  "claimNumber": "CLM-987654321",
  "type": "Return",
  "quantity": 1,
  "status": "Rejected",
  "customerId": "uuid",
  "customerName": "Ahmet Yilmaz",
  "orderNumber": "ORD-123456789",
  "explanation": "Urun hasarli geldi",
  "claimDate": "2025-06-15T10:30:00Z",
  "orderDate": "2025-06-10T08:00:00Z",
  "line": {
    "lineItemId": "uuid",
    "productName": "Ornek Urun",
    "listingId": "listing-001",
    "merchantId": "uuid",
    "hbSku": "HBCV00001XXXXX",
    "merchantSku": "MERCHANT-SKU-001",
    "price": 150.00,
    "totalPrice": 150.00,
    "productImageUrlFormat": "https://images.hepsiburada.net/{sku}/{size}.jpg"
  },
  "reason": "ProductIsBroken",
  "reports": [],
  "delivery": {
    "code": "DEL-001",
    "status": "Delivered",
    "direction": "CustomerToMerchant",
    "createdDate": "2025-06-16T12:00:00Z"
  },
  "requestedProduct": null,
  "adminRejection": {
    "reason": "ProductNotDefective",
    "statement": "Yapilan incelemede urunde kusur tespit edilmemistir."
  },
  "rejectedDate": "2025-06-18T14:00:00Z"
}
```

| Ek Alan | Tip | Aciklama |
|---------|-----|----------|
| adminRejection.reason | string | Hepsiburada red sebebi |
| adminRejection.statement | string | Hepsiburada red aciklamasi |
| rejectedDate | datetime | Red tarihi |

### Beklenen Response — `204 No Content`

---

## 9. Talep Paket Bildirimi (Claim Package)

**POST** `{baseUrl}/claims/packages`

Talep ile iliskili kargo paketi olusturuldugunda cagirilir.

### Request Body

```json
{
  "packageNumber": "PKG-001",
  "direction": "CustomerToMerchant",
  "claims": [
    {
      "claimNumber": "CLM-987654321",
      "type": "Return",
      "quantity": 1,
      "status": "AwaitingAction",
      "explanation": "Urun hasarli geldi",
      "claimDate": "2025-06-15T10:30:00Z",
      "orderNumber": "ORD-123456789",
      "orderDate": "2025-06-10T08:00:00Z",
      "line": {
        "lineItemId": "uuid",
        "productName": "Ornek Urun",
        "listingId": "listing-001",
        "merchantId": "uuid",
        "hbSku": "HBCV00001XXXXX",
        "merchantSku": "MERCHANT-SKU-001",
        "price": 150.00,
        "totalPrice": 150.00,
        "commission": 15.00,
        "unitHBDiscount": 0.00,
        "totalHBDiscount": 0.00,
        "merchantUnitPrice": 135.00,
        "merchantTotalPrice": 135.00,
        "customizedText01": null,
        "customizedText02": null,
        "customizedText03": null,
        "customizedText04": null,
        "productImageUrlFormat": "https://images.hepsiburada.net/{sku}/{size}.jpg"
      }
    }
  ]
}
```

| Alan | Tip | Aciklama |
|------|-----|----------|
| packageNumber | string | Paket numarasi |
| direction | enum | `MerchantToCustomer`, `CustomerToMerchant` |
| claims | array | Paketteki talep listesi |
| line.commission | decimal | Komisyon tutari |
| line.unitHBDiscount | decimal | Birim HB indirimi |
| line.totalHBDiscount | decimal | Toplam HB indirimi |
| line.merchantUnitPrice | decimal | Satici birim fiyati |
| line.merchantTotalPrice | decimal | Satici toplam fiyati |
| line.customizedText01–04 | string | Ozel metin alanlari |

### Beklenen Response — `201 Created`
