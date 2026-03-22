# Hepsiburada Order API — Endpoint Reference

Base URL: `https://oms-external-sit.hepsiburada.com`
Test Sipariş Base URL: `https://oms-stub-external-sit.hepsiburada.com`

> Tüm endpoint'ler HTTP Basic Auth ve `User-Agent` header gerektirir.

> **Rate Limiting:** 1 saniye içerisinde en fazla 1000 istek gönderilebilir. Aşıldığında `429 TooManyRequest` döner.
> Response header'ları: `X-RateLimit-Remaining`, `X-RateLimit-Limit`, `X-RateLimit-Reset`

---

## 1. Test Siparişi Oluşturma

**POST** `/orders/merchantId/{merchantId}`

> **Sadece SIT ortamında** kullanılabilir. `oms-stub-external-sit.hepsiburada.com` base URL kullanılır.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Request Body

```json
{
  "items": [
    {
      "hepsiburadaSku": "HBCV00001XXXXX",
      "quantity": 1,
      "price": 150.00
    }
  ]
}
```

### Response — `200 OK`

```json
{
  "orderNumber": "ORD-123456789",
  "message": "Test order created successfully"
}
```

---

## 2. Ödemesi Tamamlanmış Siparişleri Listeleme

**GET** `/orders/merchantid/{merchantId}`

Ödemesi tamamlanmış ve satıcıya düşen siparişleri listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| limit | integer | Evet | — | Sayfa boyutu (max 100) |
| offset | integer | Evet | — | Başlangıç noktası |
| begindate | datetime | Hayır | — | Başlangıç tarihi (ISO 8601) |
| enddate | datetime | Hayır | — | Bitiş tarihi (ISO 8601) |

### Response — `200 OK`

```json
{
  "items": [
    {
      "id": "uuid",
      "sku": "HBCV00001XXXXX",
      "orderId": "uuid",
      "orderNumber": "ORD-123456789",
      "quantity": 1,
      "totalPrice": 150.00,
      "unitPrice": 150.00,
      "status": "Open",
      "shippingAddress": {
        "fullName": "Ali Yılmaz",
        "address": "Atatürk Cad. No:10",
        "city": "İstanbul",
        "district": "Kadıköy",
        "zipCode": "34700",
        "phone": "05551234567"
      },
      "invoice": {
        "fullName": "Ali Yılmaz",
        "taxNumber": "12345678901",
        "taxOffice": "Kadıköy",
        "address": "Atatürk Cad. No:10"
      },
      "deliveryType": "StandardDelivery",
      "deliveryOptionId": 1,
      "dueDate": "2024-06-15T23:59:59Z",
      "commission": 12.50,
      "creationReason": "OrderCreated"
    }
  ],
  "totalCount": 42,
  "limit": 10,
  "offset": 0
}
```

### Sipariş Kalemi Alan Açıklamaları

| Alan | Tip | Açıklama |
|------|-----|----------|
| id | UUID | Sipariş kalemi ID (lineItemId) |
| sku | string | HB tarafındaki SKU |
| orderId | UUID | Siparişin unique ID'si |
| orderNumber | string | Sipariş numarası |
| quantity | integer | Adet |
| totalPrice | decimal | Toplam fiyat (KDV dahil) |
| unitPrice | decimal | Birim fiyat |
| status | string | Kalem durumu: `Open`, `Unpacked` |
| shippingAddress | object | Teslimat adresi |
| invoice | object | Fatura bilgileri |
| deliveryType | string | Teslimat tipi (aşağıya bakınız) |
| deliveryOptionId | integer | Teslimat opsiyonu (aşağıya bakınız) |
| dueDate | datetime | Kargoya verilmesi gereken son tarih |
| commission | decimal | HB komisyon tutarı |
| creationReason | string | Sipariş oluşturulma sebebi (aşağıya bakınız) |

### deliveryType Değerleri

| Değer | Açıklama |
|-------|----------|
| StandardDelivery | Standart teslimat |
| BT | Büyük Teslimat |
| YT | Yurt dışı Teslimat |

### deliveryOptionId Değerleri

| ID | Açıklama |
|----|----------|
| 1 | Standart teslimat |
| 2 | Hızlı teslimat |
| 4 | Aynı gün teslimat |

### creationReason Değerleri

| Değer | Açıklama |
|-------|----------|
| OrderCreated | Yeni sipariş oluşturuldu |
| OrderLineTransferred | Sipariş kalemi transfer edildi |
| OrderLineResend | Sipariş kalemi yeniden gönderildi |
| ClaimChangeAccepted | Talep/değişim kabul edildi |
| DeliveryCreated | Teslimat oluşturuldu |

