# Spec: Ürün Detay — Performans KPI Kartları + Header Redesign (Task #76)

**Tarih:** 2026-06-11  
**Durum:** TL onayı bekleniyor  
**Tetikleyici:** Task #75 (storefront split) + DB task #2 (ProductPerformanceManager) tamamlanınca Designer'a verilecek  
**Bağlı task:** tasks.json #76

---

## Amaç

`/products/{id}` storefront kartından kurtulduktan sonra **performans odaklı sayfaya** dönüşür. Tasarım referansı: `Features/Categories/Views/Detail.cshtml` — aynı card-sm KPI deseni + pazaryeri breakdown progress barları.

---

## Veri Kaynağı — Teyit Edildi

### `IProductPerformanceManager.GetProductPerformanceAsync(productId, daysPast=30)`

**Döndürür:** `ProductPerformanceDetailDto` (`Entity/Dtos/Product/ProductPerformanceDetailDto.cs`)

```csharp
public sealed record ProductPerformanceDetailDto(
    Guid ProductId,
    int DaysPast,
    int TotalSoldQuantity,       // KPI kart 1
    decimal TotalRevenue,         // KPI kart 2
    int TotalOrderCount,          // KPI kart 3
    int TotalReturnedQuantity,    // KPI kart 4 (ReturnRate sub-label)
    decimal ReturnRate,
    decimal AverageUnitPrice,     // KPI kart 5
    IReadOnlyList<ProductVariantPerformanceDto> VariantBreakdown,
    IReadOnlyList<ProductMarketplaceBreakdownDto> MarketplaceBreakdown
);

// Varyant kırılımı (ciroya göre azalan)
public sealed record ProductVariantPerformanceDto(
    Guid VariantId, string VariantName, string? Barcode,
    int SoldQuantity, decimal Revenue
);

// Pazaryeri kırılımı (MarketPlaceId null = Direkt/Storefront)
public sealed record ProductMarketplaceBreakdownDto(
    int? MarketPlaceId, string MarketplaceName,
    int OrderCount, decimal Revenue
);
```

### Ek: ProductDetailDto (zaten sayfa action'ında mevcut)
- `TotalStock` (tüm varyant stok toplamı) → KPI kart 6
- `ActiveVariantCount` → kart 6 sub-label
- `IsPublished` → header badge
- `CategoryName`, `BrandName`, `SalePrice`, `VatRate`, `StockCode` → datagrid

---

## Wrapper ViewModel (SWE üretir)

```csharp
// Features/Products/ViewModels/ProductDetailPageVm.cs
public class ProductDetailPageVm
{
    public ProductDetailDto Product { get; set; } = null!;
    public ProductPerformanceDetailDto Performance { get; set; } = null!;
    public int TotalStock { get; set; }
    public int ActiveVariantCount { get; set; }
    public bool EcommerceEnabled { get; set; }
    public int DaysPast { get; set; } = 30;
    public bool HasSales => Performance.TotalOrderCount > 0;
}
```

**HTMX lazy-load seçeneği:** `GET /products/{id}/performance-summary` endpoint'inden lazy-load çekilebilir — SWE tercih eder; bu spec her iki yaklaşımla da çalışır.

---

## Sayfa Yapısı

### 1. Header — Tabler `page-header`

```
[Ürün adı — büyük başlık]
[IsPublished ? badge bg-green-lt "Mağazada Yayında" : badge bg-secondary-lt "Taslak"]
                                                    [page-actions dropdown ▾]
```

**page-actions dropdown:**

| İkon | Metin | Hedef |
|------|-------|-------|
| `ti-edit` | Düzenle | `/products/{id}/edit` |
| `ti-shopping-bag` | Storefront Ayarları | `/products/{id}/storefront` |
| `ti-git-branch` | Varyantları Yönet | `/products/{id}/variants` |
| `ti-discount-2` | İndirim Uygula | HTMX modal `/products/{id}/discount` |
| `ti-barcode` | Barkod Yazdır (Agent) | `window.printAgent` JS |
| `ti-download` | ZPL İndir | `/products/{id}/barcode` |
| *(divider)* | | |
| `ti-trash` text-danger | Sil | HTMX POST `/products/{id}/delete` |

---

### 2. KPI Kartları — 6 × `card-sm`

Grid: `row row-deck row-cards mb-3` + `col-6 col-sm-4 col-xl-2`

