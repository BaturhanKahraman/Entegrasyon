# Trendyol Order API — Endpoint Reference

## Sipariş Paketlerini Çekme (getShipmentPackages)

**GET** `/integration/order/sellers/{sellerId}/orders`

Rate limit: 1000 req/min. Max size: 200. Tarih aralığı max 2 hafta.

### Önerilen Kullanım
```
GET /integration/order/sellers/{sellerId}/orders?status=Created&startDate={ts}&endDate={ts}&orderByField=PackageLastModifiedDate&orderByDirection=DESC&size=50
```

### Query Parametreleri
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| startDate | long | Timestamp (ms), GMT+3 |
| endDate | long | Timestamp (ms), GMT+3 |
| page | int | Sayfa numarası |
| size | int | Max 200 |
| status | string | Sipariş durumu filtresi |
| orderNumber | string | Sipariş numarası ile ara |
| orderByField | string | `PackageLastModifiedDate` |
| orderByDirection | string | `ASC` veya `DESC` |
| shipmentPackageIds | long | Paket numarası ile ara |

### Sipariş Durumları
| Status | Açıklama |
|--------|----------|
| **Awaiting** | Ödeme onayı bekliyor (sadece stok doğrulama için) |
| **Created** | Sipariş hazır, kargolanabilir |
| **Picking** | Hazırlanıyor |
| **Invoiced** | Faturalandı |
| **Shipped** | Kargoda |
| **AtCollectionPoint** | Teslim noktasında |
| **Cancelled** | İptal edildi |
| **UnPacked** | Bölünmüş paket |
| **Delivered** | Teslim edildi (final) |
| **UnDelivered** | Teslim edilemedi |
| **Returned** | İade edildi (final) |
| **UnSupplied** | Tedarik edilemedi |

### Response Yapısı (Özet)
```json
{
  "totalElements": 42078,
  "totalPages": 4208,
  "page": 0,
  "size": 50,
  "content": [
    {
      "shipmentPackageId": 123456,
      "orderNumber": "string",
      "orderDate": 1736154265337,
      "status": "Created",
      "shipmentPackageStatus": "Created",
      "customerFirstName": "string",
      "customerLastName": "string",
      "customerEmail": "string",
      "customerId": 123,
      "grossAmount": 250.99,
      "totalDiscount": 10.00,
      "totalPrice": 240.99,
      "currencyCode": "TRY",
      "cargoProviderName": "string",
      "cargoTrackingNumber": 123456789,
      "cargoTrackingLink": "string",
      "micro": false,
      "fastDelivery": false,
      "commercial": false,
      "shipmentAddress": {
        "firstName": "string",
        "lastName": "string",
        "fullAddress": "string",
        "city": "string",
        "district": "string",
        "postalCode": "string",
        "countryCode": "TR",
        "phone": "string"
      },
      "invoiceAddress": {
        "fullAddress": "string",
        "city": "string",
        "taxOffice": "string",
        "taxNumber": "string",
        "eInvoiceAvailable": true
      },
      "lines": [
        {
          "lineId": 123,
          "quantity": 1,
          "productName": "string",
          "productCode": 123,
          "merchantSku": "string",
          "sku": "string",
          "stockCode": "string",
          "barcode": "string",
          "productSize": "M",
          "productColor": "Mavi",
          "amount": 120.99,
          "price": 120.99,
          "vatRate": 18,
          "discount": 0,
          "currencyCode": "TRY",
          "orderLineItemStatusName": "Created"
        }
      ],
      "packageHistories": [
        {"createdDate": 1736154265337, "status": "Created"}
      ],
      "lastModifiedDate": 1736154265337,
      "estimatedDeliveryStartDate": 1736154265337,
      "estimatedDeliveryEndDate": 1736154265337,
      "warehouseId": 123
    }
  ]
}
```

---

## Askıdaki Siparişler

**GET** `/integration/order/sellers/{sellerId}/orders?status=Awaiting`

Aynı endpoint, `status=Awaiting` filtresi ile. Ödeme onayı bekleyen siparişler — sadece stok kontrolü için kullanılmalı, işleme alınmamalı.

---

## Tedarik Edememe Bildirimi (unsupplied)

