# Global Arama + Ctrl+K Command Palette — Tasarım

**Tarih:** 2026-04-18
**Durum:** Auto-onaylı
**Paket:** B (POS iyileştirme serisi — 2. paket)

## Problem

1. **POS ürün arama fuzzy değil** — typo tolerant değil, muhtemelen `ILIKE` veya prefix.
2. **Sistemde "her şeyi ara" mekanizması yok** — kullanıcı müşteri/ürün/kategori/sayfaya ulaşmak için ayrı ayrı menüleri dolaşıyor.
3. Modern komut paleti (Linear, Vercel, GitHub) UX'i eksik: `Ctrl+K` ile anlık, kategorize, klavye-navigable sonuç gerekiyor.

## Kapsam

**Dahil:**
- PostgreSQL trigram index'leri eksik tablolar için (ProductVariants, Brands)
- `IGlobalSearchManager` — birleşik, çok-kaynaklı arama API'si
- `/api/search?q=X&limit=10` JSON endpoint — Ctrl+K UI için
- `Ctrl+K` command palette UI (modal, klavye-navigable, anlık sonuç)
- Sayfa kataloğu (statik route listesi — `/products`, `/customers`, `/sales`, ...)
- POS ürün arama aynı endpoint'i kullanır (`sources=products` filtresiyle)

**Dışında:**
- Arama geçmişi / favoriler (ileride)
- "Son ziyaret edilenler" (ileride)
- AI/NLP özellikleri
- Admin panel'de arama (ayrı site)

## Mimari

### Yüksek Seviye

```
┌─ Browser ──────────────────────────────────┐
│  Ctrl+K → modal açılır                     │
│  Input her keyup'ta → GET /api/search?q=X  │
│  (debounce 150ms)                          │
│  Sonuçlar kategorilere göre liste          │
│  ↑↓ ile seç, Enter ile git                 │
└────────────────┬───────────────────────────┘
                 │ JSON
                 ▼
┌─ SearchController (/api/search) ───────────┐
│  IGlobalSearchManager.SearchAllAsync(q, l) │
└────────────────┬───────────────────────────┘
                 │
                 ▼
┌─ GlobalSearchManager ──────────────────────┐
│  Parallel.ForEach over ISearchSource       │
│  • ProductSearchSource                     │
│  • CustomerSearchSource                    │
│  • CategorySearchSource                    │
│  • BrandSearchSource                       │
│  • PageSearchSource (in-memory)            │
│  Her biri {Type, Title, Subtitle, Url,     │
│  Score} döner. Score'a göre merge-sort.   │
└────────────────┬───────────────────────────┘
                 │
                 ▼ (sadece DB kaynakları)
┌─ PostgreSQL ───────────────────────────────┐
│  pg_trgm similarity + ILIKE %q%            │
│  tsvector @@ websearch_to_tsquery          │
└────────────────────────────────────────────┘
```

### Arama Kaynağı Interface

```csharp
public interface ISearchSource
{
    string SourceKey { get; }              // "products", "customers", ...
    string Label { get; }                  // UI'da gösterilecek başlık
    string Icon { get; }                   // Tabler icon class
    Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit);
}

public sealed record SearchHitDto(
    string SourceKey,
    string Title,
    string? Subtitle,
    string Url,
    double Score,
    string? Badge = null);
```

Her kaynak kendi tablosundan sorumlu, `IGlobalSearchManager` paralel çağırır ve sonuçları birleştirir.

### Paralel Çağrı + DbContext

