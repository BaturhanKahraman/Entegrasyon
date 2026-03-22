# Hepsiburada Product Update API — Endpoint Reference

Base URL: `https://mpop-sit.hepsiburada.com/ticket-api`

> **Önemli:** Hepsiburada tarafından zenginleştirilmiş ve düzeltilmiş ürün bilgilerinin ezilmemesi için **sadece güncellenmek istenen alanlar** iletilmelidir.

---

## Ürün Güncelleme Servisi (uploadTicketViaFile)

**POST** `/api/integrator/import`

Mevcut ürünlerin bilgilerini güncellemek için kullanılır. JSON dosyası `multipart/form-data` olarak gönderilir.

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| version | integer | Hayır | 1 | API versiyonu |

### Request — Form Data

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| file | binary | Evet | .json uzantılı dosya |

### JSON Dosyası Formatı

```json
{
  "merchantId": "9b3e3fa8-cb96-4056-a83c-3192a4299f59",
  "items": [
    {
      "hbSku": "SAMPLE-SKU-INT-0",
      "productName": "Güncel Ürün Adı",
      "productDescription": "Güncel açıklama metni",
      "image1": "https://example.com/new-image1.jpg",
      "image2": "https://example.com/new-image2.jpg",
      "image3": "https://example.com/new-image3.jpg",
      "image4": "https://example.com/new-image4.jpg",
      "image5": "https://example.com/new-image5.jpg",
      "image6": "https://example.com/new-image6.jpg",
      "image7": "https://example.com/new-image7.jpg",
      "image8": "https://example.com/new-image8.jpg",
      "image9": "https://example.com/new-image9.jpg",
      "image10": "https://example.com/new-image10.jpg",
      "video": "https://example.com/video.mp4",
      "attributes": {
        "renk_variant_property": "Siyah",
        "numara_variant_property": "44",
        "malzeme": "Rugan"
      }
    },
    {
      "hbSku": "SAMPLE-SKU-INT-1",
      "attributes": {
        "malzeme": "",
        "desen": "Desenli"
      }
    }
  ]
}
```

### Güncelleme Alan Açıklamaları

| Alan | Tip | Açıklama |
|------|-----|----------|
| merchantId | string | Merchant ID (**zorunlu**) |
| hbSku | string | Hepsiburada SKU (**zorunlu**, güncellenemez — tanımlayıcı olarak kullanılır) |
| productName | string | Ürün adı |
| productDescription | string | Ürün açıklaması |
| image1 – image10 | string | Ürün görselleri (yeni URL ile gönderin, aynı URL üzerinden güncelleme yapmayın) |
| video | string | Video URL (yeni URL ile gönderin) |
| attributes | object | Özellik değerleri |
| kdv | string | KDV oranı |
| warrantyPeriod | string | Garanti süresi |
| desi | string | Desi bilgisi |
| isCustomizable | string | Özelleştirilebilir mi |
| barcode | string | Barkod |

### Özellik Güncelleme Kuralları

- **Değer silmek:** Özellik adını boş string olarak gönderin (`"malzeme": ""`)
- **Mevcut değer:** Zaten var olan bir özellik değerini aynı şekilde gönderirseniz talep oluşmaz
- **Sadece değişen alanları gönderin** — HB'nin zenginleştirdiği verilerin ezilmesini önler

### Response

```json
{
  "trackingId": "ed4723e3-4b3f-4afe-bdf8-c57a2fdde58b"
}
```

### Hata Kodları

| Kod | Mesaj |
|-----|-------|
| 4000 | Satıcı Bulunamadı |
| 4001 | HB SKU Bulunamadı |
| 4003 | Satıcı Boş Olamaz |
| 4004 | XXX Satıcısı için erişim reddedildi |
| 4005 | Geçersiz json dosyası |
| 4006 | Maksimum ürün sayısı aşıldı |
| 4007 | Tracking ID Bulunamadı |
| 4008 | Entegratör Öğeleri Bulunamadı |

### HTTP Hata Kodları

| Kod | Anlam |
|-----|-------|
| 400 | Bad Request — Geçersiz dosya |
| 403 | Forbidden — Yetki hatası |
| 405 | Method Not Allowed |
| 409 | Conflict |
