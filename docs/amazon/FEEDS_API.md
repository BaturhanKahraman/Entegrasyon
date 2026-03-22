# Amazon Feeds API (v2021-06-30)

## Genel Bakış

Feeds API, toplu veri gönderimi için kullanılır. Ürün/stok/fiyat güncelleme async olarak işlenir.

## Workflow

```
1. createFeedDocument → feedDocumentId + presigned URL
2. Upload content → presigned URL'ye PUT
3. createFeed → feedId
4. getFeed → processingStatus polling (IN_QUEUE → IN_PROGRESS → DONE/FATAL)
```

## Endpoint'ler

### createFeedDocument
**POST** `/feeds/2021-06-30/documents`

```json
{"contentType": "application/json"}
```

**Yanıt:**
```json
{
  "feedDocumentId": "doc-123",
  "url": "https://s3.amazonaws.com/presigned-url..."
}
```

### Upload Content
**PUT** `{presigned URL}` (auth header gerekmez)

Content-Type: `application/json` veya `text/xml`

### createFeed
**POST** `/feeds/2021-06-30/feeds`

```json
{
  "feedType": "JSON_LISTINGS_FEED",
  "marketplaceIds": ["A33AVAJ2PDY3EV"],
  "inputFeedDocumentId": "doc-123"
}
```

**Yanıt:**
```json
{"feedId": "feed-456"}
```

### getFeed — Status Polling
**GET** `/feeds/2021-06-30/feeds/{feedId}`

**Yanıt:**
```json
{
  "feedId": "feed-456",
  "feedType": "JSON_LISTINGS_FEED",
  "processingStatus": "DONE",
  "resultFeedDocumentId": "result-doc-789"
}
```

## Feed Status Değerleri

| Status | Açıklama |
|--------|----------|
| `IN_QUEUE` | Kuyruğa alındı |
| `IN_PROGRESS` | İşleniyor |
| `DONE` | Tamamlandı |
| `CANCELLED` | İptal edildi |
| `FATAL` | Kritik hata |

## Feed Tipleri

| Feed Type | Kullanım |
|-----------|----------|
| `JSON_LISTINGS_FEED` | Toplu listing oluşturma/güncelleme (JSON) |
| `POST_INVENTORY_AVAILABILITY_DATA` | Stok güncelleme |
| `POST_PRODUCT_PRICING_DATA` | Fiyat güncelleme |
