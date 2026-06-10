# N11 REST API Dokümantasyonu

> Kaynak: https://Mağazadestek.n11.com (2026-03-30 tarihinde alınmıştır)

## Auth

Tüm REST endpoint'lerinde:
- Authorization: no auth
- Headers: `appkey` + `appsecret`

## REST Endpoint'ler

### 1. Ürün Yükleme (CreateProduct)
- **POST** `https://api.n11.com/ms/product/tasks/product-create`
- Max 1000 SKU per request
- Task-based: Response'ta `id` (taskId) döner
- Varyantlı yükleme: aynı `productMainId` ile

Request:
```json
{
    "payload": {
        "integrator": "EntegratorAdi",
        "skus": [
            {
                "title": "Ürün Adı",
                "description": "Açıklama",
                "categoryId": 1000476,
                "currencyType": "TL",
                "productMainId": "grup1",
                "preparingDay": 3,
                "shipmentTemplate": "1",
                "maxPurchaseQuantity": 5,
                "stockCode": "SKU001",
                "catalogId": null,
                "barcode": 8806094924862,
                "quantity": 10,
                "images": [
                    {"url": "https://...", "order": 0}
                ],
                "attributes": [
                    {"id": 1, "valueId": null, "customValue": "Marka"},
                    {"id": 429, "valueId": 444058, "customValue": "null"}
                ],
                "salePrice": 2000,
                "listPrice": 2200,
                "vatRate": 10
            }
        ]
    }
}
```

Response:
```json
{
    "id": 1092,
    "type": "PRODUCT_CREATE",
    "status": "IN_QUEUE",
    "reasons": ["1 sku işlenmeye alındı."]
}
```

Parametreler:
| Parametre | Zorunlu | Açıklama | Tip |
|-----------|---------|----------|-----|
| integrator | Evet | Entegratör ismi | string |
| title | Evet | Ürün adı | string |
| description | Evet | Ürün açıklaması | HTML-string |
| categoryId | Evet | En alt kırılım kategori ID | long |
| currencyType | Evet | TL, USD, EUR | string |
| productMainId | Evet | Varyant gruplama kodu | string |
| preparingDay | Evet | Kargoya gönderim süresi (gün) | integer |
| shipmentTemplate | Evet | Kargo şablon adı | string |
| maxPurchaseQuantity | Hayır | Max satın alım adedi | integer |
| stockCode | Evet | Satıcı stok kodu (max 255) | string |
| catalogId | Hayır | N11 katalog ID | long |
| barcode | Hayır | Ulusal barkod | string |
| quantity | Evet | Stok miktarı (max 999.999) | integer |
| images | Evet | URL + order | list |
| attributes | Evet | Kategori özellikleri (mandatory=true zorunlu) | list |
| salePrice | Evet | Satış fiyatı | amount |
| listPrice | Evet | Liste fiyatı (PSF) | amount |
| vatRate | Evet | KDV oranı (0,1,10,20) | integer |

### 2. Fiyat/Stok Güncelleme (UpdateProductPriceAndStock)
- **POST** `https://api.n11.com/ms/product/tasks/price-stock-update`
- Max 1000 SKU per request
- Task-based

Request:
```json
{
    "payload": {
        "integrator": "EntegratorAdi",
        "skus": [
            {
                "stockCode": "SKU001",
                "listPrice": 2000,
                "salePrice": 1600,
                "quantity": 2,
                "currencyType": "TL"
            }
        ]
    }
}
```

Response:
```json
{
    "id": 1092,
    "type": "SKU_UPDATE",
    "status": "IN_QUEUE",
    "reasons": ["1 sku işlenmeye alındı."]
}
```

Notlar:
- listPrice ve salePrice birlikte gönderilmeli
- Küsürat nokta ile ayrılmalı, noktadan sonra 2 hane
- Sadece fiyat veya sadece stok gönderilebilir (diğer alan gönderilmezse güncellenmez)
- listPrice >= salePrice olmalı

