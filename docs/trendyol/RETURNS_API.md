# Trendyol Returns (İade) API — Endpoint Reference

## İade Süreci Akışı

```
Müşteri iade talebi oluşturur
  → getClaims ile iade siparişlerini çek
  → customerClaimItemReason kontrol et
  → Onaylanacak mı?
    EVET → approveClaim (WaitingInAction durumundaki item'lar)
      → WaitingFraudCheck → Accepted
    HAYIR → getClaimIssueReasons ile red sebeplerini çek
      → createClaimIssue ile ret talebi oluştur (dosya eki zorunlu)
      → Ürün müşteriye geri gönderilir
```

**Önemli:** Satıcı 48 saat içinde aksiyon almazsa iade otomatik onaylanır (`autoAccepted: true`).

---

## İade Siparişlerini Çekme (getClaims)

**GET** `/integration/order/sellers/{sellerId}/claims`

### Query Parametreleri
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| claimIds | string | Belirli iade ID'leri (verilirse diğer parametreler yok sayılır) |
| claimItemStatus | string | Durum filtresi |
| startDate | long | Timestamp (ms) |
| endDate | long | Timestamp (ms) |
| orderNumber | string | Sipariş numarası |
| size | int | Sayfa boyutu |
| page | int | Sayfa numarası |

### İade Durumları (claimItemStatus)
| Durum | Açıklama |
|-------|----------|
| **Created** | Müşteri iade talebi oluşturdu |
| **WaitingInAction** | İade depoya ulaştı — satıcı aksiyon bekliyor |
| **WaitingFraudCheck** | Onaylandı, fraud kontrolü yapılıyor |
| **Accepted** | İade onaylandı |
| **Rejected** | İade reddedildi |
| **Unresolved** | İade tartışmalı (issue report sonrası) |
| **Cancelled** | İade iptal edildi |
| **InAnalysis** | İade analiz ediliyor |

### Response (Özet)
```json
{
  "totalElements": 2099,
  "totalPages": 2099,
  "page": 0,
  "size": 1,
  "content": [
    {
      "claimId": "f9da2317-876b-...",
      "orderNumber": "65745805",
      "orderDate": 1524826343886,
      "claimDate": 1525844162827,
      "customerFirstName": "string",
      "customerLastName": "string",
      "cargoTrackingNumber": 72602420957047272632,
      "cargoProviderName": "Aras Kargo Marketplace",
      "orderShipmentPackageId": 3853354,
      "lastModifiedDate": 1723275767111,
      "items": [
        {
          "orderLine": {
            "id": 28717254,
            "productName": "string",
            "barcode": "99999999999",
            "merchantSku": "2083667",
            "productColor": "GREEN CS4",
            "productSize": "21",
            "price": 12.95,
            "vatRate": 8,
            "productCategory": "Sandalet"
          },
          "claimItems": [
            {
              "id": "b71461e3-...",
              "orderLineItemId": 29815493,
              "customerClaimItemReason": {
                "id": "451",
                "name": "Diğer",
                "externalReasonId": 23,
                "code": "UNFIT"
              },
              "claimItemStatus": {"name": "WaitingInAction"},
              "customerNote": "Müşteri notu",
              "autoAccepted": false,
              "acceptedBySeller": false
            }
          ]
        }
      ],
      "replacementOutboundpackageinfo": {},
      "rejectedpackageinfo": {}
    }
  ]
}
```

---

## İade Sebepleri (Return Reasons)

### Müşteri Sebepleri
| ID | Açıklama |
|----|----------|
| 251 | Modelini beğenmedim |
| 301 | Kusurlu ürün gönderildi |
| 351 | Yanlış ürün gönderildi |
| 401 | Vazgeçtim |
| 451 | Diğer |
| 501 | Bedeni küçük geldi |
| 551 | Bedeni büyük geldi |
| 651 | Ürün belirtilen özelliklere sahip değil |
| 701 | Yanlış sipariş verdim |
| 1651 | Kalitesini beğenmedim |
| 2030 | Daha iyi bir fiyat mevcut |
| 2042 | Beğenmedim |
| 2043 | Ürünümün parçası/aksesuarı eksik gönderildi |

