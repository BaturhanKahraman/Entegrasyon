# Hepsiburada Listing API — Endpoint Reference

Base URL: `https://listing-external-sit.hepsiburada.com`

> Tüm endpoint'ler HTTP Basic Auth ve `User-Agent` header gerektirir.

---

## Listing Bilgilerini Sorgulama

**GET** `/listings/merchantid/{merchantId}`

Satıcıya ait listing bilgilerini listeler. Pagination zorunludur.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | UUID | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| offset | integer | Evet | 0 | Başlangıç noktası |
| limit | integer | Evet | 10 | Sayfa boyutu |
| hbSkuList | string | Hayır | — | HB SKU filtresi |
| merchantSkuList | string | Hayır | — | Merchant SKU filtresi |
| salable-listings | boolean | Hayır | — | Satışta olanlar |
| notsalable-listings | boolean | Hayır | — | Satışta olmayanlar |
| updateStartDate | datetime | Hayır | — | Güncelleme başlangıç tarihi |
| updateEndDate | datetime | Hayır | — | Güncelleme bitiş tarihi |
| productId | string | Hayır | — | Ürün ID filtresi |

### Response

```json
{
  "listings": [
    {
      "listingId": "uuid",
      "hepsiburadaSku": "HBCV00001XXXXX",
      "merchantSku": "SKU-001",
      "price": 130.50,
      "availableStock": 100,
      "dispatchTime": 3,
      "cargoCompany1": "Yurtiçi Kargo",
      "cargoCompany2": null,
      "cargoCompany3": null,
      "shippingAddressLabel": "Depo Adresi",
      "shippingProfileName": "Profil-1",
      "claimAddressLabel": "İade Adresi",
      "maximumPurchasableQuantity": 10,
      "minimumPurchasableQuantity": 1,
      "pricings": [
        {
          "finalPrice": 110.00,
          "startDate": "2024-01-01T00:00:00Z",
          "endDate": "2024-01-31T23:59:59Z",
          "debtors": [
            { "debtor": "Hepsiburada", "amount": 50.0 }
          ]
        }
      ],
      "isSalable": true,
      "isSuspended": false,
      "isLocked": false,
      "lockReasons": [],
      "isFrozen": false,
      "freezeReasons": [],
      "priceIncreaseDisabled": false,
      "priceDecreaseDisabled": false,
      "stockDecreaseDisabled": false,
      "isFulfilledByHB": false,
      "hasVariant": true,
      "productId": "product-uuid"
    }
  ],
  "totalCount": 500,
  "limit": 10,
  "offset": 0
}
```

### Listing Alan Açıklamaları

| Alan | Tip | Açıklama |
|------|-----|----------|
| hepsiburadaSku | string | HB tarafındaki unique ID |
| merchantSku | string | Satıcı tarafındaki unique ID |
| price | decimal | Listing fiyatı |
| availableStock | integer | Stok miktarı |
| dispatchTime | integer | Kargoya veriliş süresi (gün) |
| cargoCompany1-3 | string | Tanımlı kargo firmaları |
| shippingAddressLabel | string | Gönderici adresi |
| shippingProfileName | string | Teslimat profili |
| claimAddressLabel | string | İade adresi |
| pricings.finalPrice | decimal | Kampanya sonrası satış fiyatı |
| pricings.startDate | datetime | Kampanya başlangıcı |
| pricings.endDate | datetime | Kampanya bitişi |
| pricings.debtors | object[] | Kampanyayı karşılayan firma/oran |
| maximumPurchasableQuantity | integer | Bir seferde max alım |
| isSalable | boolean | Satışta mı |
| isSuspended | boolean | Askıda mı |
| isLocked | boolean | Kilitli mi |
| lockReasons | string[] | Kilitlenme nedenleri |
| isFrozen | boolean | Dondurulmuş mu |
| priceIncreaseDisabled | boolean | Fiyat artışı kapalı |
| priceDecreaseDisabled | boolean | Fiyat düşürme kapalı |
| stockDecreaseDisabled | boolean | Stok düşürme kapalı |

---

## Listing Fiyat Güncelleme

**POST** `/listings/merchantid/{merchantId}/price-uploads`

### Request Body

```json
[
  {
    "hepsiburadaSku": "HBCV00001XXXXX",
    "merchantSku": "SKU-001",
    "price": 149.90
  }
]
```

> `hepsiburadaSku` ve `merchantSku` tek başına veya birlikte gönderilebilir.

### Response

```json
{ "id": "3957bf91-a1ee-4657-92a0-fcb07bb69d83" }
```

---

