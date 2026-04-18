# Birleşik Satış & Sipariş Sayfası — Tasarım

**Tarih:** 2026-04-18
**Durum:** Tasarım onaylandı, implementasyon planı yazılacak
**İlgili:** `Features/Sales`, `Features/Orders`, `Features/POS`

## 1. Problem

Şu an üç ayrı giriş noktası var:
- `/sales` — iç `Sale` entity'si (POS + Manuel)
- `/orders` — Trendyol vb. pazaryeri `Order`'ları
- `/marketplace/orders` — pazaryeri odaklı filtreli liste

Kullanıcı bir günün toplam satışlarını ya da belirli müşterinin tüm kanal aktivitesini göremiyor. "Hiçbir şey göremiyoruz" geri bildirimi tam olarak bu dağınıklıktan kaynaklanıyor. Ek olarak mevcut tablolar dar (tutar/durum/kargo bilgisi rekabet ediyor) ve sağda buton kalabalığı var.

## 2. Hedefler

1. **Tek birleşik liste** — POS, Manuel, Marketplace (Trendyol, N11, Hepsiburada, ...), Storefront kanallarının hepsi aynı sayfada.
2. **Bilgi yoğun ama okunabilir tablo** — Tabler datagrid pattern'i: iki katlı satırlar, renkli kaynak rozetleri, kolon başına ikincil bilgi.
3. **Satır-tıklama ile detaya gitme** — sağda buton yok. Dropdown/işlem menüsü detay sayfasında.
4. **Kaynağa göre ayrı detay sayfaları** — Sale ve Order entity'leri farklı alanlara sahip, içerikleri de farklı olacak.
5. **Eski URL'ler kırılmasın** — `/orders`, `/marketplace/orders` → 301 redirect.

## 3. Kapsam

### 3.1 Kapsam Dahilinde
- Yeni `/sales` birleşik liste sayfası (KPI kartları + kaynak sekmeleri + filtre çubuğu + tablo)
- `/sales/sale/{id}` ve `/sales/order/{id}` detay route'ları (mevcut `/sales/{id}` ve `/orders/{id}` yerine)
- Paylaşılan detay sayfası üst layout'u (breadcrumb + başlık + durum + işlemler dropdown)
- DB view `vw_unified_sales` + keyless EF entity
- `/orders`, `/marketplace/orders` redirect'leri

### 3.2 Kapsam Dışında
- Yeni iade akışı (mevcut `SaleReturn` / polymorphic iade kullanılacak)
- POS hızlı satış ekranı (`/pos`) — dokunulmayacak
- X/Z rapor sayfaları
- Fatura kesme akışı (mevcut kullanılacak)
- Yeni istatistik/dashboard — bu iş sadece liste sayfası

## 4. URL & Routing

| Eski | Yeni | Davranış |
|---|---|---|
| `GET /sales` | `GET /sales` | Birleşik liste (yeniden yazılıyor) |
| `GET /sales/{id}` | `GET /sales/sale/{id}` | Sale detay (mevcut kod taşınıyor) |
| `GET /orders` | `GET /sales` | 301 Redirect |
| `GET /orders/{id}` | `GET /sales/order/{id}` | Order detay (mevcut kod taşınıyor) |
| `GET /marketplace/orders` | `GET /sales?source=trendyol` (varsayılan) | 301 Redirect query preserved |

Sidebar nav: "Satışlar" ve "Siparişler" iki ayrı öğe yerine **tek "Satışlar"** kalacak.

## 5. Veri Modeli

### 5.1 Yaklaşım — DB View + Keyless Entity

