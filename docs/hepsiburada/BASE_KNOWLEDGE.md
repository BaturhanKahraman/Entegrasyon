# Hepsiburada Marketplace API — Base Knowledge

## 1. Ortamlar

| Ortam | Katalog API URL | Listeleme API URL | Ürün Güncelleme API URL | Not |
|-------|----------------|-------------------|------------------------|-----|
| **TEST (SIT)** | `https://mpop-sit.hepsiburada.com/product` | `https://listing-external-sit.hepsiburada.com` | `https://mpop-sit.hepsiburada.com/ticket-api` | Test ortamı |
| **PROD** | `https://mpop.hepsiburada.com/product` | `https://listing-external.hepsiburada.com` | `https://mpop.hepsiburada.com/ticket-api` | URL'deki "-sit" kaldırılır |

> **Not:** Canlı ortam URL'leri, endpoint içindeki "-sit" ifadesi kaldırılarak oluşturulur.

## 2. Yetkilendirme (Authentication)

**Yöntem:** HTTP Basic Authentication

```
Authorization: Basic base64({username}:{password})
```

**Zorunlu Header — User-Agent:**
```
User-Agent: {entegrator-adi}
```

**Credentials:** Test ortam bilgileri mail ile iletilir. Canlı ortam bilgileri ayrıca verilir.

**Servis Anahtarı:** Entegratöre Servis Anahtarı Ekleme/Görüntüleme işlemleri gereklidir.

## 3. Rate Limiting

| Endpoint Grubu | Limit |
|----------------|-------|
| Kategori Bilgilerini Alma | 200 istek / 1 dakika (IP başına) |
| Özellik Değerini Alma | 50 istek / 1 dakika (IP başına) |
| Ürüne Ait Statü Bilgisi Çekme | 500 istek / 1 saniye (IP başına) |
| Komisyon Bilgisi Sorgulama | 240 istek / 1 dakika (merchant başına, max 50 SKU/istek) |
| Listing Güncelleme | Aynı anda max 5 bekleyen işlem, tek istekte max 4000 SKU |

**429 Too Many Requests** — Rate limit aşıldığında döner.

## 4. API Yapısı — Üç Ayrı Servis

Hepsiburada API'si üç bağımsız servisten oluşur:

### 4.1 Katalog Ürün Entegrasyonu
- **Base URL:** `https://mpop-sit.hepsiburada.com/product`
- **Amaç:** Kategori ağacı, özellikler, ürün yaratma/eşleştirme, durum sorgulama
- **Ürün gönderimi:** JSON dosyası `multipart/form-data` olarak gönderilir

### 4.2 Ürün Güncelleme Entegrasyonu
- **Base URL:** `https://mpop-sit.hepsiburada.com/ticket-api`
- **Amaç:** Mevcut ürünlerin bilgilerini güncelleme (isim, açıklama, görseller, özellikler)
- **Güncelleme:** JSON dosyası `multipart/form-data` olarak gönderilir

### 4.3 Listeleme Entegrasyonu
- **Base URL:** `https://listing-external-sit.hepsiburada.com`
- **Amaç:** Listing CRUD, fiyat/stok/teslimat güncelleme, buybox, kilit kaldırma
- **Format:** JSON ve XML desteklenir

## 5. Ürün Yaşam Döngüsü

```
Ürün Gönderimi → TrackingId alınır → Durum Sorgulama
                                        ├── İncelenecek (WAITING) → HB ekibi kontrol eder
                                        ├── Eşleşen (PRE_MATCHED) → Onay/Red gerekir
                                        ├── Eksik Bilgi (MISSING_INFO) → Revize et, tekrar gönder
                                        ├── Katalog Sürecinde → HB işleme aldı, güncelleme yapılamaz
                                        ├── Görev Açılmış → Hatalı bilgi, güncelleme gerekli
                                        ├── Satışa Hazır (MATCHED) → Listing oluştu
                                        └── Yaratıldı (CREATED) → Listing oluştu
```

### Ürün Statüleri

| Statü | API Değeri | Açıklama | API Güncelleme |
|-------|-----------|----------|----------------|
| İncelenecek | WAITING | HB ekibinin kontrolünü bekliyor | Evet |
| Ürün bilgileri eksik | MISSING_INFO | Eksik bilgi var, revize edilmeli | Evet |
| Katalog Sürecinde | IN_EXTERNAL_PROGRESS | HB ekibi işleme aldı | **Hayır** |
| Eşleşen | PRE_MATCHED | HB'de mevcut ürünle eşleşti, onay/red bekleniyor | Evet |
| Ön Katalog Eşleşen | MATCHED_WITH_STAGED | HB işleme aldı | **Hayır** |
| Satışa Hazır | MATCHED | Listing oluştu | **Hayır** |
| Görev Açılmış | — | Hatalı bilgi, güncelleme gerekli | Evet |
| Yaratıldı | CREATED | Listing oluştu | — |
| Reddedilen | REJECTED | Eşleşme reddedildi | — |
| Engelli | BLOCKED | Engellendi | — |