---

## 3. Ödemesi Beklenen Siparişleri Listeleme

**GET** `/orders/merchantid/{merchantId}/paymentawaiting`

Ödemesi henüz tamamlanmamış siparişleri listeler. Stok rezervesi yapılması amacıyla kullanılır.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| limit | integer | Evet | — | Sayfa boyutu (max 50) |
| offset | integer | Evet | — | Başlangıç noktası |

### Response — `200 OK`

Aynı sipariş kalemi yapısı kullanılır (bkz. Endpoint 2).

---

## 4. Aynı Pakete Konulabilecek Kalemleri Listeleme

**GET** `/lineitems/merchantid/{merchantId}/packageablewith/lineitemid/{lineItemId}`

Belirtilen sipariş kalemiyle aynı pakete konulabilecek diğer kalemleri listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| lineItemId | UUID | Evet | Referans sipariş kalemi ID |

### Response — `200 OK`

```json
{
  "items": [
    {
      "id": "uuid",
      "sku": "HBCV00001XXXXX",
      "orderNumber": "ORD-123456789",
      "quantity": 1
    }
  ]
}
```

### Hata Kodları

| HTTP Kodu | Açıklama |
|-----------|----------|
| 404 | Paketlenebilecek başka kalem bulunamadı |
| 409 | Line item `Open` statüsünde değil |

---

## 5. Kalem veya Kalemleri Paketleme

**POST** `/packages/merchantid/{merchantId}`

Sipariş kalemlerini paketler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Request Body

