# Çiçeksepeti Order API

## Sipariş Listeleme

**Endpoint:** `POST /api/v1/Order/GetOrders`
**Rate Limit:** 1 req / 5 sn (farklı body), 1 req / 1 dk (aynı body)

**Request Body:**
```json
{
  "startDate": "2026-03-01T00:00:00+03:00",
  "endDate": "2026-03-15T00:00:00+03:00",
  "pageSize": 50,
  "page": 0,
  "statusId": 1,
  "orderNo": null,
  "orderItemNo": null,
  "isOrderStatusActive": null
}
```

### Request Alanları

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `startDate` | datetime | Evet* | Başlangıç tarihi |
| `endDate` | datetime | Evet* | Bitiş tarihi |
| `pageSize` | int | Hayır | Sayfa boyutu (max 100, default: 20) |
| `page` | int | Hayır | Sayfa numarası (0-based) |
| `statusId` | int | Hayır | Sipariş durumu filtresi |
| `orderNo` | long | Hayır | Sipariş numarası |
| `orderItemNo` | long | Hayır | Sipariş kalem numarası |
| `isOrderStatusActive` | bool | Hayır | Aktif durumları filtrele |

> *`orderNo` veya `orderItemNo` verilmezse `startDate`/`endDate` zorunludur.

### Tarih Kısıtlaması

**Max tarih aralığı: 2 hafta.** Daha geniş aralıklar için birden fazla istek yapılmalıdır.

### Status Değerleri

| statusId | Açıklama |
|----------|----------|
| 1 | Yeni |
| 2 | Hazırlanıyor |
| 5 | Kargoya Verildi |
| 7 | Teslim Edildi |
| 11 | Kargoya Verilecek |

### Response

```json
{
  "orderListCount": 150,
  "supplierOrderListWithBranch": [
    {
      "branchId": 12345,
      "orderId": 67890,
      "orderItemId": 111,
      "orderItemStatusId": 1,
      "orderDate": "2026-03-10T14:30:00+03:00",
      "productName": "Kırmızı Gül Buketi",
      "productCode": "MAIN-001",
      "stockCode": "SKU-001-RED",
      "quantity": 1,
      "salesPrice": 299.99,
      "listPrice": 399.99,
      "invoicePrice": 299.99,
      "allowanceRate": 15.0,
      "receiverName": "Ali Yılmaz",
      "receiverAddress": "...",
      "receiverCity": "İstanbul",
      "receiverDistrict": "Kadıköy",
      "receiverPhone": "+905551234567",
      "senderName": "Ayşe Demir",
      "cargoCompany": "MNG Kargo",
      "cargoTrackingNumber": "...",
      "cargoTrackingUrl": "...",
      "deliveryType": 2,
      "deliveryMessageType": 1,
      "barcode": "8680001234567",
      "cancellationResult": null,
      "note": "Lütfen öğleden önce teslim edin"
    }
  ]
}
```

### Önemli Response Alanları

| Alan | Tip | Açıklama |
|------|-----|----------|
| `branchId` | int | Mağaza/şube ID |
| `orderId` | long | Sipariş numarası |
| `orderItemId` | long | Sipariş kalem numarası (sub-order) |
| `orderItemStatusId` | int | Sipariş kalem durumu |
| `salesPrice` | decimal | Satış fiyatı |
| `invoicePrice` | decimal | Fatura fiyatı |
| `allowanceRate` | decimal | Komisyon oranı (%) |
| `receiverPhone` | string | Alıcı telefonu (sadece kendi kargo entegrasyonunda) |
| `cancellationResult` | int? | İptal sonucu (1=Müşteri haklı, 2=Satıcı haklı) |
| `note` | string | Sipariş notu |

> **NOT:** Alt siparişler (sub-orders) ayrı item olarak listelenir. `receiverPhone` yalnızca kendi kargo entegrasyonu kullanan satıcılar için görünür.
