# Çiçeksepeti Return (İade) API

## 1. İade Sipariş Listeleme

**Endpoint:** `POST /api/v1/Order/getcanceledorders`
**Rate Limit:** 1 req / 5 sn (farklı body)

**Request Body:**
```json
{
  "orderItemStatusId": 20,
  "pageSize": 50,
  "page": 0,
  "startDate": "2026-02-22T00:00:00+03:00",
  "endDate": "2026-03-22T00:00:00+03:00"
}
```

### Request Alanları

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderItemStatusId` | int | Hayır | İade durumu filtresi |
| `pageSize` | int | Hayır | Sayfa boyutu |
| `page` | int | Hayır | Sayfa numarası (0-based) |
| `startDate` | datetime | Hayır | Başlangıç tarihi |
| `endDate` | datetime | Hayır | Bitiş tarihi |

### Tarih Kısıtlaması

**Max tarih aralığı: 1 ay.** Tarih, sipariş oluşturulma tarihidir — iade tarihi değil.

### İade Status Değerleri (orderItemStatusId)

| Değer | Açıklama |
|-------|----------|
| 20 | İade Süreci Başlatıldı |
| 21 | İade Kargoda |
| 22 | İade Tedarikçide |
| 23 | İade Tedarikçi Onayı Bekliyor |

### İptal Sonuç Değerleri (cancelStatusId)

| Değer | Açıklama |
|-------|----------|
| 1 | Müşteri Haklı |
| 2 | Bayi Haklı |
| 4 | Bayi Onay |
| 8 | Bayi Red |
| 16 | İptal |

### Response

```json
{
  "orderItemList": [
    {
      "orderId": 67890,
      "orderItemId": 111,
      "orderItemStatusId": 20,
      "customerName": "Ali Yılmaz",
      "salesPrice": 299.99,
      "cancelReason": "Ürün beklentimi karşılamadı",
      "cancelStatusId": null,
      "cargoCompany": "MNG Kargo",
      "cargoTrackingNumber": "MNG1234567",
      "productName": "Kırmızı Gül Buketi",
      "stockCode": "SKU-001-RED",
      "personalizationTexts": ["İyi ki doğdun Ali"]
    }
  ]
}
```

---

## 2. İade Teslim Aldım

**Endpoint:** `POST /api/v1/Order/refundprocessstartreceivedprocess`

İade ürününün tedarikçiye ulaştığını bildirir. Status 20 → 22 geçişi yapar.

**Request Body:**
```json
{
  "orderItemIds": [111, 222]
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderItemIds` | int[] | Evet | İade kalem ID'leri |

> **Kısıtlama:** Sadece status **20** (İade Süreci Başlatıldı) olan kalemler.

---

## 3. İade Onaylama veya Reddetme

**Endpoint:** `POST /api/v1/Order/cancelevaluation`
**Rate Limit:** 1 req / 5 sn (farklı body)

**Request Body:**
```json
{
  "orderItemId": 111,
  "process": 1
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderItemId` | int | Evet | İade kalem ID |
| `process` | int | Evet | İşlem tipi |

### Process Değerleri

| Değer | Açıklama |
|-------|----------|
| 1 | Onayla |
| 3 | Reddet |

> **Kısıtlama:** Sadece status **22** (İade Tedarikçide) olan kalemler.

### Onay Sonuçları

- **Değişim talebi onaylandıysa:** Sipariş "Yeni" statüsüne döner
- **İade talebi onaylandıysa:** Sipariş deaktif edilir, müşteriye para iadesi yapılır
- **Reddedildiyse:** Çiçeksepeti değerlendirmesine gider