App-level merge (iki manager'dan sonuç alıp bellekte birleştirmek) pagination'ı bozar: `Skip(N).Take(M)` iki ayrı kaynak için doğru offset vermez. Bu yüzden PostgreSQL `VIEW` + EF Core `keyless entity` mapping kullanılır.

### 5.2 View Şeması

```sql
CREATE VIEW vw_unified_sales AS
SELECT
    s."Id"                               AS "Id",
    0                                    AS "EntityType",   -- 0=Sale, 1=Order
    CASE s."Source"
        WHEN 1 THEN 1  -- POS
        WHEN 2 THEN 2  -- Manual
    END                                  AS "Source",
    s."SaleNumber"                       AS "Number",
    s."CreatedAt"                        AS "SaleDate",
    s."CustomerId"                       AS "CustomerId",
    s."TotalPrice"                       AS "TotalPrice",
    s."ItemCount"                        AS "ItemCount",
    s."Status"                           AS "StatusCode",
    NULL                                 AS "CargoTrackingNumber",
    NULL                                 AS "MarketPlaceId",
    s."TenantId"                         AS "TenantId",
    s."IsDeleted"                        AS "IsDeleted"
FROM "Sales" s
WHERE s."IsDeleted" = false

UNION ALL

SELECT
    o."Id"                               AS "Id",
    1                                    AS "EntityType",
    CASE
        WHEN o."MarketPlaceId" IS NOT NULL THEN 10 + o."MarketPlaceId"  -- 11=Trendyol, 12=N11, ...
        ELSE 3                                                           -- Storefront
    END                                  AS "Source",
    o."OrderNumber"                      AS "Number",
    COALESCE(o."OrderDate", o."CreatedAt") AS "SaleDate",
    o."CustomerId"                       AS "CustomerId",
    o."TotalPrice"                       AS "TotalPrice",
    o."TotalQuantity"                    AS "ItemCount",
    COALESCE(o."StorefrontOrderStatus"::int, 0) AS "StatusCode",
    o."CargoTrackingNumber"              AS "CargoTrackingNumber",
    o."MarketPlaceId"                    AS "MarketPlaceId",
    o."TenantId"                         AS "TenantId",
    o."IsDeleted"                        AS "IsDeleted"
FROM "Orders" o
WHERE o."IsDeleted" = false;
```

**Not:** Kolon adları, tenant filtresi ve exact tipler migration sırasında gerçek şemaya göre revize edilir — yukarıdaki taslaktır.

### 5.3 UnifiedSaleSource enum

```csharp
public enum UnifiedSaleSource
{
    POS        = 1,
    Manual     = 2,
    Storefront = 3,
    Trendyol   = 11,
    N11        = 12,
    Hepsiburada = 13,
    Amazon      = 14,
    Pazarama    = 15,
    PttAvm      = 17,
    Ciceksepeti = 18
}
```

`10 + MarketPlaceId` offset'i kullanıyoruz ki yeni pazaryeri eklendiğinde kaynak enum'u düzgün genişlesin.

### 5.4 Keyless Entity

```csharp
public sealed class UnifiedSaleView
{
    public Guid Id { get; init; }
    public int EntityType { get; init; }   // 0=Sale, 1=Order
    public UnifiedSaleSource Source { get; init; }
    public string? Number { get; init; }
    public DateTimeOffset SaleDate { get; init; }
    public int? CustomerId { get; init; }
    public decimal TotalPrice { get; init; }
    public int ItemCount { get; init; }
    public int StatusCode { get; init; }
    public string? CargoTrackingNumber { get; init; }
    public int? MarketPlaceId { get; init; }
    public int TenantId { get; init; }
}
```

`IntegrationDbContext`:
```csharp
modelBuilder.Entity<UnifiedSaleView>().ToView("vw_unified_sales").HasNoKey();
```

### 5.5 Normalize Durum (UI için)

Sale.Status ve Order durumları (marketplace / storefront) tablo için 6 normalize duruma indirgenir:

| Kod | Ad | Rozet rengi |
|---|---|---|
| 1 | Tamamlandı | yeşil |
| 2 | Beklemede/Hazırlanıyor | sarı |
| 3 | Kargoda | mavi |
| 4 | Kısmi İade | turuncu |
| 5 | Tam İade | kırmızı |
| 6 | İptal | gri |

Normalizasyon view'da değil, controller/mapper katmanında yapılır (StatusCode + EntityType + MarketPlaceId kombinasyonuna göre).

## 6. Liste UI Tasarımı

### 6.1 Sayfa İskeleti (yukarıdan aşağı)

```
┌─ Sayfa başlığı: "Satışlar"
├─ 4 KPI kartı: Toplam Ciro · Satış Sayısı · Ortalama Sepet · İade Oranı
├─ Kaynak sekmeleri (Tümü · POS · Manuel · Storefront · Trendyol · ...)
├─ Filtre barı: Tarih aralığı (Flatpickr TR) · Durum dropdown · Arama kutusu
└─ Tablo (iki katlı satırlar) + sayfalama
```

### 6.2 Tablo Kolonları

| Kolon | Ana satır | Alt muted satır |
|---|---|---|
| Tarih | `18.04 14:22` | `34 dk önce` |
| Sipariş/Satış | Number (ör. `TY-778120`) | Kaynak rozeti |
| Müşteri | Ad Soyad veya `—` | Email / "Kurumsal" / "Tekil" |
| Kalem | Adet toplamı (`3 adet`) | Farklı ürün sayısı (`2 ürün`) |
| Tutar | `₺1.249,00` | Ödeme özeti (`Kredi Kartı`, `Nakit`, vs.) |
| Durum | Normalize durum rozeti | Kargo no / kasa no / iade sayısı |

### 6.3 Satır Davranışı

- Tüm satır `<tr hx-get="/sales/sale/{id}" hx-target="#main-content" hx-push-url="true">` (veya order)
- `cursor: pointer`, hover'da soft background
- **Sağda buton YOK** — kullanıcı geri bildirimi.

### 6.4 Kaynak Rozetleri

Tabler badge renkleri (mevcut tema ile uyumlu):
- POS → `bg-blue-lt text-blue`
- Manuel → `bg-yellow-lt text-yellow`
- Storefront → `bg-cyan-lt text-cyan`
- Trendyol → `bg-orange-lt text-orange`
- N11 → `bg-red-lt text-red` (eski markasal renk)
- Hepsiburada → `bg-orange-lt text-orange` (farklı tonda — CSS değişkeni)
- Amazon → `bg-yellow-lt text-yellow`
- Pazarama, PttAVM, Çiçeksepeti → uygun renkler

### 6.5 HTMX Etkileşimi

- Filtre değişince → `hx-get="/sales"` + `hx-target="#unified-sale-list"` + `hx-push-url="true"` → `_UnifiedSaleList` partial döner
- Sayfalama → aynı partial
- Kaynak sekme değişimi → `source` query param güncellenir, aynı partial
- İlk sayfa yüklemesi klasik View (SEO/bookmark için)

## 7. Detay Sayfası

### 7.1 Ortak Üst Layout (shared partial)

```
┌─────────────────────────────────────────────────────┐
│ Satışlar  /  #POS-0054              [⋯ İşlemler ▾]  │
├─────────────────────────────────────────────────────┤
│ [Kaynak badge] [Durum badge]     18.04.2026 13:41   │
│ Müşteri: —  ·  Kasiyer: Ali  ·  Kasa 01             │
└─────────────────────────────────────────────────────┘
```

Bu blok `_UnifiedSaleDetailHeader.cshtml` olarak ortak partial'da. Hem `SaleDetail.cshtml` hem `OrderDetail.cshtml` bu partial'ı include eder.

### 7.2 İşlemler Dropdown İçeriği

**Sale (POS/Manuel):**
- İade Al (kısmi/tam)
- Fatura Kes / E-Arşiv
- Makbuz/Fiş Yazdır
- İptal Et
- Müşteri Değiştir (sadece Manuel)
- Ödeme Ekle (eksik ödemeliyse)

**Order (Marketplace):**
- Kargo Kodu Gir
- Durumu Güncelle
- Pazaryerinden Yenile
- İade Onayla/Reddet
- Fatura Kes
- Yazdır

**Order (Storefront):**
- Onayla / Hazırlanıyor
- Kargoya Ver (+ kargo kodu)
- Teslim Edildi işaretle
- İade Onayla
- Fatura Kes / Yazdır

Tabler `dropdown-menu` + `dropdown-divider` ile gruplanır. 4'ten fazla ana işlem varsa "Daha fazla" altına alınır.

### 7.3 Gövde İçeriği

Mevcut `SaleDetail.cshtml` ve `OrderDetail.cshtml` gövdeleri **korunur** — sadece header partial'ı ortaklaşır. İade modal, ödeme ekleme, kargo güncelleme gibi akışlar mevcut.

## 8. Mimari Değişiklikler

### 8.1 Yeni Dosyalar

**Entity:**
- `Entegrasyon.Entity/Sales/Views/UnifiedSaleView.cs`
- `Entegrasyon.Entity/Sales/UnifiedSaleSource.cs`
- `Entegrasyon.Entity/Sales/UnifiedSaleStatus.cs` (normalize 6'lı enum)
- `Entegrasyon.Entity/Dtos/Sale/UnifiedSaleListDto.cs`
- `Entegrasyon.Entity/Dtos/Sale/UnifiedSaleFilterDto.cs`

**DataAccess:**
- Migration: `CreateUnifiedSalesView` (CREATE VIEW statement)
- `IntegrationDbContext` — `DbSet<UnifiedSaleView>` + `ToView` mapping
- `Configurations/UnifiedSaleViewConfiguration.cs`

**Business:**
- `Abstract/IUnifiedSaleManager.cs` — `GetPageableAsync`, `GetSummaryAsync`, `GetSourceCountsAsync`
- `Concrete/UnifiedSaleManager.cs`
- `Mappers/UnifiedSaleMapper.cs` (Mapperly)

**MVC:**
- `Features/Sales/SaleController.cs` — `Index` aksiyonu yeniden yazılır. `SaleDetail` ve `OrderDetail` aksiyonları bu controller'a taşınır (route'lar değişir).
- `Features/Sales/Views/Index.cshtml` — yeniden yazılır
- `Features/Sales/Views/Partials/_UnifiedSaleTable.cshtml`
- `Features/Sales/Views/Partials/_SourceTabs.cshtml`
- `Features/Sales/Views/Partials/_UnifiedSaleFilters.cshtml`
- `Features/Sales/Views/Partials/_UnifiedKpiCards.cshtml`
- `Features/Sales/Views/Partials/_UnifiedSaleDetailHeader.cshtml` (shared)
- `Features/Sales/ViewModels/UnifiedSaleListViewModel.cs`

### 8.2 Taşınan/Yeniden Konumlandırılan Dosyalar

- `Features/Orders/OrderController.cs` → `OrderDetail` aksiyonu `SaleController`'a taşınır, controller silinir (veya sadece redirect'lere indirgenir).
- `Features/Orders/Views/OrderDetail.cshtml` → `Features/Sales/Views/OrderDetail.cshtml`
- `Features/Orders/Views/Print.cshtml` → `Features/Sales/Views/OrderPrint.cshtml`

