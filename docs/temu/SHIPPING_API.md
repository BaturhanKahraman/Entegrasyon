# Temu API - Kargo & Lojistik (Shipping API)

## Genel Bakis

Temu API uzerinden kargo bilgisi guncelleme, teslimat takibi ve lojistik islemleri yapilabilir. Tum cagrilar `POST /openapi/router` endpoint'ine `type` parametresi ile yonlendirilir.

---

## Bilinen Endpoint'ler

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| `bg.order.decryptshippinginfo.get` | Şifrelenmis kargo bilgisi cozme | Dogrulanmis |
| `bg.logistics.*` | Kargo/lojistik islemleri | TBD - Tam liste incelenecek |
| TBD | Kargo firmasi bilgisi guncelleme | TBD |
| TBD | Takip numarasi ekleme | TBD |
| TBD | Kargo durumu sorgulama | TBD |

---

## Kargo Modeli

Temu'nun kargo modeli bolgeye gore degisir:

### Cross-Border (Sinir Otesi)
- Satici urunu Temu'nun deposuna veya belirlenen lojistik noktasina gonderir
- Temu, son mil teslimatini yonetir

### Local Fulfillment (Lokal)
- Satici veya 3PL partner, urunu dogrudan musteriye gonderir
- Kargo takip bilgisi API uzerinden guncellenir
- Turkiye pazari icin bu model gecerli olabilir

---

## Kargo Bilgisi Guncelleme

TBD - Kargo takip numarasi ve firma bilgisi guncelleme endpoint'i API dokumanlarindan incelenecek.

Beklenen parametreler:
- Sipariş numarasi
- Kargo firma kodu
- Takip numarasi (tracking number)

---

## Kargo Bilgisi Cozumleme

```
POST /openapi/router
type: bg.order.decryptshippinginfo.get
```

Musteri adres ve telefon bilgileri Şifrelenmis olarak saklanir. Kargo etiketi olusturmak veya gonderim yapmak icin bu endpoint ile cozumleme gereklidir.

---

## Acik Sorular

1. Turkiye pazari icin hangi kargo modeli kullaniliyor? (cross-border / local)
2. Desteklenen kargo firmalari listesi nasil alinir?
3. Kargo etiketi API uzerinden olusturulabilir mi?
4. Kargo durumu webhook/callback ile bildirilir mi?
5. Teslimat suresi siniri var mi? (SLA)
6. Birden fazla paket ile gonderim desteklenir mi?