| # | İkon | Renk | Başlık | Değer | Sub-label |
|---|------|------|--------|-------|-----------|
| 1 | `ti-shopping-cart` | `bg-blue-lt` | Satış Adedi | `TotalSoldQuantity` | `son {DaysPast} gün` |
| 2 | `ti-cash` | `bg-green-lt` | Ciro | `TotalRevenue.ToString("N0") ₺` | `son {DaysPast} gün` |
| 3 | `ti-receipt` | `bg-azure-lt` | Sipariş | `TotalOrderCount` | `adet` |
| 4 | `ti-arrow-back-up` | `bg-red-lt` | İade Adedi | `TotalReturnedQuantity` | `oran %{ReturnRate:0.#}` |
| 5 | `ti-tag` | `bg-purple-lt` | Ort. Birim Fiyat | `AverageUnitPrice.ToString("N0") ₺` | `ağırlıklı` |
| 6 | `ti-package` | `bg-teal-lt` | Toplam Stok | `TotalStock` | `{ActiveVariantCount} varyant` |

**Sıfır satış:** Kartlar her zaman gösterilir — değerler `0`, hata/empty-state yok.

---

### 3. Ana İçerik — 2 Kolon

```
[col-lg-8: Sol]           [col-lg-4: Sağ]
  Varyant Kırılımı          Ürün Bilgileri datagrid
  Pazaryeri Kırılımı
```

#### Sol-A: Varyant Kırılımı

Kart başlığı: `<i class="ti ti-git-branch text-purple me-2"></i> Varyant Kırılımı`  
Kart alt başlığı: `son {DaysPast} gün`

**HasSales = true → tablo:**

```html
<table class="table table-vcenter card-table">
  <thead><tr><th>#</th><th>Varyant</th><th>Barkod</th>
              <th class="text-end">Satış Adedi</th><th class="text-end">Ciro</th></tr></thead>
  <tbody>
    @{ var rank = 0; }
    @foreach (var v in Model.Performance.VariantBreakdown)
    {
      <tr>
        <td><span class="badge @RankBadge(rank)">@(rank+1)</span></td>
        <td class="fw-medium">@v.VariantName</td>
        <td>@if (v.Barcode != null) {
              <span class="badge bg-secondary-lt font-monospace">@v.Barcode</span> }</td>
        <td class="text-end">@v.SoldQuantity</td>
        <td class="text-end fw-medium">@v.Revenue.ToString("N2") ₺</td>
      </tr>
      rank++;
    }
  </tbody>
</table>
```

`RankBadge` (CategoryDetail ile aynı): `0→bg-yellow-lt`, `1→bg-secondary-lt`, `2→bg-orange-lt`, `_→bg-secondary-lt`

**HasSales = false → empty-state:**

```html
<div class="empty">
  <div class="empty-icon"><i class="ti ti-chart-bar-off" style="font-size:2.5rem"></i></div>
  <p class="empty-title">Henüz satış yok</p>
  <p class="empty-subtitle text-secondary">
    Bu ürün için son @Model.DaysPast günde satış kaydı bulunmuyor.
  </p>
</div>
```

#### Sol-B: Pazaryeri Kırılımı (EcommerceEnabled = true ise)

CategoryDetail'deki **MarketplaceBreakdown** ile birebir aynı desen:

```csharp
var barColors = new[] { "bg-primary","bg-azure","bg-purple","bg-teal","bg-pink","bg-orange" };
decimal total = Model.Performance.TotalRevenue;
string Pct(decimal part) => total > 0
    ? ((double)(part/total)*100).ToString("0.#", CultureInfo.InvariantCulture) : "0";
```

```html
@{ var mIdx = 0; }
@foreach (var m in Model.Performance.MarketplaceBreakdown)
{
    var pct = Pct(m.Revenue);
    var color = barColors[mIdx % barColors.Length];
    var name = m.MarketplaceName ?? "Direkt / Storefront";
    <div class="mb-3">
      <div class="d-flex align-items-center mb-1">
        <span class="fw-medium">@name</span>
        <span class="ms-auto text-secondary small">
          @m.Revenue.ToString("N2") ₺ · @m.OrderCount sipariş · %@pct
        </span>
      </div>
      <div class="progress progress-sm">
        <div class="progress-bar @color" style="width:@pct%"
             role="progressbar" aria-valuenow="@pct" aria-valuemin="0" aria-valuemax="100"></div>
      </div>
    </div>
    mIdx++;
}
```

