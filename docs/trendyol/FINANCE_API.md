# Trendyol Muhasebe & Finans API — Endpoint Reference

Bu servisler satıcının Trendyol'daki finansal hareketlerini (satış, iade, komisyon, ödeme, kargo faturası) çekmek için kullanılır. Muhasebe yazılımı entegrasyonu ve mali raporlama için gerekli.

---

## Cari Hesap Ekstresi — Satış & İadeler (settlements)

**GET** `/integration/finance/che/sellers/{sellerId}/settlements`

Satış, iade, indirim, kupon ve komisyon hareketlerini çeker. Finansal kayıtlar sipariş tesliminden sonra oluşur.

### Zorunlu Parametreler
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| transactionType(s) | string | İşlem tipi filtresi (virgülle çoklu) |
| startDate | long | Timestamp (ms) |
| endDate | long | Timestamp (ms) |

### Opsiyonel Parametreler
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| page | int | Sayfa |
| size | int | 500 veya 1000 |
| paymentOrderId | int | Ödeme emri ID |
| paymentDate | long | En erken ödeme tarihi |

### Transaction Types (settlements)
| Tip | Açıklama |
|-----|----------|
| `Sale` | Satış |
| `Return` | İade |
| `Discount` | İndirim |
| `DiscountCancel` | İndirim iptali |
| `Coupon` | Kupon |
| `CouponCancel` | Kupon iptali |
| `ProvisionPositive` | Pozitif provizyon |
| `ProvisionNegative` | Negatif provizyon |
| `SellerRevenuePositive` | Satıcı geliri (+) |
| `SellerRevenueNegative` | Satıcı geliri (−) |
| `CommissionPositive` | Komisyon (+) |
| `CommissionNegative` | Komisyon (−) |
| + Cancel varyantları | İptal versiyonları |

### Response
```json
{
  "page": 0,
  "size": 500,
  "totalPages": 878,
  "totalElements": 438974,
  "content": [
    {
      "id": "string",
      "transactionDate": 1613397671561,
      "barcode": "string",
      "transactionType": "Sale",
      "receiptId": 48376618,
      "description": "string",
      "debt": 0.0,
      "credit": 449.99,
      "paymentPeriod": 30,
      "commissionRate": 15.0,
      "commissionAmount": 67.50,
      "commissionInvoiceSerialNumber": null,
      "sellerRevenue": 382.49,
      "orderNumber": "string",
      "paymentOrderId": 112360,
      "paymentDate": 1615989671561,
      "sellerId": 123456,
      "country": "TR",
      "orderDate": 1720107451532,
      "affiliate": "TRENDYOLTR",
      "shipmentPackageId": 1111111111
    }
  ]
}
```

### Önemli Alanlar
- **debt/credit** — borç/alacak tutarları
- **commissionRate** — Trendyol komisyon oranı (%)
- **commissionAmount** — komisyon tutarı (TL)
- **sellerRevenue** — satıcıya kalan net tutar
- **paymentPeriod** — ödeme vadesi (gün)
- **paymentOrderId** — hangi ödeme emriyle ödendiği
- **paymentDate** — ödeme tarihi
- **affiliate** — TRENDYOLTR, TRENDYOLAZ vb.

---

## Cari Hesap Ekstresi — Diğer Finansallar (otherfinancials)

**GET** `/integration/finance/che/sellers/{sellerId}/otherfinancials`

Aynı parametre yapısı. Avans, havale, fatura ve ödeme emri bilgileri.

### Transaction Types (otherfinancials)
| Tip | Açıklama |
|-----|----------|
| `CashAdvance` | Nakit avans |
| `WireTransfer` | Havale/EFT |
| `IncomingTransfer` | Gelen transfer |
| `ReturnInvoice` | İade faturası |
| `CommissionAgreementInvoice` | Komisyon anlaşma faturası |
| `PaymentOrder` | Ödeme emri |
| `DeductionInvoices` | Kesinti faturaları (kargo faturası dahil) |
| `FinancialItem` | Finansal kalem |
| `Stoppage` | Stopaj |

---

## Kargo Faturası Detayları

**GET** `/integration/finance/che/sellers/{sellerId}/cargo-invoice/{invoiceSerialNumber}/items`

Kargo faturasının sipariş bazlı detaylarını getirir.

### invoiceSerialNumber Nasıl Bulunur?
1. `otherfinancials` endpoint'inden `transactionType='DeductionInvoices'` ile çek
2. `description` alanında "Kargo Faturası" veya "Kargo Fatura" olanları bul
3. O kaydın `id` değeri = `invoiceSerialNumber`

### Response
```json
{
  "page": 0,
  "size": 500,
  "totalPages": 1,
  "totalElements": 25,
  "content": [
    {
      "shipmentPackageType": "Gönderi Kargo Bedeli",
      "parcelUniqueId": 7260001151141191,
      "orderNumber": "2111681160",
      "amount": 34.24,
      "desi": 1
    }
  ]
}
```

### shipmentPackageType Değerleri
- `Gönderi Kargo Bedeli` — gönderim kargo ücreti
- `İade Kargo Bedeli` — iade kargo ücreti
