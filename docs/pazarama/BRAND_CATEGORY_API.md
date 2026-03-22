# Pazarama — Marka & Kategori API

## 1. Marka Listesi Alma

Ürün ekleme servisine (`CreateProduct`) gönderilecek `brandId` bilgisi bu servis ile alınır.

**Servis Tipi:** GET

```
GET https://{baseurl}/brand/getBrands?Page={page}&Size={size}
GET https://{baseurl}/brand/getBrands?Page=1&Size=100&name=Tat-bi Makarna
```

### Sorgu Parametreleri

| Parametre | Açıklama | Veri Tipi | Zorunlu |
|-----------|----------|-----------|---------|
| `page` | Hangi sayfadaki markaların getirileceği (default: 1) | int | Evet |
| `size` | Sayfa başına marka sayısı (default: 10) | int | Evet |
| `name` | Marka adına göre filtreleme | string | Hayır |

### Örnek Response

```json
{
  "data": [
    {
      "id": "1cea1866-b916-4f4f-bdbe-08db68d6a026",
      "name": "Tat-bi Makarna",
      "logoUrl": "",
      "website": "",
      "status": false,
      "seoName": null
    }
  ],
  "success": true,
  "messageCode": null,
  "message": null,
  "userMessage": null,
  "fromCache": false
}
```

---

## 2. Kategori Ağacı Listesi

Ürün ekleme servisine gönderilecek `categoryId` için **en alt seviyedeki** (`leaf: true`) kategoriler kullanılmalıdır. Sadece leaf kategorilerin altına ürün açılabilir.

**Servis Tipi:** GET

```
GET https://{baseurl}/category/getCategoryTree
```

### Response Parametreleri

| Parametre | Açıklama | Veri Tipi |
|-----------|----------|-----------|
| `id` | Kategori ID değeri | guid |
| `parentId` | Ana kategori ID'si | guid |
| `parentCategories` | Alt kırılımlarıyla birlikte kategori yolu | string[] |
| `name` | Kategori adı | string |
| `displayName` | Önyüzde görünecek isim | string |
| `displayOrder` | Sıralama değeri | int |
| `description` | Kategori açıklaması | string |
| `leaf` | `true` ise en alt kategori (ürün eklenebilir) | boolean |

### Örnek Response

```json
{
  "data": [
    {
      "id": "3053ce72-9208-40bb-8775-001fb4ca654b",
      "parentId": "00b8be55-5ec1-46fd-97d9-1f70cfed5a12",
      "code": null,
      "parentCategories": [
        "Otomobil, Motosiklet ve Aksesuarları",
        "Oto Aksesuarları",
        "Oto Yedek Parça",
        "Elektrik Aksam",
        "Isı Sensörleri"
      ],
      "name": "Isı Sensörleri",
      "displayName": "Isı Sensörleri",
      "displayOrder": 1,
      "description": null,
      "leaf": true
    }
  ],
  "success": true,
  "messageCode": null,
  "message": null,
  "userMessage": null,
  "fromCache": true
}
```

---

## 3. Kategori Özellik Bilgileri

Ürün ekleme servisine gönderilecek `attributes` bilgileri bu servis ile alınır. Kategoriye ait özellikleri (renk, beden vb.) ve bu özelliklerin olası değerlerini listeler.

**Servis Tipi:** GET

```
GET https://{baseurl}/category/getCategoryWithAttributes?Id={categoryId}
```

### Sorgu Parametreleri

| Parametre | Açıklama | Veri Tipi | Zorunlu |
|-----------|----------|-----------|---------|
| `Id` | Kategori ID değeri | guid | Evet |

### Response Parametreleri

| Parametre | Açıklama | Veri Tipi |
|-----------|----------|-----------|
| `id` | Kategori ID | guid |
| `name` | Kategori adı | string |
| `displayName` | Görünen isim | string |
| `parentCategories` | Kategori yolu | string[] |
| `attributes[].id` | Özellik ID | guid |
| `attributes[].name` | Özellik adı | string |
| `attributes[].displayName` | Özellik görünen adı | string |
| `attributes[].isVariantable` | Varyant özelliği mi | boolean |
| `attributes[].isRequired` | Zorunlu mu | boolean |
| `attributes[].attributeValues[].id` | Özellik değeri ID | guid |
| `attributes[].attributeValues[].value` | Özellik değeri | string |

