# Ürün 360° Aktivite Detay Sayfası — Design Spec

**Tarih:** 2026-06-10  
**Hazırlayan:** PM Agent  
**Durum:** Taslak — TL Onayı Bekleniyor  
**İlgili:** SWE (backend endpoint + controller), Designer (Tabler UI + HTMX partials)

---

## 1. Problem

Esnaf bir ürününün "ne durumda olduğunu" görmek için bugün birden fazla sayfayı dolaşmak zorunda:

- `/products/{id}` → temel ürün bilgisi (StockCode, Brand, Category, IsPublished)
- `/marketplace/matching/{id}` → pazaryeri senkronizasyon durumu + aktivite timeline (**bu sayfada timeline ZATEN ÇALIŞIYOR**)
- Sipariş sayfaları → bu ürünü içeren siparişler

Bu dağınıklık "Ne oldu?" sorusunu yanıtlamayı zorlaştırıyor:
> *"Trendyol'dan ret geldiyse neden reddedildi? Stok düştüyse hangi sipariş tüketti? Fiyat güncellemesi N11'e yansıdı mı?"*

**Kuzey Yıldızı:** Esnaf `/products/{id}` sayfasına girince o ürünün tüm e-ticaret geçmişini — pazaryeri durumları, senkronizasyon eventleri, stok hareketleri, sipariş referansları — tek sayfada, tarih sıralı timeline ile görebilsin.

---

## 2. Mevcut Altyapı — Ne Var, Ne Eksik

### ✅ Kullanılabilir (sıfırdan yazılmayacak)

| Bileşen | Yer | Notlar |
|---------|-----|--------|
| `ProductActivityLog` entity | `Entity/Logs/ProductActivityLog.cs` | 16 activity type, 4 status (Info/Success/Warning/Error) |
| `IProductActivityLogger` interface | `Business/Abstract/` | `LogAsync` + `GetTimelineAsync(productId, limit=50)` |
| `ProductActivityLogger` concrete | `Business/Concrete/` | `IDbContextFactory` DI, BRIN index optimize |
| 10+ pazaryeri polling servisi | `BackgroundServices/` | Trendyol / N11 / Hepsiburada / Amazon / Çiçeksepeti / Pazarama / PTTAVM hepsi `LogAsync` çağırıyor |
| `ProductMarketplace` entity | `Entity/Products/` | Status, ExternalId, LastSyncedAt, IsApproved, StatusMessage |
| `StockMovement` entity | `Entity/Products/` | Type: Sale/MarketplaceSale/Return/Transfer/ManualAdjustment |
| Timeline kodu (çalışan referans) | `Features/MarketplaceSync/ProductSyncController.cs` | `timelineResult = await productActivityLogger.GetTimelineAsync(id, 50)` — copy-pasta değil, extract ve reuse |
| `ProductSyncDetailVm.ActivityTimeline` | `Features/MarketplaceSync/ViewModels/` | Var, SWE ürün detay VM'sine ekleyecek |
| DB index'leri | Migration `20260314102448_AddProductActivityLog` | `IX_ProductActivityLogs_ProductId_CreatedAt` (composite) + BRIN |
| `OrderItem.ProductId` FK | `Entity/Orders/OrderItem.cs` | Ürün → Sipariş köprüsü mevcut |

### ❌ Mevcut Değil (bu spec'in çözdüğü gap'ler)

| Gap | Faz | Çözüm |
|-----|-----|-------|
| `/products/{id}` sayfasında timeline sekmesi yok | **Faz 1** | Mevcut `Detail.cshtml`'e sekme ekle + HTMX lazy endpoint |
| `GetTimelineAsync` filtre/pagination desteği yok | **Faz 1** | Interface + concrete genişlet |
| Sipariş referansları timeline/sekmesinde yok | **Faz 1** | `OrderItem JOIN Order WHERE ProductId` query + partial |
| Fiyat geçmişi tablosu yok | **Faz 2** | Ayrı entity/migration — Faz 1 kapsam dışı |
| `Product.ChangeHistory` JSONB doldurulmuyor | **Faz 2** | Faz 1'de `ProductActivityType.ContentUpdated` mesajı yeterli |

---

## 3. Akış / Log Türleri — Gösterim Tasarımı

