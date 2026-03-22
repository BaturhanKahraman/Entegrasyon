# Pazarama — Soru & Cevap API

## 1. Soru Statülerini Alma

**Servis Tipi:** GET

```
GET https://{baseurl}/QuestionAnswer/getQuestionStatus
```

### Statüler

| ID | Değer |
|----|-------|
| 0 | Cevap Bekliyor |
| 1 | Cevaplandı |
| 2 | Onay Bekliyor |
| 3 | Reddedildi |

---

## 2. Soru Konuları

**Servis Tipi:** POST

```
POST https://{baseurl}/QuestionAnswer/questionTopics
```

### Örnek Response

```json
{
  "data": [
    { "id": "2884d198-babd-4199-b90f-9a13bbee6ec9", "topic": "Renk Beden" },
    { "id": "0ce396f6-9bb2-4a84-9448-ba0b16e3a6b8", "topic": "Teslimat ve Kargo" },
    { "id": "f153675f-0da8-4bef-80fb-cb0c291fb172", "topic": "İptal ve İade" },
    { "id": "cd6a0496-6b0f-471f-9d6f-eb53c3d0cb20", "topic": "Garanti Koşulları" },
    { "id": "c7c20577-f962-4cfa-b171-ff802eeeb161", "topic": "Ürün Özellikleri" }
  ],
  "success": true
}
```

---

## 3. Soruları Listeleme (Özet)

**Servis Tipi:** GET

```
GET https://{baseurl}/QuestionAnswer/getApprovalAnswersByMerchant
```

### Örnek Response

```json
{
  "data": {
    "sellerId": "ef9bf337-83bb-4617-ef46-08d97204cc6d",
    "approvalAnswersByMerchant": [
      {
        "productName": "Tükenmez Kalem",
        "questionId": "56643287-ad2c-40be-9088-08dc076bf0d7",
        "questionDate": "2023-12-28T09:12:16.333",
        "questionStatus": 0
      }
    ],
    "unAnsweredCount": 2,
    "pageResponse": {
      "pageIndex": 1,
      "pageSize": 10,
      "totalCount": 2,
      "totalPages": 1
    }
  },
  "success": true
}
```

---

## 4. Soru Detayı (ID ile)

**Servis Tipi:** GET

```
GET https://{baseurl}/QuestionAnswer/getApprovalAnswerById?questionId={questionId}
```

### Örnek Response

```json
{
  "data": {
    "sellerId": "ef9bf337-83bb-4617-ef46-08d97204cc6d",
    "questionId": "56643287-ad2c-40be-9088-08dc076bf0d7",
    "productName": "Tükenmez Kalem",
    "productImageUrl": "https://cdn.pazarama.com/asset/MSTK55550/images/tkenmezkalem-1.jpeg",
    "barcode": "MSTK55550",
    "brand": "4 Element Yayınları",
    "maskedUserName": "M**** S****",
    "question": "garanti var mı",
    "questionDate": "2023-12-28T09:12:16.333",
    "questionStatus": 0
  },
  "success": true
}
```

---

## 5. Soruya Cevap Verme

**Servis Tipi:** PUT

```
PUT https://{baseurl}/QuestionAnswer/sellerAnswer
```

### Örnek Request

```json
{
  "questionId": "56643287-ad2c-40be-9088-08dc076bf0d7",
  "text": "Merhaba, Evet efendim, tüm ürünlerimiz garanti kapsamındadır."
}
```

---

## 6. Soruları Filtreleme (Detaylı)

**Servis Tipi:** POST

```
POST https://{baseurl}/QuestionAnswer/getApprovalAnswersByMerchantSearch
```

### Örnek Request

```json
{
  "barcode": null,
  "topicId": null,
  "questionStartDate": null,
  "questionEndDate": null,
  "questionStatus": null,
  "pageIndex": 1,
  "pageSize": 10
}
```

### Örnek Response

```json
{
  "data": {
    "sellerId": "ef9bf337-83bb-4617-ef46-08d97204cc6d",
    "approvalAnswersByMerchantSearchs": [
      {
        "questionId": "56643287-ad2c-40be-9088-08dc076bf0d7",
        "productName": "Tükenmez Kalem",
        "productImageUrl": "https://cdn.pazarama.com/asset/MSTK55550/images/tkenmezkalem-1.jpeg",
        "barcode": "MSTK55550",
        "brand": "4 Element Yayınları",
        "maskedUserName": "M**** S****",
        "question": "garanti var mı",
        "questionDate": "2023-12-28T09:12:16.333",
        "answer": "Merhaba, Evet efendim, tüm ürünlerimiz garanti kapsamındadır.",
        "answerDate": "2023-12-28T10:17:13.023",
        "questionStatus": 2,
        "topicId": "cd6a0496-6b0f-471f-9d6f-eb53c3d0cb20"
      }
    ],
    "pageResponse": {
      "pageIndex": 1,
      "pageSize": 10,
      "totalCount": 1,
      "totalPages": 1
    }
  },
  "success": true
}
```