**PUT** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/items/unsupplied`

```json
{
  "lines": [{"lineId": 0, "quantity": 0}],
  "reasonId": 0
}
```

### Neden Kodları
| reasonId | Açıklama |
|----------|----------|
| 500 | Stok tükendi |
| 501 | Kusurlu/hasarlı ürün |
| 502 | Hatalı fiyat |
| 504 | Entegrasyon hatası |
| 505 | Toplu alım |
| 506 | Mücbir sebep |

Bildirim sonrası mevcut paket iptal edilir, yeni `ShipmentPackageID` oluşturulur.

---

## Sipariş Paketini Bölme (splitShipmentPackage)

4 farklı bölme yöntemi:

### 1. Çoklu Paket (quantity bazlı)
**POST** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/split-packages`
```json
{
  "splitPackages": [
    {"packageDetails": [{"orderLineId": 12345, "quantities": 2}]},
    {"packageDetails": [{"orderLineId": 123, "quantities": 1}]}
  ]
}
```

### 2. Tekli Bölme (orderLineId bazlı)
**POST** `.../{packageId}/split`
```json
{"orderLineIds": [12345]}
```

### 3. Multi-Split
**POST** `.../{packageId}/multi-split`
```json
{
  "splitGroups": [
    {"orderLineIds": [3, 5, 6]},
    {"orderLineIds": [7, 8, 9]}
  ]
}
```

### 4. Quantity Split
**POST** `.../{packageId}/quantity-split`
```json
{
  "quantitySplit": [{"orderLineId": 0, "quantities": [2, 2]}]
}
```

Bölme sonrası orijinal paket `UnPacked` olur, yeni paketler oluşturulur.

---

## Desi ve Koli Bilgisi (updateBoxInfo)

**PUT** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/box-info`

```json
{"boxQuantity": 4, "deci": 4.4}
```

Horoz ve CEVA Lojistik için zorunlu.

---

## Kargo Firması Değiştirme (changeCargoProvider)

**PUT** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/cargo-providers`

```json
{"cargoProvider": "ARASMP"}
```

Geçerli değerler: `YKMP`, `ARASMP`, `SURATMP`, `HOROZMP`, `DHLECOMMP`, `PTTMP`, `CEVAMP`, `TEXMP`, `KOLAYGELSINMP`, `CEVATEDARIK`

Kısıtlama: Paket başına 5 dakikada 1 kez.

---

## Depo Bilgisi Güncelleme

**PUT** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/warehouse`

```json
{"warehouseId": 123}
```

Sadece Trendyol Express kullanan satıcılar için. Paket durumu Created/Invoiced/Picking olmalı.

---

## Yetkili Servis İle Gönderim

**PUT** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/delivered-by-service`

Request body yok. Sadece "Tedarikçi Öder" modeli + Horoz Lojistik kullananlar için.

---

## Alternatif Teslimat