### 3.1 ActivityType → Timeline Satırı Eşlemesi

| ActivityType | Gösterim Mesajı | Tabler İkonu | Renk Sınıfı |
|---|---|---|---|
| `Created` | "Ürün oluşturuldu" | `ti-circle-plus` | `text-blue` |
| `Updated` | "Ürün güncellendi" | `ti-pencil` | `text-blue` |
| `MappingValidated` | "[MP]: Kategori eşleme doğrulandı" | `ti-check` | `text-green` |
| `PublishRequested` | "[MP]: Yayın isteği gönderildi" | `ti-send` | `text-blue` |
| `PublishSent` | "[MP]: Pazaryerine iletildi — Batch: #[ref]" | `ti-upload` | `text-azure` |
| `BatchCompleted` | "[MP]: Batch tamamlandı" | `ti-checks` | `text-green` |
| `BatchFailed` | "[MP]: Batch başarısız" | `ti-circle-x` | `text-red` |
| `Approved` | "[MP]: Ürün onaylandı" | `ti-shield-check` | `text-green` |
| `Rejected` | "[MP]: Ürün reddedildi" | `ti-shield-x` | `text-red` |
| `Archived` | "[MP]: Ürün arşivlendi" | `ti-archive` | `text-muted` |
| `StockUpdated` | "Stok güncellendi: [eski]→[yeni]" | `ti-package` | `text-blue` |
| `PriceUpdated` | "Fiyat güncellendi: [eski TL]→[yeni TL]" | `ti-currency-lira` | `text-blue` |
| `ContentUpdated` | "İçerik güncellendi: [alan adı]" | `ti-file-text` | `text-blue` |
| `ImageUpdated` | "Görsel güncellendi" | `ti-photo` | `text-blue` |
| `Deleted` | "Ürün silindi" | `ti-trash` | `text-red` |

**Not:** `[MP]` = `MarketplaceName` field'ı doluysa göster, boşsa gösterme.

### 3.2 Ek Veri Kaynakları (Timeline'la Birleştirme Stratejisi)

Faz 1'de tüm veriler `ProductActivityLog` tablosundan gelir — mevcut BackgroundService'ler zaten bu tabloya yazıyor. Ek kaynaklar ayrı sekmelerde sunulur:

| Kaynak | Sekme | Faz |
|--------|-------|-----|
| `ProductActivityLog` | "Aktivite" sekmesi | Faz 1 |
| `OrderItem` → `Order` | "Siparişler" sekmesi | Faz 1 |
| `StockMovement` | "Stok Hareketleri" sekmesi | Faz 1 (okuma only) |
| Fiyat tarihi | (henüz yok) | Faz 2 |

---

## 4. UI Bölümleri

### 4.1 Sayfa Mimarisi

**Yaklaşım: Mevcut `/products/{id}` sayfasına sekme sistemi eklenir.**

Neden sekme (alternatif: ayrı sayfa, drawer):
- `/marketplace/matching/{id}` zaten sekme mantığıyla çalışıyor — pattern tutarlı olur
- Esnaf aynı URL'de kalır, geçmişe/yeniye tıklarken context kaybetmez
- HTMX `hx-get` + `hx-target` ile sekme içeriği lazy yüklenir → sayfa açılış süresi artmaz
- Tabler'ın `nav nav-tabs` bileşeni hazır

### 4.2 Bölüm A: Pazaryeri Durum Kartları

Sayfanın üst kısmında, mevcut ürün başlık + butonlar satırının hemen altına eklenir:

```
┌──────────────────────────────────────────────────────────────────────┐
│  Ürün: Nike Air Max 90    [Düzenle]  [Varyantlar]  [Yayınla]  [...]  │
├──────────────┬──────────────┬──────────────┬─────────────────────────┤
│  TRENDYOL    │   N11        │  HEPSİBURADA  │  STOREFRONT             │
│  ✅ Onaylı  │  🟡 Bekliyor │  ❌ Hata      │  ✅ Yayında             │
│  2 saat önce │  5 gün önce  │  Batch hata   │                         │
└──────────────┴──────────────┴──────────────┴─────────────────────────┘
```

