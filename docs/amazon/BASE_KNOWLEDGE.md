# Amazon SP-API — Temel Bilgiler

## Genel Bakış

Amazon Selling Partner API (SP-API), satıcıların Amazon marketplace'lerinde sipariş, envanter, ürün ve diğer iş verilerine programatik erişim sağlayan REST tabanlı bir API'dir.

## Ortamlar

| Ortam | Base URL | AWS Region |
|-------|----------|------------|
| **Production (EU)** | `https://sellingpartnerapi-eu.amazon.com` | eu-west-1 |
| Production (NA) | `https://sellingpartnerapi-na.amazon.com` | us-east-1 |
| Production (FE) | `https://sellingpartnerapi-fe.amazon.com` | us-west-2 |

## Yetkilendirme (OAuth 2.0 / LWA)

**Yöntem:** Login with Amazon (LWA) OAuth 2.0

**Token Endpoint:** `https://api.amazon.com/auth/o2/token`

**Token İsteği:**
```
POST /auth/o2/token
Host: api.amazon.com
Content-Type: application/x-www-form-urlencoded;charset=UTF-8

grant_type=refresh_token
&refresh_token=Aztr|...
&client_id=amzn1.application-oa2-client...
&client_secret=...
```

**Token Yanıtı:**
```json
{
  "access_token": "Atza|...",
  "token_type": "bearer",
  "expires_in": 3600,
  "refresh_token": "Atzr|..."
}
```

**Kurallar:**
- Access token **1 saat** geçerli
- Refresh token **süresiz** geçerli
- Her API isteğinde `x-amz-access-token` header'ı zorunlu
- `user-agent` header'ı zorunlu (max 500 karakter)

## Türkiye + Avrupa Marketplace ID'leri

| Ülke | Marketplace ID | Country Code |
|------|----------------|--------------|
| **Türkiye** | **A33AVAJ2PDY3EV** | TR |
| Almanya | A1PA6795UKMFR9 | DE |
| İngiltere | A1F83G8C2ARO7P | UK |
| Fransa | A13V1IB3VIYZZH | FR |
| İtalya | APJ6JRA9NG5V4 | IT |
| İspanya | A1RKKUPIHCS9HS | ES |
| Hollanda | A1805IZSGTT6HS | NL |
| Polonya | A1C3SOZRARQ6R3 | PL |
| İsveç | A2NODRKZP88ZB9 | SE |
| Belçika | AMEN7PMS3EDWL | BE |

Hepsi EU region — tek endpoint, tek credential seti.

## API Kategorileri

| API | Açıklama |
|-----|----------|
| Catalog Items | ASIN ile ürün arama, detay |
| Product Type Definitions | Ürün tipi JSON Schema (zorunlu/opsiyonel alanlar) |
| Listings Items | Ürün oluşturma/güncelleme/silme |
| Orders | Sipariş listeleme, detay, kargo onay |
| Feeds | Toplu stok/fiyat/ürün güncelleme (async) |
| Notifications | Event subscription (sipariş, listing değişiklik) |

## Temel Kavramlar

- **ASIN:** Amazon Standard Identification Number — ürünün global tanımlayıcısı
- **SKU:** Seller Stock Keeping Unit — satıcının kendi ürün kodu
- **Product Type:** Ürün kategorisi — her type'ın JSON Schema'sı var
- **FBM:** Fulfilled by Merchant — satıcı kendi deposundan kargolar
- **FBA:** Fulfilled by Amazon — Amazon deposuna gönder, Amazon kargolasın

## Rate Limiting

Her endpoint'in kendi rate limit'i vardır. `x-amz-RateLimit-Limit` response header'ında döner.
429 Too Many Requests alındığında retry yapılmalı.

| API | Rate Limit |
|-----|------------|
| Catalog Items | 5 req/sec |
| Product Type Definitions | 5 req/sec |
| Listings Items | 5 req/sec |
| Orders | 1 req/sec |
