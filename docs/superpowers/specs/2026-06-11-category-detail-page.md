# Spec: Kategori Detay Sayfası — `/categories/{id}`

**Tarih:** 2026-06-11
**Yazar:** PA (Baturhan onaylı bağlam)
**Durum:** TL onayı bekliyor
**İlgili task:** tasks.json — "Kategori Detay Sayfası (`/categories/{id}`)"

---

## 1. Problem

Mevcut `/categories` sayfası split layout: sol ağaç, sağda HTMX partial kutusu (`_CategoryDetail.cshtml`). Bu kutuda yalnızca statik meta bilgi var (üst kategori, alt kategori sayısı, ürün sayısı, özellikler). Satış performansı, pazaryeri kırılımı, iade oranı gibi herhangi bir ticari metrik YOK.

Esnaf şu an "Bu kategoride ne kadar sattım? Hangi pazaryerinde daha iyi gidiyor? İade oranı ne?" sorularını cevaplayamıyor — bu bilgiye ulaşmak için ayrı rapor sayfalarına gidip kategoriyi elle filtrelemesi gerekiyor.

**Ayrıca:** Sağ kutudaki "Düzenle / Sil" eylemlerinin kalıcı, bookmark'lanabilir bir URL'si yok. "Detaya Git" butonunun yönlendireceği tam sayfa eksik.

---

## 2. Çözüm

`GET /categories/{id}` — tam sayfa, HTMX partial değil — açılır. Bu sayfa mevcut sağ kutuyu absorbe eder (meta + özellikler) ve üzerine satış metriklerini ekler.

---

## 3. Metrik / Bölüm Listesi (veri kaynağıyla)

### 3.1 Özet KPI'lar (son 30 gün varsayılan)

| Metrik | Kaynak | Hesaplama |
|---|---|---|
| Toplam satılan adet | `OrderItem.Quantity` - `OrderItem.ReturnedQuantity` | `SUM(Quantity - ReturnedQuantity)` |
| Toplam ciro (brüt) | `OrderItem.UnitPrice * OrderItem.Quantity` | `SUM(UnitPrice * Quantity)` |
| Sipariş sayısı | `Order` (distinct) | `COUNT(DISTINCT OrderId)` |
| İade adedi | `OrderItem.ReturnedQuantity` | `SUM(ReturnedQuantity)` |
| İade oranı (%) | türetilmiş | `SUM(ReturnedQuantity) / SUM(Quantity) * 100` |
| Ortalama birim fiyat | türetilmiş | `SUM(UnitPrice * Quantity) / SUM(Quantity)` |

**Bağlantı zinciri:** `OrderItem.ProductId` → `ProductVariant.ProductId` → `MainProduct.CategoryId` = `{id}`

**Alt kategori kararı → Bölüm 7'ye bak.**

### 3.2 Kategori Bilgileri (meta)

Mevcut `_CategoryDetail.cshtml`'den taşınır:

- Üst kategori adı (`SuperCategory.Name`)
- Alt kategori sayısı (`SubCategories.Count()`)
- Ürün sayısı (`productService.GetProductCountByCategoryId`)
- Özellik sayısı (`CategoryAttributes.Count`)
- Favori (`IsFavorite`)
- Varsayılan KDV (`DefaultVatRate`)

### 3.3 En Çok Satan Ürünler (Top 5)

Kategori içinde son 30 günde en çok satılan ürünler tablosu.

| Sütun | Kaynak |
|---|---|
| Ürün adı | `MainProduct.Title` |
| Satılan adet | `SUM(OrderItem.Quantity)` |
| Ciro | `SUM(OrderItem.UnitPrice * OrderItem.Quantity)` |
| İade oranı | `SUM(ReturnedQuantity) / SUM(Quantity) * 100` |

Kayıtlar `TotalRevenue DESC` sıralamasıyla ilk 5 ürün. Satışı olmayan kategoride bu tablo "henüz satış yok" boş state gösterir.

### 3.4 Pazaryeri Kırılımı

Son 30 günde hangi pazaryerinden ne kadar satış geldiği, `Order.MarketPlaceId` → `MarketPlace.Name` bazında.

| Sütun | Kaynak |
|---|---|
| Pazaryeri adı | `MarketPlace.Name` (null ise "Direkt / Storefront") |
| Satılan adet | `SUM(OrderItem.Quantity)` |
| Ciro | `SUM(UnitPrice * Quantity)` |
| Pay (%) | türetilmiş (satır cirosu / toplam ciro) |

Tabler `progress` bar ile görsel oran gösterimi (ağır JS kütüphanesi yok).

### 3.5 Özellikler Tablosu

Mevcut `_CategoryDetail.cshtml`'deki tablo aynen taşınır: Özellik adı, Zorunlu, Varyanter, Dilimleyici badge'leri.

### 3.6 Eylemler

Mevcut sağ kutudaki "Düzenle" (→ `/categories/{id}/edit`) ve "Sil" (HTMX POST `/categories/{id}/delete`) butonları sayfa header'ına taşınır.

---

## 4. MVP Sınırı

### V1'e giriyor (bu task)

