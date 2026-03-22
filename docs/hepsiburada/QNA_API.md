# Hepsiburada Saticiciya Sor (Q&A) API — Endpoint Reference

Base URL: `https://api-asktoseller-merchant-sit.hepsiburada.com`

> Tum endpoint'ler HTTP Basic Auth, `User-Agent` ve `merchantId` **header**'i gerektirir. merchantId path'te degil, header'da gonderilir.

---

## Soru Olusturma (sadece SIT)

**POST** `/api/v1.0/issues`

SIT ortaminda test amacli soru olusturur.

### Headers

| Header | Tip | Zorunlu | Aciklama |
|--------|-----|---------|----------|
| merchantId | string | Evet | Satici ID |

### Request Body

```json
{
  "issueCount": 3
}
```

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| issueCount | integer | Evet | Olusturulacak soru sayisi (min 1) |

### Response — 201 Created

Olusturulan sorularin numaralarini icerir (array).

---

## Soru Listesi

**GET** `/api/v1.0/issues`

### Headers

| Header | Tip | Zorunlu | Aciklama |
|--------|-----|---------|----------|
| merchantId | string | Evet | Satici ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Aciklama |
|-----------|-----|---------|---------|----------|
| status | integer[] | Hayir | — | Durum filtresi (birden fazla gonderilebilir) |
| page | integer | Hayir | 1 | Sayfa numarasi |
| size | integer | Hayir | 25 | Sayfa boyutu |
| sortBy | integer | Hayir | 0 | Siralama alani |
| desc | boolean | Hayir | true | Azalan siralama |
| source | integer | Hayir | — | Kaynak filtresi |
| subject | string | Hayir | — | Konu filtresi |
| minCreatedAt | datetime | Hayir | — | Olusturulma baslangic tarihi |
| maxCreatedAt | datetime | Hayir | — | Olusturulma bitis tarihi |
| minModifiedAt | datetime | Hayir | — | Guncelleme baslangic tarihi |
| maxModifiedAt | datetime | Hayir | — | Guncelleme bitis tarihi |
| issueNumber | string | Hayir | — | Soru numarasi filtresi |
| search | string | Hayir | — | Serbest metin arama |

### Status Degerleri

| Deger | Aciklama |
|-------|----------|
| 1 | WaitingForAnswer — Cevap bekliyor |
| 2 | Answered — Cevaplandi |
| 3 | Rejected — Sorun bildirildi |
| 4 | AutoClosed — Otomatik kapatildi |

### Source Degerleri

| Deger | Aciklama |
|-------|----------|
| 1 | Siparise ilgili degil |
| 2 | Siparise ilgili |

### SortBy Degerleri

| Deger | Aciklama |
|-------|----------|
| 0 | CreatedAt — Olusturulma tarihine gore |
| 1 | LastModifiedAt — Son guncelleme tarihine gore |

### Response

```json
{
  "data": [],
  "currentPage": 1,
  "currentPageSize": 25,
  "totalPageCount": 10,
  "totalItemCount": 243,
  "nextPage": 2,
  "previousPage": null
}
```

---

## Soru Detayi

**GET** `/api/v1.0/issues/{number}`

Belirli bir sorunun detayini getirir.

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| number | string | Evet | Soru numarasi |

### Headers

| Header | Tip | Zorunlu | Aciklama |
|--------|-----|---------|----------|
| merchantId | string | Evet | Satici ID |

### Response — Issue Modeli

```json
{
  "id": "uuid",
  "createdAt": "2024-06-15T10:30:00Z",
  "issueNumber": "ISS-00001",
  "customerId": "customer-uuid",
  "orderNumber": "ORD-123456",
  "lineItemId": "line-item-uuid",
  "status": "WaitingForAnswer",
  "subject": {
    "id": 1,
    "description": "Urun hakkinda bilgi"
  },
  "lastContent": "Son mesaj icerigi",
  "conversations": [
    {
      "id": "conv-uuid",
      "type": "Question",
      "createdAt": "2024-06-15T10:30:00Z",
      "content": "Bu urun ne zaman kargoya verilir?",
      "from": "Customer",
      "files": [],
      "customerFeedback": null,
      "rejectReason": null
    }
  ],
  "merchant": {
    "id": "merchant-uuid",
    "name": "Magaza Adi"
  },
  "product": {
    "sku": "HBCV00001XXXXX",
    "name": "Urun Adi",
    "imageUrl": "https://...",
    "stockCode": "STK-001"
  },
  "expireDate": "2024-06-16T10:30:00Z",
  "lastModifiedAt": "2024-06-15T10:30:00Z"
}
```