### Örnek Response

```json
{
  "data": {
    "id": "a0fdcfde-8b5f-4c4c-82d1-a900b1732ddb",
    "name": "Adımsayar",
    "displayName": "Adımsayar",
    "description": null,
    "attributes": [
      {
        "id": "08b2020b-e519-405f-85e2-1fd712104097",
        "name": "Renk",
        "displayName": "Renk",
        "isVariantable": false,
        "isRequired": true,
        "description": null,
        "attributeValues": [
          { "id": "2ddb5aeb-3c25-4fb1-975d-031b436f3319", "value": "Siyah" },
          { "id": "6e4deed0-2555-4ecd-ae1b-051aa30b774a", "value": "Kahverengi" },
          { "id": "544e1e86-678c-4e19-a7a4-230f180b2ed2", "value": "Mor" },
          { "id": "75d0b61d-e6bd-4250-946d-40e9e262e497", "value": "Gri" },
          { "id": "96bc3661-77b6-4a6d-8745-574c9adb4a03", "value": "Yeşil" },
          { "id": "6faa548f-7f02-42c7-80ba-6b73b84fbbef", "value": "Sarı" },
          { "id": "a804b5e8-93b5-48e1-8b63-8096a8e83ad8", "value": "Lacivert" },
          { "id": "c7e562e1-ae2e-4a59-b656-81723601bdbf", "value": "Mavi" },
          { "id": "aef8fe0b-4f80-4dc3-91f3-b902e6fc4c4c", "value": "Beyaz" },
          { "id": "ec98860d-668c-4c4f-824c-b918e47f1abf", "value": "Pembe" },
          { "id": "52a2e275-c6c0-4603-96a4-c3511432e210", "value": "Turuncu" },
          { "id": "57ecdb59-f9ff-4775-814f-c7a98cfc066e", "value": "Kırmızı" }
        ]
      },
      {
        "id": "4e2192a3-47a5-41ae-81ad-808104ade74b",
        "name": "Uyku Takibi",
        "displayName": "Uyku Takibi",
        "isVariantable": false,
        "isRequired": false,
        "description": null,
        "attributeValues": [
          { "id": "03ab4fb0-eee1-4ef7-bfc2-7e8a6cf1e234", "value": "Yok" },
          { "id": "0a4c2b61-79fa-4460-9f40-e04a17773c37", "value": "Var" }
        ]
      }
    ]
  },
  "success": true,
  "messageCode": null,
  "message": null,
  "userMessage": null,
  "fromCache": false
}
```

---

## 4. Şehir Bilgisi Sorgulama

Ürüne teslimat tipi bilgisi girilmesi durumunda şehir seçimi için ID değerlerinin sorgulandığı servis.

**Servis Tipi:** GET

```
GET https://{baseurl}/parameter/cities
```

### Örnek Response

```json
{
  "data": [
    { "id": "86f65eba-a61f-428d-a689-0d5f3adee575", "code": "34", "name": "İstanbul" },
    { "id": "3e3690b0-3161-42e4-9ad8-2c8e0e2a8e47", "code": "6", "name": "Ankara" },
    { "id": "3e9f336c-7f65-46f7-b55a-690a61ba4468", "code": "35", "name": "İzmir" },
    { "id": "9e12c423-44dc-4d6a-8462-d827db8d605c", "code": "1", "name": "Adana" },
    { "id": "78ad0630-1e6e-4b5d-aa73-6935a4a1a42b", "code": "2", "name": "Adıyaman" },
    { "id": "078b0afc-1d7c-46e9-bf7c-cdc4d6c2c4a7", "code": "3", "name": "Afyonkarahisar" }
  ],
  "success": true,
  "messageCode": "0",
  "message": null,
  "userMessage": null,
  "fromCache": true
}
```
