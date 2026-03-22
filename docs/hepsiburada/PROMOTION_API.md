# Hepsiburada Promotion API — Satıcı Promosyonu Endpoint Reference

Base URL: `https://diskonto-external-sit.hepsiburada.com`

**Rate Limit:** 500 istek / 1 saniye

**Yetkilendirme:** HTTP Basic Auth + `User-Agent` header zorunlu.

---

## Tarih Kuralları (Tüm Kampanya Oluşturma Endpointleri İçin)

| Durum | Davranış |
|-------|----------|
| Başlangıç tarihi = bugün | Kampanya gönderim anından 1 saat sonra başlar (gönderilen saat dikkate alınmaz) |
| Başlangıç tarihi > bugün | Kampanya o günün 00:00'ında başlar |
| Bitiş tarihi | Kampanya her zaman belirtilen günün 23:59'unda sona erer |

---

## Sepete TL İndirimi Oluşturma

**POST** `/self-campaign/{merchantId}/tl-discount`

Belirli bir sepet tutarı üzerinde sabit TL indirimi tanımlar.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Request Body

```json
{
  "name": "Yaz İndirimi",
  "startDate": "2026-04-01",
  "endDate": "2026-04-30",
  "conditionCategories": [123, 456],
  "conditionSkus": ["SKU001", "SKU002"],
  "budget": 5000,
  "discountAmount": 50,
  "conditionAmount": 200,
  "oneTimeUsage": true
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| name | string | Evet | Kampanya açıklaması (sadece satıcıya görünür) |
| startDate | string | Evet | Başlangıç tarihi |
| endDate | string | Evet | Bitiş tarihi |
| conditionCategories | integer[] | Hayır | Kampanyanın geçerli olduğu kategori ID'leri |
| conditionSkus | string[] | Hayır | Kampanyanın geçerli olduğu SKU'lar |
| budget | integer | Evet | Kampanya bütçesi (TL) |
| discountAmount | integer | Evet | İndirim tutarı (TL) |
| conditionAmount | integer | Evet | Minimum sepet tutarı (TL) |
| oneTimeUsage | boolean | Evet | true: müşteri başına tek kullanım |

### Response

```json
{
  "success": true,
  "data": {
    "campaignId": "abc-123"
  }
}
```

---

## Sepete Yüzde İndirimi Oluşturma

**POST** `/self-campaign/{merchantId}/percent-discount`

Belirli bir sepet tutarı üzerinde yüzdelik indirim tanımlar.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Request Body

```json
{
  "name": "Bahar Kampanyası",
  "startDate": "2026-04-01",
  "endDate": "2026-04-30",
  "conditionCategories": [123],
  "conditionSkus": [],
  "discountPercentage": 15,
  "conditionAmount": 300,
  "maxDiscountAmount": 100,
  "maxCartCount": 3,
  "oneTimeUsage": false
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| name | string | Evet | Kampanya açıklaması (sadece satıcıya görünür) |
| startDate | string | Evet | Başlangıç tarihi |
| endDate | string | Evet | Bitiş tarihi |
| conditionCategories | integer[] | Hayır | Kampanyanın geçerli olduğu kategori ID'leri |
| conditionSkus | string[] | Hayır | Kampanyanın geçerli olduğu SKU'lar |
| discountPercentage | integer | Evet | İndirim yüzdesi |
| conditionAmount | integer | Evet | Minimum sepet tutarı (TL) |
| maxDiscountAmount | integer | Evet | Maksimum indirim tutarı (TL) |
| maxCartCount | integer | Evet | Maksimum sepet sayısı |
| oneTimeUsage | boolean | Evet | true: müşteri başına tek kullanım |

### Response

```json
{
  "success": true,
  "data": {
    "campaignId": "abc-456"
  }
}
```

---

## Sepete X Al Y Öde İndirimi Oluşturma

**POST** `/self-campaign/{merchantId}/xy-discount`

"X adet al, Y adet öde" kampanyası tanımlar.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Request Body

```json
{
  "name": "3 Al 2 Öde",
  "startDate": "2026-04-01",
  "endDate": "2026-04-30",
  "conditionCategories": [],
  "conditionSkus": ["SKU001"],
  "conditionProductCount": 3,
  "mustPayProductCount": 2,
  "iterationCount": 1,
  "maxCartCount": 5,
  "oneTimeUsage": true
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| name | string | Evet | Kampanya açıklaması (sadece satıcıya görünür) |
| startDate | string | Evet | Başlangıç tarihi |
| endDate | string | Evet | Bitiş tarihi |
| conditionCategories | integer[] | Hayır | Kampanyanın geçerli olduğu kategori ID'leri |
| conditionSkus | string[] | Hayır | Kampanyanın geçerli olduğu SKU'lar |
| conditionProductCount | integer | Evet | Sepete eklenmesi gereken ürün adedi (X) |
| mustPayProductCount | integer | Evet | Ödenmesi gereken ürün adedi (Y) |
| iterationCount | integer | Evet | Kampanyanın kaç kez uygulanabileceği |
| maxCartCount | integer | Evet | Maksimum sepet sayısı |
| oneTimeUsage | boolean | Evet | true: müşteri başına tek kullanım |

### Response

```json
{
  "success": true,
  "data": {
    "campaignId": "abc-789"
  }
}
```

---

## Sepet İndirimleri Sorgulama

**GET** `/self-campaign/{merchantId}/discounts`

Satıcının tüm sepet indirim kampanyalarını listeler.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Query Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| page | integer | Hayır | Sayfa numarası |
| pagesize | integer | Hayır | Sayfa boyutu |

### Response

```json
{
  "success": true,
  "data": {
    "totalCount": 42,
    "items": [
      {
        "campaignId": "abc-123",
        "name": "Sistem tarafından oluşturulan ad",
        "description": "Satıcının verdiği açıklama",
        "startDate": "2026-04-01",
        "endDate": "2026-04-30",
        "status": 2,
        "limit": 5000
      }
    ]
  }
}
```

### Kampanya Durumları (status)

| Değer | Açıklama |
|-------|----------|
| 1 | Yakında Başlayacak |
| 2 | Devam Ediyor |
| 3 | Süresi Doldu |
| 4 | Tükendi |
| 5 | İptal Edildi |

> **Not:** Response'taki `name` alanı sistem tarafından otomatik oluşturulur ve müşteriye gösterilir. Kampanya oluştururken gönderilen `name` değeri ise `description` alanına karşılık gelir ve sadece satıcıya görünür.

---

## Sepet İndirimi Detay Sorgulama

**GET** `/self-campaign/{merchantId}/discount/{campaignId}`

Belirli bir kampanyanın detay bilgilerini döner.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |
| campaignId | string | Evet | Kampanya ID |

### Response

```json
{
  "success": true,
  "data": {
    "type": "tl-discount",
    "budget": 5000,
    "remainingBudget": 3500,
    "conditionAmount": 200,
    "discountAmount": 50,
    "maxDiscountAmount": null,
    "maxUsageAmount": 100,
    "remainingUsageCount": 70,
    "iterationCount": null,
    "startDate": "2026-04-01",
    "endDate": "2026-04-30",
    "status": 2,
    "conditionCategories": [123, 456],
    "conditionSkus": ["SKU001"]
  }
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| type | string | Kampanya tipi (tl-discount, percent-discount, xy-discount) |
| budget | integer | Toplam bütçe |
| remainingBudget | integer | Kalan bütçe |
| conditionAmount | integer | Minimum sepet tutarı |
| discountAmount | integer | İndirim tutarı (TL kampanyaları için) |
| maxDiscountAmount | integer | Maksimum indirim tutarı (yüzde kampanyaları için) |
| maxUsageAmount | integer | Maksimum kullanım adedi |
| remainingUsageCount | integer | Kalan kullanım adedi |
| iterationCount | integer | Tekrar sayısı (X Al Y Öde kampanyaları için) |
| startDate | string | Başlangıç tarihi |
| endDate | string | Bitiş tarihi |
| status | integer | Kampanya durumu (bkz. durum tablosu) |
| conditionCategories | integer[] | Geçerli kategori ID'leri |
| conditionSkus | string[] | Geçerli SKU'lar |

---

## Sepet İndirimi İptali

**POST** `/self-campaign/{merchantId}/cancel-discount`

Aktif veya yakında başlayacak bir kampanyayı iptal eder.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Request Body

```json
{
  "campaignId": "abc-123"
}
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| campaignId | string | Evet | İptal edilecek kampanya ID |

### Response

```json
{
  "success": true
}
```

---

## Sepet İndirimi Limitleri Sorgulama

**GET** `/self-campaign/{merchantId}/limits`

Kampanya oluştururken kullanılabilecek sepet eşik değerlerini ve her eşik için geçerli indirim tutarlarını döner. Tüm satıcılar için aynıdır.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Response

```json
{
  "success": true,
  "data": {
    "rowCount": 5,
    "limits": [
      {
        "lowerLimit": 100,
        "campaignAmounts": [10, 15, 20, 25]
      },
      {
        "lowerLimit": 200,
        "campaignAmounts": [20, 30, 40, 50]
      }
    ]
  }
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| rowCount | integer | Toplam eşik sayısı |
| limits[].lowerLimit | integer | Minimum sepet tutarı eşiği (TL) |
| limits[].campaignAmounts | integer[] | Bu eşik için kullanılabilecek indirim tutarları |

---

## Sepet İndirimi Bütçeleri Sorgulama

**GET** `/self-campaign/{merchantId}/budgets`

Kampanya oluştururken seçilebilecek bütçe seçeneklerini döner. Tüm satıcılar için aynıdır.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Response

```json
{
  "success": true,
  "data": [1000, 2500, 5000, 10000, 25000, 50000]
}
```

---

## Satıcı Ürün Kategorileri Sorgulama

**GET** `/categories/{merchantId}`

Satıcının ürün sattığı kategorileri listeler. Kampanya oluştururken `conditionCategories` için bu kategori ID'leri kullanılır.

### Path Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| merchantId | string | Evet | Satıcı ID |

### Response

```json
{
  "success": true,
  "data": [
    {
      "categoryId": 123,
      "categoryName": "Elektronik",
      "parentCategoryId": null,
      "categoryStatus": "ACTIVE",
      "parentCategories": [],
      "categorySortId": 1,
      "categoryLevel": 1,
      "isCampaign": true,
      "isHX": false,
      "isLeaf": false
    }
  ]
}
```

| Alan | Tip | Açıklama |
|------|-----|----------|
| categoryId | integer | Kategori ID |
| categoryName | string | Kategori adı |
| parentCategoryId | integer | Üst kategori ID (null ise kök kategori) |
| categoryStatus | string | Kategori durumu |
| parentCategories | array | Üst kategori listesi |
| categorySortId | integer | Sıralama ID |
| categoryLevel | integer | Kategori derinlik seviyesi |
| isCampaign | boolean | Kampanya yapılabilir mi |
| isHX | boolean | HepsiExpress kategorisi mi |
| isLeaf | boolean | Uç kategori mi |

---

## Hata Yanıtı

Tüm endpointler hata durumunda aşağıdaki formatta yanıt döner:

```json
{
  "success": false,
  "errors": [
    "Kampanya bütçesi geçersiz.",
    "Başlangıç tarihi bugünden önce olamaz."
  ]
}
```
