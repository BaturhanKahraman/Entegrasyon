# Pazarama Marketplace API — Base Knowledge

## 1. Amaç ve Kapsam

API entegrasyonu; ürün aktarımı, stok ve fiyat güncellemeleri, sipariş işlemleri, fatura gönderimi ve müşteri soruları yönetimi gibi işlemleri tek bir sistem üzerinden gerçekleştirmeye olanak sağlar.

## 2. Ortamlar

| Ortam | Base URL | Not |
|-------|----------|-----|
| **PROD** | `https://isortagimapi.pazarama.com` | Tüm API istekleri bu base URL üzerinden yapılır |
| **Auth** | `https://isortagimgiris.pazarama.com` | Token alma endpoint'i |
| **Panel** | `https://isortagim.pazarama.com` | Satıcı paneli (clientId/clientSecret buradan alınır) |

## 3. Yetkilendirme (Authentication)

**Yöntem:** OAuth2 Client Credentials

**Token Alma:**

```
POST https://isortagimgiris.pazarama.com/connect/token
Content-Type: application/x-www-form-urlencoded
Authorization: Basic base64({clientId}:{clientSecret})
```

**Body (form-urlencoded):**
```
grant_type=client_credentials
scope=merchantgatewayapi.fullaccess
```

**Credentials:** Satıcı paneli (`isortagim.pazarama.com`) → Hesap Bilgileri
- `clientId`
- `clientSecret`

> **NOT:** Access Token geçerlilik süresi **1 saat**tir. Süresi dolan token yenilenmelidir.

**API İsteklerinde Kullanım:**
Alınan `access_token` değeri, tüm API isteklerinde `Authorization: Bearer {access_token}` header'ı ile gönderilir.

## 4. Rate Limiting / Servis Limitleri

| Alan | Servis | Limit | Açıklama |
|------|--------|-------|----------|
| Ürün | Stok-Fiyat Güncelleme | 10 sn rate limit | Her istek arasında client bazında 10 saniyelik bekleme |
| Ürün | Stok-Fiyat Güncelleme | Max 3000 ürün/istek | Bir istekte en fazla 3000 adet ürün |
| Ürün | Ürün Ekleme | 10 sn rate limit | Her istek arasında client bazında 10 saniyelik bekleme |
| Ürün | Ürün Ekleme | Max 500 ürün/istek | Bir istekte en fazla 500 adet ürün |

## 5. Asenkron İşlemler (Batch Processing)

Ürün ekleme, stok ve fiyat güncelleme işlemleri **asenkron** çalışır:

1. İstek gönder → Başarılı yanıt + `batchRequestId` (veya `dataId`) döner
2. İlgili batch sorgulama servisi ile durumu kontrol et
3. Batch request'ler **4 saat** boyunca sorgulanabilir

**Ürün Batch Statusleri (`getProductBatchResult`):**

| Status | Değer | Açıklama |
|--------|-------|----------|
| InProgress | 1 | İşleniyor |
| Done | 2 | Tamamlandı |
| Error | 3 | Hata |

**Stok/Fiyat Batch Statusleri (`lake-projections`):**

| Status | Değer |
|--------|-------|
| Başarılı | 0 |
| Tamamlanamadı | 1 |
| İşleniyor | 3 |

## 6. Genel Response Yapısı

Tüm API yanıtları standart bir wrapper içinde döner:

```json
{
  "data": { ... },
  "success": true,
  "messageCode": null,
  "message": null,
  "userMessage": null,
  "fromCache": false
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| `data` | object/array | İşlem sonucu veya liste |
| `success` | boolean | İşlem başarılı mı |
| `messageCode` | string | Sistem mesaj kodu |
| `message` | string | Teknik mesaj |
| `userMessage` | string | Kullanıcıya gösterilebilir mesaj |
| `fromCache` | boolean | Yanıt cache'den mi geldi |

## 7. Sayfalama (Pagination)

Listeleme endpoint'lerinde standart sayfalama parametreleri:

| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| `page` / `pageIndex` | int | Sayfa numarası (default: 1) |
| `size` / `pageSize` | int | Sayfa başına kayıt sayısı (default: 10) |

Sayfalı yanıtlarda ek alanlar:
```json
{
  "pageIndex": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalCount": 42,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

## 8. Veri Tipleri

- **ID'ler:** GUID formatında (`uniqueidentifier`)
- **Fiyatlar:** `decimal(18,2)`
- **Tarihler:** ISO 8601 formatında (`2026-02-27T14:53:40.776+03:00`)
- **Para birimi:** `TRY` (Türk Lirası)