### 3. Ürün Güncelleme (UpdateProduct)
- **POST** `https://api.n11.com/ms/product/tasks/product-update`
- Max 1000 SKU per request
- Task-based

Request:
```json
{
    "payload": {
        "integrator": "EntegratorAdi",
        "skus": [
            {
                "stockCode": "SKU001",
                "status": "Active",
                "preparingDay": 3,
                "shipmentTemplate": "STANDART",
                "deleteProductMainId": false,
                "productMainId": "grup1",
                "deleteMaxPurchaseQuantity": false,
                "maxPurchaseQuantity": 3,
                "description": "Yeni açıklama",
                "vatRate": 10,
                "attributes": [
                    {"id": 1000000, "valueId": null, "customValue": "Firma A.Ş."}
                ]
            }
        ]
    }
}
```

### 4. Task Detail Sorgulama (TaskDetails)
- **POST** `https://api.n11.com/ms/product/task-details/page-query`

Request:
```json
{
    "taskId": 362,
    "pageable": {"page": 0, "size": 1000}
}
```

Response:
```json
{
    "taskId": 1095,
    "status": "PROCESSED",
    "skus": {
        "content": [
            {
                "itemCode": "SKU001",
                "status": "SUCCESS",
                "sku": {
                    "salePrice": 1100.00,
                    "listPrice": 1200.00,
                    "stock": 9
                },
                "reasons": ["Başarıyla tamamlandı."]
            }
        ]
    }
}
```

Task status: IN_QUEUE (işleniyor), PROCESSED (tamamlandı), REJECT (işlenmedi)

### 5. Sipariş Listeleme (GetShipmentPackages)
- **GET** `https://api.n11.com/rest/delivery/v1/shipmentPackages`
- Rate limit: 1000 req/dakika
- 2024 Kasım öncesi sipariş verisi dönmüyor

