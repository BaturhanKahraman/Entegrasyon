# Temu API - Urun Yonetimi (Product API)

## Genel Bakis

Temu API uzerinden urun CRUD islemleri, kategori/marka yonetimi ve urun durumu kontrolleri yapilabilir. Tum cagrilar `POST /openapi/router` endpoint'ine `type` parametresi ile yonlendirilir.

---

## Bilinen Endpoint'ler

### Urun Islemleri

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| `bg.goods.add` | Yeni urun olusturma | TBD - Parametre detaylari incelenecek |
| `bg.goods.edit` / `bg.goods.edit.v2` | Urun guncelleme | TBD |
| `bg.goods.get` | Urun bilgisi sorgulama | TBD |
| `bg.goods.sales.status` | Urun satis durumu guncelleme (on/off) | TBD |
| `bg.goods.sku.get` | SKU bilgisi sorgulama | TBD |

> **Not:** Yukaridaki method isimleri Pinduoduo/Temu API pattern'inden turetilmistir. Gercek method isimleri resmi dokumantasyondan dogrulanmalidir.

### Kategori Islemleri

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| `bg.goods.cats.get` | Kategori agaci listeleme | TBD |
| `bg.goods.cat.template.get` | Kategori ozellik sablonu | TBD |

### Marka Islemleri

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| TBD | Marka listeleme | TBD - API dokumanlarindan incelenecek |

---

## Urun Ekleme (Tahmini Yapi)

```
POST /openapi/router
type: bg.goods.add (veya benzeri)
```

Beklenen parametreler (Pinduoduo pattern'inden turetilmistir):

```json
{
  "type": "bg.goods.add",
  "app_key": "...",
  "timestamp": "...",
  "access_token": "...",
  "sign": "...",
  "cat_id": 12345,
  "goods_name": "Urun Adi",
  "goods_desc": "Urun Aciklamasi",
  "sku_list": [
    {
      "thumb_url": "https://...",
      "price": 1999,
      "stock": 100,
      "outer_id": "SKU-001",
      "spec_id_list": "[{\"parent_id\":1,\"id\":10}]"
    }
  ],
  "goods_properties": [...],
  "image_url_list": [...]
}
```

> **UYARI:** Yukaridaki JSON yapisi DOGRULANMAMISTIR. Gercek parametre yapisi resmi dokumantasyondan alinmalidir.

---

## Urun Sorgulama

```
POST /openapi/router
type: bg.goods.get (veya benzeri)
```

Beklenen response:

```json
{
  "success": true,
  "result": {
    "goods_id": 123456,
    "goods_name": "...",
    "goods_desc": "...",
    "cat_id": 12345,
    "sku_list": [...],
    "status": 1,
    "image_url_list": [...]
  }
}
```

> **UYARI:** Yukaridaki response yapisi DOGRULANMAMISTIR.

---

## Bilinen Ozellikler

### Gorsel Gereksinimleri
- TBD - Gorsel boyut, format ve adet sinirlari API dokumanlarindan incelenecek

### Fiyat Formati
- Fiyatlar tipik olarak **cent (kurus)** cinsinden integer olarak gonderilir (Pinduoduo pattern'i)
- TBD - Dogrulanmalidir

### SKU & Varyant
- Her urun birden fazla SKU'ya sahip olabilir
- SKU'lar spec (ozellik) kombinasyonlari ile tanimlanir
- `outer_id` ile dis sistem SKU Eşleşmesi saglanir

---

## Acik Sorular

1. Urun ekleme ve guncelleme senkron mu, asenkron (batch) mu?
2. Kategori agaci kac seviye derinlikte?
3. Gorsel upload ayri bir endpoint mi yoksa URL referansi mi?
4. Urun onay sureci var mi? (moderation)
5. Varyant (spec) yapisi nasil tanimlaniyor?
6. Lokal pazaryeri (Turkiye) icin ozel alan gereksinimleri neler?