**EcommerceEnabled = false → bölüm gizlenir.** Opsiyonel bilgi banner:
```html
<div class="alert alert-info alert-dismissible">
  <i class="ti ti-info-circle me-2"></i>
  E-ticaret modülü pasif — pazaryeri kırılımı gösterilmiyor.
</div>
```

#### Sağ: Ürün Bilgileri (`datagrid`)

```html
<div class="card mb-3">
  <div class="card-header">
    <h3 class="card-title">Ürün Bilgileri</h3>
    <div class="card-actions">
      <a href="/products/@Model.Product.Id/edit" class="btn btn-sm btn-outline-primary">
        <i class="ti ti-edit me-1"></i> Düzenle
      </a>
    </div>
  </div>
  <div class="card-body">
    <div class="datagrid">
      <!-- Kategori, Marka, Barkod/StockCode, KDV, Satış Fiyatı, Yayın Durumu -->
    </div>
  </div>
</div>
```

Datagrid satırları (ProductDetailDto'dan):

| datagrid-title | datagrid-content |
|----------------|-----------------|
| Kategori | `CategoryName` |
| Marka | `BrandName` |
| Ürün Kodu | `StockCode` (font-monospace badge) |
| KDV | `%{VatRate}` |
| Satış Fiyatı | `{SalePrice} ₺` |
| Yayın Durumu | IsPublished badge |

---

### 4. `_Product360` Partial — DEĞİŞMEZ

KPI + 2-kolon alanın **altında**, tam genişlikte:

```html
<div class="mt-3">
    <partial name="Partials/_Product360" model="Model.Product" />
</div>
```

---

## Tam Sayfa Sırası

```
page-header (başlık + badge + actions)
──────────────────────────────────────
6 × KPI card-sm  (row row-deck)
──────────────────────────────────────
row row-cards
  col-lg-8                    col-lg-4
  ┌─ Varyant Kırılımı ──────┐ ┌─ Ürün Bilgileri ─────────┐
  │  tablo / empty-state    │ │  datagrid + Düzenle btn  │
  └─────────────────────────┘ └──────────────────────────┘
  ┌─ Pazaryeri Kırılımı ────┐
  │  progress bars          │
  │  (EcommerceEnabled ise) │
  └─────────────────────────┘
──────────────────────────────────────
_Product360 partial (tam genişlik)
```

---

## Kabul Kriterleri

1. Sayfada "Mağaza Ayarları" kartı YOK (task #75 bağımlılığı).
2. 6 KPI kartı görünüyor; sıfır satışta 0 değer, hata yok.
3. Satış varsa Varyant Kırılımı tablosu Revenue azalan sırada, rank rozetleri doğru.
4. Pazaryeri Kırılımı progress barları Revenue'ya oransal, `null` MarketPlaceId "Direkt / Storefront".
5. EcommerceEnabled=false → Pazaryeri Kırılımı gizlenir.
6. page-actions tüm aksiyonları içeriyor; "Storefront Ayarları" `/products/{id}/storefront`'a gidiyor.
7. Datagrid doğru ürün meta bilgisi gösteriyor.
8. `_Product360` sekmeleri çalışıyor.
9. Mobilde KPI kartları en az 2-sütun.

---

## Manuel Test Adımları

1. `/products/{id}` aç — 6 KPI kartı sayfa başında görünmeli.
2. Satışı olan ürün: Varyant Kırılımı tablosunda satırlar, 1. sıra altın rozet.
3. Satışı olmayan ürün: varyant alanında "Henüz satış yok" empty-state.
4. Birden fazla pazaryeri satışı varsa: her biri ayrı progress bar satırı.
5. page-actions dropdown → "Storefront Ayarları" → `/products/{id}/storefront` açılmalı.
6. Header badge: yayında → yeşil "Mağazada Yayında", taslak → gri "Taslak".
7. Datagrid: kategori + marka + stok kodu + KDV + fiyat doğru değerlerde.
8. "Düzenle" butonu (datagrid card-actions) → `/products/{id}/edit`.
9. `_Product360` sekme tıklamaları çalışıyor (Aktivite, Pazaryeri, Siparişler, Stok).
10. Mobil (375px): KPI kartları 2-sütun, dropdown erişilebilir, tablo yatay scroll.
