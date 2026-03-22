# Pazarama — Sipariş API

## 1. Sipariş Listeleme

Başarılı siparişleri tarih aralığı, sipariş numarası veya sayfalama ile sorgular.

> **NOT:** `endDate` parametresindeki tarihten **önceki** siparişler getirilir (o gün dahil değil).
> Başlangıç ile bitiş tarihi arası **1 ayı geçemez**.
> Sipariş numarası ile sorguda **6 aya kadar** olan siparişler gelir.

**Servis Tipi:** POST

```
POST https://{baseurl}/order/getOrdersForApi
```

### DeliveryType Enum

| Değer | Açıklama |
|-------|----------|
| 1 | Cargo (Kargo) |
| 2 | Courier (Kurye) |
| 3 | Store (Mağazadan Teslimat) |
| 4 | Digital |
| 5 | Donation |
| 10001 | DeliveryPoint |

> Dijital teslimatta iletilecek kod `deliveryDetail.phoneNumber` alanındadır. Diğer teslimat tiplerinde bu alan null gelir.

### OrderItemStatus Enum

| Değer | Açıklama |
|-------|----------|
| 3 | Siparişiniz Alındı |
| 5 | Siparişiniz Kargoya Verildi |
| 6 | Siparişiniz İptal Edildi |
| 7 | İade Süreci Başlatıldı |
| 8 | İade Onaylandı |
| 9 | İade Reddedildi |
| 10 | İade Edildi |
| 11 | Teslim Edildi |
| 12 | Siparişiniz Hazırlanıyor |
| 13 | Tedarik Edilemedi |
| 14 | Teslim Edilemedi |
| 16 | Siparişiniz Mağazada |
| 18 | İptal Süreci Başlatıldı |
| 19 | Siparişiniz Teslimat Noktasında |

### InvoiceType Enum

| Değer | Açıklama |
|-------|----------|
| 1 | Bireysel |
| 2 | Kurumsal |

### PaymentType Enum

| Değer | Açıklama |
|-------|----------|
| 1 | Banka/Kredi Kartı |
| 5 | Cüzdan |
| 8 | Visa-Tek Tıkla Öde |
| 11 | Taksitli Ek Hesap |

### Response Parametreleri

| Parametre | Açıklama | Veri Tipi |
|-----------|----------|-----------|
| `orderId` | Sipariş ID | guid |
| `orderNumber` | Müşteriye gösterilen sipariş numarası | long |
| `orderDate` | Sipariş tarihi | datetime |
| `orderAmount` | Sipariş tutarı | decimal |
| `paymentType` | Ödeme tipi | int |
| `orderStatus` | Sipariş statüsü | int |
| `customerId` | Müşteri ID | guid |
| `customerName` | Müşteri adı soyadı | string |
| `customerEmail` | Müşteri e-mail | string |
| `shipmentAddress` | Taşıma adresi | object |
| `billingAddress` | Fatura adresi | object |
| `items` | Sipariş kalemleri | list |
| `items[].orderItemId` | Ürün kalemi ID | guid |
| `items[].orderItemStatus` | Ürün sipariş durumu | int |
| `items[].quantity` | Adet | int |
| `items[].listPrice` | Liste fiyatı | money object |
| `items[].salePrice` | Satış fiyatı | money object |
| `items[].taxAmount` | Vergi tutarı | money object |
| `items[].shipmentAmount` | Kargo ücreti | money object |
| `items[].totalPrice` | Toplam tutar | money object |
| `items[].discountAmount` | İndirim tutarı | money object |
| `items[].deliveryType` | Teslimat tipi | int |
| `items[].shipmentCode` | Anlaşmalı kargo gönderi kodu | string |
| `items[].shipmentCost` | Anlaşmalı kargo maliyet bilgisi | money object |
| `items[].cargo.companyName` | Kargo firması adı | string |
| `items[].cargo.trackingNumber` | Kargo takip numarası | string |
| `items[].cargo.trackingUrl` | Kargo takip linki | string |
| `items[].product.code` | Ürün barkodu | string |
| `items[].product.name` | Ürün adı | string |
| `items[].product.vatRate` | KDV oranı | int |