**PUT** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/alternative-delivery`

### Kargo Takip Linki
```json
{"isPhoneNumber": false, "trackingInfo": "http://...", "params": {}}
```

### Telefon Numarası
```json
{"isPhoneNumber": true, "trackingInfo": "5555555555", "params": {}, "boxQuantity": 1, "deci": 1.4}
```

### Dijital Ürün
```json
{"isPhoneNumber": true, "trackingInfo": "5555555555", "params": {"digitalCode": "AX4567fasdf"}}
```

### Manuel Teslimat/İade
- Teslimat (tracking): **PUT** `.../{sellerId}/manual-deliver/{cargoTrackingNumber}`
- Teslimat (packageId): **PUT** `.../{packageId}/manual-deliver`
- İade (tracking): **PUT** `.../{sellerId}/manual-return/{cargoTrackingNumber}`
- İade (packageId): **PUT** `.../{packageId}/manual-return`

---

## İşçilik Bedeli Gönderme

**PUT** `/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/labor-costs`

```json
[
  {"orderLineId": 3653527482, "laborCostPerItem": 32.12},
  {"orderLineId": 3653527483, "laborCostPerItem": 78.65}
]
```

Sadece kuyumculuk/değerli metal kategorileri. Teslim edilene kadar güncellenebilir. Opsiyonel.

---

# Teslimat & Etiket Entegrasyonu

## Ortak Barkod Süreci (Akış)

```
Siparişleri Çek → Kargo Firması Değiştir (opsiyonel) → Barkod Talebi (POST createCommonLabel)
→ ~15 dk bekle (Trendyol + Kargo firması ZPL hazırlar) → Barkod Al (GET getCommonLabel)
→ ZPL'i yazdır veya image'a çevir → Pakete yapıştır
```

TEX ve Aras Kargo için geçerli (Trendyol'un kargo bedelini karşıladığı gönderimler).

---

## Ortak Etiket Barkod Talebi (createCommonLabel)

**POST** `/integration/sellers/{sellerId}/common-label/{cargoTrackingNumber}`

```json
{
  "format": "ZPL",
  "boxQuantity": 5,
  "volumetricHeight": 3.5
}
```

| Alan | Zorunlu | Açıklama |
|------|---------|----------|
| format | Evet | Şimdilik sadece `"ZPL"` |
| boxQuantity | Hayır | Koli adedi |
| volumetricHeight | Hayır | Hacimsel yükseklik |

Sipariş durumu `Picking` veya `Invoiced` olduktan sonra çağrılmalı.

**Response:** 200 (body yok) — barkod oluşturma sıraya alındı.

---

## Oluşan Barkodun Alınması (getCommonLabel)

**GET** `/integration/sellers/{sellerId}/common-label/{cargoTrackingNumber}`

**Response:**
```json
{
  "data": [
    {
      "label": "^XA.......^XZ",
      "format": "ZPL"
    }
  ]
}
```

Çoklu koli → çoklu etiket döner. Barkod talebi sonrası ~15 dk beklenmeli (TEX için daha kısa). Hazır değilse mevcut dummy etiket döner.

---

## Tazmin Entegrasyonu (TEX Compensation)

**GET** `/integration/tex/compensation/sellers/{sellerId}/tickets`

| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| page | int | Sayfa |
| size | int | Max 100 (default 10) |
| startDate | long | Timestamp (ms), GMT+3 |
| endDate | long | Timestamp (ms), GMT+3 |

### Response
```json
{
  "data": [
    {
      "cargoProvider": "TEX",
      "compensateReason": "Kayıp",
      "createDate": 1000999888777,
      "currentState": "MarkInCompensation",
      "deliveryNumber": "string",
      "orderNumber": "string",
      "requestedBy": "TEX",
      "stateMessage": "string",
      "totalItemsAmount": "250.00"
    }
  ],
  "totalCount": 42
}
```

### Tazmin Durumları
| Durum Grubu | Anlamı |
|-------------|--------|
| Empty, NotCompensationCase, MarkCompensationCancel | Talep reddedildi |
| MarkInCompensation, OpenedForRefund, CreateCompensationTicket | İnceleniyor |
| StartCompensationFinanceProgress, CompensationApproved, FinalizeCompensation | Onaylandı — 15 iş günü içinde fatura kesilecek |
| FoundAfterCompensationComplete, FoundInCompensation, FoundInvestigationProgress | Paket bulundu — iade edilecek |
| FoundInvestigationProgressDeliveredToCustomer | Bulundu — müşteriye teslim edilecek |

---

## Adres Bilgileri

| Endpoint | URL |
|----------|-----|
| Ülkeler | `GET /integration/member/countries` |
| TR Şehirler | `GET /integration/member/countries/domestic/TR/cities` |
| TR İlçeler | `GET /integration/member/countries/domestic/TR/cities/{cityCode}/districts` |
| TR Mahalleler | `GET /integration/member/countries/domestic/TR/cities/{cityCode}/districts/{districtCode}/neighborhoods` |
| AZ Şehirler | `GET /integration/member/countries/domestic/AZ/cities` |
| Körfez Şehirler | `GET /integration/member/countries/{countryCode}/cities` |

---

## Test Sipariş Oluşturma (Sadece STAGE)

**POST** `https://stageapigw.trendyol.com/integration/test/order/orders/core`

Header: `SellerID: {testSellerId}`

Müşteri bilgileri, adres, ürün barkod/adet ile test siparişi oluşturulur. Response'da `orderNumber` döner.

### Test Statü Güncelleme (Sadece STAGE)

**PUT** `https://stageapigw.trendyol.com/integration/test/order/sellers/{sellerId}/shipment-packages/{packageId}/status`

```json
{
  "lines": [{"lineId": 4944785, "quantity": 1}],
  "params": {},
  "status": "Delivered"
}
```

Statü akışı sıralı: Shipped → AtCollectionPoint → Delivered → UnDelivered → Returned. Geri alınamaz.
