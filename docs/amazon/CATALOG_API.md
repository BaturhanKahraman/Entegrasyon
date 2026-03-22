# Amazon Catalog Items API (v2022-04-01)

## Endpoint'ler

### searchCatalogItems
**GET** `/catalog/2022-04-01/items`

**Parametreler:**
- `keywords` — Arama kelimesi (identifiers ile birlikte kullanılamaz)
- `identifiers` — Ürün tanımlayıcısı (EAN, UPC, ISBN) — max 20
- `identifiersType` — `EAN`, `UPC`, `ISBN`, `SKU` (identifiers kullanılıyorsa zorunlu)
- `marketplaceIds` — Zorunlu, tek marketplace per request
- `includedData` — summaries, attributes, identifiers, images, productTypes, salesRanks
- `pageSize` — Max 20, default 10
- `pageToken` — Pagination

**Rate:** 5 req/sec

### getCatalogItem
**GET** `/catalog/2022-04-01/items/{asin}`

**Parametreler:**
- `marketplaceIds` — Zorunlu
- `includedData` — summaries, attributes, identifiers, images, productTypes

**Rate:** 5 req/sec

## Barkod ile Arama (EAN/UPC)
```
GET /catalog/2022-04-01/items
  ?identifiers=1234567890123
  &identifiersType=EAN
  &marketplaceIds=A33AVAJ2PDY3EV
  &includedData=summaries,identifiers,images,productTypes
```
