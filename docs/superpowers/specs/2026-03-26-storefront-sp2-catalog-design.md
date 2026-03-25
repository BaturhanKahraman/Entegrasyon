# SP-2: Urun Vitrin & Katalog — Tasarim Dokumani

## Ozet

Storefront'un urun gosterim katmanini olusturur: kategori listeleme, urun detay, arama (autocomplete), filtreler, vitrin bolumleri ve SEO structured data. Mevcut business layer servislerini kullanir — yeni manager olusturulmaz, sadece mevcut servislere slug-based lookup metodlari eklenir.

## Kapsam

**Dahil:**
- CatalogController (kategori, marka, arama, tum urunler, yeni urunler, cok satanlar)
- ProductController (urun detay)
- HomeController guncelleme (vitrin bolumleri: yeni urunler, cok satanlar)
- SeoController guncelleme (dinamik sitemap)
- MegaMenuViewComponent guncelleme (kategori agaci)
- Arama autocomplete (JSON endpoint + frontend JS)
- Filtre paneli (fiyat araligi, marka, ozellikler)
- Sayfalama
- JSON-LD Product + CollectionPage schema
- Yeni DTO'lar ve view model'lar
- Mevcut servislere SeoSlug lookup metodlari
- Unit + integration testler

**Haric:**
- Sepet (SP-4)
- Uyelik/Auth (SP-3)
- Urun degerlendirme/yorum (SP-2 Faz 2'de)
- Kupon sistemi (SP-8)

---

## 1. Route'lar

```
/kategori/{slug}             -> CatalogController.Category
/kategori/{parent}/{child}   -> CatalogController.Category (alt kategori)
/urun/{slug}                 -> ProductController.Detail
/marka/{slug}                -> CatalogController.Brand
/urunler                     -> CatalogController.AllProducts
/arama?q=...                 -> CatalogController.Search
/api/arama/oneri?q=...       -> CatalogController.SearchSuggest (JSON)
/yeni-urunler                -> CatalogController.NewProducts
/cok-satanlar                -> CatalogController.BestSellers
```

Program.cs'e eklenecek route'lar:
```csharp
app.MapControllerRoute("category", "/kategori/{slug}",
    new { controller = "Catalog", action = "Category" });
app.MapControllerRoute("subcategory", "/kategori/{parentSlug}/{slug}",
    new { controller = "Catalog", action = "Category" });
app.MapControllerRoute("product", "/urun/{slug}",
    new { controller = "Product", action = "Detail" });
app.MapControllerRoute("brand", "/marka/{slug}",
    new { controller = "Catalog", action = "Brand" });
app.MapControllerRoute("allProducts", "/urunler",
    new { controller = "Catalog", action = "AllProducts" });
app.MapControllerRoute("search", "/arama",
    new { controller = "Catalog", action = "Search" });
app.MapControllerRoute("searchSuggest", "/api/arama/oneri",
    new { controller = "Catalog", action = "SearchSuggest" });
app.MapControllerRoute("newProducts", "/yeni-urunler",
    new { controller = "Catalog", action = "NewProducts" });
app.MapControllerRoute("bestSellers", "/cok-satanlar",
    new { controller = "Catalog", action = "BestSellers" });
```

---

## 2. Mevcut Servislere Eklenen Metodlar

Yeni manager olusturulmaz. Mevcut interface'lere metodlar eklenir:

### ICategoryService + CategoryManager

```csharp
Task<IDataResult<Category>> GetCategoryBySeoSlugAsync(string slug);
Task<IDataResult<List<CategoryTreeDto>>> GetCategoryTreeAsync();
```

- `GetCategoryBySeoSlugAsync`: SeoSlug ile kategori lookup (Include SubCategories)
- `GetCategoryTreeAsync`: ilk 2 seviye kategori agaci (mega menu + kategori sayfasi)

### IProductService + ProductManager

```csharp
Task<IDataResult<Product>> GetProductBySeoSlugAsync(string slug);
Task<IDataResult<Pageable<StorefrontProductCardDto>>> GetStorefrontProductsAsync(StorefrontCatalogQuery query);
Task<IDataResult<List<StorefrontProductCardDto>>> GetNewProductsAsync(int count);
Task<IDataResult<List<StorefrontProductCardDto>>> GetBestSellersAsync(int count);
Task<IDataResult<List<StorefrontSearchSuggestionDto>>> GetSearchSuggestionsAsync(string query, int maxResults);
```

- `GetProductBySeoSlugAsync`: SeoSlug ile urun lookup (Include Variants, Attributes, Images, Brand, Category)
- `GetStorefrontProductsAsync`: filtreleme + siralama + sayfalama (kategori, marka, fiyat araligi, arama, ozellikler)
- `GetNewProductsAsync`: son eklenen N urun (CreatedAt desc)
- `GetBestSellersAsync`: en cok satilan N urun (TotalSoldQuantity desc)
- `GetSearchSuggestionsAsync`: autocomplete icin urun + kategori + marka onerileri

### IBrandService + BrandService

```csharp
Task<IDataResult<Brand>> GetBrandBySeoSlugAsync(string slug);
```

---

## 3. Yeni DTO'lar

```csharp
// Entegrasyon.Entity/Dtos/Storefront/StorefrontProductCardDto.cs
public record StorefrontProductCardDto(
    Guid Id,
    string Title,
    string SeoSlug,
    string? ImageUrl,        // ilk varyant gosel
    decimal MinPrice,        // varyantlar arasi min
    decimal MaxPrice,        // varyantlar arasi max
    decimal? OldPrice,       // indirimli ise eski fiyat
    int TotalStock,
    string? BrandName,
    string CategoryName,
    bool IsNew);             // son 7 gun icerisinde eklenmis

// Entegrasyon.Entity/Dtos/Storefront/StorefrontProductDetailDto.cs
public record StorefrontProductDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? StockCode,
    string SeoSlug,
    string? SeoTitle,
    string? SeoDescription,
    string? BrandName,
    string? BrandSlug,
    string CategoryName,
    string? CategorySlug,
    int? CategoryId,
    List<StorefrontVariantDto> Variants,
    List<StorefrontAttributeDto> Attributes,
    List<BreadcrumbItemDto> Breadcrumbs);

public record StorefrontVariantDto(
    Guid Id,
    string? Barcode,
    decimal ListPrice,
    decimal SalePrice,
    int Stock,
    List<StorefrontVariantAttributeDto> Attributes,
    List<string> ImageUrls);

public record StorefrontVariantAttributeDto(
    string AttributeKey,
    string AttributeValue);

public record StorefrontAttributeDto(
    string Key,
    string HumanizedKey,
    string Value);

public record BreadcrumbItemDto(string Name, string Url);

// Entegrasyon.Entity/Dtos/Storefront/StorefrontCatalogQuery.cs
public record StorefrontCatalogQuery(
    int? CategoryId,
    int? BrandId,
    string? SearchQuery,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,          // newest, price-asc, price-desc, bestseller
    int Page = 1,
    int PageSize = 24);

// Entegrasyon.Entity/Dtos/Storefront/CategoryTreeDto.cs
public record CategoryTreeDto(
    int Id,
    string Name,
    string? SeoSlug,
    int ProductCount,
    List<CategoryTreeDto> Children);

// Entegrasyon.Entity/Dtos/Storefront/StorefrontSearchSuggestionDto.cs
public record StorefrontSearchSuggestionDto(
    string Text,
    string Url,
    string Type,              // "product", "category", "brand"
    string? ImageUrl);

// Entegrasyon.Entity/Dtos/Storefront/FilterOptionDto.cs
public record FilterOptionDto(
    int AttributeId,
    string AttributeKey,
    string HumanizedKey,
    List<FilterValueDto> Values);

public record FilterValueDto(
    int ValueId,
    string DisplayText,
    int ProductCount);
```

---

## 4. Controller'lar

### CatalogController

```csharp
public class CatalogController(
    IStorefrontTenantContext tenant,
    IProductService productService,
    ICategoryService categoryService,
    IBrandService brandService) : Controller
```

**Aksiyonlar:**
- `Category(string slug, string? parentSlug, StorefrontCatalogQuery query)` — ResponseCache 300s, VaryByQueryKeys
- `AllProducts(StorefrontCatalogQuery query)` — tum urunler, filtreleme
- `Search(string q, StorefrontCatalogQuery query)` — full-text arama
- `Brand(string slug, StorefrontCatalogQuery query)` — marka sayfasi
- `NewProducts()` — yeni urunler vitrin
- `BestSellers()` — cok satanlar vitrin
- `SearchSuggest(string q)` — JSON autocomplete endpoint

### ProductController

```csharp
public class ProductController(
    IStorefrontTenantContext tenant,
    IProductService productService) : Controller
```

**Aksiyonlar:**
- `Detail(string slug)` — ResponseCache 300s, urun detay + JSON-LD Product schema + breadcrumb

---

## 5. Views

```
Views/
├── Catalog/
│   ├── Categories.cshtml           -> tum kategoriler grid
│   ├── Category.cshtml             -> kategori + urun grid + filtreler
│   ├── AllProducts.cshtml          -> tum urunler (Category.cshtml ile ayni layout)
│   ├── Search.cshtml               -> arama sonuclari
│   ├── Brand.cshtml                -> marka sayfasi
│   ├── NewProducts.cshtml          -> yeni urunler
│   └── BestSellers.cshtml          -> cok satanlar
├── Product/
│   └── Detail.cshtml               -> urun detay
├── Shared/
│   ├── _ProductCard.cshtml         -> urun karti partial (grid item)
│   ├── _FilterSidebar.cshtml       -> filtre paneli partial
│   ├── _Pagination.cshtml          -> sayfalama partial
│   └── _Breadcrumb.cshtml          -> breadcrumb partial
└── Home/
    └── Index.cshtml                -> guncelleme (vitrin bolumleri)
```

### Urun Karti (_ProductCard.cshtml)

```
┌─────────────────────┐
│  [Gorsel]           │  hover: 2. gorsel (varsa)
│  [YENI] [-%20]      │  badge'ler
├─────────────────────┤
│  Marka Adi          │  kucuk, gri
│  Urun Basligi       │  2 satir, bold
│  ~~₺399~~ ₺299,90   │  eski/yeni fiyat
│  ★★★★☆ (24)         │  puan (stub — SP-2'de sabit)
│  Stokta ✓           │  yesil / Son 3! turuncu / Tukendi kirmizi
└─────────────────────┘
```

### Filtre Paneli (_FilterSidebar.cshtml)

```
Kategoriler (akordeon)
├── Giyim (42)
│   ├── Tisort (18)
│   └── Pantolon (24)
└── Ayakkabi (15)

Fiyat Araligi
├── Min: [____] ₺
├── Max: [____] ₺
└── [Uygula]

Markalar (checkbox)
├── ☑ Nike (12)
├── ☐ Adidas (8)
└── ☐ Puma (5)
```

### Urun Detay (Detail.cshtml)

```
Breadcrumb: Ana Sayfa > Giyim > Tisort > Siyah Basic Tisort

┌──────────────┬─────────────────────────┐
│  [Gorsel]    │  Marka (link)           │
│  [Gorsel]    │  Urun Basligi           │
│  [Gorsel]    │  ~~₺399~~ ₺299,90      │
│  (thumbnail) │  Stok: Stokta ✓         │
│              │  Renk: ○● ○ ○           │
│              │  Beden: [S] [M] [L] [XL]│
│              │  Miktar: [-] 1 [+]      │
│              │  [🛒 Sepete Ekle]       │
│              │  Paylasim ikonlari      │
└──────────────┴─────────────────────────┘

Tabs:
├── Aciklama (HTML)
├── Ozellikler (tablo)
├── Teslimat Bilgisi
└── Iade Bilgisi

Ilgili Urunler (carousel — ayni kategori)
```

---

## 6. SEO

### JSON-LD Eklemeleri (JsonLdBuilder'a)

```csharp
// BuildProduct — urun detay sayfasinda
{
  "@context": "https://schema.org",
  "@type": "Product",
  "name": "...",
  "image": ["..."],
  "brand": { "@type": "Brand", "name": "..." },
  "sku": "...",
  "offers": {
    "@type": "AggregateOffer",
    "lowPrice": 299.90,
    "highPrice": 399.90,
    "priceCurrency": "TRY",
    "availability": "https://schema.org/InStock",
    "itemCondition": "https://schema.org/NewCondition"
  }
}

// BuildCollectionPage — kategori sayfasinda
{
  "@context": "https://schema.org",
  "@type": "CollectionPage",
  "name": "...",
  "description": "..."
}
```

### Sitemap Guncelleme

SeoController.Sitemap dinamik hale getirilir:
- Tum urunler: `https://{domain}/urun/{seoSlug}` (changefreq weekly, priority 0.8)
- Tum kategoriler: `https://{domain}/kategori/{seoSlug}` (changefreq weekly, priority 0.7)
- Tum markalar: `https://{domain}/marka/{seoSlug}` (changefreq monthly, priority 0.5)
- Mevcut yasal sayfalar korunur

---

## 7. Autocomplete

### Endpoint: GET /api/arama/oneri?q={query}

```json
[
  { "text": "Siyah Basic Tisort", "url": "/urun/siyah-basic-tisort", "type": "product", "imageUrl": "/img/..." },
  { "text": "Tisort", "url": "/kategori/tisort", "type": "category", "imageUrl": null },
  { "text": "Nike", "url": "/marka/nike", "type": "brand", "imageUrl": null }
]
```

- Max 8 sonuc (5 urun + 2 kategori + 1 marka)
- Debounce: 300ms (frontend)
- Min 2 karakter

### Frontend (search.js)

```
Arama input'una focus -> oneri dropdown gorunur
Yazarken (300ms debounce) -> fetch /api/arama/oneri?q=...
Enter -> /arama?q=... sayfasina yonlendir
Oneri tiklama -> dogrudan o sayfaya git
```

---

## 8. MegaMenuViewComponent Guncelleme

Mevcut bos MegaMenuViewComponent kategori agacini gosterecek:

```
Hover menu:
┌──────────────────────────────────────────────┐
│  Giyim ▸        Ayakkabi ▸        Aksesuar   │
│  ├── Tisort     ├── Spor         ├── Saat    │
│  ├── Pantolon   ├── Klasik       ├── Cuzdan  │
│  └── Gomlek     └── Bot          └── Kemer   │
└──────────────────────────────────────────────┘
```

- `ICategoryService.GetCategoryTreeAsync()` cagirir, 30 dk cache
- Ilk 2 seviye gosterilir

---

## 9. HomeController Guncelleme

Ana sayfa vitrin bolumleri eklenir:

```
[Hero Banners]         -> mevcut (SP-1)
[Guven Rozetleri]      -> mevcut (SP-1)
[One Cikan Kategoriler] -> YENI: ust seviye kategoriler gorsel grid
[Yeni Urunler]         -> YENI: GetNewProductsAsync(8) carousel
[Cok Satanlar]         -> YENI: GetBestSellersAsync(8) carousel
[Newsletter]           -> mevcut (SP-1)
```

---

## 10. Cache

| Icerik | Cache Suresi | Invalidation |
|--------|-------------|--------------|
| Kategori agaci (mega menu) | 30 dk IMemoryCache | Kategori CRUD'da |
| Kategori sayfasi | 300s ResponseCache | VaryByQueryKeys |
| Urun detay | 300s ResponseCache | - |
| Arama sonuclari | 60s ResponseCache | VaryByQueryKeys |
| Autocomplete | 60s ResponseCache | VaryByQueryKeys |
| Sitemap | 3600s ResponseCache | - |
| Yeni urunler/cok satanlar | 300s ResponseCache | - |

---

## 11. Test Stratejisi

### Unit Tests
- `JsonLdBuilderTests`: BuildProduct, BuildCollectionPage
- `StorefrontCatalogQueryTests`: default degerler, siralama secenekleri
- `GetProductBySeoSlugAsync`: var/yok senaryolari
- `GetCategoryBySeoSlugAsync`: var/yok senaryolari
- `GetSearchSuggestionsAsync`: sonuc formati, limit

### Integration Tests
- Kategori listeleme (gercek DB)
- Urun detay slug lookup (gercek DB)
- Arama + sayfalama (gercek DB + full-text)

---

## 12. Dosya Yapisi Ozeti

```
Yeni/degisen dosyalar:
├── Application/Entegrasyon.Entity/Dtos/Storefront/
│   ├── StorefrontProductCardDto.cs
│   ├── StorefrontProductDetailDto.cs
│   ├── StorefrontCatalogQuery.cs
│   ├── CategoryTreeDto.cs
│   ├── StorefrontSearchSuggestionDto.cs
│   └── FilterOptionDto.cs
├── Application/Entegrasyon.Business/
│   ├── Abstract/ICategoryService.cs (2 metod eklenir)
│   ├── Abstract/IProductService.cs (5 metod eklenir)
│   ├── Abstract/IBrandService.cs (1 metod eklenir)
│   ├── Concrete/CategoryManager.cs (2 metod eklenir)
│   ├── Concrete/ProductManager.cs (5 metod eklenir)
│   └── Concrete/BrandService.cs (1 metod eklenir)
├── Application/Entegrasyon.Storefront/
│   ├── Controllers/CatalogController.cs (yeni)
│   ├── Controllers/ProductController.cs (yeni)
│   ├── Controllers/HomeController.cs (guncelleme)
│   ├── Controllers/SeoController.cs (guncelleme)
│   ├── Infrastructure/JsonLdBuilder.cs (2 metod eklenir)
│   ├── ViewComponents/MegaMenuViewComponent.cs (guncelleme)
│   ├── Views/Catalog/ (7 view)
│   ├── Views/Product/Detail.cshtml
│   ├── Views/Shared/_ProductCard.cshtml
│   ├── Views/Shared/_FilterSidebar.cshtml
│   ├── Views/Shared/_Pagination.cshtml
│   ├── Views/Shared/_Breadcrumb.cshtml
│   ├── Views/Home/Index.cshtml (guncelleme)
│   ├── wwwroot/js/search.js (yeni)
│   └── Program.cs (route'lar eklenir)
└── Test/
    ├── Entegrasyon.Test/Storefront/ (yeni testler)
    └── Entegrasyon.IntegrationTest/Storefront/ (yeni testler)
```
