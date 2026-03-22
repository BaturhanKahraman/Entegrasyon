# Pazarama — Teslimat Tipi API

## 1. Teslimat Tipi Görüntüleme

Firmaların ürün özelinde veya genel olarak teslimat tiplerini görüntülediği servis.

### Desteklenen Teslimat Tipleri

| Tip | Açıklama |
|-----|----------|
| CargoDelivery | Kargo |
| FastDelivery | Kurye |
| StoreDelivery | Mağazadan Teslimat |

**Servis Tipi:** GET

```
GET https://{baseurl}/sellerRegister/getSellerDelivery
```

### Response Parametreleri

| Parametre | Açıklama | Veri Tipi |
|-----------|----------|-----------|
| `deliveryId` | Teslimat tipi ID | guid |
| `cargoCompanyId` | Kargo firması ID | guid |
| `price` | Teslimat ücreti | decimal |
| `campaignPrice` | Kampanyalı teslimat fiyatı | decimal |
| `campaignAmount` | Kampanyalı fiyat için limit | decimal |
| `campaignText` | Kampanya metni | string |
| `storeName` | Mağazadan teslimat için mağaza adı | string |
| `city` | Teslimat şehri (boş ise tüm şehirler) | guid |
| `addressDetail` | Mağaza adres bilgisi | string |
| `deliveryDuration` | Teslimat süresi | int |
| `canDeliverWeekend` | Hafta sonu teslimat | boolean |
| `divideCargo` | Parçalı kargo | boolean |
| `autoShipmentCode` | Otomatik gönderi kodu | boolean |

### Örnek Response

```json
{
  "data": {
    "cargoCompany": {
      "deliveryId": "f46132bc-1f24-47dc-b396-08d9720587ab",
      "price": 9.9,
      "campaignPrice": 0,
      "campaignAmount": 1,
      "campaignText": "1 TL ve üzeri alışverişinize ücretsiz kargo.",
      "deliveryDuration": 4,
      "canDeliverWeekend": false,
      "divideCargo": true,
      "autoShipmentCode": false,
      "cargoCompanies": [
        {
          "cargoCompanyId": "8eb9aeb7-fd11-425b-9930-08d8e48cc18f",
          "cargoCardType": 2,
          "cargoCompanyName": "MNG"
        },
        {
          "cargoCompanyId": "7b5567ff-abe7-487e-5c79-08d8e480366a",
          "cargoCardType": 1,
          "cargoCompanyName": "Yurtiçi Kargo"
        }
      ]
    },
    "fastDelivery": null,
    "storeDelivery": null,
    "digital": null,
    "donation": null,
    "sellerDeliveryPreferences": {
      "canFastDelivery": false,
      "canStoreDelivery": false,
      "otherCargoOption": true,
      "canEnteredOtherCargo": true,
      "saturdayIsWorkDay": false
    }
  },
  "success": true
}
```
