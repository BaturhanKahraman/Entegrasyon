# Trendyol Webhook API — Endpoint Reference

## Genel Bakış

Webhook'lar sipariş durumu değişikliklerinde otomatik POST isteği gönderir — polling'e gerek kalmaz. Ancak **garanti edilmez** — webhook ile birlikte periyodik `getShipmentPackages` polling'i de yapılmalı.

## Desteklenen Event'ler (subscribedStatuses)

| Status | Açıklama |
|--------|----------|
| CREATED | Sipariş oluşturuldu |
| PICKING | Hazırlanıyor |
| INVOICED | Faturalandı |
| SHIPPED | Kargoya verildi |
| CANCELLED | İptal edildi |
| DELIVERED | Teslim edildi |
| UNDELIVERED | Teslim edilemedi |
| RETURNED | İade edildi |
| UNSUPPLIED | Tedarik edilemedi |
| AWAITING | Ödeme bekliyor |
| UNPACKED | Paket bölündü |
| AT_COLLECTION_POINT | Teslim noktasında |
| VERIFIED | Doğrulandı |

Boş `subscribedStatuses` array'i → tüm event'lere abone olur.

## Webhook Payload

Hangi status tetiklerse tetiklesin **tam sipariş verisi** gönderilir. Format `getShipmentPackages` response'u ile aynı:
- Sipariş bilgileri (orderNumber, id, shipmentPackageId, status)
- Müşteri bilgileri (firstName, lastName, email)
- Adresler (shipmentAddress, invoiceAddress)
- Ürün detayları (lines: barcode, productName, sku, price, quantity)
- Kargo bilgileri (cargoTrackingNumber, cargoProviderName)
- Finansal veriler (grossAmount, discount, commission)
- Özel alanlar: `micro`, `commercial`, `fastDelivery`, `isCod`, `giftBoxRequested`
- `packageHistories` — durum geçiş geçmişi
- `createdBy` — "order-creation", "cancel", "split", "transfer"

## Authentication Tipleri

Webhook endpoint'inize gelen isteklerin doğrulanması:

| Tip | Header | Açıklama |
|-----|--------|----------|
| **API_KEY** | `x-api-key: {apiKey}` | API key header olarak gönderilir |
| **BASIC_AUTHENTICATION** | `Authorization: Basic ...` | Username + password |

## Retry Mekanizması

1. Endpoint başarısız olursa **5 dakikada bir** otomatik retry
2. Tekrarlı başarısızlıkta webhook **PASSIVE** durumuna geçer
3. E-posta bildirimi #1: webhook ID + retry devam süresi
4. E-posta bildirimi #2: webhook deaktive edildi
5. Servisi düzelttikten sonra **manuel aktivasyon** gerekir

## Kısıtlamalar

- URL'de "Trendyol", "Dolap", "Localhost" bulunamaz
- Satıcı başına **max 15 webhook** (pasifler dahil)
- Teslimat **garanti edilmez** — polling ile desteklenmeli

---

## Webhook Oluşturma (createWebhook)

**POST** `/integration/webhook/sellers/{sellerId}/webhooks`

```json
{
  "url": "https://myserver.com/api/trendyol/webhook",
  "username": "user",
  "password": "password",
  "authenticationType": "API_KEY",
  "apiKey": "my-secret-key",
  "subscribedStatuses": ["CREATED", "SHIPPED", "DELIVERED", "CANCELLED"]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| url | string | Webhook endpoint URL'i |
| authenticationType | string | `API_KEY` veya `BASIC_AUTHENTICATION` |
| apiKey | string | API_KEY tipi için |
| username | string | BASIC_AUTH tipi için |
| password | string | BASIC_AUTH tipi için |
| subscribedStatuses | array | Abone olunacak durumlar (boş = hepsi) |

**Response:**
```json
{"id": "5297c986-6e09-4615-9f16-0deff65a0890"}
```

---

## Webhook Listeleme

**GET** `/integration/webhook/sellers/{sellerId}/webhooks`

```json
[
  {
    "id": "5297c986-...",
    "createdDate": 1733317686667,
    "lastModifiedDate": 1734010262454,
    "url": "https://myserver.com/webhook",
    "username": "user",
    "authenticationType": "API_KEY",
    "status": "ACTIVE",
    "subscribedStatuses": ["CREATED", "SHIPPED", "DELIVERED"]
  }
]
```

**status:** `ACTIVE` veya `PASSIVE`

---

## Webhook Güncelleme

**PUT** `/integration/webhook/sellers/{sellerId}/webhooks/{webhookId}`

Aynı request body (createWebhook ile aynı). URL, auth, subscribedStatuses güncellenebilir.

---

## Webhook Silme

**DELETE** `/integration/webhook/sellers/{sellerId}/webhooks/{webhookId}`

Response: 200 OK

---

## Webhook Aktif/Pasif Alma

### Aktifleştirme
**PUT** `/integration/webhook/sellers/{sellerId}/webhooks/{webhookId}/activate`

### Pasifleştirme
**PUT** `/integration/webhook/sellers/{sellerId}/webhooks/{webhookId}/deactivate`

Her ikisi de body gerektirmez. Response: 200 OK.

Deaktive olan webhook'u düzelttikten sonra activate ile yeniden başlatılır.
