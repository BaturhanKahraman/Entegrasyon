# Çiçeksepeti Invoice API

## Fatura Gönderimi

**Endpoint:** `POST /Branch/SendInvoiceMail`

> **DİKKAT:** Bu endpoint `/api/v1/` prefix'i kullanmaz! Path doğrudan `/Branch/SendInvoiceMail`'dir.

Sipariş kalemlerine PDF fatura ekler.

**Request Body:**
```json
{
  "items": [
    {
      "orderItemId": 111,
      "document": "JVBERi0xLjQK... (base64 encoded PDF)",
      "documentUrl": null
    }
  ]
}
```

### Alanlar

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderItemId` | int | Evet | Sipariş kalem ID |
| `document` | string | Koşullu | Base64 encoded PDF içeriği |
| `documentUrl` | string | Koşullu | PDF dosyasının URL'i |

> `document` veya `documentUrl` alanlarından **en az biri** gönderilmelidir. Her ikisi de gönderilirse `document` (base64) öncelik alır.

### Response

- **200 OK:** Fatura başarıyla gönderildi
- **500 Internal Server Error:** Fatura gönderilemedi

### Notlar

- Fatura formatı PDF olmalıdır
- Base64 encoding ile doğrudan body'de gönderilebilir veya erişilebilir bir URL verilebilir
- Bu endpoint'in farklı path prefix'i nedeniyle API client'ta özel handle gereklidir
