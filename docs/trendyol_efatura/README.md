# Trendyol e-Faturam Entegrasyonu

Marketplace API'sinden bağımsız, ayrı bir platform. GİB'e doğrudan bağlanmadan e-fatura, e-arşiv ve e-irsaliye işlemleri yapılır.

## Ortamlar

| Ortam | URL |
|-------|-----|
| **STAGE** | `https://stage-apigateway.trendyolefaturam.com` |
| **PROD** | `https://apigateway.trendyolecozum.com` |

## Auth Akışı

```
1. Partner Sign-In → access token al (entegratör hesabı)
2. Customer Sign-In → müşteri token al (partner token header'da)
   → Response: userId, companyId, partnerCustomerId, accessToken
3. Tüm sonraki isteklerde müşteri token'ı kullan
```

**Her müşteri için `userId` ve `companyId` saklanmalı** — test ve canlıda farklılık gösterir.

## Kritik Kurallar

- **Tutarlar kuruş olarak girilir:** 114.55 TL → `11455`
- **1 Temmuz 2024'ten itibaren** internet satış e-arşiv faturalarında `paymentInfo` ve `deliveryInfo` alanları zorunlu
- **source:** Pazaryeri entegratörleri için `PARTNER`

---

## Authorization

### Partner Sign-In
**POST** `/api/auth/signin`

```json
{"email": "string", "password": "string"}
```
Response: Token header'da döner (access token + refresh token).

### Customer Sign-In
**POST** `/api/invoice/partners/customer/signin`

Header'da partner token gerekli.

```json
{"email": "string", "password": "string", "taxId": "string"}
```
`taxId`: VKN (10 hane) veya TCKN (11 hane).

Response:
```json
{"userId": 123, "companyId": 456, "partnerCustomerId": 789, "accessToken": "..."}
```

---

## Mükellef Sorgulama

### Tüm Mükellefleri İndir
**GET** `/api/invoice/taxpayers/download`

ZIP formatında tüm mükellef listesi download URL'i döner.

### VKN ile Mükellef Sorgula
**GET** `/api/invoice/taxpayers/{taxId}?showDeleted=false`

```json
[
  {
    "taxId": "800319933",
    "alias": "urn:mail:defaultgb@trendyolefaturam.com",
    "title": "Firma Adı",
    "gibUserType": "string",
    "createdAt": "2024-01-01T00:00:00Z",
    "aliasCreationTime": "2024-01-01T00:00:00Z",
    "postBoxType": "string",
    "aliasType": "INVOICE"
  }
]
```
`aliasType`: `INVOICE` veya `DESPATCH_ADVICE`

---

## e-Arşiv Fatura

### e-Arşiv Oluştur
**POST** `/api/invoice/documents/earchive`