### Issue Model Alan Aciklamalari

| Alan | Tip | Aciklama |
|------|-----|----------|
| id | string | Unique soru ID |
| createdAt | datetime | Olusturulma tarihi |
| issueNumber | string | Soru numarasi |
| customerId | string | Musteri ID |
| orderNumber | string | Siparis numarasi (varsa) |
| lineItemId | string | Siparis kalemi ID (varsa) |
| status | string | WaitingForAnswer / Answered / Rejected / AutoClosed |
| subject | object | Konu bilgisi (id, description) |
| lastContent | string | Son mesaj icerigi |
| conversations | object[] | Konusma gecmisi |
| conversations[].from | string | Mesaj gonderen: Customer veya Merchant |
| conversations[].files | string[] | Ekli dosyalar |
| conversations[].customerFeedback | string | Musteri geri bildirimi |
| conversations[].rejectReason | string | Red nedeni |
| merchant | object | Satici bilgisi (id, name) |
| product | object | Urun bilgisi (sku, name, imageUrl, stockCode) |
| expireDate | datetime | Cevaplama suresi son tarihi |
| lastModifiedAt | datetime | Son guncelleme tarihi |

---

## Soru Cevaplama

**POST** `/api/v1.0/issues/{number}/answer`

**Content-Type:** `multipart/form-data`

> Cevaplama suresi 1 is gunudur. Sure dolunca soru otomatik olarak **AutoClosed** durumuna gecer.

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| number | string | Evet | Soru numarasi |

### Headers

| Header | Tip | Zorunlu | Aciklama |
|--------|-----|---------|----------|
| merchantId | string | Evet | Satici ID |

### Form Fields

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| Answer | string | Evet | Cevap metni (max 2000 karakter) |
| Files | binary[] | Hayir | Eklenecek dosyalar |

### Desteklenen Dosya Tipleri

`.jpg`, `.png`, `.bmp`, `.pdf`, `.docx`, `.xlsx`

### Response — 201 Created

---

## Sorun Bildirme

**POST** `/api/v1.0/issues/{number}/reject`

Soruyu reddeder / sorun bildirir. Sorun bildirme metni Hepsiburada icin yazilir, musteriye gosterilmez.

### Path Parameters

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| number | string | Evet | Soru numarasi |

### Headers

| Header | Tip | Zorunlu | Aciklama |
|--------|-----|---------|----------|
| merchantId | string | Evet | Satici ID |

### Request Body

```json
{
  "rejectReason": "Bu soru urunumuzla ilgili degil.",
  "rejectConversationId": "conv-uuid"
}
```

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| rejectReason | string | Evet | Red nedeni (max 2000 karakter) |
| rejectConversationId | string | Hayir | Ilgili konusma ID |

### Response — 201 Created

---

## Statu Bazli Soru Sayisi

**GET** `/api/v1.0/issues/count`

Her statudeki soru sayisini doner.

### Headers

| Header | Tip | Zorunlu | Aciklama |
|--------|-----|---------|----------|
| merchantId | string | Evet | Satici ID |

### Response

```json
{
  "waitingForAnswer": 12,
  "answered": 345,
  "reported": 8,
  "autoClosedInLastWeek": 3
}
```

---

## Hata Kodlari

| HTTP Kodu | Aciklama |
|-----------|----------|
| 400 | Bad Request — Gecersiz istek |
| 401 | Unauthorized — Kimlik dogrulama hatasi |
| 404 | Not Found — Soru bulunamadi |
| 415 | Unsupported Media Type — Desteklenmeyen dosya tipi |
| 422 | Unprocessable Entity — Dosya tipi hatali |
| 500 | Internal Server Error — Sunucu hatasi |