### 8.3 Silinen Dosyalar

- `Features/Orders/Views/Index.cshtml`
- `Features/Orders/Views/MarketplaceOrders.cshtml`
- `Features/Orders/Views/Partials/_OrderTable.cshtml`
- `Features/Sales/Views/Partials/_SaleTable.cshtml` (yerine Unified tablosu)
- `Features/Sales/Views/Partials/_SaleSummaryCards.cshtml` (yerine Unified KPI)

### 8.4 Dokunulmayan

- `Sale`, `Order`, `SaleItem`, `OrderItem` entity'leri — sadece okuyoruz
- `SaleManager`, `OrderManager`, `TrendyolOrderService` — değişmiyor
- İade akışı, POS hızlı satış, X/Z raporlar
- Print view içeriği (sadece yolu değişebilir)

## 9. Çok-Kanal Filtreleme Mantığı

`UnifiedSaleFilterDto`:
```csharp
public record UnifiedSaleFilterDto(
    UnifiedSaleSource? Source,   // null = tümü
    UnifiedSaleStatus? Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? SearchText,          // number veya müşteri adı
    int? CustomerId,
    int PageIndex,
    int PageSize = 25
);
```

Manager sorgusu:
```csharp
var q = dbContext.Set<UnifiedSaleView>()
    .Where(x => x.TenantId == tenantId)
    .Where(x => x.SaleDate >= start && x.SaleDate < end);

if (filter.Source.HasValue)
    q = q.Where(x => x.Source == filter.Source.Value);
// ... diğer filtreler
```

