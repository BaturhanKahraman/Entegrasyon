# Temu API - Iade & Iptal (Returns API)

## Genel Bakis

Temu API uzerinden iade talepleri listeleme, onaylama/reddetme ve iptal islemleri yapilabilir. Tum cagrilar `POST /openapi/router` endpoint'ine `type` parametresi ile yonlendirilir.

---

## Bilinen Endpoint'ler

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| `bg.aftersale.*` | Satis sonrasi (iade/iptal) islemleri | TBD - Tam liste incelenecek |
| TBD | Iade talebi listeleme | TBD |
| TBD | Iade talebi onaylama | TBD |
| TBD | Iade talebi reddetme | TBD |
| TBD | Iptal talebi islemleri | TBD |

---

## Iade Sureci (Beklenen Akis)

Diger pazaryeri entegrasyonlarindan turetilmistir:

1. Musteri iade talebi olusturur
2. Satici, API uzerinden iade taleplerini listeler
3. Satici, talebi onaylar veya reddeder
4. Onaylanan iadelerde musteri urunu iade eder
5. Urun alindiktan sonra iade islemleri tamamlanir
6. Iade tutari musteriye iade edilir

---

## Iptal Islemleri

TBD - Siparis iptal endpoint'leri ve is kurallari API dokumanlarindan incelenecek.

Beklenen durumlar:
- Kargoya verilmeden once iptal
- Kargodayken iptal (kargo iade)
- Teslim sonrasi iade

---

## Acik Sorular

1. Iade talebi sureci tam olarak nasil isliyor?
2. Iade nedenleri (reason codes) nasil tanimlaniyor?
3. Kismi iade (partial refund) destekleniyor mu?
4. Iade kargo masrafi kime ait?
5. Iade suresi siniri nedir?
6. Otomatik iade onay sureci var mi? (satici cevaplamazsa)
7. Iade webhook/callback bildirimi var mi?