Query parametreleri:
| Parametre | Açıklama | Tip |
|-----------|----------|-----|
| startDate | Timestamp (ms, GMT+3) | long |
| endDate | Timestamp (ms, GMT+3) | long |
| page | Sayfa (0'dan başlar) | int |
| size | Max 100 | int |
| orderNumber | Sipariş no ile sorgulama | string |
| packageIds | Paket no ile sorgulama | string |
| status | Created, Picking, Shipped, Cancelled, Delivered, Unpacked, UnSupplied | string |
| orderByDirection | ASC veya DESC | string |
| orderByField | true: lastModifiedDate'e göre sorgula | string |

Sipariş statü mapping (SOAP → REST):
- 2: Ödendi → Created
- 5: Kabul Edilmiş → Picking
- 6: Kargoda → Shipped
- 7: Teslim Edilmiş → Delivered
- 4: İptal Edilmiş → Cancelled
- 8: Reddedilmiş → UnSupplied

### 6. Sipariş Onaylama (UpdateOrder)
- **PUT** `https://api.n11.com/rest/order/v1/update`

Request:
```json
{
    "lines": [{"lineId": 426659152}],
    "status": "Picking"
}
```

### 7. Kategori Ağacı Listeleme (GetCategories)
- **GET** `https://api.n11.com/cdn/categories`
- Tüm kategori ağacı tek istekle
- `subCategories: null` → en alt kırılım (leaf)

### 8. Kategori Özellikleri (GetCategoryAttributesList)
- **GET** `https://api.n11.com/cdn/category/{categoryId}/attribute`

Response:
```json
{
    "id": 1002571,
    "name": "Makyaj Çantası",
    "categoryAttributes": [
        {
            "attributeId": 1,
            "attributeName": "Marka",
            "isMandatory": true,
            "isVariant": false,
            "isSlicer": false,
            "isCustomValue": true,
            "attributeValues": [
                {"id": 8372688, "value": "Abay"}
            ]
        }
    ]
}
```

### 9. Satıcı Ürün Sorgulama (GetProductQuery)
- **GET** `https://api.n11.com/ms/product-query`

Query parametreleri: id, productMainId, stockCode, saleStatus (On_Sale, Out_Of_Stock), productStatus (Active, InCatalogApproval, Suspended, CatalogRejected, Prohibited, Unlisted, InApproval), brandName, categoryIds, page, size (max 250)

### 10. Paket Bölme (SplitPackages)
- **POST** `https://api.n11.com/rest/delivery/v1/splitCombinePackage`
- Sadece Picking statüsünde bölünebilir

### 11. Miktar Bazlı Paket Bölme + İptal (SplitPackagesByQuantity)
- **POST** `https://api.n11.com/rest/delivery/v1/splitPackageByQuantity`
- cancelReasonId: 61 (Stok Tükendi), 62 (Kusurlu), 63 (Hatalı Fiyat), 64 (Mücbir Sebep), 65 (Diğer)

### 12. İşçilik Bedeli Ekleme
- **PUT** `https://api.n11.com/rest/order/v1/labor-costs`
- Sadece belirli kategoriler (altın, gümüş, pırlanta takılar)

---

## SOAP Endpoint'ler (REST karşılığı yok)

### Fatura Linki (SaveLinkSellerInvoice)
- WSDL: `https://api.n11.com/ws/SellerInvoiceService.wsdl`
- url + orderNumber zorunlu
- url: sadece pdf, png, jpeg, https, max 2048 karakter

### Ürün Soruları (GetProductQuestionList)
- WSDL: `https://api.n11.com/ws/ProductService.wsdl`
- productId, status (OPEN/CLOSED), tarih filtresi

### İptal Talebi Onay/Red (ClaimCancelService)
- WSDL: `https://api.n11.com/ws/ClaimCancelService.wsdl`
- ClaimCancelApprove(claimCancelId)
- ClaimCancelDeny(claimCancelId, denyReasonId, denyReasonNote)
- ClaimCancelPartial(orderItemId, cancelQuantity, cancelReasonTypeId)

### İade Talepleri (ReturnService)
- WSDL: `https://api.n11.com/ws/ReturnService.wsdl`
- ClaimReturnList(status, executer, searchInfoType, period)
- ClaimReturnApprove(claimReturnId)
- ClaimReturnDeny(claimReturnId, denyReasonId, denyReasonNote, returnShipmentType, trackingNumber)
- ClaimReturnPending(claimReturnId, pendingReasonId, pendingDayCount, pendingReasonNote)

### Ürün Silme (DeleteProductById)
- WSDL: `https://api.n11.com/ws/ProductService.wsdl`
- productId ile silme

### Katalog Arama (SearchCatalog)
- WSDL: `https://api.n11.com/ws/CatalogService.wsdl`
- productTitles, categoryId, uscs (barcode), brandName, catalogIds

### n11faturam E-Fatura
- Link formatı: `https://ebelge.n11faturam.com/ViewDocument.aspx?ID={VKN}&UUID={UUID}&doctype={arcinv|outinvoice}`

### Sipariş SOAP (hala mevcut, REST karşılığı var)
- WSDL: `https://api.n11.com/ws/OrderService.wsdl`
- DetailedOrderList → REST GetShipmentPackages
- OrderItemAccept → REST UpdateOrder
- MakeOrderItemShipment (SOAP only — REST karşılığı yok)
- OrderItemDelivery (SOAP only)

---

## REST API Hata Mesajları

Yaygın hatalar:
- "Girdiğiniz X seller stock code tarafınızdan kullanılmaktadır" → stok kodu çakışması
- "Marka bilgisi girilmelidir" → marka attribute eksik
- "X idli özellik değeri kategorisinde bulunmamaktadır" → valueId güncel değil
- "vatRate alanı boş olamaz" → KDV zorunlu
- "listPrice salePrice dan küçük olamaz" → fiyat kuralı
- "shipmentTemplate alanı geçersizdir" → kargo şablon hatası
- "Ürün ekleme isteğinizde 15 karakter altında ürün adları tespit edilmiştir" → min 15 karakter

Full hata listesi için bkz: Mağazadestek.n11.com RestAPI hata mesajları sayfası.