## Listing Fiyat Güncelleme Sorgulama

**GET** `/listings/merchantid/{merchantId}/price-uploads/id/{id}`

### Response

```json
{
  "id": "3957bf91-...",
  "status": "Done",
  "createdAt": "2024-01-15T10:30:00Z",
  "total": 5,
  "errors": [
    {
      "elementNo": 2,
      "hepsiburadaSku": "HBCV00001XXXXX",
      "merchantSku": "SKU-002",
      "errors": ["InvalidPrice"]
    }
  ],
  "priceValidations": [
    {
      "elementNo": 1,
      "hepsiburadaSku": "HBCV00002XXXXX",
      "merchantSku": "SKU-003",
      "type": "MaxLock",
      "minPrice": 899.8,
      "maxPrice": 13767.0,
      "description": "Yüksek fiyat sebebiyle kilitlendi..."
    }
  ]
}
```

---

## Listing Stok Güncelleme

**POST** `/listings/merchantid/{merchantId}/stock-uploads`

### Request Body

```json
[
  {
    "hepsiburadaSku": "HBCV00001XXXXX",
    "merchantSku": "SKU-001",
    "availableStock": 50,
    "maximumPurchasableQuantity": 10
  }
]
```

### Response

```json
{ "id": "uuid" }
```

---

## Listing Stok Güncelleme Sorgulama

**GET** `/listings/merchantid/{merchantId}/stock-uploads/id/{id}`

Response formatı Fiyat Güncelleme Sorgulama ile aynı yapıda (id, status, errors).

---

## Listing Teslimat Güncelleme

**POST** `/listings/merchantid/{merchantId}/shipping-info-uploads`

### Request Body

```json
[
  {
    "hepsiburadaSku": "HBCV00001XXXXX",
    "merchantSku": "SKU-001",
    "dispatchTime": 2,
    "cargoCompany1": "Yurtiçi Kargo",
    "cargoCompany2": "Aras Kargo",
    "shippingProfileName": "Hızlı Teslimat",
    "shippingAddressLabel": "Depo-1",
    "claimAddressLabel": "İade Deposu"
  }
]
```

### Kargo Firma İsimleri

```
Yurtiçi Kargo
Aras Kargo
PTT Kargo
Borusan Lojistik
Horoz Lojistik
HepsiJet
MNG Kargo
Sürat Kargo
Ceva Lojistik
UPS
Mağaza Hesabı
```

> **Önemli:** HepsiJet tek başına seçilemez — yanında standart bir kargo firması da seçilmelidir. Aksi halde `MissingStandardCargoCompany` hatası alınır.

### Response

```json
{ "id": "uuid" }
```

---

## Listing Teslimat Güncelleme Sorgulama

**GET** `/listings/merchantid/{merchantId}/shipping-info-uploads/id/{id}`

Response formatı Fiyat Güncelleme Sorgulama ile aynı yapıda.

---

## Listing Ek Bilgiler Güncelleme Sorgulama

**GET** `/listings/merchantid/{merchantId}/additional-info-uploads/id/{id}`

Response formatı Fiyat Güncelleme Sorgulama ile aynı yapıda.

---

## Listing Tekil Fiyat/Stok Güncelleme (BETA)

**POST** `/listings/merchantid/{merchantId}/sku/{sku}/merchantsku/{merchantSku}`

Tek listing için fiyat, stok ve kargolama süresini aynı anda günceller.

> **Uyarı:** BETA sürümdür. Hata alınması durumunda ayrı fiyat/stok güncelleme servisleri kullanılmalıdır.

### Request Body

```json
{
  "newAvailableStock": 50,
  "newPrice": {
    "currency": "TRY",
    "amount": 149.90
  },
  "newDispatchTime": 2
}
```

---

## Listing Activate

**POST** `/listings/merchantid/{merchantId}/sku/{sku}/activate`

Listing'i satışa açar. Stok ve fiyat bilgisi önceden girilmiş olmalıdır (0 olan listing açılamaz).

---

## Listing Deactivate

**POST** `/listings/merchantid/{merchantId}/sku/{sku}/deactivate`

Listing'i satıştan kapatır. Alternatif olarak stok/fiyat 0 gönderilebilir.

---

## Listing Silme

**DELETE** `/listings/merchantid/{merchantId}/sku/{sku}/merchantsku/{merchantSku}`

Satışta olan listing silinemez.

---

## Buybox Sıralama Sorgulama

**GET** `/buybox-orders/merchantid/{merchantId}`

Listing'lerin satış sıralama bilgisini sorgular. Max 10 SKU virgülle ayrılarak gönderilebilir.