### Trendyol Sistem Sebepleri
| ID | Açıklama |
|----|----------|
| 51 | Sebep yok |
| 101 | Depo kayıp |
| 151 | Çapraz hatalı |
| 201 | Müşteri iade kayıp |
| 751 | Diğer - Fraud kaynaklı |
| 1701 | Kargo teslimatı gecikmesi |
| 2000 | Cezalı onay |
| 2001 | Cezasız onay |
| 2002-2017 | Çeşitli analiz/tamirat/tekrar sevk sebepleri |

---

## İade Red Sebeplerini Çekme (getClaimIssueReasons)

**GET** `/integration/order/claim-issue-reasons`

```json
[
  {"id": 1, "name": "İade gelen ürün sahte"},
  ...
]
```

Tam liste API çağrısı ile alınır. **Not:** Reason ID 1651 için ilk 24 saatte bu seçenek kullanılamaz.

---

## İade Onaylama (approveClaim)

**PUT** `/integration/order/sellers/{sellerId}/claims/{claimId}/items/approve`

```json
{
  "claimLineItemIdList": ["f9da2317-876b-4b86-b8f7-0535c3b65731"],
  "params": {}
}
```

Sadece **WaitingInAction** durumundaki item'lar onaylanabilir. `claimId` ve `claimLineItemIdList` değerleri `getClaims`'den alınır. Onay sonrası `WaitingFraudCheck` → `Accepted` akışı başlar.

---

## İade Ret Talebi Oluşturma (createClaimIssue)

**POST** `/integration/order/sellers/{sellerId}/claims/{claimId}/issue?claimIssueReasonId={id}&claimItemIdList={itemIds}&description={text}`

Parametreler query string olarak gönderilir. Dosya eki **form-data (file)** olarak zorunlu (pdf, jpeg vb.).

**İstisnalar:** Reason ID 1651, 451, 2101 için dosya eki zorunlu değil.

**Kısıtlamalar:**
- Sadece **WaitingInAction** durumundaki iade'ler
- `description` max 500 karakter
- `claimIssueReasonId` → `getClaimIssueReasons`'dan alınır

---

## İade Talebi Oluşturma (createClaim — Satıcı Tarafından)

**POST** `/integration/order/sellers/{sellerId}/claims/create`

İade kodu olmadan depoya ulaşan paketler için satıcı tarafından iade talebi oluşturulur.

```json
{
  "claimItems": [
    {
      "barcode": "string",
      "customerNote": "İade kodu olmadan gelen ürün",
      "quantity": 1,
      "reasonId": 401
    }
  ],
  "customerId": 123,
  "excludeListing": true,
  "forcePackageCreation": true,
  "orderNumber": "65745805",
  "shipmentCompanyId": 4
}
```

### Response
```json
{
  "claimId": "string",
  "cargoTrackingNumber": 733123456,
  "claimItemIds": ["string"]
}
```

---

## İade Audit Bilgileri (getClaimAudits)

**GET** `/integration/order/sellers/{sellerId}/claims/items/{claimItemsId}/audit`

İade sürecinin tüm durum geçişlerini takip eder.

```json
[
  {
    "claimId": "UUID",
    "claimItemId": "UUID",
    "previousStatus": "WaitingInAction",
    "newStatus": "Accepted",
    "userInfoDocument": {
      "executorApp": "SellerIntegrationApi",
      "executorUser": "user@example.com"
    },
    "date": 1723275767111
  }
]
```

**executorApp değerleri:**
- `SellerIntegrationApi` — API üzerinden yapılan işlem
- `Seller Center Orders BFF` — Panel üzerinden yapılan işlem
