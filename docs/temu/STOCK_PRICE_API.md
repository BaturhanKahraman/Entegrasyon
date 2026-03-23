# Temu API - Stok & Fiyat Guncelleme (Stock & Price API)

## Genel Bakis

Temu API uzerinden SKU bazli stok ve fiyat guncellemeleri yapilabilir. Tum cagrilar `POST /openapi/router` endpoint'ine `type` parametresi ile yonlendirilir.

---

## Bilinen Endpoint'ler

| Method (type) | Aciklama | Durum |
|----------------|----------|-------|
| `bg.local.goods.priceorder.change.sku.price` | SKU fiyat guncelleme | Dogrulanmis (web arastirmasi) |
| TBD | SKU stok guncelleme | TBD - API dokumanlarindan incelenecek |
| TBD | Toplu (batch) stok/fiyat guncelleme | TBD |

---

## SKU Fiyat Guncelleme

```
POST /openapi/router
type: bg.local.goods.priceorder.change.sku.price
```

Bu endpoint, belirli bir SKU'nun fiyatini guncellemek icin kullanilir.

**Beklenen Parametreler:**

```json
{
  "type": "bg.local.goods.priceorder.change.sku.price",
  "app_key": "...",
  "timestamp": "...",
  "access_token": "...",
  "sign": "...",
  "sku_id": 123456,
  "price": 1999
}
```

> **UYARI:** Parametre detaylari DOGRULANMAMISTIR. `bg.local.goods.*` namespace'i lokal pazar operasyonlarini isaret eder.

---

## Stok Guncelleme

TBD - Stok guncelleme endpoint'i ve parametreleri API dokumanlarindan incelenecek.

Beklenen pattern (diger marketplace'lerden turetilmistir):
- SKU bazli stok miktari set etme (absolute)
- veya stok miktari artirma/azaltma (incremental)

---

## Onemli Notlar

### Fiyat Formati
- Fiyatlar muhtemelen **cent (kurus)** cinsinden integer olarak gonderilir
- TBD - Para birimi ve format dogrulanmalidir

### Stok/Fiyat Sync Stratejisi
- Diger entegrasyonlardaki gibi EventChannel ile tetiklenen periyodik sync
- Batch guncelleme destegi varsa tercih edilmeli (API round-trip azaltma)
- Rate limit (20 QPS) goz onunde tutulmali

### Fiyat Is Kurallari
- TBD - Fiyat degisim siniri var mi? (ornek: Ciceksepeti %50 siniri)
- TBD - ListPrice vs SalesPrice ayrimi var mi?

---

## Acik Sorular

1. Stok guncelleme senkron mu, asenkron mu?
2. Toplu guncelleme (batch) destegi var mi? Max kac SKU/batch?
3. Fiyat para birimi nedir? (TRY, USD, EUR — bolgeye gore mi?)
4. Fiyat cent cinsinden mi, birim fiyat mi?
5. Minimum/maximum fiyat sinirlari var mi?
6. Stok sifir oldugunda urun otomatik pasife alinir mi?