```json
{
  "autoInvoiceId": true,
  "companyId": 0,
  "userId": 0,
  "source": "PARTNER",
  "xsltCode": "string",
  "prefix": "ABC",
  "localReferenceId": "ERP-123",
  "notes": ["Sipariş notu"],
  "issuedAt": "2025-03-14T10:00:00Z",
  "recipientInfo": {
    "taxId": "string",
    "countryCode": "TR",
    "city": "İstanbul",
    "district": "Kadıköy",
    "address": "Adres bilgisi",
    "postalCode": "34000",
    "phone": "5551234567",
    "email": "musteri@email.com",
    "name": "Ad",
    "surname": "Soyad",
    "taxOffice": "Kadıköy"
  },
  "currencyInfo": {
    "currency": "TRY",
    "hasExchange": false
  },
  "invoiceInfo": {
    "invoiceType": "EARSIVFATURA",
    "invoiceTypeCode": "SATIS"
  },
  "invoiceLines": [
    {
      "unitCode": "C62",
      "quantity": 1,
      "totalAmount": 11455,
      "taxAmount": 1755,
      "taxableAmount": 9700,
      "taxPercent": 18,
      "taxName": "KDV",
      "taxCode": "0015",
      "itemName": "Ürün Adı",
      "unitPriceAmount": 11455,
      "totalDiscountAmount": 0,
      "totalTax": {
        "totalTaxAmount": 1755,
        "subTotalTaxes": [
          {
            "taxableAmount": 9700,
            "taxAmount": 1755,
            "taxType": "KDV",
            "percent": 18
          }
        ]
      }
    }
  ],
  "totalTax": {
    "totalTaxAmount": 1755,
    "subTotalTaxes": [{"taxableAmount": 9700, "taxAmount": 1755, "taxType": "KDV", "percent": 18}]
  },
  "invoiceTotal": {
    "lineExtensionAmount": 9700,
    "taxExclusiveAmount": 9700,
    "taxInclusiveAmount": 11455,
    "payableAmount": 11455,
    "allowanceTotalAmount": 0
  },
  "paymentInfo": {
    "purchaseUrl": "https://trendyol.com/order/123",
    "paymentMeans": "CREDIT_CARD",
    "paymentDate": "2025-03-14T10:00:00Z"
  },
  "deliveryInfo": {
    "carrierTaxId": "string",
    "carrierName": "Aras Kargo",
    "sentAt": "2025-03-14"
  },
  "orderInfo": {
    "orderId": "65745805",
    "orderDate": "2025-03-14"
  }
}
```

Response:
```json
{
  "id": 123,
  "invoiceUuid": "3fa85f64-...",
  "invoiceId": "ABC2025000000001",
  "status": 10,
  "scenario": "EARSIVFATURA",
  "invoiceTypeCode": "SATIS",
  "taxExcludedPrice": 9700,
  "taxAmount": 1755,
  "payableAmount": 11455,
  "gibStatus": "READY_TO_BE_REPORTED"
}
```

### e-Arşiv Dosya ile Oluştur
**POST** `/api/invoice/documents/earchive-by-file`

`multipart/form-data`: `invoiceFile` (XML) + `additionalInformation`.

### e-Arşiv Durum Sorgula
**GET** `/api/invoice/documents/earchive/status/{invoiceUuid}`

```json
{"status": 205, "gibStatus": "REPORTED", "invoiceUuid": "..."}
```

### e-Arşiv İptal
**POST** `/api/invoice/documents/earchive/cancel`

```json
{"invoiceUuid": "3fa85f64-...", "companyId": 456}
```

---

## Giden e-Fatura

### e-Fatura Oluştur
**POST** `/api/invoice/documents/outgoing-einvoice`

e-Arşiv ile aynı request body yapısı. Ek alanlar:
- `targetAlias` — alıcı etiket bilgisi (birden fazla etiketi olan firmalar için)
- `intermediaryInfo` — aracı firma bilgisi

Response'ta ek: `envelopeId` (GİB zarf UUID), `targetAlias`, `replyType`.

### e-Fatura Dosya ile Oluştur
**POST** `/api/invoice/documents/outgoing-einvoice-by-file`

### e-Fatura Durum Sorgula
**GET** `/api/invoice/documents/outgoing-einvoice/status/{invoiceUuid}`

```json
{"status": 40, "gibStatusCode": 1200, "invoiceUuid": "..."}
```

### Gönderim Yanıtı Sorgula
**GET** `/api/invoice/documents/sent-reply/{companyId}/{invoiceUuid}`

```json
{
  "uuid": "...",
  "invoiceUuid": "...",
  "responseType": "APPROVED",
  "note": "Fatura kabul edildi"
}
```
`responseType`: `APPROVED`, `AUTO_APPROVED`, `DENIED`, `RETURNED`

### e-Fatura Yeniden Gönder
**POST** `/api/invoice/documents/outgoing-einvoice/resend`

```json
{"invoiceUuid": "...", "newTargetAlias": "string", "companyId": 456}
```

---

