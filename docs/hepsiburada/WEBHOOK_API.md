# Hepsiburada Webhook API — Sipariş Webhook Modeli

## Genel Bilgiler

Webhook modeli ile calisacak firmalar kendi **BaseURL**'lerini olusturur ve Hepsiburada'ya iletir. HB, Sipariş yasam dongusundeki event'leri bu URL'lere **push** eder (POST/PUT).

> **Guvenlik:** Tum webhook istekleri **Basic Auth** ile dogrulanir.

> **Idempotent Tasarim:** Servise response iletirken idempotent mantigi kullanilmalidir. Ayni event birden fazla kez gelebilir.

> **API Kaynakli Islemler:** API uzerinden kendiniz yaptiginiz islemler webhook uzerinden tekrar gonderilmez.

> **IIS Uyarisi:** IIS'de baseurl olusturuldugunda default PUT servisi kapali gelir. PUT event'lerini alabilmek icin bu servisin acilmasi gerekir.

> **Test Sureci:** Webhook entegrasyonu once test ortaminda test edilmeli, ardindan canli ortama gecilmelidir.

---

## 1. Create Order

**POST** `{baseUrl}/orders`

HB yeni Sipariş olusturuldugunda bu endpoint'e Sipariş bilgilerini push eder.

### Response — `201 Created`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| dueDate | string | Son islem tarihi |
| lastStatusUpdateDate | string | Son durum guncelleme tarihi |
| id | string | Sipariş kalem ID |
| sku | string | HB SKU |
| orderNumber | string | Sipariş numarasi |
| orderDate | string | Sipariş tarihi |
| quantity | int | Adet |
| merchantId | UUID | Satici ID |
| totalPrice | decimal | Toplam fiyat |
| unitPrice | decimal | Birim fiyat |
| hbDiscount | decimal | HB indirimi |
| vat | decimal | KDV tutari |
| vatRate | decimal | KDV orani |
| customerName | string | Musteri adi |
| status | string | Durum (`Open`, `Unpacked`) |
| shippingAddress | object | Teslimat adresi |
| invoice | object | Fatura bilgileri |
| invoice.turkishIdentityNumber | string | TC kimlik no |
| invoice.taxNumber | string | Vergi numarasi |
| invoice.taxOffice | string | Vergi dairesi |
| invoice.address | string | Fatura adresi |
| sapNumber | string | SAP numarasi |
| dispatchTime | int | Sevk suresi |
| commission | decimal | Komisyon |
| paymentTermInDays | int | Odeme vadesi (gun) |
| commissionType | string | Komisyon tipi |
| cargoCompanyModel | object | Kargo firmasi bilgileri |
| cargoCompanyModel.id | int | Kargo firma ID |
| cargoCompanyModel.name | string | Kargo firma adi |
| cargoCompanyModel.shortName | string | Kisa ad |
| cargoCompanyModel.logoUrl | string | Logo URL |
| cargoCompanyModel.trackingUrl | string | Takip URL |
| customizedText01–04 | string | Ozel metin alanlari |
| deliveryType | string | Teslimat tipi |
| deliveryOptionId | int | Teslimat secenegi (`1`=Standard, `5`=Same Day, `6`=Tomorrow) |
| slot | object | Zaman dilimi |
| pickUpTime | string | Teslim alma zamani |
| merchantSKU | string | Satici SKU |
| purchasePrice | decimal | Alis fiyati |
| discountPriceToBeInvoicedHb | decimal | HB'ye faturalanacak indirim |
| creationReason | string | Olusturulma nedeni |
| properties | object | Urun ozellikleri |
| warehouse | object | Depo bilgisi |

**creationReason degerleri:**

| Deger | Aciklama |
|-------|----------|
| OrderCreated | Yeni Sipariş |
| OrderLineTransferred | Kalem transferi |
| OrderLineResend | Kalem tekrar gonderimi |
| ClaimChangeAccepted | Degisim talebi kabul |
| DeliveryCreated | Teslimat olusturuldu |

### Ornek Request Body