```json
{
  "lineItemRequests": [
    {
      "id": "uuid",
      "quantity": 1,
      "serialNumbers": ["SN-001"]
    }
  ],
  "parcelQuantity": 1,
  "deci": 5.0
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| lineItemRequests | array | Evet | Paketlenecek kalemler |
| lineItemRequests[].id | UUID | Evet | Sipariş kalemi ID |
| lineItemRequests[].quantity | integer | Evet | Adet |
| lineItemRequests[].serialNumbers | string[] | Hayır | Seri numaraları |
| parcelQuantity | integer | Hayır | Koli adedi |
| deci | decimal | Hayır | Desi bilgisi |

### Response — `200 OK`

```json
{
  "packageNumber": "PKG-987654321",
  "barcode": "HB987654321TR"
}
```

---

## 6. Paket Bilgilerini Listeleme

**GET** `/packages/merchantid/{merchantId}`

Paketlenmiş siparişlerin bilgilerini listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| begindate | datetime | Hayır* | — | Başlangıç tarihi |
| enddate | datetime | Hayır* | — | Bitiş tarihi (begindate ile arası max 24 saat) |
| timespan | integer | Hayır* | — | Son X saat (begindate/enddate yerine) |
| limit | integer | Evet | — | Sayfa boyutu (max 10) |
| offset | integer | Evet | — | Başlangıç noktası |

> *`begindate`/`enddate` veya `timespan` parametrelerinden biri kullanılmalıdır.

### Response — `200 OK`

```json
{
  "items": [
    {
      "packageNumber": "PKG-987654321",
      "barcode": "HB987654321TR",
      "status": "Packaged",
      "lineItems": [
        {
          "id": "uuid",
          "sku": "HBCV00001XXXXX",
          "quantity": 1
        }
      ],
      "createdDate": "2024-06-10T14:30:00Z"
    }
  ],
  "totalCount": 5,
  "limit": 10,
  "offset": 0
}
```

---

## 7. Paket Bozma

**DELETE** `/packages/merchantid/{merchantId}/packagenumber/{packageNumber}`

Paketi bozar. Bozulan paketin kalemleri tekrar ödemesi tamamlanmış siparişler listesine düşer.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| packageNumber | string | Evet | Paket numarası |

### Response — `200 OK`

```json
{
  "message": "Package unpacked successfully"
}
```

---

## 8. Paket Bölme

**POST** `/packages/merchantid/{merchantId}/packagenumber/{packageNumber}/split`

Mevcut paketten belirli kalemleri ayırarak yeni bir paket oluşturur.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| packageNumber | string | Evet | Bölünecek paketin numarası |

### Request Body

```json
{
  "lineItemRequests": [
    {
      "id": "uuid",
      "quantity": 1
    }
  ]
}
```

### Response — `200 OK`

```json
{
  "packageNumber": "PKG-987654322",
  "barcode": "HB987654322TR"
}
```

---

## 9. Bozulan Paket Bilgilerini Listeleme

**GET** `/packages/merchantid/{merchantId}/unpacked`

Bozulmuş (unpacked) paketleri listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| limit | integer | Evet | — | Sayfa boyutu |
| offset | integer | Evet | — | Başlangıç noktası |

### Response — `200 OK`

```json
{
  "items": [
    {
      "packageNumber": "PKG-987654321",
      "unpackedDate": "2024-06-11T10:00:00Z",
      "lineItems": [
        {
          "id": "uuid",
          "sku": "HBCV00001XXXXX",
          "quantity": 1
        }
      ]
    }
  ],
  "totalCount": 2
}
```

---

## 10. Paketlenecek Siparişin Kargo Firması Değiştirilebilir Listeleme

**GET** `/delivery/changeablecargocompanies/merchantid/{merchantId}/orderlineid/{orderLineId}`

Henüz paketlenmemiş (Open) bir sipariş kalemi için değiştirilebilir kargo firmalarını listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| orderLineId | UUID | Evet | Sipariş kalemi ID |

### Response — `200 OK`

```json
{
  "cargoCompanies": [
    {
      "shortName": "YK",
      "name": "Yurtiçi Kargo"
    },
    {
      "shortName": "ARAS",
      "name": "Aras Kargo"
    },
    {
      "shortName": "HJ",
      "name": "HepsiJet"
    }
  ]
}
```

---

## 11. Paketlenecek Siparişin Kargo Firmasını Değiştirme

**PUT** `/lineitems/merchantid/{merchantId}/orderlineid/{id}/cargocompany`

Henüz paketlenmemiş sipariş kaleminin kargo firmasını değiştirir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| id | UUID | Evet | Sipariş kalemi ID |

### Request Body

```json
{
  "CargoCompanyShortName": "ARAS"
}
```

### Response — `200 OK`

```json
{
  "message": "Cargo company updated successfully"
}
```

---

## 12. Paketli Siparişin Kargo Firması Değiştirilebilir Listeleme

**GET** `/packages/merchantid/{merchantId}/packagenumber/{packageNumber}/changablecargocompanies`

Paketlenmiş sipariş için değiştirilebilir kargo firmalarını listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| packageNumber | string | Evet | Paket numarası |

### Response — `200 OK`

```json
{
  "cargoCompanies": [
    {
      "shortName": "YK",
      "name": "Yurtiçi Kargo"
    },
    {
      "shortName": "ARAS",
      "name": "Aras Kargo"
    }
  ]
}
```

---

## 13. Paketli Siparişin Kargo Firmasını Değiştirme

**PUT** `/packages/merchantid/{merchantId}/packagenumber/{packageNumber}/changecargocompany`

Paketlenmiş siparişin kargo firmasını değiştirir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| packageNumber | string | Evet | Paket numarası |

### Request Body

```json
{
  "CargoCompanyShortName": "HJ"
}
```

### Response — `200 OK`

```json
{
  "message": "Cargo company updated successfully"
}
```

---

## 14. Paket İçin Kargo Bilgilerini Listeleme

**GET** `/packages/merchantid/{merchantId}/packagenumber/{packageNumber}/tracking`

Paketin kargo takip bilgilerini listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| packageNumber | string | Evet | Paket numarası |

### Response — `200 OK`

```json
{
  "barcode": "HB987654321TR",
  "status": "Intransit",
  "trackingInfoCode": "TRK-123456",
  "trackingInfoUrl": "https://kargo.example.com/track/TRK-123456",
  "cargoCompany": "Yurtiçi Kargo",
  "lastUpdateDate": "2024-06-12T08:30:00Z"
}
```

### Kargo Status Değerleri

| Değer | Açıklama |
|-------|----------|
| Intransit | Kargoda |
| Delivered | Teslim edildi |

---

## 15. Ortak Barkod Oluşturma

**POST** `/packages/merchantid/{merchantId}/packagenumber/{packageNumber}/barcode`

Paket için kargo barkodu oluşturur. HepsiJet ve Aras kargo firmaları desteklenir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| packageNumber | string | Evet | Paket numarası |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| format | string | Evet | — | Barkod formatı |

### Desteklenen Barkod Formatları

| Format | Açıklama |
|--------|----------|
| zpl | ZPL (Zebra Printer Language) |
| base64zpl | Base64 ile encode edilmiş ZPL |
| pdf | PDF formatı |
| png | PNG görsel |
| jpg | JPG görsel |

### Response — `200 OK`

Response, seçilen formata göre ilgili content type ile döner.

---

## 16. İptal Bilgisi Gönderme

**POST** `/lineitems/merchantid/{merchantId}/id/{lineId}/cancelbymerchant`

Sipariş kalemini satıcı tarafından iptal eder. Sadece `Open` statüsündeki kalemler iptal edilebilir.

> **Günlük iptal limiti:** 100 adet. Limit aşıldığında işlem reddedilir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| lineId | UUID | Evet | İptal edilecek sipariş kalemi ID |

### Request Body

```json
{
  "reasonId": 1
}
```

### Response — `200 OK`

```json
{
  "message": "Line item cancelled successfully"
}
```

### İptal Ceza Tablosu

Satıcı tarafından yapılan iptallerde ürün fiyatına göre ceza uygulanır:

| Ürün Fiyat Aralığı (TL) | Ceza (TL) |
|--------------------------|-----------|
| 0 – 50 | 10 |
| 50 – 100 | 30 |
| 100 – 200 | 50 |
| 200 – 1.000 | 100 |
| 1.000 – 3.000 | 300 |
| 3.000 – 6.000 | 600 |
| 6.000 – 10.000 | 1.000 |
| 10.000 – 20.000 | 1.500 |
| 20.000+ | 2.000 |

---

## 17. Fatura Linki Gönderme

**POST** `/lineitems/merchantid/{merchantId}/id/{lineId}/invoice`

Sipariş kalemine fatura dosyası ekler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| lineId | UUID | Evet | Sipariş kalemi ID |

### Headers

| Header | Değer | Açıklama |
|--------|-------|----------|
| Content-Type | `application/pdf` veya `text/html` | Fatura dosya formatı |

### Request Body

Fatura dosyasının binary içeriği (PDF veya HTML).

### Response — `200 OK`

```json
{
  "message": "Invoice uploaded successfully"
}
```

### Hata Kodları

| HTTP Kodu | Açıklama |
|-----------|----------|
| 409 | Fatura zaten eklenmiş |

---

## 18. Digital Kod Bilgisi Gönderimi

**POST** `/lineitems/merchantid/{merchantId}/id/{lineId}/digitalcode`

Dijital ürünler için teslim bilgisi gönderir ve kalemin statüsünü "Teslim Edildi" olarak günceller.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| lineId | UUID | Evet | Sipariş kalemi ID |

### Request Body

```json
{
  "receivedDate": "2024-06-12T10:00:00Z",
  "receivedBy": "Ali Yılmaz",
  "digitalCodes": [
    "CODE-ABC-123",
    "CODE-DEF-456"
  ]
}
```

### Response — `200 OK`

```json
{
  "message": "Digital codes submitted successfully"
}
```

---

## 19. İptal Sipariş Bilgileri Listeleme

**GET** `/orders/merchantid/{merchantId}/cancelled`

İptal edilmiş siparişleri listeler. Son 1 aya kadar sorgulanabilir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| limit | integer | Evet | — | Sayfa boyutu (max 50) |
| offset | integer | Evet | — | Başlangıç noktası |
| begindate | datetime | Hayır | — | Başlangıç tarihi |
| enddate | datetime | Hayır | — | Bitiş tarihi |

### Response — `200 OK`

Aynı sipariş kalemi yapısı kullanılır (bkz. Endpoint 2). Kalem statüsü `CancelledByMerchant`, `CancelledByCustomer` veya `CancelledBySap` olabilir.

---

## 20. Teslim Edilemedi Siparişlerin Listelenmesi

**GET** `/packages/merchantid/{merchantId}/undelivered`

Teslim edilememiş paketleri listeler. Son 1 aya kadar sorgulanabilir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| limit | integer | Evet | — | Sayfa boyutu (max 50) |
| offset | integer | Evet | — | Başlangıç noktası |

### Response — `200 OK`

```json
{
  "items": [
    {
      "packageNumber": "PKG-987654321",
      "orderNumber": "ORD-123456789",
      "status": "Undelivered",
      "undeliveredDate": "2024-06-14T09:00:00Z",
      "lineItems": [
        {
          "id": "uuid",
          "sku": "HBCV00001XXXXX",
          "quantity": 1
        }
      ]
    }
  ],
  "totalCount": 3,
  "limit": 50,
  "offset": 0
}
```

---

## 21. Teslim Edilen Siparişlerin Listelenmesi

**GET** `/packages/merchantid/{merchantId}/delivered`

Teslim edilmiş paketleri listeler. Son 1 aya kadar sorgulanabilir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| limit | integer | Evet | — | Sayfa boyutu (max 50) |
| offset | integer | Evet | — | Başlangıç noktası |

### Response — `200 OK`

```json
{
  "items": [
    {
      "packageNumber": "PKG-987654321",
      "orderNumber": "ORD-123456789",
      "status": "Delivered",
      "deliveredDate": "2024-06-13T15:00:00Z",
      "lineItems": [
        {
          "id": "uuid",
          "sku": "HBCV00001XXXXX",
          "quantity": 1
        }
      ]
    }
  ],
  "totalCount": 15,
  "limit": 50,
  "offset": 0
}
```

---

## 22. Kargoya Verilen Siparişlerin Listelenmesi

**GET** `/packages/merchantid/{merchantId}/shipped`

Kargoya verilmiş paketleri listeler. Son 1 aya kadar sorgulanabilir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| limit | integer | Evet | — | Sayfa boyutu (max 50) |
| offset | integer | Evet | — | Başlangıç noktası |

### Response — `200 OK`

```json
{
  "items": [
    {
      "packageNumber": "PKG-987654321",
      "orderNumber": "ORD-123456789",
      "status": "InTransit",
      "shippedDate": "2024-06-12T08:00:00Z",
      "cargoCompany": "Yurtiçi Kargo",
      "trackingNumber": "TRK-123456",
      "lineItems": [
        {
          "id": "uuid",
          "sku": "HBCV00001XXXXX",
          "quantity": 1
        }
      ]
    }
  ],
  "totalCount": 8,
  "limit": 50,
  "offset": 0
}
```

---

## 23. Siparişe Ait Detay Listeleme

**GET** `/orders/merchantid/{merchantId}/ordernumber/{orderNumber}`

Belirli bir siparişin detaylarını getirir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| orderNumber | string | Evet | Sipariş numarası |

### Response — `200 OK`

```json
{
  "orderNumber": "ORD-123456789",
  "items": [
    {
      "id": "uuid",
      "sku": "HBCV00001XXXXX",
      "orderId": "uuid",
      "orderNumber": "ORD-123456789",
      "quantity": 1,
      "totalPrice": 150.00,
      "unitPrice": 150.00,
      "status": "Delivered",
      "shippingAddress": {
        "fullName": "Ali Yılmaz",
        "address": "Atatürk Cad. No:10",
        "city": "İstanbul",
        "district": "Kadıköy"
      },
      "invoice": {
        "fullName": "Ali Yılmaz",
        "taxNumber": "12345678901"
      },
      "deliveryType": "StandardDelivery",
      "deliveryOptionId": 1,
      "dueDate": "2024-06-15T23:59:59Z",
      "commission": 12.50,
      "creationReason": "OrderCreated"
    }
  ]
}
```

### Sipariş Statü Değerleri

| Statü | Açıklama |
|-------|----------|
| Open | Paketlenmeyi bekliyor |
| Packaged | Paketlenmiş |
| InTransit | Kargoda |
| Delivered | Teslim edildi |
| CancelledByMerchant | Satıcı tarafından iptal |
| CancelledByCustomer | Müşteri tarafından iptal |
| CancelledBySap | Sistem tarafından iptal |
| ClaimCreated | Talep/şikayet oluşturuldu |

---

## 24. Sipariş Kalemi İşçilik Maliyeti Güncelleme

**PUT** `/lineitems/merchantid/{merchantId}/orderlineid/{id}/laborcost`

Altın ürünler için sipariş kaleminin işçilik maliyetini günceller.

> Sadece altın kategorisindeki ürünler için geçerlidir.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |
| id | UUID | Evet | Sipariş kalemi ID |

### Request Body

```json
{
  "unitLaborCost": 250.00
}
```

### Response — `200 OK`

```json
{
  "message": "Labor cost updated successfully"
}
```

---

## Genel Hata Kodları

| HTTP Kodu | Açıklama |
|-----------|----------|
| 400 | Geçersiz istek parametreleri |
| 401 | Yetkilendirme hatası |
| 404 | Kaynak bulunamadı |
| 409 | Çakışma (conflict) — işlem mevcut durumla uyumsuz |
| 429 | Rate limit aşıldı (1000 istek/saniye) |
| 500 | Sunucu hatası |