> **Anlaşmalı kargo notları:**
> - Anlaşmalı firmalar için kargo gönderi kodu `shipmentCode` alanında görüntülenir.
> - Anlaşmalı firmalar için kargo ücreti `shipmentAmount` alanında görüntülenmez, maliyet `shipmentCost` alanındadır.
> - `estimatedShippingDate` ile termin süresi gösterimi eklenmiştir.

### Örnek Request (Tarih + Saat/Dakika)

```json
{
  "orderNumber": 735071747,
  "startDate": "2021-05-14T13:30",
  "endDate": "2021-05-24T17:30"
}
```

### Örnek Request (Sayfalama)

```json
{
  "pageSize": 500,
  "pageNumber": 1,
  "startDate": "2023-02-01",
  "endDate": "2023-02-17"
}
```

### Örnek Response

```json
{
  "data": [
    {
      "orderId": "d4fb5f23-6330-4eb0-a81c-35847560df86",
      "orderNumber": 235795225,
      "orderDate": "2023-01-25 15:22",
      "orderAmount": 1,
      "shipmentAmount": 0,
      "discountAmount": 0.2,
      "discountDescription": "Moda100",
      "currency": "TL",
      "paymentType": 1,
      "orderStatus": 3,
      "customerId": "f442961c-d818-4de3-1e11-08d941fd1d2f",
      "customerName": "Pazarama Sipariş",
      "customerEmail": "pzrmsprs@pazarama.com",
      "shipmentAddress": {
        "addressId": "faeebdb9-7c21-4b59-8b37-03a3793f1978",
        "title": "İş Bankası İkamet Adresi",
        "nameSurname": "Pazarama Sipariş",
        "cityName": "Kütahya",
        "districtName": "Merkez",
        "addressDetail": "Cami sk. NO: 5",
        "phoneNumber": "054243029677"
      },
      "billingAddress": {
        "invoiceType": 1,
        "identityNumber": null,
        "companyName": null,
        "taxNumber": null,
        "taxOffice": null,
        "isEInvoiceObliged": false
      },
      "items": [
        {
          "orderItemId": "5516bd97-5344-4d22-a346-0f263c0f51ca",
          "orderItemStatus": 3,
          "deliveryType": 1,
          "quantity": 1,
          "listPrice": { "value": 1, "valueString": "1,00 TL", "currency": "TL" },
          "salePrice": { "value": 1, "valueString": "1,00 TL", "currency": "TL" },
          "taxAmount": { "value": 0.15, "valueString": "0,15 TL" },
          "totalPrice": { "value": 1, "valueString": "1,00 TL" },
          "cargo": {
            "companyName": "PTT",
            "trackingNumber": null,
            "trackingUrl": "https://gonderitakip.ptt.gov.tr"
          },
          "product": {
            "productId": "0de9dde9-b7e6-47a8-7205-08daef63e1be",
            "name": "Deneme Tişört Deneme",
            "code": "deneroyaldene-1",
            "vatRate": 18
          }
        }
      ]
    }
  ],
  "success": true,
  "messageCode": "ORD0"
}
```

---

## 2. Sipariş Akışı (Önemli)

Siparişlerin **OrderItemId** bazlı takip edilmesi gerekmektedir.

### Sipariş Durum Akışı

1. Sipariş ilk oluştuğunda **statü 3** (Siparişiniz Alındı)
2. Alıcı statü 3'te siparişi **iptal edebilir** → statü 6
3. Kargoya verilecekse → **statü 12** (Siparişiniz Hazırlanıyor) yapılmalı
4. Pazarama Kargo Anlaşmalı satıcılarda **statü 12 olmadan kargo kartı açılamaz**
5. Kargolanamayacak siparişler → **statü 13** (Tedarik Edilemedi)
6. Hazırlanıyor (12) statüsünde alıcı **iptal talebi** iletebilir → statü 18
7. Hazırlanıyor'dan sonra:
   - Dijital ürünler → statü 11 (Teslim Edildi) veya 14 (Teslim Edilemedi)
   - Anlaşmalı kargo → süreç otomatik ilerler
   - Anlaşmasız kargo → statü 5 (Kargoya Verildi) yapılmalı, takip tedarikçide
8. Teslim Edildi (11) sonrası alıcı **iade süreci** başlatabilir

---

## 3. Kargo Takip Durumu Bildirme