```json
{
  "items": [
    {
      "dueDate": "2026-03-25T23:59:59",
      "lastStatusUpdateDate": "2026-03-22T14:30:00",
      "id": "abc12345-6789-def0-1234-567890abcdef",
      "sku": "HBCV00001ABCDE",
      "orderNumber": "HB-100200300",
      "orderDate": "2026-03-22T10:15:00",
      "quantity": 2,
      "merchantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "totalPrice": 599.90,
      "unitPrice": 299.95,
      "hbDiscount": 0.00,
      "vat": 95.98,
      "vatRate": 20.00,
      "customerName": "Ahmet Yilmaz",
      "status": "Open",
      "shippingAddress": {
        "address": "Ataturk Cad. No:15 D:3",
        "city": "Istanbul",
        "town": "Kadikoy",
        "district": "Caferaga",
        "postalCode": "34710",
        "countryCode": "TR"
      },
      "invoice": {
        "turkishIdentityNumber": "12345678901",
        "taxNumber": "",
        "taxOffice": "",
        "address": "Ataturk Cad. No:15 D:3 Kadikoy/Istanbul"
      },
      "sapNumber": "SAP-001234",
      "dispatchTime": 2,
      "commission": 45.00,
      "paymentTermInDays": 14,
      "commissionType": "PERCENTAGE",
      "cargoCompanyModel": {
        "id": 1,
        "name": "Yurtici Kargo",
        "shortName": "YK",
        "logoUrl": "https://images.hepsiburada.net/cargo/yk-logo.png",
        "trackingUrl": "https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code="
      },
      "customizedText01": "",
      "customizedText02": "",
      "customizedText03": "",
      "customizedText04": "",
      "deliveryType": "STANDARD",
      "deliveryOptionId": 1,
      "slot": null,
      "pickUpTime": null,
      "merchantSKU": "SELLER-SKU-001",
      "purchasePrice": 200.00,
      "discountPriceToBeInvoicedHb": 0.00,
      "creationReason": "OrderCreated",
      "properties": {},
      "warehouse": {
        "id": "wh-001",
        "name": "Ana Depo"
      }
    }
  ]
}
```

---

## 2. Create Packages

**POST** `{baseUrl}/packages`

Otomatik veya panel uzerinden paketleme işlemi sonrasi paket bilgileri push edilir.

### Response — `201 Created`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| id | string | Paket ID |
| status | string | Paket durumu |
| customerId | string | Musteri ID |
| customerName | string | Musteri adi |
| orderDate | string | Sipariş tarihi |
| dueDate | string | Son islem tarihi |
| barcode | string | Barkod |
| packageNumber | string | Paket numarasi |
| cargoCompany | string | Kargo firmasi |
| shippingAddressDetail | string | Teslimat adresi detayi |
| recipientName | string | Alici adi |
| email | string | E-posta |
| phoneNumber | string | Telefon |
| billingAddress | string | Fatura adresi |
| City | string | Sehir |
| Town | string | Ilce |
| District | string | Mahalle |
| PostalCode | string | Posta kodu |
| taxOffice | string | Vergi dairesi |
| taxNumber | string | Vergi numarasi |
| identityNo | string | TC kimlik no |
| merchantId | UUID | Satici ID |
| warehouse | object | Depo bilgisi |

### Paket Items Alanlari

| Alan | Tip | Aciklama |
|------|-----|----------|
| lineItemId | string | Sipariş kalemi ID |
| listingId | string | Listeleme ID |
| hbSku | string | HB SKU |
| merchantSku | string | Satici SKU |
| quantity | int | Adet |
| price | decimal | Fiyat |
| vat | decimal | KDV tutari |
| totalPrice | decimal | Toplam fiyat |
| commission | decimal | Komisyon |
| unitHBDiscount | decimal | Birim HB indirimi |
| totalHBDiscount | decimal | Toplam HB indirimi |
| merchantUnitPrice | decimal | Satici birim fiyati |
| merchantTotalPrice | decimal | Satici toplam fiyati |
| cargoPaymentInfo | object | Kargo odeme bilgisi |
| properties | object | Urun ozellikleri |
| productName | string | Urun adi |
| orderNumber | string | Sipariş numarasi |
| deliveryType | string | Teslimat tipi |
| weight | decimal | Agirlik |
| gtip | string | GTIP kodu |
| vatRate | decimal | KDV orani |
| purchasePrice | decimal | Alis fiyati |
| discountToBeBilledToHB | decimal | HB'ye faturalanacak indirim |
| productBarcode | string | Urun barkodu |
| creationReason | string | Olusturulma nedeni |

### Ornek Request Body

