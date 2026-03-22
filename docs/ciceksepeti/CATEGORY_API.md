# Çiçeksepeti Category & Attribute API

## 1. Kategori Listeleme

**Endpoint:** `GET /api/v1/Categories`

Tüm kategori ağacını recursive olarak döner. Ürün işlemlerinde **leaf (yaprak)** kategori ID'si kullanılmalıdır.

**Headers:**
```
x-api-key: {API_KEY}
```

**Response:**
```json
{
  "categories": [
    {
      "id": 123,
      "name": "Çiçek",
      "parentCategoryId": null,
      "subCategories": [
        {
          "id": 456,
          "name": "Buket Çiçekler",
          "parentCategoryId": 123,
          "subCategories": []
        }
      ]
    }
  ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| `id` | int | Kategori ID |
| `name` | string | Kategori adı |
| `parentCategoryId` | int? | Üst kategori ID (root için null) |
| `subCategories` | array | Alt kategoriler (recursive) |

> **NOT:** Ürün ekleme/güncelleme işlemlerinde her zaman en alt seviye (leaf) `subCategoryId` kullanılmalıdır.

---

## 2. Kategori Özellikleri

**Endpoint:** `GET /api/v1/Categories/{categoryId}/attributes`

Belirtilen leaf kategorinin özellik (attribute) listesini döner.

**Path Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| `categoryId` | int | Evet | Leaf kategori ID |

**Response:**
```json
{
  "categoryId": 456,
  "categoryName": "Buket Çiçekler",
  "categoryAttributes": [
    {
      "attributeId": 101,
      "attributeName": "Renk",
      "required": true,
      "varianter": true,
      "type": "Variant Ozellik",
      "attributeValues": [
        { "id": 1001, "name": "Kırmızı" },
        { "id": 1002, "name": "Beyaz" }
      ]
    },
    {
      "attributeId": 102,
      "attributeName": "Çiçek Sayısı",
      "required": false,
      "varianter": false,
      "type": "Urun Ozellik",
      "attributeValues": []
    }
  ]
}
```

### Özellik Tipleri (type)

| Type | Açıklama | varianter | Davranış |
|------|----------|-----------|----------|
| `Variant Ozellik` | Varyant oluşturucu özellik | `true` | Aynı mainProductCode altında farklı stockCode'lar oluşturur |
| `Urun Ozellik` | Ürün özelliği | `false` | Ürün bilgisi olarak saklanır, varyant oluşturmaz |
| `Kisisellestirilebilir Ozellik` | Kişiselleştirme | `false` | Müşterinin özelleştirebildiği özellik (ör. isim yazdırma) |

### Özellik Alanları

| Alan | Tip | Açıklama |
|------|-----|----------|
| `attributeId` | int | Özellik ID |
| `attributeName` | string | Özellik adı |
| `required` | bool | Zorunlu mu |
| `varianter` | bool | Varyant oluşturucu mu |
| `type` | string | Özellik tipi (yukarıdaki tablo) |
| `attributeValues` | array | Önceden tanımlı değerler (id, name) |

> **NOT:** `attributeValues` boş ise serbest metin girişi yapılabilir (TextLength ile). Dolu ise listeden seçim yapılmalıdır.
