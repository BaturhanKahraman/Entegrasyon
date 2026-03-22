# Pazarama — Ürün Temin Bilgileri & Güvenlik API

## 1. Satıcı Şablonları (Product Commercials)

Ürün temin bilgileri (imalatçı/ithalatçı vb.) şablon üzerinden yönetilir.

**Akış:**
1. Önce şablon oluştur
2. Şablona bilgileri gir
3. Ürüne şablonu ata

### Type Enum Değerleri

**İthal Menşeli:**

| Değer | Açıklama |
|-------|----------|
| 1 | İthalatçı |
| 2 | Türkiye'de Yerleşik Yetkili Temsilci |
| 3 | Türkiye'de Yerleşik İfa Hizmet Sağlayıcı |

**Yerli Menşeli:**

| Değer | Açıklama |
|-------|----------|
| 0 | Türkiye'de Yerleşik İmalatçı |
| 3 | Türkiye'de Yerleşik İfa Hizmet Sağlayıcı |

### 1.1 Şablonları Listeleme

**Servis Tipi:** GET

```
GET https://isortagimapi.pazarama.com/product-commercials
```

#### Örnek Response

```json
{
  "data": {
    "productCommercialList": [
      {
        "commercialId": "4a5a0181-9c73-4d3e-99bb-08ddc848c9b9",
        "isImported": true,
        "type": 3,
        "name": "test1",
        "title": "test1",
        "brand": "test1",
        "email": "test@hotmail.com",
        "address": "test1"
      }
    ]
  },
  "success": true,
  "messageCode": "MER0"
}
```

### 1.2 Şablon Oluşturma

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product-commercials
```

#### Örnek Request

```json
{
  "isImported": true,
  "type": 0,
  "name": "string",
  "title": "string",
  "brand": "string",
  "email": "string",
  "address": "string"
}
```

### 1.3 Şablon Güncelleme

**Servis Tipi:** PUT

```
PUT https://isortagimapi.pazarama.com/product-commercials
```

Request body aynı POST formatındadır.

### 1.4 Şablon Silme

**Servis Tipi:** DELETE

```
DELETE https://isortagimapi.pazarama.com/product-commercials/id/{id}/product-commercial
```

---

## 2. Ürüne Temin Bilgisi Atama

Oluşturulan şablon ID'si ile ürüne temin bilgisini atar.

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product/upsertSellerProductCommercial
```

### Örnek Request

```json
{
  "code": "string",
  "securityDescription": "string",
  "commercialIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  ]
}
```

> **NOT:** `code` alanı zorunludur. Diğerleri girilmezse mevcut değerleri sıfırlar, girilirse girilen değer ile değiştirir.

---

## 3. Tekil Üründe Temin Bilgisi Girişi

Ürün ekleme servisine (`product/create`) ek alanlar olarak da gönderilebilir:

```json
{
  "productCommercialAdditionalInfo": {
    "securityDescription": "string"
  },
  "productCommercials": {
    "productCommercialId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
}
```

---

## 4. Uyarı Görseli (Güvenlik İşaretleri)

Denetim Yönetmeliği kapsamında ürün temin bilgilerine uyarı görseli eklenebilir. Eklenen görseller Pazarama Backoffice'te onaya düşer.

### 4.1 Uyarı Görseli Ekleme

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product-security-descriptions
```

#### Örnek Request

```json
{
  "content": "test3",
  "imageUrl": "https://cdntest.pazarama.com/gallery/.../image.jpg"
}
```

> **NOT:** `content` (açıklama) zorunludur. `imageUrl` CDN linki olmalıdır.

### 4.2 Onaylanmış Uyarı Görsellerini Listeleme

**Servis Tipi:** GET

```
GET https://isortagimapi.pazarama.com/product-security-descriptions/approved?pageIndex=1&pageSize=100
```

#### Örnek Response

```json
{
  "pageIndex": 1,
  "pageSize": 100,
  "totalCount": 59,
  "data": [
    {
      "id": "f60fd4f3-1e4e-4cd1-f42a-08de3392cb93",
      "content": "test546",
      "imageUrl": "https://cdntest.pazarama.com/gallery/.../image.png",
      "createdDate": "2025-12-05T03:11:10.727",
      "status": "Onaylandı",
      "statusDescription": null
    }
  ]
}
```

### 4.3 Ürüne Uyarı Görseli Atama/Güncelleme

Onaylanmış görsellerin ID'leri ile ürüne atama yapılır. Güncelleme için farklı ID gönderilir, silmek için ID listesinden çıkarılır.

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product/upsertSellerProductSecurityDescriptions (güncelleme/atama için öngörülen endpoint)
```

#### Örnek Request

```json
{
  "code": "testsinir4802",
  "securityDescriptionIds": [
    "ed0066d2-532d-4e30-db02-08de117916e8",
    "2f829f47-64f4-43a7-c6bc-08de116512fb"
  ]
}
```

Alternatif olarak `product/create` servisine doğrudan eklenebilir:

```json
{
  "securityDescriptionIdList": [
    "f60fd4f3-1e4e-4cd1-f42a-08de3392cb93",
    "ed0066d2-532d-4e30-db02-08de117916e8"
  ]
}
```

---

## 5. İzlenebilirlik — Ambalaj Bilgileri

Denetim Yönetmeliği gereksinimleri doğrultusunda ürünlere kullanım kılavuzu, ambalaj görselleri, parti/seri numarası ve son kullanma tarihi eklenebilir.

### securityDocuments Type Enum

| Değer | Açıklama | Format | Max Boyut |
|-------|----------|--------|-----------|
| 1 | Kullanma kılavuzu | PDF | 5MB |
| 2 | Ön ambalaj görseli | JPEG/PNG | 5MB |
| 3 | Arka ambalaj görseli | JPEG/PNG | 5MB |

> **Kısıtlamalar:**
> - Parti No ve Seri No: max 25 karakter
> - Son kullanma tarihi: ISO tarih formatı (`2026-02-26T00:00:00.000`)

### 5.1 Parti/Seri No & Son Kullanma Tarihi Güncelleme

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product/upsertSellerProductBatchInfo
```

#### Örnek Request

```json
{
  "code": "8690530054158",
  "productBatchInfo": {
    "batchNumber": "TESTPARTINO",
    "serialNumber": "00001111",
    "expirationDate": "2026-02-26T00:00:00.000"
  }
}
```

### 5.2 Kullanım Kılavuzu & Ambalaj Görselleri Güncelleme

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/product/upsertSellerProductSecurityDocuments
```

#### Örnek Request

```json
{
  "code": "8690530054158",
  "productSecurityDocument": [
    {
      "url": "https://cdntest.pazarama.com/document/.../file.pdf",
      "type": 1
    },
    {
      "url": "https://cdntest.pazarama.com/gallery/.../front.jpeg",
      "type": 2
    },
    {
      "url": "https://cdntest.pazarama.com/gallery/.../back.png",
      "type": 3
    }
  ]
}
```

### 5.3 Ürün Ekleme ile Birlikte Gönderme

`product/create` servisine ek alanlar olarak gönderilebilir:

```json
{
  "productBatchInfo": {
    "batchNumber": "string",
    "serialNumber": "string",
    "expirationDate": "2026-02-13T13:49:09.048Z"
  },
  "securityDocuments": [
    { "url": "string", "type": 1 }
  ]
}
```

### Başarılı Response (Tüm temin servisleri için)

```json
{
  "data": true,
  "success": true,
  "messageCode": "MER0",
  "message": null,
  "userMessage": null,
  "fromCache": false
}
```
