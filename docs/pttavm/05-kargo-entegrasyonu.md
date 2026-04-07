# PttAVM API — Kargo Entegrasyonu

Base URL: `https://shipment.pttavm.com`
Auth: Basic Auth

> **Not:** Kargo API'si katalog/Sipariş API'sinden farkli bir base URL ve farkli auth mekanizmasi kullanir.

## 1. Depo Listeleme

**POST** `/api/v1/get-warehouse`

Magazaya ait depo verilerini dondurur.

### Response

```json
[
  {
    "id": 100301619,
    "name": "Ana Depo",
    "error": false,
    "msg": "",
    "status": true
  }
]
```

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| id | integer | Depo ID |
| name | string | Depo adi |
| error | boolean | Hata durumu |
| msg | string | Mesaj |
| status | boolean | Islem durumu |

---

## 2. Barkod Olustur

**POST** `/api/v1/create-barcode`

### Request

```json
{
  "orders": [
    {
      "order_id": "PTT-0BO6M672N-180925",
      "warehouse_id": 100301619
    }
  ]
}
```

| Parametre | Tur | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| order_id | string | Evet | Sipariş numarasi |
| warehouse_id | integer | Evet | Depo ID |

### Response

```json
{
  "tracking_id": "cb3fa78459a6edae7dca9be0389a9861",
  "count": 2,
  "code": 200,
  "success": true,
  "message": "",
  "error": false
}
```

### Kurallar

- Ayni Sipariş numarasi farkli depo numaralariyla gonderilemez
- Toplu islem icin orders dizisinde birden fazla nesne eklenebilir
- Donen `tracking_id` sonraki sorgulama islemlerinde kullanilir

### HTTP Durum Kodlari

- 200: Basarili
- 422: Hatali istek

---

## 3. Barkod Olusturma Kontrolu

**POST** `/api/v1/barcode-status`

### Request

```json
{
  "tracking_id": "cb3fa78459a6edae7dca9be0389a9861"
}
```

### Response

```json
{
  "tracking_id": "cb3fa78459a6edae7dca9be0389a9861",
  "status": "completed",
  "data": [
    {
      "order_id": "PTT-12345-180925",
      "barcodes": ["67890"]
    }
  ],
  "error": ""
}
```

### Durum Degerleri

| Durum | Aciklama |
|-------|----------|
| completed | Tamamlandi |
| error | Hata olustu |
| pending | Bekleniyor |

---

## 4. Barkod Etiket Bilgisi

**POST** `/api/v1/get-barcode-tag`

### Request

```json
{
  "barcode": "1234567890",
  "order_id": "PTT-12345-180925",
  "type": null
}
```

| Parametre | Tur | Aciklama |
|-----------|-----|----------|
| barcode | string | Barkod numarasi |
| order_id | string | Sipariş ID |
| type | string | `"zpl"` = Zebra yazici formati, `null` = HTML ciktisi |

### Response

`type: "zpl"` gonderilirse ZPL formati, `null` gonderilirse HTML formati etiket bilgisi doner. Gonderici/alici bilgileri, barkod goruntusu, adres, agirlik ve urun detaylari icerir.

### HTTP Durum Kodlari

- 200: Basarili
- 400: Hatali istek

---

## 5. Barkod Status Guncelle (Dijital Urunler)

**POST** `/api/v1/update-no-shipping-order`

Kargosuz Siparişleri (dijital urunler) "teslim edildi" durumuna gecirir.

### Request

```json
{
  "order_id": "PTT-12345-061024"
}
```

### Response (Basarili)

```json
{
  "message": "string",
  "status": true
}
```

### Response (Basarisiz)

```json
{
  "status": false,
  "message": "Available no shipping order"
}
```

### Kisitlamalar

- Yalnizca dijital urun kategorisindeki Siparişler icin
- Hazirlik veya gonderilmis asamasindaki Siparişler icin gecerli

---

## Kargo Akisi Ozeti

```
1. GetWarehouses() → depo listesi al, depo sec
2. CreateBarcodes(orderId, warehouseId) → tracking_id doner
3. CheckBarcodeStatus(tracking_id) → barkod numarasi al
4. GetBarcodeTag(barcode, orderId) → etiket yazdir (ZPL veya HTML)
5. (Dijital urun ise) UpdateNoShippingOrder(orderId) → teslim edildi yap
```