**Kart içeriği (Tabler `card` + status-dot):**
- Pazaryeri adı + logo/ikon
- `ProductMarketplaceStatus` → badge rengi: Published=yeşil, Pending=sarı, Failed/Rejected=kırmızı
- `LastSyncedAt` — "X dk/saat/gün önce" formatı
- `StatusMessage` varsa → Tabler `tooltip` ile hover'da göster
- Karta tıklanınca → Aktivite sekmesi açılır + o pazaryerinin filtresi aktive olur (`hx-get="/products/{id}/activity?marketplace=trendyol"`)

Sadece ürünün eklendiği pazaryerleri gösterilir (`ProductMarketplace` kayıtları); eklenmemiş olanlar kartda çıkmaz.

### 4.3 Bölüm B: Sekmeler

```
[Genel ▶] [Aktivite] [Siparişler] [Stok Hareketleri]
```

- **Genel** (varsayılan): Mevcut ürün detay içeriği (StockCode, Brand, Category, vb.) — **değişmez**
- **Aktivite**: `ProductActivityLog` timeline + filtre
- **Siparişler**: Bu ürünü içeren son 20 sipariş
- **Stok Hareketleri**: `StockMovement` kayıtları variant bazlı

Sekme başlıklarına badge eklenir (Aktivite sekmesine son 7 günlük hata sayısı varsa kırmızı badge):
```
[Aktivite  🔴 2]
```

### 4.4 Bölüm C: Aktivite Timeline Sekmesi

```
┌────────────────────────────────────────────────────────────────────┐
│ Filtrele:  [Pazaryeri ▼]  [Tip ▼]  [Durum ▼]  [📅 Tarih]  [Ara]  │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  ✅  10 Haz 14:32  ·  Trendyol                                    │
│      BATCH TAMAMLANDI                                              │
│      Batch #abc123 başarıyla işlendi. İçerik onaylandı.           │
│                                                                    │
│  ✅  10 Haz 09:15  ·  Trendyol                                    │
│      ONAYLANDI                                                     │
│      Ürün Trendyol tarafından onaylandı. ContentId: 98765432      │
│                                                                    │
│  ❌  09 Haz 18:00  ·  N11                                         │
│      BATCH BAŞARISIZ                                               │
│      Hata: Kategori özelliği eksik (Renk zorunlu).               │
│      [▼ Detayı Göster]                                            │
│        → detail: {"missingField":"Renk","categoryId":42}          │
│                                                                    │
│                    [⬇ Daha Fazla Yükle]                           │
└────────────────────────────────────────────────────────────────────┘
```

**HTMX akışı:**
1. Sekme tıklanınca `hx-get="/products/{id}/activity" hx-target="#activity-panel" hx-trigger="click once"`
2. Filtre değişince `hx-get="/products/{id}/activity?{params}" hx-push-url="true"` → URL güncellenir (paylaşılabilir)
3. "Daha Fazla Yükle" butonu cursor-based pagination: `?cursor={lastCreatedAt}&page=2`
4. `Detail` field'ı dolu satırlarda `hx-get="/products/{id}/activity/{logId}/detail"` accordion

**Filtre elemanları (Tabler form bileşenleri):**
- Pazaryeri: Tom Select (çoklu seçim) — `ProductMarketplace` kayıtlarından doldurulur
- Tip: Tom Select (16 ActivityType) — `ActivityType` enum'dan i18n label'larla
- Durum: Radio button group — Info / Success / Warning / Error
- Tarih aralığı: Flatpickr range picker (TR locale)

### 4.5 Bölüm D: Siparişler Sekmesi

```
┌──────────────────────────────────────────────────────────────────┐
│  Sipariş No     │ Tarih    │ Platform   │ Variant        │ Adet  │
│  TY-987654321   │ 10 Haz   │ Trendyol   │ Kırmızı / 42   │ 2    │
│  N11-123456     │ 08 Haz   │ N11        │ Mavi / 38      │ 1    │
│  POS-00045      │ 05 Haz   │ Mağaza     │ Beyaz / 40     │ 3    │
└──────────────────────────────────────────────────────────────────┘
```

- Satıra tıklanınca `/orders/{orderId}` sipariş detayına gider
- Yalnızca son 20 sipariş gösterilir (pagination yok — Faz 1 kapsam dışı)