**Servis Tipi:** PUT

```
PUT https://{baseurl}/order/updateOrderStatus
```

### Request Parametreleri

| Parametre | Açıklama | Veri Tipi | Zorunlu |
|-----------|----------|-----------|---------|
| `orderNumber` | Sipariş numarası | long | Evet |
| `item.orderItemId` | Ürün ID | guid | Evet |
| `item.status` | Yeni statü (5=Kargoya Verildi) | int | Evet |
| `item.shippingTrackingNumber` | Kargo takip numarası | string | Evet |
| `item.trackingUrl` | Kargo takip linki | string | Opsiyonel |
| `item.cargoCompanyId` | Kargo firması ID | guid | Evet |
| `item.deliveryType` | Teslimat tipi | int | Evet |

### Örnek Request

```json
{
  "orderNumber": 127063369,
  "item": {
    "orderItemId": "6b09a841-2fb1-4afb-bdc1-7078f95ba4c5",
    "status": 5,
    "deliveryType": 1,
    "shippingTrackingNumber": "1615598038857",
    "trackingUrl": "https://www.yurticikargo.com/",
    "cargoCompanyId": "7b5567ff-abe7-487e-5c79-08d8e480366a"
  }
}
```

---

## 4. Siparişe Ait Ürün Durumu (OrderItem) Güncelleme

> **NOT:** Statü 3'ten sonra kargoya verilecekse **önce statü 12** yapılmalıdır.

**Servis Tipi:** PUT

```
PUT https://{baseurl}/order/updateOrderStatus
```

### Örnek Request

```json
{
  "orderNumber": 735071747,
  "item": {
    "orderItemId": "d6da388c-f465-4f9c-8800-afc9271b801b",
    "status": 12
  }
}
```

---

## 5. Toplu Sipariş Durumu Güncelleme

Sipariş statüsünü item bazında değil toplu olarak günceller.

**Servis Tipi:** PUT

```
PUT https://{baseurl}/order/updateOrderStatusList
```

### Örnek Request

```json
{
  "orderNumber": 735071747,
  "status": 11
}
```

---

## 6. Toplu Item Statü Güncelleme (Tek Request)

Bir siparişteki tüm item'ların statüsünü tek request'te günceller.

> **NOT:** `deliveryType: 1` ve `subStatus: 0` olarak işlem yapılır.
> Teslim edildi için status hariç diğer alanlar null gönderilir.

**Servis Tipi:** POST

```
POST https://isortagimapi.pazarama.com/order/api/bulk-status-update
```

### Örnek Request

```json
{
  "orderNumber": 236730742,
  "orderItemIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"],
  "updateShipmentDto": {
    "cargoCompanyId": "8eb9aeb7-fd11-425b-9930-08d8e48cc18f",
    "deliveryType": 1,
    "shipmentNumber": "PZörnek",
    "shippingTrackingNumber": "604164846124örnek",
    "status": 5,
    "subStatus": 0,
    "trackingUrl": "https://www.araskargo.com.tr/#!/takipörnek"
  }
}
```

---

## 7. Split Order / Split Refund (V2)

Parçalı sipariş kullanan iş ortakları için V2 servis. Aynı üründen birden fazla sipariş olduğunda farklı `orderItemId` altında `quantity: 1` olarak görülür.

**Servis Tipi:** POST

```
POST https://{baseurl}/order/getOrdersForApiV2
```

> Başlangıç ile bitiş tarihi arası **1 ayı geçemez**.

### Örnek Request

```json
{
  "orderNumber": 735071747,
  "startDate": "2024-09-01T00:01",
  "endDate": "2024-09-30T23:59"
}
```

### İadeler için Split

```
POST https://{baseurl}/order/getRefund
```

`SplitItems: true` gönderildiğinde quantity 1 olarak ve refundId farklılaştırılmış olarak görülür.

```json
{
  "pageSize": 10,
  "pageNumber": 1,
  "refundStatus": 1,
  "SplitItems": true,
  "requestStartDate": "2021-10-01",
  "requestEndDate": "2021-10-08"
}
```

---

## 8. Sipariş Paket Bölme

Farklı depodan/adresten gönderilebilmesi için sipariş paketleme.

### 8.1 Paket Bölme

**Servis Tipi:** PUT

