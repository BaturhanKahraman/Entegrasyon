# Çiçeksepeti Cargo API

Çiçeksepeti'nde iki farklı kargo modeli vardır:
1. **CS Kargo Entegrasyonu** — Çiçeksepeti'nin anlaşmalı kargo firmasını kullanma
2. **Kendi Kargo Entegrasyonu** — Satıcının kendi kargo firmasını kullanması

Ek kargo işlemleri: kargo firması değiştirme, desi/adet bilgisi, dijital kod gönderimi.

---

## 1. CS Kargo Entegrasyonu

**Endpoint:** `PUT /api/v1/Order/readyforcargowithcsintegration`

Siparişleri "Kargoya Hazır" durumuna geçirir. CS, kargo kodunu oluşturur ve kargo firmasına iletir.

**Request Body:**
```json
{
  "orderItemsGroup": [
    {
      "orderItemIds": [111, 222]
    },
    {
      "orderItemIds": [333]
    }
  ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| `orderItemsGroup` | array | Sipariş kalem grupları |
| `orderItemIds` | int[] | Aynı grupta gönderilecek sipariş kalemleri |

### Kurallar
- Sadece **"Yeni"** statüsündeki siparişler
- Aynı gruptaki kalemler aynı ana siparişe, aynı alıcıya ve aynı adrese ait olmalı
- Response'ta `partialNumber` (kargo takip no) ve `cargoCompany` döner

---

## 2. Kendi Kargo Entegrasyonu

**Endpoint:** `PUT /api/v1/Order/statusupdatewithsupplierintegration`

Satıcı kendi kargo firmasını seçerek sipariş durumunu günceller.

**Request Body:**
```json
{
  "orderItems": [
    {
      "orderItemId": 111,
      "orderItemStatusId": 5,
      "cargoBusinessId": 43,
      "shipmentNumber": "ARS1234567890",
      "shipmentTrackingUrl": "https://www.araskargo.com.tr/takip?no=ARS1234567890",
      "receiverName": "Ali Yılmaz",
      "deliveryTime": "2026-03-25T14:00:00+03:00"
    }
  ]
}
```

### Request Alanları

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderItemId` | int | Evet | Sipariş kalem ID |
| `orderItemStatusId` | int | Evet | Hedef durum |
| `cargoBusinessId` | int | Koşullu | Kargo firması ID |
| `shipmentNumber` | string | Koşullu | Kargo takip numarası |
| `shipmentTrackingUrl` | string | Hayır | Kargo takip URL'i |
| `receiverName` | string | Hayır | Teslim alan kişi |
| `deliveryTime` | datetime | Hayır | Teslim zamanı |

### orderItemStatusId Değerleri

| Değer | Açıklama | Not |
|-------|----------|-----|
| 1 | Yeni | — |
| 2 | Hazırlanıyor | — |
| 3 | Arabaya Verildi | Servis aracı teslimatları için (deliveryType=1) |
| 5 | Kargoya Verildi | cargoBusinessId + shipmentNumber zorunlu |
| 7 | Teslim Edildi | — |
| 11 | Kargoya Verilecek | CS Yurtiçi'ye otomatik veri gönderir |
| 18 | Firmaya İade Edildi | — |

### cargoBusinessId Değerleri

| ID | Kargo Firması |
|----|---------------|
| 1 | MNG Kargo |
| 2 | Yurtiçi Kargo |
| 25 | Sürat Kargo |
| 43 | Aras Kargo |
| 44 | PTT Kargo |
| 45 | UPS |
| 46 | Horoz Lojistik |
| 55 | Ceva Lojistik |
| 59 | Sendeo |
| 116 | kargomSENDE |
| 117 | Kolay Gelsin |
| 118 | Arvato |

### Önemli Notlar
- Yurtiçi Kargo + "Kargoya Verilecek" (11) seçildiğinde CS otomatik olarak Yurtiçi'ye veri gönderir
- Servis aracı teslimatlarında status 3 (Arabaya Verildi) kullanılmalı, 5 değil

---

## 3. Kargo Firması Değiştirme

**Endpoint:** `PUT /api/v1/Order/CargoCompany`

**Request Body:**
```json
{
  "items": [
    {
      "orderProductId": 111,
      "cargoId": 43
    }
  ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| `orderProductId` | int | Sipariş ürün ID |
| `cargoId` | int | Yeni kargo firması ID |

### cargoId Değerleri

| ID | Firma |
|----|-------|
| 1 | MNG Kargo |
| 2 | Yurtiçi Kargo |
| 25 | Sürat Kargo |
| 43 | Aras Kargo |
| 44 | PTT Kargo |
| 45 | UPS |
| 46 | Horoz Lojistik |
| 49 | Borusan Lojistik |

> **Kısıtlama:** Sadece **"Yeni"** statüsündeki siparişlerde ve yalnızca atanmış kargo firmaları arasında değişiklik yapılabilir.

---

## 4. Desi ve Adet Bilgisi Gönderimi

**Endpoint:** `POST /api/v1/Order/CargoMeasurement`

Lojistik atanmış siparişler için desi ve adet bilgisi gönderir.

**Request Body:**
```json
{
  "items": [
    {
      "orderProductId": 111,
      "desi": 3.5,
      "quantity": 2
    }
  ]
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderProductId` | int | Evet | Sipariş ürün ID |
| `desi` | decimal | Evet | Desi bilgisi |
| `quantity` | int | Evet | Adet bilgisi |

> **Kısıtlama:** Sadece lojistik atanmış ve **"Yeni"** statüsündeki siparişler.

---

## 5. Dijital Kod Gönderimi

**Endpoint:** `PUT /api/v1/Order/digital-order-status-update`

Fiziksel kargo gerektirmeyen dijital ürünler için teslim bilgisi gönderir.

**Request Body:**
```json
{
  "items": [
    {
      "orderProductId": 111,
      "receiverName": "Ali Yılmaz",
      "deliveryTime": "2026-03-22T14:00:00+03:00"
    }
  ]
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderProductId` | int | Evet | Sipariş ürün ID |
| `receiverName` | string | Evet | Teslim alan kişi |
| `deliveryTime` | datetime | Evet | Teslim zamanı |