- Özet KPI'lar (6 metrik, son 30 gün sabit — filtre yok)
- Kategori meta bilgisi (mevcut _CategoryDetail içeriği)
- Top 5 ürün tablosu
- Pazaryeri kırılımı tablosu + Tabler progress bar
- Özellikler tablosu
- Düzenle / Sil eylemleri
- Boş state (satış olmayan kategori)
- Breadcrumb: Kategoriler → {CategoryName}

### V2'ye bırakılan (bu task'a dahil değil)

- Tarih aralığı filtresi (son 7 / 30 / 90 gün veya custom)
- Zaman serisi grafik (günlük satış sparkline) — Tabler/ApexCharts olsa bile V2; V1'de KPI card yeterli
- Alt kategori kırılımı (hangi alt kategoride daha çok satış)
- "Detaya Git" butonunu _CategoryDetail.cshtml'e eklemek (designer ayrı task)

---

## 5. ViewModel Şekli — `CategoryDetailPageVm`

```csharp
// /Features/Categories/ViewModels/CategoryDetailPageVm.cs

public sealed class CategoryDetailPageVm
{
    // --- Meta ---
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = "";
    public string? SuperCategoryName { get; init; }
    public int SubCategoryCount { get; init; }
    public int ProductCount { get; init; }
    public int AttributeCount { get; init; }
    public bool IsFavorite { get; init; }
    public decimal? DefaultVatRate { get; init; }
    public List<CategoryAttributeRowVm> Attributes { get; init; } = [];

    // --- KPI (son 30 gün) ---
    public int TotalSoldQuantity { get; init; }          // SUM(Qty - ReturnedQty)
    public decimal TotalRevenue { get; init; }            // SUM(UnitPrice * Qty)
    public int TotalOrderCount { get; init; }             // COUNT(DISTINCT OrderId)
    public int TotalReturnedQuantity { get; init; }       // SUM(ReturnedQty)
    public decimal ReturnRate { get; init; }              // % (0-100)
    public decimal AverageUnitPrice { get; init; }        // SUM(UnitPrice*Qty)/SUM(Qty)

    // --- Top 5 Ürün ---
    public List<CategoryTopProductVm> TopProducts { get; init; } = [];

    // --- Pazaryeri Kırılımı ---
    public List<CategoryMarketplaceBreakdownVm> MarketplaceBreakdown { get; init; } = [];
}

public sealed class CategoryAttributeRowVm
{
    public string AttributeName { get; init; } = "";
    public bool IsRequired { get; init; }
    public bool IsVarianter { get; init; }
    public bool IsSlicer { get; init; }
}

public sealed class CategoryTopProductVm
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public int TotalSold { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal ReturnRate { get; init; }  // %
}

public sealed class CategoryMarketplaceBreakdownVm
{
    public string MarketplaceName { get; init; } = "";
    public int TotalSold { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal RevenueSharePercent { get; init; }  // 0-100, görsel progress için
}
```

---

## 6. Yeni Servis Metodu — `ICategoryService`

Mevcut `ICategoryService`'e eklenecek tek metot:

```csharp
Task<CategoryDetailPageVm?> GetCategoryDetailPageAsync(int categoryId, int daysPast = 30);
```

Bu metot:
1. Kategori meta bilgisini çeker (mevcut `GetCategoryDetailById` mantığıyla örtüşür).
2. `OrderItem` → `ProductVariant` → `MainProduct` join'iyle son `daysPast` günün satış aggregate'ini çeker.
3. Top 5 ürünü hesaplar.
4. Pazaryeri kırılımını hesaplar.
5. Kategori bulunamazsa `null` döner (controller 404 verir).

**DB Master notu:** Bu metot `Include` zinciri değil, `GroupBy` + join içerdiği için DB Master review şart (CLAUDE.md kuralı). Sorgu `AsNoTracking`, `Select` projeksiyon, `AsSplitQuery` gerekliliği DB Master'ın kararı.

---

## 7. Alt Kategori Kararı

**Karar: Sadece doğrudan bu kategoriye bağlı ürünlerin satışları (alt ağaç dahil değil).**

Gerekçe:
- `MainProduct.CategoryId` tek bir kategori ID'si tutar — ürünler tek bir yaprak kategoriye aittir.
- Alt ağaç toplamı için recursive CTE veya uygulama katmanında çoklu kategori ID toplama gerekir; bu V1 için gereksiz karmaşıklık.
- Esnafın ilk ihtiyacı "bu kategorideki ürünlerimin satışı" — parent kategori aggregate'i ileride "alt kategoriler kartı" ile sağlanabilir.

**Not:** Bu karar view'da küçük bir bilgi notu olarak gösterilmeli: "Bu metriklere yalnızca bu kategoriye doğrudan bağlı ürünler dahildir."

---

## 8. Kabul Kriterleri

