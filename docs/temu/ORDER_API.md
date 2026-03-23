# Temu API - Siparis Yonetimi (Order API)

## Genel Bakis

Temu API uzerinden siparis listeleme, siparis detayi sorgulama ve siparis durumu guncelleme islemleri yapilabilir. Tum cagrilar `POST /openapi/router` endpoint'ine `type` parametresi ile yonlendirilir.

---

## Bilinen Endpoint'ler

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| `bg.order.list.v2.get` | Siparis listesi sorgulama | Dogrulanmis (web arastirmasi) |
| `bg.order.decryptshippinginfo.get` | Sifrelenmis kargo bilgisi cozme | Dogrulanmis (web arastirmasi) |
| TBD | Siparis detayi | TBD - API dokumanlarindan incelenecek |
| TBD | Siparis durumu guncelleme | TBD |

---

## Siparis Listeleme

```
POST /openapi/router
type: bg.order.list.v2.get
```

**Bilinen Parametreler:**
- Tarih araligina gore filtreleme
- Siparis durumuna gore filtreleme
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

Temu, musteri kisisel verilerini (adres, telefon) sifrelenmis olarak saklar. Bu endpoint ile gercek kargo bilgilerine erisilir.

---

## Siparis Durumlari

TBD - Tam siparis durum listesi API dokumanlarindan incelenecek.

Beklenen durumlar (marketplace genel pattern):
- Odenme Bekliyor
- Odendi / Hazirlaniyor
- Kargoya Verildi
- Teslim Edildi
- Iptal Edildi
- Iade Surecinde

---

## Acik Sorular

1. Siparis listeleme tarih araligi siniri nedir? (ornek: Ciceksepeti max 2 hafta)
2. Sayfalama 0-based mi, 1-based mi?
3. Siparis icindeki alt kalemlerin (sub-order) yapisi nasil?
4. Kargo bilgisi her zaman sifreli mi donuyor?
5. Siparis durumu webhook/callback ile bildirilir mi?
6. Fatura bilgileri siparis icerisinde mi ayri endpoint'te mi?