Her `ISearchSource` kendi `IDbContextFactory<IntegrationDbContext>` ile çalışır — böylece `Task.WhenAll` güvenli (memory feedback: "No Task.WhenAll with DbContext" — ayrı DbContext'ler).

## PostgreSQL Değişiklikleri

### 1. ProductVariants için Trigram Index

```sql
CREATE INDEX "IX_ProductVariants_ProductTitle_Trgm"
  ON "ProductVariants" USING gin (/* türetilmiş kolon gerektiği için:
     aşağıda ProductVariantsSearchView ile çözüldü */);
```

**Sorun:** `ProductVariant` tablosunda arama için uygun tek kolon `Barcode` (unique zaten). Başlık `Product.Title`'da. JOIN'li search yavaş olur.

**Çözüm:** `Product` tablosunda zaten `SearchVector` var. `ProductSearchSource` doğrudan `Products` tablosu üzerinden arar, variant'lar için ek bir indexe gerek yok. POS'ta ürün → variant seçimi iki adımda olur (ürün seç → variant seç) ya da ilk variant otomatik.

### 2. Brand için Trigram Index

```sql
CREATE INDEX "IX_Brands_Name_Trgm"
  ON "Brands" USING gin ("Name" gin_trgm_ops)
  WHERE NOT "IsDeleted";
```

### 3. Customers için Tsvector Indeksi

`RetailCustomer.RetailSearchVector` ve `CorporateCustomer.CorporateSearchVector` zaten var ama GIN index'i teyit etmek gerek.

```sql
CREATE INDEX IF NOT EXISTS "IX_Customers_RetailSearchVector_Gin"
  ON "Customers" USING gin ("RetailSearchVector");

CREATE INDEX IF NOT EXISTS "IX_Customers_CorporateSearchVector_Gin"
  ON "Customers" USING gin ("CorporateSearchVector");
```

### 4. Sorgu Stratejisi

Her kaynakta iki aşamalı skor:
- **Score 1.0 — exact:** `Barcode = q` veya `Email = q` (varsa öncelikli)
- **Score 0.7–0.9 — tsvector:** `@@ websearch_to_tsquery('turkish', q)` + `ts_rank`
- **Score 0.3–0.7 — trigram:** `similarity(column, q)` — typo tolerant fallback
- **Score 0.1 — prefix ILIKE:** `column ILIKE q || '%'` — basit fallback

Union + en yüksek skoru al. PostgreSQL'de tek sorguda:

```sql
SELECT id, title,
       GREATEST(
         CASE WHEN search_vector @@ websearch_to_tsquery('turkish', @q) THEN 0.8 + ts_rank(...) ELSE 0 END,
         similarity(title, @q) * 0.6
       ) AS score
FROM Products
WHERE search_vector @@ websearch_to_tsquery('turkish', @q)
   OR similarity(title, @q) > 0.2
ORDER BY score DESC
LIMIT @limit;
```

Her kaynak bu stratejinin bir varyantını uygular.

## UI — Command Palette

### Tetikleyici

- `Ctrl+K` / `Cmd+K` → modal aç
- Navbar'da "Ara…" butonu (görsel ipucu) → aynı modalı açar
- `Esc` → kapat

### Modal İçeriği

```
┌─────────────────────────────────────────────┐
│  🔍 Ara… [ayse]                         Esc │
├─────────────────────────────────────────────┤
│  MÜŞTERİLER                                 │
│  ┊ 👤  Ayşe Yılmaz        ayse@mail.com    │
│  ┊ 👤  Ayşegül Demir      kurumsal         │
│                                             │
│  ÜRÜNLER                                    │
│  ┊ 📦  Ayşe Kazan           Stok: 12       │
│                                             │
│  SAYFALAR                                   │
│  ┊ 📄  Ayarlar            /settings        │
│                                             │
│  ↑↓ seç, ↵ git, Esc kapat                  │
└─────────────────────────────────────────────┘
```

### Klavye Navigasyonu

- `↑/↓` — satır değiştir
- `Enter` — seçili satırın URL'sine git
- `Tab` — kategori atla (advanced)
- `Esc` — kapat

### HTMX mi JS mi?

**JS tercih edildi:**
- Debounce (150ms), keyboard state, fetch abort → vanilla JS basitliği yeterli
- HTMX'in `hx-trigger="keyup changed delay:150ms"` + `hx-get` da aynısını yapar ama klavye navigasyonu için custom JS gerek
- Bu nedenle tek dosyada (`wwwroot/js/command-palette.js`) saf fetch + DOM manipülasyonu

## API Sözleşmesi

```http
GET /api/search?q=ayse&limit=8
Authorization: Cookie auth

{
  "groups": [
    {
      "sourceKey": "customers",
      "label": "Müşteriler",
      "icon": "ti-user",
      "hits": [
        { "title": "Ayşe Yılmaz", "subtitle": "ayse@mail.com", "url": "/customers/42", "score": 0.95 }
      ]
    },
    { "sourceKey": "products", "label": "Ürünler", "icon": "ti-package", "hits": [...] },
    { "sourceKey": "pages",    "label": "Sayfalar", "icon": "ti-layout", "hits": [...] }
  ],
  "totalHits": 7,
  "elapsedMs": 32
}
```

## POS Entegrasyonu

POS ekranındaki ürün arama kutusu aynı endpoint'i çağırır:

```
GET /api/search?q=X&sources=products&limit=12
```

`sources` parametresi filtreleme için. POS için sadece `products`. Ctrl+K'da filtre yok (hepsi).

## Sayfa Kataloğu

Statik bir C# listesi: `AppPage[] { Title, Url, Icon, Keywords, RequiredPermission }`.

```csharp
public static class AppPages
{
    public static readonly AppPage[] All =
    [
        new("Ürünler",     "/products",     "ti-package", ["urun", "product", "stok"], Permissions.Products.View),
        new("Müşteriler",  "/customers",    "ti-users",   ["musteri", "customer"], Permissions.Customers.View),
        // ... tüm sidebar URL'leri
    ];
}
```

`PageSearchSource` bunu bellekte filtreler (permission-aware). Hızlı çünkü sayı az (50'den az).

## Dosya Yapısı

**Yeni dosyalar:**
- `Entegrasyon.Entity/Dtos/Search/SearchHitDto.cs`
- `Entegrasyon.Entity/Dtos/Search/SearchGroupDto.cs`
- `Entegrasyon.Entity/Dtos/Search/SearchResponseDto.cs`
- `Entegrasyon.Business/Abstract/Search/ISearchSource.cs`
- `Entegrasyon.Business/Abstract/IGlobalSearchManager.cs`
- `Entegrasyon.Business/Concrete/Search/GlobalSearchManager.cs`
- `Entegrasyon.Business/Concrete/Search/ProductSearchSource.cs`
- `Entegrasyon.Business/Concrete/Search/CustomerSearchSource.cs`
- `Entegrasyon.Business/Concrete/Search/CategorySearchSource.cs`
- `Entegrasyon.Business/Concrete/Search/BrandSearchSource.cs`
- `Entegrasyon.Business/Concrete/Search/PageSearchSource.cs`
- `Entegrasyon.Business/Concrete/Search/AppPages.cs`
- `Entegrasyon.MVC/Features/Search/SearchController.cs` — `[HttpGet("/api/search")]`
- `Entegrasyon.MVC/Shared/Views/_CommandPalette.cshtml` — layout include'u
- `Entegrasyon.MVC/wwwroot/js/command-palette.js`
- `Entegrasyon.MVC/wwwroot/css/command-palette.css`
- Migration: `AddBrandTrigramIndex`

**Değiştirilen:**
- `Entegrasyon.MVC/Shared/Views/_Layout.cshtml` — CommandPalette partial include + JS+CSS link
- `Entegrasyon.MVC/Shared/Views/_Navbar.cshtml` — "Ara…" butonu (opsiyonel)
- `Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` — Search source'ların kaydı (IEnumerable<ISearchSource>)
- POS ekranı — ürün arama kutusu yeni endpoint'i kullanacak şekilde güncellenecek (ayrı partial)

## Performans

- Her kaynak `limit` kadar sonuç döndürür (varsayılan 5)
- Paralel fetch — toplam latency = en yavaş kaynak
- Hedef: < 200ms p99 (tipik pg_trgm + tsvector)
- Cache yok (ilk sürüm) — arama zaten hızlı

## Yetkilendirme

- `SearchController` `[Authorize]`
- Her `ISearchSource` kullanıcının permission'ını kontrol eder:
  - `PageSearchSource` — `AppPage.RequiredPermission` filtreler
  - `CustomerSearchSource` — `Permissions.Customers.View` yoksa boş liste döner
  - Diğerleri benzer

## Test

- **Unit:** Her `ISearchSource` için tek tablo seed + sorgu + beklenen hit.
- **Integration:** `/api/search?q=ayse` → en az 2 grup, hit'lerde URL formatı geçerli.
- **E2E:** Ctrl+K bas → modal aç → 3 harf yaz → ilk hit'e tıkla → navigasyon.

## Başarı Kriterleri

1. Herhangi bir sayfada `Ctrl+K` bas → command palette açılır.
2. "ayse" yazınca hem ilgili müşteri hem de ürün (varsa) görünür, en alakalı ilk sırada.
3. POS ekranında ürün arama kutusu typo'ya toleranslıdır ("akamen" → "akademik" bulur).
4. Sayfa arama: "müşter" yazınca `/customers` linkine ulaşabilir.
5. Permission'ı olmayan kullanıcı bir kaynağın sonuçlarını görmez.
6. `/api/search` endpoint'i 200ms altında yanıt verir (tipik veri).

## Açık Sorular

Yok — implementasyon sırasında küçük detaylar plan aşamasında çözülür.