## 10. Multi-Tenant

View'da `TenantId` kolonu korunuyor. Tüm manager sorgularında `WHERE TenantId = :tenant` zorunlu. Multi-tenant fazında view'ın kendisi de tenant-aware olarak kalır (DB-per-tenant kullanılıyor olsa bile view yapısı uyumlu).

## 11. Test Planı

### 11.1 Unit Tests (`Entegrasyon.UnitTest`)
- `UnifiedSaleManagerTests` — filtreleme (source, date, status, search), pagination, summary hesaplama
- `UnifiedSaleStatusMapperTests` — Sale ve Order durumlarının 6'lı enum'a normalize mapping'i

### 11.2 Integration Tests (`Entegrasyon.IntegrationTest`)
- `UnifiedSaleViewIntegrationTests` — view SELECT sorgusu, view'a Sale + Order ekleyince ikisi de gelir
- `UnifiedSalesControllerIntegrationTests` — `/sales` endpoint'i, kaynak sekmesi filtreleri
- Redirect testleri — `/orders`, `/marketplace/orders` → `/sales`

### 11.3 E2E Tests (`Entegrasyon.E2E`)
- `UnifiedSalesPageE2E` — login + `/sales` aç + filtre değiştir + satır tıkla + detay açıl
- Kaynak sekme geçişi
- Detay sayfasındaki İşlemler dropdown'u açılır

