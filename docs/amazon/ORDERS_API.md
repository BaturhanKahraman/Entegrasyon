# Amazon Orders API (v0)

## Endpoint'ler

### getOrders — Sipariş Listeleme
**GET** `/orders/v0/orders`

**Parametreler:**
- `MarketplaceIds` — Zorunlu (birden fazla marketplace)
- `CreatedAfter` — ISO 8601 tarih
- `OrderStatuses` — `Unshipped`, `PartiallyShipped`, `Shipped`, `Canceled`
- `FulfillmentChannels` — `MFN` (FBM), `AFN` (FBA)
- `MaxResultsPerPage` — Max 100

### getOrder — Sipariş Detay
**GET** `/orders/v0/orders/{orderId}`

### getOrderItems — Sipariş Kalemleri
**GET** `/orders/v0/orders/{orderId}/orderItems`

### confirmShipment — FBM Kargo Onayı
**POST** `/orders/v0/orders/{orderId}/shipment/confirm`

```json
{
  "marketplaceId": "A33AVAJ2PDY3EV",
  "packageDetail": {
    "packageReferenceId": "PKG-001",
    "carrierCode": "YURTICI_KARGO",
    "trackingNumber": "TR123456789",
    "shipDate": "2026-03-22T10:00:00Z",
    "orderItems": [
      {"orderItemId": "item-001", "quantity": 1}
    ]
  }
}
```

## Sipariş Durumları

| Durum | Açıklama |
|-------|----------|
| `Pending` | Ödeme bekleniyor |
| `Unshipped` | Ödeme tamamlandı, kargoya verilmedi |
| `PartiallyShipped` | Kısmen kargoya verildi |
| `Shipped` | Kargoya verildi |
| `Canceled` | İptal edildi |
| `Unfulfillable` | Karşılanamaz |

## Rate Limiting
- getOrders: 1 req/sec (burst 1)
- getOrder: 1 req/sec
- getOrderItems: 1 req/sec
- confirmShipment: 2 req/sec
