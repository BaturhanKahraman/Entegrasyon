# Trendyol Marketplace API — Base Knowledge

## 1. Ortamlar

| Ortam | URL | Not |
|-------|-----|-----|
| **PROD** | `https://apigw.trendyol.com` | IP yetkilendirmesi gerekmez |
| **STAGE** | `https://stageapigw.trendyol.com` | IP whitelisting zorunlu (0850 258 58 00 ile açılır) |

## 2. Yetkilendirme (Authentication)

**Yöntem:** Basic Authentication

```
Authorization: Basic base64({apiKey}:{apiSecret})
```

**Credentials:** Satıcı paneli → Hesap Bilgilerim → Entegrasyon Bilgileri
- `supplierId` (Satıcı ID) — URL'lerde `{sellerId}` olarak kullanılır
- `API KEY`
- `API SECRET KEY`

**Zorunlu Header — User-Agent:**
```
User-Agent: {SatıcıId} - SelfIntegration
```
Eksikse **403 Forbidden** döner. Max 30 karakter, alfanümerik.

## 3. Rate Limiting

| Endpoint | Limit |
|----------|-------|
| Ürün Aktarma | 1000 req/min |
| Ürün Güncelleme | 1000 req/min |
| Stok ve Fiyat Güncelleme | **Limitsiz** |
| Toplu İşlem Kontrolü | 1000 req/min |
| Ürün Filtreleme | 2000 req/min |
| Ürün Silme | 100 req/min |
| Adres Bilgileri | 1 req/hour |
| Marka/Kategori/Özellik Listeleri | 50 req/min |
| Ürün Buybox Kontrol | 1000 req/min |

**Genel kural:** Aynı endpoint'e 10 saniye içinde max 50 request. 51. request → **429 Too Many Requests**.

## 4. Asenkron İşlemler (Batch Processing)

Ürün yaratma, güncelleme, stok-fiyat güncelleme **asenkron** çalışır:
1. İstek gönder → HTTP 200 + `batchRequestId` döner (işlem sıraya alındı, başarılı demek DEĞİL)
2. `getBatchRequestResult` ile durumu kontrol et
3. Batch request'ler **4 saat** sorgulanabilir

## 5. Varyantlı Ürün Aktarımı

- Aynı `productMainId` → aynı ürünün varyantları
- Her varyantın `barcode`, `stockCode` ve varyant özelliği (beden/renk) farklı olmalı
- Diğer tüm alanlar (title, description, brandId, categoryId vb.) aynı olmalı
- **Slicer** özellikler: ayrı ürün sayfası oluşturur (renk)
- **Varianter** özellikler: aynı sayfa içinde seçenek (beden)

## 6. HTTP Status Kodları

| Kod | Anlam |
|-----|-------|
| 200 | Başarılı (asenkron işlemlerde "sıraya alındı") |
| 400 | Eksik/geçersiz alan |
| 401 | Hatalı API Key |
| 403 | User-Agent eksik veya yetki dışı |
| 404 | Yanlış URL |
| 429 | Rate limit aşıldı |
| 500 | Sunucu hatası (tekrar dene) |
| 503 | Servis kesintisi (stage'de IP yetkisi yoksa) |

## 7. Önemli Kurallar

- `salePrice` ≤ `listPrice` olmalı
- `vatRate` sadece 0, 1, 10 veya 20 olabilir
- `currencyType` = "TRY"
- Görseller **HTTPS** zorunlu, max 8 adet, önerilen 1200x1800px @96dpi
- `barcode` max 40 karakter, özel karakter: sadece `.` `-` `_`
- `title` max 100 karakter
- `description` max 30.000 karakter (HTML destekli)
- `stockCode` max 100 karakter, satıcıya özel benzersiz
- Onaylanmış ürünlerde `barcode`, `productMainId`, `brandId`, `categoryId`, slicer/varianter özellikler **güncellenemez**

## 8. Pazaryeri İş Modelleri

### Türkiye Pazaryeri
Türkiye'deki satıcıların Türkiye pazarındaki ürün listeleme ve satış operasyonları.

### Mikro İhracat Modeli
600 kg ağırlığı ve 30.000 EUR değeri geçmeyen, kesin ve satış amacıyla yapılan ihracat işlemlerinin **ETGB (Elektronik Ticaret Gümrük Beyanı)** ile yapılması.

**Desteklenen Ülkeler:**
- **Körfez:** Suudi Arabistan, BAE, Katar, Kuveyt, Umman, Bahreyn
- **Avrupa:** Romanya, Bulgaristan, Yunanistan, Ukrayna, Moldova, Sırbistan
- **Azerbaycan**

### Mikro İhracat Entegrasyon Kuralları

| Konu | Kural |
|------|-------|
| **Ürün listeleme** | Ayrı endpoint yok — Türkiye ürün listesinden mikro ihracata açılır (panel veya Excel ile) |
| **Stok/Fiyat** | Ayrı endpoint yok — Türkiye güncellemeleri otomatik mikro ihracata yansır |
| **Sipariş çekme** | Aynı `getShipmentPackages` endpoint'i, mikro siparişlerde `"micro": true` döner |
| **Ülke tespiti** | `countryCode` alanından kontrol edilir (sadece `micro: true` ile birlikte) |
| **Fatura** | Mikro ihracat faturalarında KDV **%0** zorunlu, fatura tipi İstisna (301 - 11/1-a Mal İhracatı) |
| **Fatura zorunluluğu** | Fatura oluşturulmadan kargo etiketi **bastırılamaz** |
| **Fatura alanları** | `invoiceNumber` ve `invoiceDateTime` **zorunlu** (normal siparişlerde opsiyonel) |
| **GTIP kodu** | Ürünün ihracata açılması için GTIP (Gümrük Tarife İstatistik Pozisyonu) kodu ve menşei bilgisi gerekli |
| **Azerbaycan/Avrupa** | Sözleşme onay süreci gerekli (Firma Bilgileri menüsünden) |

### Fatura → Etiket Sırası (KRİTİK)

```
Sipariş oluştu → Fatura oluştur (zorunlu) → Kargo etiketi bastır → Kargoya ver
```

**Fatura oluşturulmadan kargo etiketi bastırılamaz.** Bu hem Türkiye hem mikro ihracat siparişleri için geçerli.

## 9. API Versiyon Tercihi

| Özellik | V1 | V2 |
|---------|-----|-----|
| Ürün yaratma | `/sellers/{id}/products` | `/sellers/{id}/v2/products` |
| cargoCompanyId | Zorunlu | **Kaldırıldı** |
| currencyType | Zorunlu ("TRY") | **Kaldırıldı** (varsayılan TRY) |
| Onaylı/Onaysız güncelleme | Tek endpoint | **Ayrı endpoint'ler** |
| Kategori özellik değerleri | attributes response'unda | **Ayrı paginated endpoint** |
| `allowMultipleAttributeValues` | Yok | **Var** |
| `contentId` | Yok | Onaylı ürün güncellemede zorunlu |
| `nextPageToken` | Yok | 10.000+ ürün için cursor pagination |
| Önerilen | Eski entegrasyonlar | **Yeni entegrasyonlar için V2 kullanılmalı** |
