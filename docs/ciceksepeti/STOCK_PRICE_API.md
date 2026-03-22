# Çiçeksepeti Stock & Price Update API

## Stok ve Fiyat Güncelleme

**Endpoint:** `PUT /api/v1/Products/price-and-stock`
**Rate Limit:** 1 req / 1 sn (farklı body), 1 req / 30 dk (aynı body)
**Max Items:** 200 ürün/istek
**Asenkron:** Evet — `batchId` döner, max 4 saat

**Request Body:**
```json
{
  "items": [
    {
      "stockCode": "SKU-001-RED",
      "StockQuantity": 50,
      "salesPrice": 279.99,
      "listPrice": 379.99
    },
    {
      "stockCode": "SKU-001-BLUE",
      "StockQuantity": 30
    }
  ]
}
```

### Alanlar

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `stockCode` | string | Evet | Stok kodu (benzersiz tanımlayıcı) |
| `StockQuantity` | int | Hayır* | Stok adedi |
| `salesPrice` | decimal | Hayır* | Satış fiyatı |
| `listPrice` | decimal | Hayır | Liste fiyatı (üzeri çizili) |

> *En az `StockQuantity` veya `salesPrice` alanlarından biri gönderilmelidir.

### İş Kuralları

1. **listPrice tek başına gönderilemez** — `salesPrice` ile birlikte gönderilmelidir
2. **Fiyat düşüş limiti:** Tek seferde %50'den fazla fiyat düşüşü yapılamaz
3. **listPrice - salesPrice farkı:** %1'den fazla ve %80'den az olmalıdır
4. **Sadece stok gönderilebilir:** `StockQuantity` tek başına gönderilebilir
5. **Sadece fiyat gönderilebilir:** `salesPrice` (opsiyonel `listPrice` ile) tek başına gönderilebilir

**Response:**
```json
{
  "batchId": "abc-123-def-456"
}
```

### Batch Status Kontrolü

Aynı batch status endpoint'i kullanılır: `GET /api/v1/Products/batch-status/{batchId}`

Stok/fiyat batch'leri max **4 saat** içinde tamamlanır (ürün batch'lerinden farklı olarak 24 saat değil).
