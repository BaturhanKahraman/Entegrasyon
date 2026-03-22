# Pazarama — Fatura & Finans API

## 1. Fatura Linki Güncelleme (Sipariş Bazlı)

`deliveryCompanyId` ve `trackingNumber` null gönderilirse tüm siparişteki ürünlere fatura yüklenir. Değer gönderilirse ilgili paketteki ürünlere fatura yüklenir.

**Servis Tipi:** POST

```
POST https://{baseurl}/order/invoice-link
```

### Örnek Request (Tüm Sipariş)

```json
{
  "invoiceLink": "https://faturaUrl.pdf",
  "orderid": "0d145804-5d05-4e4a-a4a0-921e45c8a5e3",
  "deliveryCompanyId": null,
  "trackingNumber": null
}
```

### Örnek Request (Paket Bazlı)

```json
{
  "invoiceLink": "https://faturaUrl.pdf",
  "orderid": "0d145804-5d05-4e4a-a4a0-921e45c8a5e3",
  "deliveryCompanyId": "6d6e004a-23b1-43d1-4d30-08d8e87e3449",
  "trackingNumber": "321321321"
}
```

---

## 2. İtem Bazlı Fatura Yükleme

Tek bir item'a fatura yüklemek için:

**Servis Tipi:** POST

```
POST https://{baseurl}/order/invoice-link
```

```json
{
  "invoiceLink": "https://faturaUrl.pdf",
  "orderItemId": "209c4d98-08de-4ea4-b681-c4b148b2e8fb",
  "orderid": "0d145804-5d05-4e4a-a4a0-921e45c8a5e3",
  "deliveryCompanyId": "6d6e004a-23b1-43d1-4d30-08d8e87e3449",
  "trackingNumber": "321321321"
}
```

### Farklı Item'lara Fatura Yükleme

**POST:** `https://isortagimapi.pazarama.com/order/multiple-invoice-link`

```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "invoiceLink": "string",
  "deliveryCompanyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "trackingNumber": "string",
  "orderItemIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

---

## 3. Muhasebe ve Finans Servisi

Satış, iade gibi finansal verileri tarih aralığı ile sorgular.

**Servis Tipi:** POST

```
POST https://{baseurl}/order/paymentAgreement
```

### Örnek Request

```json
{
  "startDate": "2023-01-01T00:00:01.768Z",
  "endDate": "2023-01-02T23:59:59.768Z",
  "allowanceDate": null,
  "orderId": null
}
```

### Response Parametreleri

| Parametre | Açıklama | Veri Tipi |
|-----------|----------|-----------|
| `orderId` | Sipariş numarası | long |
| `trxCode` | İşlem kodu | string |
| `trxId` | İşlem ID | guid |
| `amount` | İşlem tutarı | decimal |
| `installmentNumber` | Taksit sayısı | int |
| `commissionAmount` | Komisyon tutarı | decimal |
| `couponDiscount` | Kupon indirimi | decimal |
| `allowanceAmount` | Hakediş tutarı | decimal |
| `status` | İşlem durumu (Satış, İade vb.) | string |
| `transactionDate` | İşlem tarihi | datetime |
| `transferredDate` | Transfer tarihi | datetime |
| `totalAmount` | Toplam tutar | decimal |
| `totalCommission` | Toplam komisyon | decimal |
| `totalAllowance` | Toplam hakediş | decimal |

### Örnek Response

```json
{
  "data": {
    "transactionList": [
      {
        "orderId": 459916556,
        "trxCode": "ORDER-23003TArH07110322",
        "trxId": "cab6971e-946b-40ec-97ab-08daed6ab000",
        "amount": 1,
        "installmentNumber": 0,
        "commissionAmount": 0,
        "couponDiscount": 0,
        "allowanceAmount": 1,
        "status": "Satış",
        "transactionDate": "2023-01-03T19:00:00",
        "transferredDate": "2023-02-03T00:00:00"
      }
    ],
    "totalAmount": 1,
    "totalCommission": 0,
    "totalAllowance": 1
  },
  "success": true
}
```