## Gelen e-Fatura

### Gelen e-Fatura Ara
**POST** `/api/invoice/documents/incoming-einvoice/search`

```json
{
  "startDate": "2024-06-05",
  "endDate": "2024-06-05",
  "companyId": 456,
  "scenario": "TEMELFATURA",
  "status": 30,
  "pagination": {"page": 0, "size": 20}
}
```

### Gelen e-Faturaya Yanıt Ver
**POST** `/api/invoice/documents/sent-reply`

```json
{
  "invoiceUuid": "...",
  "responseType": "APPROVED",
  "note": "Kabul edildi",
  "companyId": 456
}
```

### Gelen Yanıt Sorgula
**GET** `/api/invoice/documents/received-reply/{companyId}/{invoiceUuid}`

---

## e-İrsaliye (Sevk İrsaliyesi)

### İrsaliye Oluştur
**POST** `/api/invoice/documents/outgoing-despatch-advice`

```json
{
  "autoInvoiceId": true,
  "companyId": 456,
  "userId": 123,
  "source": "PARTNER",
  "totalPrice": 11455,
  "despatchedAt": "2025-03-14T10:00:00Z",
  "recipientInfo": {"taxId": "...", "countryCode": "TR", "city": "İstanbul", "...": "..."},
  "shipmentInfo": {
    "carrier": {"taxId": "...", "companyName": "Aras Kargo"},
    "driverInfos": [{"driverName": "Ali", "driverSurname": "Yılmaz", "driverPhone": "555..."}],
    "plateNo": "34 ABC 123"
  },
  "deliveryInfo": {"country": "Türkiye", "city": "İstanbul", "address": "..."},
  "despatchInfo": {"despatchType": "TEMELIRSALIYE", "despatchTypeCode": "SEVK"},
  "desPatchLines": [
    {"unitCode": "C62", "quantity": 1, "totalAmount": 11455, "itemName": "Ürün Adı"}
  ]
}
```

### İrsaliye Durum Sorgula
**GET** `/api/invoice/documents/outgoing-despatch-advice/status/{despatchUuid}`

### Gelen İrsaliyeye Yanıt Ver
**POST** `/api/invoice/documents/despatch-advice/sent-reply`

```json
{
  "despatchUuid": "...",
  "responseType": "APPROVED",
  "companyId": 456,
  "source": "PARTNER",
  "despatchReplyLines": [
    {"lineId": 1, "receivedQuantity": 10, "receivedUnitCode": "C62", "rejectedQuantity": 0}
  ]
}
```
`responseType`: `APPROVED`, `APPROVED_COMPLAINED`, `DAMAGED_REJECTED`, `INCORRECT_AMOUNT`

### Gelen İrsaliye Ara
**POST** `/api/invoice/documents/incoming-despatch-advice/search`

---

## Diğer Servisler

### Başvuru Durumu
**GET** `/api/invoice/partners/{partnerId}/application-status/by-tax-id/{taxId}`

### Partner Müşteri Bilgileri
**GET** `/api/invoice/partners/corporate/{taxId}`

```json
{"email": "...", "taxId": "...", "phone": "...", "title": "Firma Adı", "address": "..."}
```

### Kalıcı Doküman İndir
**POST** `/api/invoice/documents/download/permanent-url`

```json
{"documentType": "EARCHIVE", "fileExtension": "pdf", "documentUuid": "...", "companyId": 456}
```
Response: İndirme URL'i (string).

### Geçici Doküman İndir
**POST** `/api/invoice/documents/download`

Ek alanlar: `inline` (preview), `envelope` (zarf ise true).

### Kalan Kredi/Kontör
**GET** `/api/invoice/partners/{partnerId}/credits/remaining/customers/{partnerCustomerId}`

```json
{"taxId": "800319933", "remainingCredit": 450, "remainingCreditStatus": "SUFFICIENT"}
```
`remainingCreditStatus`: `SUFFICIENT`, `LOW`, `RAN_OUT`