### Query Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| skuList | string | Hayır | HB SKU listesi (virgülle ayrılmış) |

### Response Alanları

| Alan | Tip | Açıklama |
|------|-----|----------|
| SKU | string | HB unique ID |
| Rank | integer | Satış sıralama değeri |
| Price | decimal | Listing fiyatı |
| DispatchTime | integer | Kargoya veriliş süresi |
| MerchantRating | decimal | Mağaza puanı |

---

## Komisyon Bilgisi Sorgulama

**GET** `/commissions/merchantid/{merchantId}`

Listing'lerin komisyon bilgisini sorgular.

**Rate Limit:** 240 istek / 1 dakika (merchant başına), max 50 SKU/istek

### Query Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| skuList | string | Hayır | HB SKU listesi (virgülle ayrılmış) |

---

## ShippingProfileName Listeleme

Merchant üzerinde tanımlı teslimat profillerini listeler.

### Response Alanları

| Alan | Tip | Açıklama |
|------|-----|----------|
| Name | string | Profil adı |
| CreatedAt | datetime | Oluşturulma tarihi |
| UpdatedAt | datetime | Son güncelleme tarihi |
| Id | string | Profil unique ID |

---

## Toplu Kilit Kaldırma

**POST** `/listings/merchantid/{merchantId}/bulk-unlock`

Fiyat threshold'u aşılması veya sipariş iptali nedeniyle kilitlenen listing'lerin kilitlerini toplu olarak kaldırır.

### Request Body

```json
{
  "hbSkuList": ["HBCV00001XXXXX", "HBCV00002YYYYY"]
}
```

### Kilitlenme Nedenleri

| Neden | Açıklama |
|-------|----------|
| PriceIsLessThanThreshold | Düşük fiyat — platform aralığının altı |
| PriceIsHigherThanThreshold | Yüksek fiyat — platform aralığının üstü |
| ByCheckoutPriceAnomaly | Sepetteki kampanyalı final fiyat aralık dışı |
| OrderCancellation | Sipariş iptali sonrası otomatik kilit + stok 0 |

---

## Güncelleme Hata Mesajları

| Hata | Açıklama |
|------|----------|
| ProductNotFound | HB SKU katalogda yok |
| MismatchingSkusSpecified | HB SKU ile Merchant SKU yanlış eşleştirilmiş |
| DuplicateHepsiburadaSkuSpecified | İstek içinde aynı HB SKU birden fazla |
| DuplicateMerchantSkuSpecified | İstek içinde aynı Merchant SKU birden fazla |
| MissingHeaders | XML tag veya Excel başlık eksik |
| InvalidPrice | Fiyat decimal değil veya nokta ile yazılmış |
| InvalidAvailableStock | Stok tam sayı değil |
| InvalidDispatchTime | Kargolama süresi tam sayı değil |
| DiscountedListingPriceIncrease | İndirim sürecinde fiyat artırılamaz |
| MerchantAlreadyListedAgainstProduct | Mükerrer listing — askıdaki silinmeli |
| ListingDeletedRecently | Silinmiş listing güncellenemez |
| ListingFrozen | Kilitli listing güncellenemez |
| MissingStandardCargoCompany | HepsiJet/Horoz/Borusan tek başına seçilemez |
| OutOfPriceRange | Fiyat threshold dışı |
| restrictedProductBrand | Marka kısıtı — kategori yöneticisiyle iletişim |
| InvalidMaximumPurchasableQuantity | Geçersiz max alım miktarı |

## XML Listing Güncelleme Örneği

Listing güncelleme endpoint'lerine XML de gönderilebilir. Header'da `Content-Type: application/xml` kullanılmalıdır.

```xml
<?xml version="1.0" encoding="utf-8"?>
<listings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
          xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <listing>
    <HepsiburadaSku>HBV00000TWKQJ</HepsiburadaSku>
    <MerchantSku>HBV00000TWKQJ_TEST</MerchantSku>
    <ProductName>Ürün Adı</ProductName>
    <Price>118,97</Price>
    <AvailableStock>9</AvailableStock>
    <DispatchTime>3</DispatchTime>
    <MaximumPurchasableQuantity>0</MaximumPurchasableQuantity>
    <ShippingProfileName>Profil-1</ShippingProfileName>
  </listing>
</listings>
```

XML Response:
```xml
<?xml version="1.0"?>
<Result xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Id>3957bf91-a1ee-4657-92a0-fcb07bb69d83</Id>
</Result>
```

> **Not:** JSON response almak için: `Accept: application/json`. XML response almak için: `Accept: application/xml`
