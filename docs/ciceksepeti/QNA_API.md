# Çiçeksepeti Question & Answer (Soru-Cevap) API

## 1. Ürün Sorularını Çekme

**Endpoint:** `GET /api/v1/sellerquestions`
**Rate Limit:** 1 req / 5 sn (farklı body), 1 req / 10 sn (aynı body)

**Query Parameters:**

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| `Id` | int | Hayır | Belirli soru ID |
| `ProductCode` | string | Hayır | Ürün kodu filtresi |
| `Answered` | bool | Hayır | Cevaplanmış mı filtresi |
| `CreateStartDate` | datetime | Koşullu* | Başlangıç tarihi |
| `CreateEndDate` | datetime | Koşullu* | Bitiş tarihi |
| `BranchActionId` | int | Hayır | Satıcı aksiyon filtresi |
| `AgentActionId` | int | Hayır | CS temsilci aksiyon filtresi |
| `Approve` | bool | Hayır | Onay durumu filtresi |
| `SortType` | int | Hayır | Sıralama yönü |
| `SortField` | int | Hayır | Sıralama alanı |
| `Page` | int | Hayır | Sayfa numarası (1-based) |

> *`Id` verilmezse `CreateStartDate`/`CreateEndDate` zorunludur. **Max tarih aralığı: 32 gün.**

### Response

```json
{
  "items": [
    {
      "id": 12345,
      "question": "Bu bukette kaç adet gül var?",
      "answer": null,
      "answered": false,
      "createdDate": "2026-03-20T10:00:00+03:00",
      "product": {
        "code": "MAIN-001",
        "name": "Kırmızı Gül Buketi",
        "url": "https://www.ciceksepeti.com/...",
        "imageUrl": "https://..."
      },
      "branchActionId": null,
      "agentActionId": null,
      "approve": null
    }
  ],
  "hasNextPage": true
}
```

### Sayfalama

`hasNextPage` boolean alanı ile kontrol edilir. Son sayfa olduğunda `false` döner.

---

## 2. Ürün Sorularını Cevaplama

**Endpoint:** `PUT /api/v1/sellerquestions/{id}`

**Path Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| `id` | int | Evet | Soru ID |

**Request Body:**
```json
{
  "answer": "Bukette 25 adet gül bulunmaktadır.",
  "branchActionId": 1,
  "branchActionDetailId": null,
  "branchDescription": null
}
```

### Alanlar

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `answer` | string | Koşullu | Cevap metni |
| `branchActionId` | int | Evet | Satıcı aksiyonu |
| `branchActionDetailId` | int | Koşullu | Alt aksiyon detayı |
| `branchDescription` | string | Koşullu | Ek açıklama |

### branchActionId Değerleri

| ID | Açıklama | answer Zorunlu | branchActionDetailId |
|----|----------|----------------|---------------------|
| 1 | Soru ve cevabı yayınla | Evet | Hayır |
| 2 | Sadece müşteriye cevap gönder | Evet | Hayır |
| 3 | Uygunsuz içerik bildirimi | Hayır | Evet (alt kategori seç) |
| 4 | Başka satıcıya ait soru | Hayır | Hayır |

---

## 3. Aksiyon Tiplerini Listeleme

**Endpoint:** `GET /api/v1/sellerquestions/actions`

Mevcut aksiyon tiplerini ve alt aksiyonlarını listeler. branchActionId ve branchActionDetailId değerleri bu endpoint'ten alınır.

**Response:**
```json
{
  "actions": [
    {
      "id": 1,
      "name": "Soru ve cevabı yayınla",
      "details": []
    },
    {
      "id": 3,
      "name": "Uygunsuz içerik",
      "details": [
        { "id": 301, "name": "Reklam içerikli" },
        { "id": 302, "name": "Hakaret içerikli" }
      ]
    }
  ]
}
```