### 4.6 Bölüm E: Stok Hareketleri Sekmesi

`StockMovement` tablosundaki kayıtlar, variant bazlı gruplandırılmış:

```
┌──────────────────────────────────────────────────────────────────┐
│  Variant: Kırmızı / Beden 42                                     │
│  10 Haz 14:30  Pazaryeri Satışı   -2    98 → 96   TY-987654321  │
│  08 Haz 09:00  Manuel Düzeltme    +10   88 → 98   admin         │
│                                                                  │
│  Variant: Mavi / Beden 38                                        │
│  08 Haz 11:00  Satış              -1    45 → 44   N11-123456    │
└──────────────────────────────────────────────────────────────────┘
```

---

## 5. Backend Gap'leri ve Yapılacaklar

### 5.1 `ProductController.Detail` Değişikliği

**Mevcut:** Sadece `IProductService.GetProductDetailById(id)` çağırıyor.

**Eklenecek (Faz 1, direkt DI — DB review gerekmez, by-PK lookup):**
```csharp
// Pazaryeri durum kartları için — ProductId ile by-PK
var marketplaceStatuses = await productMarketplaceService.GetByProductIdAsync(id);
```

Aktivite timeline, sipariş ve stok verileri HTMX lazy endpoint'lerden gelir — `Detail` action'a eklenMEZ.

**ViewModel genişletmesi:**
```csharp
public class ProductDetailVm
{
    // mevcut alanlar...
    public List<ProductMarketplaceStatusDto> MarketplaceStatuses { get; set; } = [];
}
```

### 5.2 Yeni HTMX Endpoint'ler — `ProductActivityController.cs`

Ayrı controller (tek sorumluluk):

```
GET /products/{id}/activity
    ?marketplace=trendyol        (opsiyonel, çoklu: marketplace=trendyol&marketplace=n11)
    ?activityType=BatchFailed    (opsiyonel, çoklu)
    ?status=Error                (opsiyonel)
    ?from=2026-06-01             (opsiyonel)
    ?to=2026-06-10               (opsiyonel)
    ?cursor={lastCreatedAt}      (pagination)
→ Partial: _ActivityTimeline.cshtml
→ DB Master review: HAYIR (IX_ProductActivityLogs_ProductId_CreatedAt index mevcut)

GET /products/{id}/activity/{logId}/detail
→ Partial: _ActivityLogDetail.cshtml (accordion expand)
→ DB Master review: HAYIR (by-PK)

GET /products/{id}/orders
→ Partial: _ProductOrders.cshtml
→ DB Master review: EVET — Orders büyük tablo, OrderItem.ProductId index kontrolü şart

GET /products/{id}/stock-movements
→ Partial: _ProductStockMovements.cshtml
→ DB Master review: EVET — StockMovement büyük tablo, variant bazlı gruplama
```

### 5.3 `IProductActivityLogger` Genişletmesi

Mevcut imza yeterli değil. Yeni overload eklenecek (mevcut `GetTimelineAsync` kırılmayacak — yeni overload):

```csharp
// Mevcut (değişmez):
Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(Guid productId, int limit = 50);

// Yeni eklenecek:
Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(
    Guid productId,
    ProductActivityTimelineFilter filter,
    int page = 1,
    int pageSize = 20);
```

`ProductActivityTimelineFilter` DTO (yeni):
```csharp
public record ProductActivityTimelineFilter(
    List<string>? MarketplaceNames,
    List<ProductActivityType>? ActivityTypes,
    List<ProductActivityStatus>? Statuses,
    DateTimeOffset? From,
    DateTimeOffset? To,
    DateTimeOffset? Cursor  // cursor-based pagination
);
```

### 5.4 Sipariş Referans Query (DB Master Review Şart)

```
OrderItem JOIN ProductVariant WHERE ProductVariant.ProductId = {id}
JOIN Order ON Order.Id = OrderItem.OrderId
ORDER BY Order.CreatedAt DESC
LIMIT 20
```

**Neden DB Master review şart:** Orders büyük tablo, `OrderItem` üzerinde `ProductId` veya `ProductVariantId` index durumu bilinmiyor. Seq scan riski var. DB Master `EXPLAIN ANALYZE` ile doğrulamalı.

---

## 6. İş Bölümü

