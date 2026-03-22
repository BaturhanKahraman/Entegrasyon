# Çiçeksepeti Marketplace API — Base Knowledge

## 1. Amaç ve Kapsam

Çiçeksepeti API entegrasyonu; ürün yönetimi (oluşturma, güncelleme, stok/fiyat), sipariş işlemleri, kargo süreçleri, iade yönetimi, fatura gönderimi ve müşteri soru-cevap yönetimi işlemlerini kapsar.

## 2. Ortamlar

| Ortam | Base URL | Not |
|-------|----------|-----|
| **PROD** | `https://apis.ciceksepeti.com` | Tüm API istekleri `/api/v1/` prefix'i ile |
| **Sandbox** | `https://sandbox-apis.ciceksepeti.com` | Test ortamı, ayrı API key |

> **NOT:** Fatura gönderimi endpoint'i (`/Branch/SendInvoiceMail`) farklı bir path prefix'i kullanır — `/api/v1/` altında değildir.

## 3. Yetkilendirme (Authentication)

**Yöntem:** API Key

Tüm API isteklerinde `x-api-key` HTTP header'ı ile API anahtarı gönderilir.

```
x-api-key: {API_KEY}
```

- Test ve production ortamları için **ayrı** API anahtarları kullanılır.
- API anahtarı satıcı panelinden talep edilir.

**TLS:** Minimum TLS 1.2 zorunludur.

## 4. Genel Request Yapısı

- **Content-Type:** `application/json` (raw JSON body)
- POST/PUT request'lerde body JSON olarak gönderilir
- GET request'lerde parametreler query string olarak gönderilir

## 5. Asenkron İşlemler (Batch Processing)

Yazma işlemleri (ürün oluşturma, güncelleme, stok/fiyat) **asenkron** çalışır:

1. İstek gönder → HTTP 200 + `batchId` döner
2. `GET /api/v1/Products/batch-status/{batchId}` ile durumu kontrol et
3. Her item'ın durumu ayrı ayrı takip edilir

**Batch Status Değerleri:**

| Status | Açıklama |
|--------|----------|
| `Pending` | Beklemede |
| `Processing` | İşleniyor |
| `Success` | Başarılı |
| `Failed` | Hata |
| `Warning` | Uyarı (kısmi başarı) |

**Batch Süreleri:**
- Ürün oluşturma/güncelleme: max **24 saat**
- Stok/fiyat güncelleme: max **4 saat**

## 6. Rate Limiting

Rate limit'ler endpoint ve body bazlıdır:

| Endpoint | Farklı Body | Aynı Body |
|----------|-------------|-----------|
| Ürün Oluşturma (POST Products) | 1 req / 5 sn | — |
| Ürün Listeleme (GET Products) | 1 req / 5 sn | 1 req / 10 dk |
| Ürün Güncelleme (PUT Products) | 1 req / 1 sn | — |
| Stok/Fiyat (PUT price-and-stock) | 1 req / 1 sn | 1 req / 30 dk |
| Sipariş Listeleme (POST GetOrders) | 1 req / 5 sn | 1 req / 1 dk |
| İade Listeleme (POST getcanceledorders) | 1 req / 5 sn | — |
| Soru Listeleme (GET sellerquestions) | 1 req / 5 sn | 1 req / 10 sn |
| İade Onay/Red (POST cancelevaluation) | 1 req / 5 sn | — |
| Batch Status (GET batch-status) | 5 req / 1 sn | 1 req / 1 dk (aynı batchId) |

## 7. Batch Boyut Limitleri

| İşlem | Max Item/Request |
|-------|-----------------|
| Ürün Oluşturma | 1000 |
| Ürün Güncelleme | 200 |
| Stok/Fiyat Güncelleme | 200 |
| Ürün Listeleme | PageSize max 60 |
| Sipariş Listeleme | PageSize max 100 |

## 8. Sayfalama (Pagination)

Listeleme endpoint'lerinde sayfalama parametreleri:

| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| `Page` | int | Sayfa numarası (0-based sipariş, 1-based soru-cevap) |
| `PageSize` | int | Sayfa başına kayıt sayısı |

> **NOT:** Sipariş listeleme 0-based sayfalama kullanır, soru-cevap 1-based kullanır.

## 9. Tarih Aralığı Kısıtlamaları

| Endpoint | Max Tarih Aralığı |
|----------|-------------------|
| Sipariş Listeleme | 2 hafta |
| İade Listeleme | 1 ay |
| Soru Listeleme | 32 gün |

## 10. Veri Tipleri

- **ID'ler:** Integer (sipariş), String (ürün kodları — mainProductCode, stockCode)
- **Fiyatlar:** Decimal
- **Tarihler:** ISO 8601 formatında
- **Para birimi:** TRY (Türk Lirası)

## 11. Görsel Kuralları

- **Format:** JPG veya PNG
- **Boyut:** 500x500 — 2000x2000 px
- **Dosya boyutu:** Max 2–5 MB
- **Önerilen oran:** 10:11
- İlk görsel ana görsel olarak kullanılır

## 12. Ürün Adı ve Açıklama Kuralları

- **Ürün adı:** Max 255 karakter, yasaklı kelimeler içermemeli
- **Açıklama:** Min 30 karakter (plain text), max 20.000 karakter (HTML dahil), harici link yasak

## 13. Ürün Kodu Yapısı

- `mainProductCode`: Varyantları gruplar (ana ürün kodu)
- `stockCode`: Her varyant için benzersiz (stok birimi kodu)
- `barcode`: Opsiyonel, bir kez set edildikten sonra değiştirilemez