```
PUT https://{baseurl}/order/api/delivery/split
```

```json
{
  "orderId": "d5ffad30-05d0-4ddb-ae0d-02bae029b3ec",
  "shipmentCode": "PZ00156261297",
  "sellerAddressId": "41883dc0-03c5-402a-3b09-08dcde201845",
  "cargoCompanyId": "6d6e004a-23b1-43d1-4d30-08d8e87e3449",
  "items": [
    { "orderItemId": "9ec8e34f-830b-432b-bc7a-6793b69f97c6", "quantity": 1 }
  ]
}
```

### 8.2 Paketten Vazgeç

**Servis Tipi:** PUT

```
PUT https://{baseurl}/order/api/delivery/cancel
```

```json
{
  "orderId": "d5ffad30-05d0-4ddb-ae0d-02bae029b3ec",
  "shipmentCode": "PZ98214690189"
}
```

---

## 9. Paket Yapısı Yönetimi

### PackageStatus Enum

| Değer | Açıklama |
|-------|----------|
| 0 (NONE) | Paket yok |
| 10 (PACKED) | Güncel paket |
| 20 (UNPACKED) | Önceki paket (yeni paket oluşturulunca) |
| 30 (UNPACKED_NOTSUPPLY) | Tedarik edilemedi yapılan item'ın paketi |
| 40 (UNPACKED_CANCELLED) | İptal edilen item'ın paketi |

### 9.1 Paket Listeleme

**GET:** `https://isortagimapi.pazarama.com/order/api/shipment-packages`

```json
{ "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

### 9.2 Paket Oluşturma

**POST:** `https://isortagimapi.pazarama.com/order/api/packages-split`

> `sellerAddressId` boş gönderilirse varsayılan depo adresi kullanılır.

```json
{
  "orderId": "36ccecf2-786f-4b6b-82dc-87874df2e2dc",
  "sellerId": "bdd4d613-d4c8-4bd6-8c24-08dd3556af10",
  "apiOrderItemPackages": [
    {
      "orderItemIds": ["d5fd944b-562b-48b3-ada1-18e42b63564c"],
      "sellerAddressId": "CCC02724-9F12-4789-E3A2-08DD4A725426",
      "cargoCompanyId": "8eb9aeb7-fd11-425b-9930-08d8e48cc18f"
    }
  ]
}
```

### 9.3 Paket Güncelleme

**PUT:** `https://isortagimapi.pazarama.com/order/update-packages`

```json
{
  "packages": [
    {
      "packageNumber": 1249788788,
      "cargoCompanyId": "603228D3-C1D8-4AF7-94AE-08DE5AD2AC5A",
      "sellerAddressId": "ccc02724-9f12-4789-e3a2-08dd4a725426"
    }
  ]
}
```

---

## 10. Ürün Maximum Satış Stok Adeti

Bir siparişte alınabilecek maximum adet. `productSaleLimitQuantity` create ederken zorunlu değildir; body'ye eklenmediğinde veya 0 gönderildiğinde boş yansır.

**POST:** `https://isortagimapi.pazarama.com/product/upsertSellerProductSaleLimit`

```json
{
  "code": "productSaleLimittest",
  "Quantity": 1
}
```

---

## 11. Stopaj — Altın Kategorisi İşçilik Bedeli

Siparişteki item için işçilik tutarı gönderimi.

> **NOT:** Sipariş alındı, hazırlanıyor, kargoya verildi statülerinde giriş yapılabilir. Teslim edildi, iade onaylandı/reddedildi, iptal onaylandı/reddedildi aşamalarında giriş yapılamaz.

**Servis Tipi:** PUT

```
PUT https://{baseurl}/order/api/{orderId}/items/labor-costs
```

### Request Parametreleri

| Parametre | Açıklama | Veri Tipi | Zorunlu |
|-----------|----------|-----------|---------|
| `orderItemId` | Item ID | guid | Evet |
| `laborCost` | İşçilik maliyet değeri | int | Evet |
| `processCount` | Kaç item için işlem yapılacağı | int | Evet |

```json
{
  "items": [
    {
      "orderItemId": "7caa8b89-894f-4ce5-9175-47c496e17657",
      "laborCost": 10,
      "processCount": 0
    }
  ]
}
```
