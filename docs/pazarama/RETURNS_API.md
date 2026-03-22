# Pazarama — İade & İptal API

## 1. İade Taleplerini Listeleme

İade taleplerini tarih aralığı ve statü ile sorgular.

> **NOT:** Anlaşmalı kargo firmalar için İade Kargo Kodu (`shipmentCode`) üretilir.
> Teslim edilemedi siparişlerin iadesinde `shipmentCompanyName` alanı dolu gelir.

**Servis Tipi:** POST

```
POST https://{baseurl}/order/getRefund
```

### Response Parametreleri

| Parametre | Açıklama | Veri Tipi |
|-----------|----------|-----------|
| `refundId` | İade ID | guid |
| `orderNumber` | Sipariş numarası | long |
| `orderDate` | Sipariş tarihi | datetime |
| `refundNumber` | İade numarası | long |
| `refundStatus` | İade statüsü | int |
| `refundType` | İade sebebi | string |
| `refundStatusName` | İade statü adı | string |
| `paymentType` | Ödeme tipi | int |
| `refundDate` | İade talep tarihi | datetime |
| `totalAmount` | Sipariş toplam tutarı | money object |
| `refundAmount` | İade edilecek tutar | money object |
| `customerId` | Müşteri ID | guid |
| `customerName` | Müşteri adı soyadı | string |
| `customerEmail` | Müşteri e-mail | string |
| `customerPhoneNumber` | Müşteri telefon | string |
| `productName` | Ürün adı | string |
| `productCode` | Ürün barkod | string |
| `productStockCode` | Stok kodu | string |
| `shipmentCompanyName` | Kargo firması | string |
| `shipmentCode` | İade kargo kodu | string |
| `description` | İade sebebi açıklaması | string |

### Örnek Request

```json
{
  "pageSize": 10,
  "pageNumber": 1,
  "refundStatus": 1,
  "requestStartDate": "2021-10-01",
  "requestEndDate": "2021-10-08"
}
```

### Örnek Response

```json
{
  "data": {
    "responsePage": {
      "pageSize": 100,
      "pageIndex": 1,
      "totalCount": 49,
      "totalPages": 1
    },
    "pageReport": {
      "totalRefundCount": 49,
      "totalWaitingRefundCount": 5,
      "totalApprovedRefundCount": 31,
      "totalRejectedRefundCount": 13
    },
    "refundList": [
      {
        "refundId": "ef2affaf-fdfc-4128-b5bc-3a4c1129e662",
        "orderNumber": 818589784,
        "orderDate": "6 Ekim 2021 14:53",
        "refundNumber": 564213,
        "refundType": "Ürünün parçası eksik",
        "refundStatus": 1,
        "refundStatusName": "İade Onayı Bekliyor",
        "paymentType": "Banka/Kredi Kartı",
        "refundDate": "6 Ekim 2021 19:46",
        "totalAmount": { "value": 23.5, "valueString": "23,50 TL" },
        "refundAmount": { "value": 1.75, "valueString": "1,75 TL" },
        "customerName": "Dilara Demiral",
        "productName": "Böğürtlen Reçeli 30 g.",
        "productCode": "201040500",
        "shipmentCompanyName": "MNG",
        "shipmentCode": 54895647251356
      }
    ]
  },
  "success": true,
  "messageCode": "ORD0"
}
```

---

## 2. İade Durum Güncelleme

İade taleplerini onaylama veya reddetme.

> **NOT:** Firmalar sadece **Onaylama (2)** ve **Ret (3)** statüleri ile güncelleme yapabilir.

**Servis Tipi:** POST

```
POST https://{baseurl}/order/updateRefund
```

### RefundStatus Enum

| Değer | Açıklama |
|-------|----------|
| 1 | Onay Bekliyor |
| 2 | Tedarikçi Tarafından Onaylandı |
| 3 | Tedarikçi Tarafından Reddedildi |
| 4 | Backoffice Tarafından Onaylandı |
| 5 | Backoffice Tarafından Reddedildi |
| 6 | Auto Approved |
| 7 | Talep İptal Edildi |
| 8 | Direkt Onay |

### RefundRejectType Enum (Ret sebebi, statü 3 için zorunlu)