---

## Fatura Statü Kodları

| Kod | Durum |
|-----|-------|
| 10 | İşleniyor |
| 20 | Doküman hazırlanıyor |
| 29 | Doküman hatası |
| 30 | Oluşturuldu |
| 40 | GİB'e gönderildi |
| 50 | Yanıt bekleniyor (ticari fatura / irsaliye) |
| 100 | Reddediliyor |
| 105 | Reddedildi |
| 200 | Onaylanıyor |
| **205** | **Onaylandı** (e-arşiv, temel fatura için final) |
| 305 | İptal edildi (e-arşiv) |
| 405 | Hatalı |

---

## Enum Değerleri

| Alan | Değerler |
|------|----------|
| **source** | PORTAL, WEB, MOBILE, PARTNER |
| **paymentMeans** | CREDIT_CARD, EFT, ON_DELIVERY, MEDIATOR, OTHER |
| **gibStatus** | REPORTED, READY_TO_BE_REPORTED |
| **scenario** | EARSIVFATURA, TEMELFATURA, TICARIFATURA, HKS, IHRACAT, KAMU, ENERJI, ILAC_TIBBICIHAZ, YOLCUBERABERFATURA |
| **invoiceTypeCode** | SATIS, IADE, TEVKIFAT, ISTISNA, OZELMATRAH, IHRACKAYITLI, TEVKIFATIADE, SEVK, MATBUDAN, KONAKLAMAVERGISI, SARJ, SARJANLIK, TEKNOLOJIDESTEK, KOMISYONCU, HKSSATIS, HKSKOMISYONCU |
| **aliasType** | INVOICE, DESPATCH_ADVICE |
| **responseType** | APPROVED, DENIED, RETURNED |
| **documentType** | EARCHIVE, EINVOICE, INCOMING_EINVOICE, SENT_REPLY, RECEIVED_REPLY, DESPATCHADVICE, INCOMING_DESPATCH |
| **fileExtension** | xml, pdf, html |
| **externalCancellationType** | KEP, NOTARY, GIB_PORTAL |
| **remainingCreditStatus** | SUFFICIENT, LOW, RAN_OUT |
| **despatchResponseType** | APPROVED, APPROVED_COMPLAINED, DAMAGED_REJECTED, INCORRECT_AMOUNT |

---

## Entegrasyon Akışı (Sipariş → Fatura)

```
1. Sipariş tamamlandı (marketplace getShipmentPackages)
2. Alıcının VKN'si ile mükellef sorgula (getTaxPayersByTaxId)
   → aliasType == INVOICE → e-Fatura mükellefi
   → Bulunamadı → e-Arşiv kullan
3a. e-Fatura mükellefi → createOutgoingEInvoice
3b. Değilse → createEArchive (paymentInfo + deliveryInfo zorunlu)
4. Durum sorgula (getStatus) → status == 205 (onaylandı)?
5. PDF indir (getPermanentDocumentDownloadUrl)
6. Fatura linkini Trendyol marketplace'e gönder (INVOICE_API sendInvoiceLink)
7. Kargo etiketi bastır (artık fatura var, etiket bastırılabilir)
```

## Endpoint Özeti (27 endpoint)

| Kategori | Sayı | Endpoint'ler |
|----------|------|-------------|
| Auth | 2 | signIn, customerSignIn |
| Mükellef | 2 | download, getByTaxId |
| e-Arşiv | 4 | create, createByFile, getStatus, cancel |
| Giden e-Fatura | 5 | create, createByFile, getStatus, getSentReply, resend |
| Gelen e-Fatura | 3 | search, reply, getReceivedReply |
| e-İrsaliye | 4 | create, getStatus, reply, search |
| Diğer | 5 | appStatus, corporateInfo, permanentDownload, tempDownload, remainingCredit |
| Enum | 2 | variablesInfo, enums |