## 6. İki Farklı Ürün Yükleme Yöntemi

### 6.1 Yeni Ürün Yaratma (Tam Süreç)
1. **Kategori Bilgilerini Alma** → leaf=true, status=ACTIVE, available=true
2. **Kategori Özelliklerini Alma** → mandatory=true olanlar zorunlu
3. **Özellik Değerlerini Alma** → type=enum olanlar için
4. **Ürün Bilgisi Gönderme** → JSON dosyası olarak
5. **Ürün Durumu Sorgulama** → trackingId ile
6. **Eşleşen Statü Onay/Red** → Gerekirse

### 6.2 Hızlı Ürün Yükleme
- Sadece HB katalogunda **mevcut** ürünler için
- Minimum bilgi: merchantId, merchantSku, productName, barcode
- Kategori ağacı çekmeye gerek yok
- HB'de bulunmayan ürünler işleme alınmaz

## 7. Varyantlı Ürün Gönderimi

- Aynı `VaryantGroupID` → aynı ürünün varyantları
- Renk, Beden vb. özelliklere göre ayrılan ürünler için aynı VaryantGroupID gönderilir
- Varyantsız ürünler için unique bir VaryantGroupID atanabilir
- Varyant oluşturabilmek için kategori özelliklerindeki varyant attribute'larından en az 1'i dolu gönderilmeli

## 8. Fiyat Kuralları

Canlı ortamda yanlış fiyatlandırmalara karşı threshold mekanizması:

| Fiyat Aralığı | Max Artış |
|---------------|-----------|
| 0 – 50 TL | %250 |
| 50 – 100 TL | %150 |
| 100 – 200 TL | %120 |
| 200 – 500 TL | %100 |
| 500 – 2000 TL | %90 |
| 2000 TL+ | %80 |

- Kuruşlu fiyat girişinde **virgül** ile ayrılmalı (ör: `14,50`)
- Virgülden sonra max 2 hane
- Threshold dışı fiyat → ürün 0 fiyat/0 stok ile satışa hazır olur veya listing kilitlenir

## 9. Görsel Kuralları

- Format: **PNG** veya **JPG** (GIF ve diğer formatlar kabul edilmez)
- Fon: Beyaz veya açık renk
- Yazı, logo, filigran olmamalı
- Ürün görselin %80'ini kaplamalı
- Beyaz fon çıkarıldıktan sonra en az 1 kenar min 250 piksel
- Max 5 görsel + 1 video (sadece MP4)
- Image URL'leri ulaşılabilir olmalı, aksi halde 5 kere denenir

## 10. Barkod Kuralları

- **EAN13** formatında, 13 karakter
- Kendi içinde algoritması olan uluslararası ürün numarası
- Her ürün için tekil olmalı
- Doğru barkod eşleşme sürecinde kritik önem taşır

## 11. MerchantSku Kuralları

- **Büyük harf** olarak gönderilmeli
- Boşluk bırakılmadan giriş yapılmalı
- Küçük harf gönderilirse otomatik büyük harfe dönüştürülür

## 12. HTTP Status Kodları

| Kod | Anlam |
|-----|-------|
| 200 | Başarılı |
| 400 | Geçersiz istek / eksik parametre |
| 401 | Unauthorized — Şifre/credentials hatalı |
| 404 | URL hatalı |
| 405 | HTTP metod hatası |
| 429 | Rate limit aşıldı |
| 500 | Sunucu hatası — Ticket açılmalı |

## 13. Ortak Response Yapısı (Katalog API)

```json
{
  "success": true,
  "code": 0,
  "version": 1,
  "message": "",
  "data": { }
}
```

- `success`: true = başarılı, false = hata
- `code`: 0 = başarı, diğer = hata kodu
- `message`: Hata mesajı (başarılıysa boş)
- `data`: Yanıt verisi

### Sayfalı (Paginated) Response

```json
{
  "success": true,
  "code": 0,
  "version": 1,
  "message": "",
  "totalElements": 5000,
  "totalPages": 5,
  "number": 0,
  "numberOfElements": 1000,
  "first": true,
  "last": false,
  "data": [ ]
}
```

## 14. Canlı Ortam Geçiş Süreci

1. Test ortamında tüm endpoint'leri başarıyla tamamla
2. Test ortamından alınan trackingId'yi Hepsiburada'ya ilet
3. **Yol:** Canlı Ortam Merchant Panel → Yardım Merkezi → Talepler → API Entegrasyon → API Entegrasyon Teknik Destek
4. Canlı ortam bilgileri verilir
5. Canlı ortamda kategori ağacını tekrar listele (test ile canlı farklı olabilir)
