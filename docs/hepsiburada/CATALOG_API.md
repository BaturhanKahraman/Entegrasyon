# Hepsiburada Catalog API — Endpoint Reference

Base URL: `https://mpop-sit.hepsiburada.com/product`

---

## Kategori Bilgilerini Alma (getAllCategoriesByParameters)

**GET** `/api/categories/get-all-categories`

Hepsiburada kategori ağacını listeler. Ürün yaratabilmek için leaf=true, status=ACTIVE, available=true olan kategoriler kullanılmalıdır.

**Rate Limit:** 200 istek / 1 dakika (IP başına)

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| leaf | boolean | Hayır | true | true: uç kategori (ürün açılabilir) |
| status | string | Hayır | ACTIVE | ACTIVE, INACTIVE, ARCHIVED |
| available | boolean | Hayır | true | true: ürün açılabilir |
| type | string | Hayır | — | HX, HB, HC |
| version | integer | Hayır | 1 | Versiyon 1 gönderilmeli |
| page | integer | Hayır | 0 | Sayfa numarası (0'dan başlar) |
| size | integer | Hayır | 1000 | Sayfa boyutu (max 2000) |

### Response — Kategori Veri Modeli

```json
{
  "success": true,
  "code": 0,
  "version": 1,
  "message": "",
  "totalElements": 5000,
  "totalPages": 5,
  "number": 0,
  "numberOfElements": 1000,
  "first": true,
  "last": false,
  "data": [
    {
      "categoryId": 18021982,
      "name": "Spor Ayakkabı",
      "displayName": "Spor Ayakkabı",
      "parentCategoryId": 123456,
      "paths": ["Giyim", "Ayakkabı", "Spor Ayakkabı"],
      "leaf": true,
      "status": "ACTIVE",
      "type": "HB",
      "sortId": "1",
      "merge": false
    }
  ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| categoryId | integer | Kategori ID |
| name | string | Kategori adı |
| displayName | string | Görüntülenen ad |
| parentCategoryId | integer | Üst kategori ID |
| paths | string[] | Breadcrumb yol bilgisi |
| leaf | boolean | true: ürün açılabilir, false: açılamaz |
| status | string | AKTİF / AKTİF DEĞİL |
| available | boolean | true: ürün açılabilir |
| type | string | HX, HB, HC |

---

## Kategori Özelliklerini Alma (getAllAttributesByCategory)

**GET** `/api/categories/{categoryId}/attributes`

Sadece leaf=true ve available=true olan kategorilerde özellik bilgisi mevcuttur.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| categoryId | integer | Evet | Kategori ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| version | integer | Hayır | 2 | API versiyonu |
| modifiedAtSince | datetime | Hayır | — | Bu tarihten sonra güncellenen kayıtlar (version=2 ise zorunlu) |

### Response

```json
{
  "success": true,
  "code": 0,
  "version": 2,
  "message": "",
  "data": {
    "baseAttributes": [
      {
        "name": "Marka",
        "id": "marka",
        "mandatory": true,
        "type": "enum",
        "multiValue": false
      }
    ],
    "attributes": [
      {
        "name": "Malzeme",
        "id": "malzeme",
        "mandatory": false,
        "type": "string",
        "multiValue": false
      }
    ],
    "variantAttributes": [
      {
        "name": "Renk",
        "id": "renk_variant_property",
        "mandatory": true,
        "type": "enum",
        "multiValue": false
      }
    ]
  }
}
```

### Özellik Veri Modeli (AttributeDTO)

| Alan | Tip | Açıklama |
|------|-----|----------|
| name | string | Özellik adı |
| id | string | Özellik ID |
| mandatory | boolean | true: zorunlu, false: opsiyonel |
| type | string | `string`: freetext giriş, `enum`: HB listesinden seçim |
| multiValue | boolean | true: birden fazla değer alabilir |

### Özellik Grupları

| Grup | Açıklama |
|------|----------|
| baseAttributes | Temel özellikler (Marka, Garanti Süresi vb.) |
| attributes | Ürün özellikleri (Malzeme, Desen vb.) |
| variantAttributes | Varyant özellikleri (Renk, Beden vb.) — varyant oluşturmak için en az 1 tanesi dolu olmalı |

### Hata Kodları

| Kod | Mesaj |
|-----|-------|
| 1001 | Kategori leaf değildir |
| 1002 | Kategori aktif değildir |
| 1003 | Kategori leaf ve aktif bir kategori değildir |
| 1004 | CategoryId ile herhangi bir kategori bulunamadı |
| 1005 | İlgili categoryId için kategori ilişkisi bulunamadı |
| 1006 | Kategori mevcut değildir |

---

## Özellik Değerini Alma (getAllAttributeValuesByCategoryIdAndAttributeId)

**GET** `/api/categories/{categoryId}/attribute/{attributeId}/values`

type=enum olan özellikler için kullanılabilecek değerleri listeler. Pagination zorunludur.

**Rate Limit:** 50 istek / 1 dakika (IP başına)

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| categoryId | integer | Evet | Kategori ID |
| attributeId | string | Evet | Özellik ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Default | Açıklama |
|-----------|-----|---------|---------|----------|
| version | integer | Hayır | 5 | API versiyonu |
| modifiedAtSince | datetime | Hayır | — | Bu tarihten sonra güncellenen kayıtlar (version=5 ise geçerli) |
| page | integer | Hayır | 0 | Sayfa numarası |
| size | integer | Hayır | 1000 | Sayfa boyutu (max 1000) |

### Response

```json
{
  "success": true,
  "code": 0,
  "version": 5,
  "message": "",
  "data": [
    {
      "id": "siyah",
      "value": "Siyah"
    },
    {
      "id": "beyaz",
      "value": "Beyaz"
    }
  ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| id | string | Değer ID |
| value | string | Değer adı (kullanılacak değer) |

> **Not:** Total count bilgisi response header içerisinden alınabilir. Enum değer listenizde olmayan bir değer gönderebilirsiniz (freetext).

### Hata Kodları

| Kod | Mesaj |
|-----|-------|
| 1001 | Kategori leaf değildir |
| 1002 | Kategori aktif değildir |
| 1003 | Kategori leaf ve aktif bir kategori değildir |
| 1004 | CategoryId ile herhangi bir kategori bulunamadı |
| 1005 | İlgili categoryId için kategori ilişkisi bulunamadı |
| 2001 | Özellik enum değer değildir |
| 2002 | Bu attributeId özelliğine sahip herhangi bir özellik bulunamadı |