### Designer (UI Sorumluluğu)

| # | İş | Tabler Referans | Öncelik |
|---|---|---|---|
| D-1 | Pazaryeri durum kartları (badge, status-dot, tooltip) | `tabler.io/docs/ui/cards` + `status-dot` + `dropdowns` | Faz 1 |
| D-2 | Aktivite timeline satır tasarımı (ikon, renk, accordion expand) | `tabler.io/docs/ui/timeline` | Faz 1 |
| D-3 | Filtre bar (Tom Select + Flatpickr range + radio) | Mevcut form pattern (ExportColumnSelector referans) | Faz 1 |
| D-4 | Sekme yapısı entegrasyonu (nav-tabs + HTMX lazy) | `tabler.io/docs/ui/nav` | Faz 1 |
| D-5 | Sipariş ve stok hareketi tabloları | Mevcut Tabler table pattern | Faz 1 |
| D-6 | (Faz 2) Fiyat geçmiş grafiği | Tabler charts | Faz 2 |

### SWE (Backend Sorumluluğu)

| # | İş | DB Master Review? | Öncelik |
|---|---|---|---|
| S-1 | `ProductController.Detail` → marketplace durumları yükle | Hayır (by-PK) | Faz 1 |
| S-2 | `ProductActivityController` HTMX endpoint'leri | Hayır (index var) | Faz 1 |
| S-3 | `IProductActivityLogger.GetTimelineAsync` filtre+pagination overload | Hayır | Faz 1 |
| S-4 | Sipariş referans endpoint (`/products/{id}/orders`) | **EVET** | Faz 1 |
| S-5 | Stok hareketi endpoint (`/products/{id}/stock-movements`) | **EVET** | Faz 1 |
| S-6 | `ProductDetailVm` + `ProductMarketplaceStatusDto` ViewModel'leri | Hayır | Faz 1 |
| S-7 | (Faz 2) Price history entity + migration | **EVET** | Faz 2 |
| S-8 | (Faz 2) `Product.ChangeHistory` JSONB doldurma mekanizması | Hayır | Faz 2 |

**TDD Sırası (ZORUNLU — CLAUDE.md kuralı):**
1. RED: `ProductActivityController` filtre/pagination unit test
2. GREEN: `GetTimelineAsync` overload implement
3. RED: Sipariş referans integration test (OrderItem query)
4. GREEN: DB Master onayı sonrası implement
5. E2E: Playwright ile sekme tıklama → timeline yükleme akışı

---

## 7. Kabul Kriterleri

1. `/products/{id}` sayfası açılınca pazaryeri durum kartları görünür — her cart için Status badge + LastSyncedAt
2. Hata statüslü pazaryeri kartlarında `StatusMessage` tooltip ile hover'da görünür
3. Karta tıklanınca "Aktivite" sekmesi otomatik açılır + ilgili pazaryeri filtreli
4. "Aktivite" sekmesine tıklanınca HTMX partial yüklenir (spinner görünür → kayıtlar gelir)
5. Timeline satırları: zaman damgası + pazaryeri adı + ActivityType icon + mesaj + status rengi (yeşil/sarı/kırmızı)
6. `Detail` field'ı dolu satırlarda accordion genişleyebilir
7. Filtreler (pazaryeri / tip / durum / tarih) çalışır; URL güncellenir (paylaşılabilir)
8. "Daha Fazla Yükle" ile cursor-based pagination çalışır
9. "Siparişler" sekmesi bu ürünü içeren son 20 siparişi gösterir; satıra tıklanınca sipariş detayına gider
10. "Stok Hareketleri" sekmesi variant bazlı `StockMovement` kayıtlarını gösterir
11. Sayfa açılış süresi etkilenmez — timeline HTMX lazy load (ölçüm: `Detail` action < 300ms ekstra)
12. Mobile responsive (Tabler grid)

---

## 8. Manual Test Adımları

> **Önkoşul:** Sistemde en az bir ürün olmalı, o ürün için Trendyol sync yapılmış olmalı, en az bir sipariş içermeli.  
> **Test kullanıcısı:** admin / 123456789  
> **URL:** http://localhost:5100

