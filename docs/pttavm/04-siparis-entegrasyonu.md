# PttAVM API — Siparis Entegrasyonu

Base URL: `https://integration-api.pttavm.com`

## 1. Kargo Profil Listesi

**GET** `/api/v1/shipping/cargo-profiles`

### Response

```json
{
  "cargoProfiles": [
    {
      "id": 1,
      "name": "string",
      "description": "string",
      "type": "string"
    }
  ]
}
```

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| id | integer | Profil ID |
| name | string | Profil adi |
| description | string | Aciklama |
| type | string | Gonderim profili kategorisi (birincil/ikincil) |

---

## 2. Kargo Bilgi Listesi

**GET** `/api/v1/orders/{orderId}/cargo-infos`

### Response

```json
[
  {
    "productId": "string",
    "shopId": 1,
    "inCargo": "string",
    "referenceCode": "string",
    "currentState": "string",
    "deliveryInfo": "string"
  }
]
```

### inCargo Degerleri

| Deger | Aciklama |
|-------|----------|
| null | Henuz kargoya verilmemis |
| 1 | Kargo dagitimda |
| 2 | Kargo tedarikcide |
| 3 | Kargo PTT subesinde |

### currentState Degerleri

| Deger | Aciklama |
|-------|----------|
| kargo_yapilmasi_bekleniyor | Hazilanacak |
| havale_onayi_bekleniyor | Odeme bekleniyor |
| gondericisine_teslim_edildi | Kargoya verildi |
| gonderilmis | Gonderildi |
| tamamlandi | Teslim edildi |
| iptal | Iptal |
| iade | Iade |
| onay_surecinde | Onay bekleniyor |
| odeme_gecersiz | Odeme gecersiz |

### deliveryInfo Degerleri

| Deger | Aciklama |
|-------|----------|
| null | Henuz bilgi yok |
| 1 | PTT subesinde bekliyor |
| 2 | PTT subesinden teslim alindi |

---

## 3. Siparis Detay

**GET** `/api/v1/orders/{orderId}`

### Response (Temel Alanlar)

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| SiparisNo | string | Siparis numarasi |
| SiparisDurumu | string | Durum (currentState degerleri) |
| KdvDahilToplamTutar | decimal | KDV'li toplam |
| KargoTutari | decimal | Kargo bedeli |
| siparisUrunler | array | Urun listesi |
| FaturaMusteriAdi | string | Fatura sahibi adi |
| FaturaMusteriSoyadi | string | Fatura sahibi soyadi |

---

## 4. Siparis Kontrol V2 (Arama)

**GET** `/api/v1/orders/search`

### Query Parametreleri

| Parametre | Tur | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| startDate | DateTime | Evet | Baslangic tarihi |
| endDate | DateTime | Evet | Bitis tarihi |
| isActiveOrders | boolean | Evet | true=sadece hazirlanmayanlar, false=tumu |

### Onemli Kurallar

- Tarih araligi maks 40 gun
- Bitis tarihi baslangictan once olamaz

### Response

JSON dizisi olarak siparis detaylari doner (musteri bilgisi, urun kirilimi, fatura, kargo barkodlari).

---

## 5. Fatura Gonder

**POST** `/api/v1/orders/{orderId}/invoice`

### Request

```json
{
  "lineItemId": [1, 2],
  "content": "base64-pdf-string",
  "url": "https://example.com/fatura.pdf"
}
```

| Parametre | Tur | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| lineItemId | int[] | Evet | Siparis urun ID'leri |
| content | string | Opsiyonel | PDF Base64 |
| url | string | Opsiyonel | PDF URL |

URL varsa kullanilir, yoksa Base64 content gonderilir. PDF formati zorunlu.

### Response

```json
{
  "success": true,
  "error_Message": null
}
```
