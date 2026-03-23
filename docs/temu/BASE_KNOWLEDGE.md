# Temu Partner Platform API - Temel Bilgiler

## Genel Bakis

Temu, PDD Holdings (Pinduoduo) bunyesindeki global e-ticaret platformudur. Partner Platform API uzerinden ucuncu parti uygulamalar, satis operasyonlarini (urun, siparis, stok, kargo, iade) otomatize edebilir.

**API Stili:** REST-like, tek router endpoint uzerinden method-based routing (Pinduoduo/Temu pattern)
**Request Format:** JSON (POST)
**Response Format:** JSON

---

## Authentication

Temu API, **App Key + App Secret + Access Token + MD5 Sign** mekanizmasi kullanir.

### Kimlik Bilgileri

| Alan | Aciklama |
|------|----------|
| `app_key` | Uygulama anahtari (Temu Seller Center'dan alinir) |
| `app_secret` | Uygulama sifresi (imza hesaplamasinda kullanilir) |
| `access_token` | Satici yetkilendirmesi sonrasi alinan erisim tokeni |

### Access Token Alma Akisi (OAuth-benzeri)

1. Satici, Temu Seller Center'da uygulamayi yetkilendirir
2. Yetkilendirme sonrasi bir `code` redirect URL'e gonderilir
3. `code` ile `bg.open.accesstoken.create` endpoint'i cagirilarak `access_token` alinir
4. Token suresi: **3 ay** — suresi dolmadan refresh edilmeli
5. Her bolge (territory) icin ayri access_token gerekir

### Imza (Sign) Hesaplama

1. Tum parametreleri key'e gore alfabetik sirala
2. Her key-value ciftini birlestir: `key1value1key2value2...`
3. Baslangic ve sona `app_secret` ekle: `{app_secret}key1value1key2value2{app_secret}`
4. MD5 hash hesapla, **UPPERCASE hex** formatina cevir
5. Sonuc `sign` parametresi olarak request'e eklenir

### Request Yapisi

Tum API cagrilari tek bir router endpoint'e POST olarak gonderilir:

```
POST {base_url}/openapi/router
Content-Type: application/json

{
  "type": "bg.goods.get",           // API method adi
  "app_key": "xxx",
  "timestamp": "1711234567",         // Unix timestamp (saniye)
  "access_token": "yyy",
  "sign": "ABC123...",               // MD5 imza (uppercase hex)
  "data_type": "JSON",
  // ... method-specific parametreler
}
```

> **Not:** Tum endpointler ayni URL'e gider, `type` parametresi hangi API metodunun cagrildigini belirler.

---

## Base URL'ler

| Bolge | Base URL | Aciklama |
|-------|----------|----------|
| EU | `https://openapi-b-eu.temu.com` | Avrupa (Turkiye dahil) |
| US | `https://openapi-b-us.temu.com` | Amerika |
| Global | `https://openapi-b-global.temu.com` | Meksika, Japonya ve diger bolgeler |

**Router Path:** `/openapi/router`

> **Onemli:** Turkiye operasyonlari icin **EU** endpoint kullanilmalidir.

---

## Rate Limit

- Varsayilan rate limit: **20 QPS** (queries per second) per `app_key`
- Rate limit asiminda `429 Too Many Requests` veya ozel hata kodu donebilir
- TBD - Detayli rate limit politikasi (endpoint bazli) API dokumanlarindan incelenecek

---

## Hata Yonetimi

Temu API response'lari asagidaki genel yapiyi takip eder:

```json
{
  "success": true/false,
  "error_code": 0,
  "error_msg": "",
  "result": { ... }
}
```

Bilinen hata kodlari:
- `1001` — Parametre hatasi
- TBD - Tam hata kodu listesi API dokumanlarindan incelenecek

---

## API Method Namespace'leri

Temu API method'lari `bg.` prefix'i ile baslar ve domain'e gore gruplanir:

| Namespace | Kapsam |
|-----------|--------|
| `bg.open.*` | Yetkilendirme (access token create/refresh) |
| `bg.goods.*` | Urun yonetimi |
| `bg.local.goods.*` | Lokal urun yonetimi (fiyat, stok) |
| `bg.order.*` | Siparis yonetimi |
| `bg.logistics.*` | Kargo/lojistik |
| `bg.aftersale.*` | Iade/iptal islemleri |

> **Not:** Tam endpoint listesi resmi dokumantasyondan alinacaktir. Yukaridaki namespace'ler web arastirmasindan elde edilen partial bilgilerdir.

---

## Onemli Notlar

1. **Dokumantasyon Durumu:** Temu API dokumantasyonu diger pazaryerlerine gore (Trendyol, Hepsiburada vb.) daha az detayli ve kamusal erisime kisitlidir. Resmi Partner Platform (partner.temu.com / partner-eu.temu.com) JavaScript gerektiren SPA uygulamasidir.

2. **Pinduoduo Mirasi:** Temu, PDD Holdings'in global markasi oldugu icin API yapisi Pinduoduo Open Platform ile buyuk benzerlikler tasir (tek router endpoint, MD5 sign, type parametresi).

3. **Bolge Bazli Izolasyon:** Her bolge icin ayri access_token gerekir. Turkiye operasyonlari EU endpoint'i uzerinden yurutulur.

4. **Self-Developed App:** API erisimi icin Temu Seller Center'da "self-developed app" olusturulmalidir (Apps and Services > Manage your apps > Add a self-developed app).

---

## Kaynak Linkler

- Temu Partner Platform (EU): https://partner-eu.temu.com/
- Temu Partner Platform (US): https://partner.temu.com/
- API Reference: https://partner-eu.temu.com/documentation?menu_code=7289390cfd724be4a196f11ebe45a896
- Seller Authorization Guide: https://partner-eu.temu.com/documentation?menu_code=85762c6ccc5a4dbc8c023ea5e10c6dc0
