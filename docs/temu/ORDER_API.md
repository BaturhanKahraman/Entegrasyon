# Temu API - Sipariş Yonetimi (Order API)

## Genel Bakis

Temu API uzerinden Sipariş listeleme, Sipariş detayi sorgulama ve Sipariş durumu guncelleme islemleri yapilabilir. Tum cagrilar `POST /openapi/router` endpoint'ine `type` parametresi ile yonlendirilir.

---

## Bilinen Endpoint'ler

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| `bg.order.list.v2.get` | Sipariş listesi sorgulama | Dogrulanmis (web arastirmasi) |
| `bg.order.decryptshippinginfo.get` | Şifrelenmis kargo bilgisi cozme | Dogrulanmis (web arastirmasi) |
| TBD | Sipariş detayi | TBD - API dokumanlarindan incelenecek |
| TBD | Sipariş durumu guncelleme | TBD |

---

## Sipariş Listeleme

```
POST /openapi/router
type: bg.order.list.v2.get
```

**Bilinen Parametreler:**
- Tarih araligina gore filtreleme
- Sipariş durumuna gore filtreleme
- Sayfalama (page/pageSize)

**Beklenen Response:**

```json
{
  "success": true,
  "result": {
    "total_count": 100,
    "order_list": [
      {
        "order_sn": "PO-xxx",
        "order_status": 2,
        "order_amount": 1999,
        "created_at": 1711234567,
        "item_list": [
          {
            "goods_id": 123,
            "sku_id": 456,
            "goods_name": "...",
            "quantity": 1,
            "price": 1999
          }
        ],
        "shipping_info": { ... }
      }
    ]
  }
}
```

> **UYARI:** Yukaridaki response yapisi DOGRULANMAMISTIR. Gercek yapi resmi dokumantasyondan alinmalidir.

---

## Kargo Bilgisi Cozumleme

```
POST /openapi/router
type: bg.order.decryptshippinginfo.get
```

Temu, musteri kisisel verilerini (adres, telefon) Şifrelenmis olarak saklar. Bu endpoint ile gercek kargo bilgilerine erisilir.

---

## Sipariş Durumlari

TBD - Tam Sipariş durum listesi API dokumanlarindan incelenecek.

Beklenen durumlar (marketplace genel pattern):
- Odenme Bekliyor
- Odendi / Hazirlaniyor
- Kargoya Verildi
- Teslim Edildi
- Iptal Edildi
- Iade Surecinde

---

## Acik Sorular

1. Sipariş listeleme tarih araligi siniri nedir? (ornek: Ciceksepeti max 2 hafta)
2. Sayfalama 0-based mi, 1-based mi?
3. Sipariş icindeki alt kalemlerin (sub-order) yapisi nasil?
4. Kargo bilgisi her zaman Şifreli mi donuyor?
5. Sipariş durumu webhook/callback ile bildirilir mi?
6. Fatura bilgileri Sipariş icerisinde mi ayri endpoint'te mi?
