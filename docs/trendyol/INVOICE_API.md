# Trendyol Fatura & Satıcı Bilgileri API — Endpoint Reference

## Satıcı Adres Bilgileri (getSuppliersAddresses)

**GET** `/integration/sellers/{sellerId}/addresses`

Ürün oluşturma (createProduct V2) için `shipmentAddressId` ve `returningAddressId` gereklidir. Bu endpoint ile satıcının kayıtlı adreslerini çekin.

### Response
```json
{
  "supplierAddresses": [
    {
      "id": 123,
      "addressType": "Shipment",
      "country": "Türkiye",
      "city": "İstanbul",
      "cityCode": 34,
      "district": "Kadıköy",
      "districtId": 456,
      "postCode": "34000",
      "address": "Caferağa Mah. Moda Cad. No:1",
      "fullAddress": "Caferağa Mah. Moda Cad. No:1, Kadıköy, İstanbul",
      "isShipmentAddress": true,
      "isInvoiceAddress": false,
      "isReturningAddress": false,
      "isDefault": true
    }
  ],
  "defaultShipmentAddress": { "id": 123, "..." : "..." },
  "defaultInvoiceAddress": { "id": 456, "..." : "..." },
  "defaultReturningAddress": { "present": true }
}
```

**addressType:** `Shipment`, `Invoice`, `Returning`

Rate limit: **1 req/hour** — sonucu cache'leyin.

---

## Fatura Linki Gönderme (sendInvoiceLink)

**POST** `/integration/sellers/{sellerId}/seller-invoice-links`

E-Fatura linkini Trendyol'a iletir. Müşteri sipariş detayında fatura linkini görebilir.

```json
{
  "invoiceLink": "https://efatura.example.com/324523-34523.pdf",
  "shipmentPackageId": 435346,
  "invoiceDateTime": 1678788898,
  "invoiceNumber": "TY4874324"
}
```

| Alan | Tip | Zorunlu | Not |
|------|-----|---------|-----|
| invoiceLink | string | Evet | Fatura URL'i — 10 yıl erişilebilir olmalı (yasal zorunluluk) |
| shipmentPackageId | long | Evet | Sipariş paket ID'si |
| invoiceDateTime | long | Mikro ihracat için evet | Unix timestamp (10 veya 13 haneli) |
| invoiceNumber | string | Mikro ihracat için evet | Format: `[3 alfanümerik][4 haneli yıl 2020-2099][9 rakam]` |

### Fatura Numarası Formatı
- Geçerli: `FRY2024567890123`, `1232024567890123`
- Geçersiz: `FRY12345`

**Response:** HTTP 201

**409 Conflict:** Bu paket için zaten fatura gönderilmiş veya bu fatura linki başka pakette kullanılmış.

---

## Fatura Linki Silme

**POST** `/integration/sellers/{sellerId}/seller-invoice-links/delete`

Hatalı gönderilmiş fatura linkini silip yeniden göndermek için kullanılır.

```json
{
  "serviceSourceId": 88787,
  "channelId": 1,
  "customerId": 167878
}
```

| Alan | Açıklama |
|------|----------|
| serviceSourceId | shipmentPackageId |
| channelId | Her zaman `1` |
| customerId | Müşteri ID (sipariş çekme servisinden alınır) |

**Response:** HTTP 202

---

## Fatura Dosyası Gönderme (File Upload)

**POST** `/integration/sellers/{sellerId}/seller-invoice-file`

Fatura dosyasını doğrudan yükler. **form-data** formatı (JSON değil).

### TR Siparişleri
| Form Field | Tip | Zorunlu |
|------------|-----|---------|
| shipmentPackageId | Text | Evet |
| file | File | Evet |

### Mikro İhracat Siparişleri
| Form Field | Tip | Zorunlu |
|------------|-----|---------|
| shipmentPackageId | Text | Evet |
| invoiceDateTime | Text | Evet |
| invoiceNumber | Text | Evet |
| file | File | Evet |

### Dosya Kısıtlamaları
- **Formatlar:** PDF, JPEG, PNG
- **Max boyut:** 10 MB
- Gelecek tarihli fatura kabul edilmez

**Response:** HTTP 200

### Hata Durumları
- Dosya boyutu aşıldı
- Gelecek tarihli fatura
- Yanlış satıcı
- Aynı fatura tekrar yükleme
- Paket bulunamadı
- Desteklenmeyen format
- Dosya eksik
- Mikro sipariş zorunlu alanları eksik