## 12. Riskler ve Kaldırıcılar

| Risk | Etki | Kaldırıcı |
|---|---|---|
| View performansı düşebilir (UNION ALL büyüdükçe) | Orta | Filtre ve index'ler mevcut; kritik olursa materialized view'a dönülür |
| Status normalize mapping'i tutarsız olabilir | Orta | Mapper için kapsamlı unit test; tüm kaynak+status kombinasyonları |
| Redirect'ler eski bookmark'ları koparabilir | Düşük | 301 kullan + query-preserving redirect |
| View schema değişiklikleri migration gerektirir | Orta | Migration DROP VIEW + CREATE VIEW olarak tekrar çalıştırılabilir |
| MarketPlaceId offset (10+id) çakışması | Düşük | Enum explicit olarak MarketPlaceId eşlemesi ile tanımlı |

## 13. Açık Sorular

Yok — onaylanan karar seti.

## 14. Başarı Kriterleri

1. `/sales` sayfasında bir kullanıcı: POS + Trendyol + Storefront satışlarını aynı tabloda görebilir.
2. Kaynak sekmesine tıklayınca sayı filtrelenir, URL güncellenir.
3. Bir satıra tıklayınca ilgili detay sayfasına gider (Sale veya Order).
4. Detay sayfasındaki "İşlemler" dropdown'unda kaynak-uygun aksiyonlar listelenir.
5. Eski URL'ler (`/orders`, `/marketplace/orders`) kırılmaz.
6. Test koşumu: unit + integration + E2E yeşil.