| Değer | Açıklama |
|-------|----------|
| 1 | Gelen ürün bana ait değil |
| 2 | Gelen ürün defolu/zarar görmüş |
| 3 | Gelen ürün adedi eksik |
| 4 | Gelen ürün yanlış |
| 5 | Gelen ürün kullanılmış |
| 6 | Gelen ürün sahte |
| 7 | Gelen ürünün parçası/aksesuarı eksik |
| 8 | Gönderdiğim ürün kusurlu değil |
| 9 | Gönderdiğim ürün yanlış değil |
| 10 | İade paketi boş geldi |
| 11 | İade paketi elime ulaşmadı |
| 12 | Diğer |

### İade Akışı (getRefund ↔ getOrdersForApi)

| getRefund | getOrdersForApi |
|-----------|----------------|
| 1: İade Onayı Bekliyor | 7: İade Süreci Başlatıldı |
| 2: Tedarikçi Onayladı | 8: İade Onaylandı |
| 3: Tedarikçi Reddetti | 9: İade Reddedildi |
| 4: Backoffice Onayladı | 8: İade Onaylandı |
| 5: Backoffice Reddetti | 9: İade Reddedildi |
| 6: Auto Approved | — |
| — | 10: İade Edildi |

### Örnek Request (Onaylama)

```json
{
  "refundId": "ef2affaf-fdfc-4128-b5bc-3a4c1129e662",
  "status": 2
}
```

### Örnek Request (Reddetme)

```json
{
  "refundId": "ef2affaf-fdfc-4128-b5bc-3a4c1129e662",
  "status": 3,
  "RefundRejectType": 1
}
```

---

## 3. İade Revizyon Talebi

Satıcının iade talebi için revizyon gönderebilmesi.

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/order/api/refund/revision
```

```json
{
  "refundId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "documentObjects": [
    { "name": "string", "bytes": "string" }
  ],
  "description": "string"
}
```

---

## 4. İncelemeye Gönder & Görsel Ekleme

İade talebini incelemeye gönderme veya reddetme sırasında fotoğraf ekleme.

> **NOT:**
> - "İade paketi boş geldi" ve "iade paketi elime ulaşmadı" hariç tüm ret sebeplerinde **görsel yükleme zorunlu**
> - Max 3 dosya, max 5 MB/dosya
> - İncelemeye göndermede "Diğer" seçeneğinde "Problem detayı" zorunlu

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/order/updateRefund
```

### RefundReviewType Enum

| Değer | Açıklama |
|-------|----------|
| 1 | Üretici firma incelemesi |
| 2 | Yetkili servis incelemesi |
| 3 | Diğer |

### Örnek Request

```json
{
  "refundId": "14ecbc64-5a01-4979-980b-7f7a60ebb5e8",
  "status": 9,
  "quantity": 1,
  "refundRejectType": 0,
  "documentObjects": [
    { "name": "images.png", "bytes": "iVBORw0KGgoAAAA..." }
  ],
  "refundReviewType": 2,
  "description": "apiden iade incelemeye gönderme test"
}
```

### Örnek Response

```json
{
  "data": {
    "orderNumber": 0,
    "refundId": "00000000-0000-0000-0000-000000000000",
    "refundStatus": "İncelemeye Gönderme Talep Edildi"
  },
  "success": true,
  "messageCode": "ORD0"
}
```

---

## 5. İptal Durumu ve Güncelleme

**Servis Tipi:** PUT

```
PUT https://{baseurl}/order/api/cancel
```

> **NOT:** Firmalar sadece **Onaylama (2)** ve **Ret (3)** ile güncelleme yapabilir.

İptal statüleri iade statüleri ile aynıdır (bkz. RefundStatus Enum yukarıda).

### Örnek Request

```json
{
  "refundId": "59eff35d-b399-407d-bc0e-62821ce9f623",
  "status": 3
}
```

### Örnek Response

```json
{
  "data": {
    "orderNumber": 696730236,
    "refundId": "59eff35d-b399-407d-bc0e-62821ce9f623",
    "orderItemId": "37287b10-f93f-44ef-b10b-2566709cfd64",
    "refundType": "Yanlış adres seçtim",
    "refundStatus": "Tedarikçi Tarafından Reddedildi"
  },
  "success": true,
  "messageCode": "ORD0"
}
```

---

## 6. İptal Taleplerini Sorgulama

**Servis Tipi:** POST

```
POST https://{baseurl}/order/api/cancel/items
```

### Örnek Request

```json
{
  "pageSize": 10,
  "pageNumber": 1,
  "refundStatus": null,
  "requestStartDate": "2024-02-20",
  "requestEndDate": "2024-03-02",
  "orderNumber": null
}
```