```
BÖLÜM A: Pazaryeri Durum Kartları
──────────────────────────────────
1. /products listesine git → Trendyol'a eklenmiş bir ürünün "Detay" butonuna tıkla
   BEKLENEN: Sayfanın üstünde pazaryeri durum kartları görünür
             Trendyol kartı → Status badge + LastSyncedAt tarihi
             Eklenmemiş pazaryerleri kartda görünmez

2. Hata statüslü bir Trendyol kartına hover et
   BEKLENEN: StatusMessage tooltip görünür ("Hata: Kategori zorunlu" gibi)

3. Trendyol kartına tıkla
   BEKLENEN: "Aktivite" sekmesi aktive olur + Trendyol filtresi seçili gelir

BÖLÜM B: Aktivite Timeline
──────────────────────────────────
4. "Aktivite" sekmesine tıkla
   BEKLENEN: Loading spinner → timeline yüklenir (HTMX)
             En az bir Trendyol eventi görünür

5. Her satırda: tarih/saat, pazaryeri adı, ikon, mesaj, status rengi olduğunu doğrula
   BEKLENEN: Onaylanan event → yeşil, reddedilen → kırmızı, bilgi → mavi

6. Hata satırındaki "Detayı Göster" butonuna tıkla
   BEKLENEN: Accordion açılır, `Detail` JSON/metin görünür

7. Filtre: Pazaryeri → sadece "N11" seç
   BEKLENEN: Sadece N11 eventleri listelenir + URL güncellenir

8. Filtre: Durum → "Hata" seç
   BEKLENEN: Sadece kırmızı/Error statüslü eventler görünür

9. Filtre: Tarih aralığı seç (bugün dahil)
   BEKLENEN: Aralık dışındaki eventler kaybolur

10. "Daha Fazla Yükle" butonuna tıkla (en az 20+ event varsa)
    BEKLENEN: Sonraki 20 event listenin altına eklenir (sayfa yenilenmez)

11. Aktivite sekmesi URL'ini kopyala, yeni sekmede aç
    BEKLENEN: Filtreler korunmuş gelir (URL parametreleri çalışıyor)

BÖLÜM C: Siparişler Sekmesi
──────────────────────────────────
12. "Siparişler" sekmesine tıkla
    BEKLENEN: Bu ürünü içeren sipariş listesi görünür
              Sipariş No, Tarih, Platform (Trendyol/N11/Mağaza), Variant, Adet kolonları var

13. Bir sipariş satırına tıkla
    BEKLENEN: /orders/{id} sipariş detay sayfasına yönlendirilir

BÖLÜM D: Stok Hareketleri Sekmesi
──────────────────────────────────
14. "Stok Hareketleri" sekmesine tıkla
    BEKLENEN: StockMovement kayıtları variant bazlı listelenir
              Miktar, Önceki Stok, Sonraki Stok, Hareket Tipi görünür

BÖLÜM E: Performans
──────────────────────────────────
15. Chrome DevTools Network sekmesini aç
    /products/{id} sayfasını yükle (Aktivite sekmesi pasif)
    BEKLENEN: Aktivite endpoint'i çağrılmamış (lazy load)
              Sayfa yüklenme süresi ≤ mevcut süreden +300ms

16. "Aktivite" sekmesine tıkla
    BEKLENEN: /products/{id}/activity isteği görünür (HTMX partial)
              Response < 500ms
```

---

## 9. Faz Planı

### Faz 1 (Bu Sprint)
- Pazaryeri durum kartları
- Aktivite sekmesi (timeline + filtre + pagination)
- Siparişler sekmesi (DB Master review sonrası)
- Stok hareketleri sekmesi (DB Master review sonrası)

### Faz 2 (Gelecek Sprint)
- Fiyat geçmiş tablosu ve grafiği
- `Product.ChangeHistory` JSONB doldurma (ProductManager Update metodunda)
- Timeline'da fiyat değişim eventleri

---

## 10. Bağımlılıklar

| # | Bağımlılık | Kim | Durum |
|---|---|---|---|
| 1 | Sipariş endpoint için `OrderItem.ProductId` index kontrolü | DB Master | Bekliyor |
| 2 | `StockMovement` variant query performansı | DB Master | Bekliyor |
| 3 | Roles-race infra bug fix (integration testleri için) | SWE | Bekliyor |