1. `GET /categories/42` geçerli bir kategori ID'si ile çağrıldığında HTTP 200 döner; tam sayfa (layout dahil) render edilir.
2. `GET /categories/99999` var olmayan bir kategori ID'si ile çağrıldığında HTTP 404 döner.
3. KPI kartlarında (satılan adet, ciro, sipariş sayısı, iade adedi/oranı, ort. birim fiyat) değerler gösterilir; satışı olmayan kategoride bu değerler sıfır gösterir.
4. Satışı olan bir kategoride Top 5 ürün tablosu, ciro azalan sırada listelenmiş şekilde görünür.
5. Satışı olmayan kategoride Top 5 tablosunun yerine Tabler `empty` state bileşeni görünür.
6. Pazaryeri kırılımı tablosu en az bir satır içerir (satışı olan kategori için); her satırda Tabler `progress` barı ile ciro payı görsel olarak gösterilir.
7. Kategori meta bilgileri (üst kategori, alt kategori sayısı, ürün sayısı, özellik sayısı, KDV, favori) doğru şekilde görünür.
8. Özellikler tablosu mevcut `_CategoryDetail.cshtml` ile aynı badge yapısıyla görünür.
9. "Düzenle" butonu `/categories/{id}/edit` adresine yönlendirir; "Sil" HTMX ile silme işlemi yapar.
10. Breadcrumb "Kategoriler → {CategoryName}" şeklinde görünür; "Kategoriler" linki `/categories`'e döner.
11. Sayfa `/categories`'den bağımsız olarak doğrudan URL ile açılabilir (bookmark'lanabilir).

---

## 9. Manual Test Adımları

1. Tarayıcıda `http://localhost:8085` adresine git, admin / 123456789 ile giriş yap.
2. Sol menüden "Kategoriler"e git (`/categories`), ağaçtan içinde ürün bulunan bir alt kategori seç — sağ kutuda "Detaya Git" butonu görünmeli (designer task'ı bittiğinde). Butona tıkla, `/categories/{id}` adresine yönlenmeli.
3. Doğrudan URL ile `/categories/{id}` adresine git (id = ürünleri olan bir kategori). Sayfa tam layout ile açılmalı, breadcrumb "Kategoriler → {CategoryName}" şeklinde görünmeli.
4. KPI kartlarını kontrol et: Toplam Satılan Adet, Toplam Ciro, Sipariş Sayısı, İade Adedi, İade Oranı, Ortalama Birim Fiyat değerlerinin gösterildiğini doğrula.
5. Bu kategoriye bağlı ürünlerde sipariş varsa Top 5 ürün tablosunun ciro azalan sırada listelendiğini doğrula; tablo üzerindeki her ürün adının `/products/{productId}` bağlantısını açtığını doğrula.
6. Pazaryeri kırılımı tablosunda Trendyol / Storefront / diğer pazaryerlerinin ayrı satırlar olarak göründüğünü ve progress barların toplamda %100 olduğunu doğrula.
7. Hiç sipariş olmayan bir kategoriyi aç (`/categories/{id}` — boş kategori). KPI kartlarında tüm değerler 0, Top 5 alanında Tabler empty state görünmeli.
8. `/categories/999999` adresine git — HTTP 404 sayfası dönmeli.
9. "Düzenle" butonuna tıkla — `/categories/{id}/edit` sayfası açılmalı.
10. "Sil" butonuna tıkla, onay dialog'unu onayla — kategori silinmeli ve `/categories` sayfasına redirect olunmalı.

---

## 10. Edge Case'ler

| Durum | Beklenen davranış |
|---|---|
| Satışı olmayan kategori | KPI'lar 0, Top 5 empty state, pazaryeri kırılımı tablosu görünmez veya "henüz satış yok" |
| Tüm OrderItem.ReturnedQuantity = Quantity | ReturnRate %100 gösterir, TotalSoldQuantity = 0 |
| Order.MarketPlaceId null (Storefront siparişi) | Pazaryeri adı "Direkt / Storefront" olarak gösterilir |
| Çok ürünlü kategori (50+ ürün) | Top 5 hâlâ sadece 5 satır gösterir; "Tüm ürünleri gör" linki `/reports/category-sales` gibi bir rapor sayfasına yönlendirebilir (V2) |
| Alt kategorisi olan kategori | Sadece doğrudan bu kategoriye bağlı ürünler dahil (Bölüm 7 kararı); view'da bilgi notu gösterilir |
| UnitPrice = 0 olan OrderItem | Ciro ve ortalama fiyat hesabına dahil edilir (sıfır etki); edge case değil, doğal davranış |

---

## 11. Teknik Notlar

- Controller: `CategoryController` içine `[HttpGet("/categories/{id:int}")]` action'ı eklenir.
- HTMX uyumluluğu: Bu endpoint tam sayfa olduğundan `Request.IsHtmx()` branch'i gerekmez; sade `return View(vm)`.
- View dosyası: `Features/Categories/Views/Detail.cshtml` (yeni dosya).
- Sorgu performansı: `OrderItem` → `ProductVariant` → `MainProduct` join'i için `MainProduct.CategoryId` kolonu index'li olmalı — DB Master onayı şart.
- Test: `CategoryDetailPageAsync` için unit test (mock DbContext ile aggregate hesap doğrulaması) + integration test (Testcontainers ile gerçek DB).