```json
{
  "id": "pkg-abc123",
  "status": "Packed",
  "customerId": "cust-001",
  "customerName": "Ahmet Yilmaz",
  "orderDate": "2026-03-22T10:15:00",
  "dueDate": "2026-03-25T23:59:59",
  "barcode": "HB1234567890",
  "packageNumber": "PKG-100200300-001",
  "cargoCompany": "Yurtici Kargo",
  "shippingAddressDetail": "Ataturk Cad. No:15 D:3",
  "recipientName": "Ahmet Yilmaz",
  "email": "ahmet@example.com",
  "phoneNumber": "+905551234567",
  "billingAddress": "Ataturk Cad. No:15 D:3",
  "City": "Istanbul",
  "Town": "Kadikoy",
  "District": "Caferaga",
  "PostalCode": "34710",
  "taxOffice": "",
  "taxNumber": "",
  "identityNo": "12345678901",
  "merchantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "items": [
    {
      "lineItemId": "li-001",
      "listingId": "lst-001",
      "hbSku": "HBCV00001ABCDE",
      "merchantSku": "SELLER-SKU-001",
      "quantity": 2,
      "price": 299.95,
      "vat": 47.99,
      "totalPrice": 599.90,
      "commission": 45.00,
      "unitHBDiscount": 0.00,
      "totalHBDiscount": 0.00,
      "merchantUnitPrice": 299.95,
      "merchantTotalPrice": 599.90,
      "cargoPaymentInfo": null,
      "properties": {},
      "productName": "Ornek Urun",
      "orderNumber": "HB-100200300",
      "deliveryType": "STANDARD",
      "weight": 1.5,
      "gtip": "6404199000",
      "vatRate": 20.00,
      "purchasePrice": 200.00,
      "discountToBeBilledToHB": 0.00,
      "productBarcode": "8680000000001",
      "creationReason": "OrderCreated"
    }
  ],
  "warehouse": {
    "id": "wh-001",
    "name": "Ana Depo"
  }
}
```

---

## 3. Intransit

**PUT** `{baseUrl}/packages/{packagenumber}/intransit`

Paket kargoya verildiginde push edilir.

### Response — `204 Success`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| merchantId | UUID | Satici ID |
| shippedDate | string | Kargoya verilme tarihi |
| packageNumber | string | Paket numarasi |
| barcode | string | Barkod |
| trackingInfoCode | string | Kargo takip kodu |
| trackingInfoUrl | string | Kargo takip URL |
| deci | decimal | Desi degeri |

---

## 4. Deliver

**PUT** `{baseUrl}/packages/{packagenumber}/deliver`

Paket teslim edildiginde push edilir.

### Response — `204 Success`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| merchantId | UUID | Satici ID |
| receivedDate | string | Teslim tarihi |
| receivedBy | string | Teslim alan kisi |
| packageNumber | string | Paket numarasi |
| barcode | string | Barkod |

---

## 5. Undeliver

**PUT** `{baseUrl}/packages/{packagenumber}/undeliver`

Paket teslim edilemedigi durumda push edilir.

### Response — `204 Success`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| merchantId | UUID | Satici ID |
| undeliveredDate | string | Teslim edilememe tarihi |
| undeliveredReason | string | Teslim edilememe nedeni |
| packageNumber | string | Paket numarasi |
| barcode | string | Barkod |

---

## 6. Order Cancel

**PUT** `{baseUrl}/lineitems/{lineitemid}/cancel`

Sipariş iptal edildiginde push edilir.

### Response — `204 Success`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| cancelDate | string | Iptal tarihi |
| merchantId | UUID | Satici ID |
| id | string | Kalem ID |
| quantity | int | Iptal edilen adet |
| cancelledBy | string | Iptal eden (`Merchant`, `Customer`, `Fraud`) |
| cancelReasonCode | string | Iptal neden kodu |
| orderNumber | string | Sipariş numarasi |
| isUnpackedLine | bool | Paketli kalem iptali durumunda `true` |

---

## 7. Unpack

**PUT** `{baseUrl}/packages/{packagenumber}/unpack`

Paket bozuldugunda (split/unpack) push edilir.

### Response — `204 Success`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| unpackedDate | string | Paket bozulma tarihi |
| packageNumber | string | Paket numarasi |
| merchantId | UUID | Satici ID |
| orderNumbers | string[] | Ilgili Sipariş numaralari |

---

## 8. Change Shipping Address Order

**PUT** `{baseUrl}/orders/{ordersnumber}/shippingaddress`

Teslimat adresi degistiginde push edilir.

### Response — `204 Success`

### Alanlar

| Alan | Tip | Aciklama |
|------|-----|----------|
| orderNumber | string | Sipariş numarasi |
| addressId | string | Adres ID |
| address | string | Adres detayi |
| name | string | Alici adi |
| email | string | E-posta |
| countryCode | string | Ulke kodu |
| phoneNumber | string | Telefon |
| alternatePhoneNumber | string | Alternatif telefon |
| district | string | Ilce |
| city | string | Sehir |
| town | string | Mahalle |
