# PttAVM API — Genel Bilgi ve Yetkilendirme

## Genel Bakis

PttAVM entegrasyonu iki ayri API uzerinden calisir:

| API | Base URL | Auth |
|-----|----------|------|
| Katalog / Listeleme / Siparis | `https://integration-api.pttavm.com` | Api-Key + Access-Token |
| Kargo | `https://shipment.pttavm.com` | Basic Auth |

Tum endpointler REST + JSON tabanlidir.

## Yetkilendirme

### Katalog / Listeleme / Siparis API

Her istekte asagidaki headerlar zorunludur:

| Header | Zorunlu | Aciklama |
|--------|---------|----------|
| `Api-Key` | Evet | Magaza API anahtari |
| `Access-Token` | Evet | Magaza erisim tokeni |
| `Content-Type` | Evet | `application/json` |
| `X-Correlation-Id` | Evet | Istek izleme icin benzersiz ID (GUID) |

### Kargo API

Basic Auth kullanir. `Authorization: Basic base64(username:password)` header'i gonderilir.

## Iletisim

SOAP servisleri ile ilgili erisime ihtiyac icin: `entegrasyon@pttavm.com`

## Dokumantasyon Kaynaklari

- Katalog: https://developers.pttavm.com/tr/katalog-entegrasyonu
- Listeleme: https://developers.pttavm.com/tr/listeleme-entegrasyonu
- Siparis: https://developers.pttavm.com/tr/siparis-entegrasyonu
- Kargo: https://developers.pttavm.com/tr/kargo-entegrasyonu
