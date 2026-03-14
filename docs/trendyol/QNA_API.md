# Trendyol Q&A (Soru-Cevap) API — Endpoint Reference

## Müşteri Sorularını Çekme (questionsFilter)

**GET** `/integration/qna/sellers/{sellerId}/questions/filter`

### Önerilen Kullanım
```
GET /integration/qna/sellers/{sellerId}/questions/filter?status=WAITING_FOR_ANSWER&startDate={ts}&endDate={ts}
```

### Query Parametreleri
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| barcode | long | Ürün barkodu ile filtrele |
| page | int | Sayfa numarası |
| size | int | Max 50 |
| startDate | long | Timestamp (ms) |
| endDate | long | Timestamp (ms) |
| status | string | Durum filtresi |
| orderByField | string | `LastModifiedDate` veya `CreatedDate` |
| orderByDirection | string | `ASC` veya `DESC` |

Tarih parametresi verilmezse son 7 gün. Max aralık: 2 hafta.

### Soru Durumları
| Status | Açıklama |
|--------|----------|
| **WAITING_FOR_ANSWER** | Cevaplanmayı bekliyor |
| **ANSWERED** | Cevaplanmış |
| **REPORTED** | Raporlanmış |
| **REJECTED** | Cevap reddedilmiş |
| **UNANSWERED** | Cevaplanmamış |

### Response
```json
{
  "totalElements": 864,
  "totalPages": 432,
  "page": 0,
  "size": 50,
  "content": [
    {
      "id": 12345,
      "customerId": 67890,
      "userName": "Ahmet Y.",
      "text": "Bu ürün su geçirir mi?",
      "status": "WAITING_FOR_ANSWER",
      "creationDate": 1736154265337,
      "productName": "Su Geçirmez Ceket",
      "productMainId": "1234567",
      "imageUrl": "https://...",
      "webUrl": "https://...",
      "public": true,
      "showUserName": true,
      "answer": null,
      "rejectedAnswer": null,
      "reason": null,
      "reportReason": null
    }
  ]
}
```

### Cevaplanmış Soru
```json
{
  "answer": {
    "id": 111,
    "text": "Hayır, su geçirmez değildir.",
    "creationDate": 1736200000000,
    "hasPrivateInfo": false,
    "reason": null
  }
}
```

### Reddedilmiş Cevap
```json
{
  "rejectedAnswer": {
    "id": 222,
    "text": "Önceki cevap metni",
    "creationDate": 1736200000000,
    "reason": "Cevap uygunsuz bulundu"
  },
  "rejectedDate": 1736300000000
}
```

---

## Belirli Soru Detayı (questionsFilterById)

**GET** `/integration/qna/sellers/{sellerId}/questions/{id}`

Tek bir sorunun detayını getirir. Aynı response yapısı.

---

## Müşteri Sorusunu Cevaplama

**POST** `/integration/qna/sellers/{sellerId}/questions/{id}/answers`

```json
{
  "text": "Bu ürün su geçirmez değildir, ancak su itici özelliğe sahiptir."
}
```

**Kısıtlamalar:**
- `text` min 10, max 2000 karakter
- `{id}` → cevaplanacak sorunun ID'si (`questionsFilter`'dan alınır)

**Response:** HTTP 200
