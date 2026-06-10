# Ürün 360° — ViewModel Sözleşmesi (Designer → SWE-Ahmet)

> Bu dosya tasarımın beklediği **alan adlarını** tanımlar. SWE bu sözleşmeye göre VM/DTO üretir;
> alan adları gerçek entity'lerle hizalandı (`ProductMarketplace`, `ProductActivityLog`,
> `StockMovement`, `MarketplaceSyncItemDto`). Bir alan adı değişecekse Designer'a haber ver,
> partial'lardaki referansları birlikte güncelleyelim.

---

## 1. `ProductDetailVm` (mevcut DTO'ya eklenecek alanlar)

```csharp
// ProductController.Detail action'ında doldurulur (by-PK, DB review gerekmez)
public List<MarketplaceStatusVm> MarketplaceStatuses { get; set; } = [];

// Esnaf e-ticaret kullanmıyorsa / ürün hiç gönderilmediyse FALSE → zarif boş durum
public bool HasEcommerce => MarketplaceStatuses.Count > 0;
```

## 2. `MarketplaceStatusVm` (pazaryeri durum kartı — Bölüm A)

```csharp
public sealed class MarketplaceStatusVm
{
    public int MarketPlaceId { get; init; }
    public string Name { get; init; } = "";              // "Trendyol", "Zekids Mağaza"
    public MarketplaceSyncState State { get; init; }     // mevcut enum (NeverSynced..Removed)
    public DateTimeOffset? LastSyncedAt { get; init; }   // "X saat önce" → partial hesaplar
    public string? StatusMessage { get; init; }          // hata tooltip'i (State=Failed/Rejected)
    public string? ExternalRef { get; init; }            // ContentId / ExternalProductId — kart altı
    public string? StorefrontUrl { get; init; }          // Zekids mağaza linki (varsa)
}
```

**Kart renk/ikon eşlemesi partial içinde** (`StatusVisual` helper) — SWE'nin map'lemesine gerek yok:
`MarketplaceSyncState` → (status-dot rengi, badge metni, card-status-start rengi).

## 3. Aktivite Timeline (Bölüm C — HTMX lazy `_ActivityTimeline.cshtml`)

Mevcut `ProductActivityLog` entity birebir kullanılır. Filtre için spec §5.3'teki
`ProductActivityTimelineFilter` + cursor pagination. Partial'a geçen model:

```csharp
public sealed class ActivityTimelineVm
{
    public List<ProductActivityLog> Entries { get; init; } = [];  // CreatedAt DESC
    public DateTimeOffset? NextCursor { get; init; }              // null → "Daha Fazla" gizlenir
    public ActivityFilterOptionsVm FilterOptions { get; init; } = new(); // dropdown doldurma
}

public sealed class ActivityFilterOptionsVm
{
    // Tom Select pazaryeri seçeneği — ürünün eklendiği pazaryerleri
    public List<(string Value, string Label)> Marketplaces { get; init; } = [];
}
```

> Timeline satırı ikon+renk eşlemesi (`ProductActivityType` → ikon/renk) partial içinde
> `ActivityVisual` helper'ında (spec §3.1 tablosu). SWE map yazmaz.

## 4. Siparişler (Bölüm D — HTMX lazy `_ProductOrders.cshtml`)

```csharp
public sealed record ProductOrderRowVm(
    Guid OrderId,
    string OrderNumber,        // "TY-987654321", "POS-00045"
    DateTimeOffset OrderDate,
    string PlatformName,       // "Trendyol" / "N11" / "Mağaza"
    string PlatformBadgeClass, // partial hesaplayabilir; ya da SWE "Mağaza"→secondary verir
    string VariantLabel,       // "Krem / 6-9 Ay"
    int Quantity);
```
Query: `OrderItem JOIN ProductVariant WHERE ProductId=@id JOIN Order ORDER BY Order.CreatedAt DESC LIMIT 20` (**DB Master review şart**).

## 5. Stok Hareketleri (Bölüm E — HTMX lazy `_ProductStockMovements.cshtml`)

Variant bazlı gruplanmış `StockMovement`:

```csharp
public sealed record VariantStockMovementsVm(
    Guid ProductVariantId,
    string VariantLabel,       // "Krem / 6-9 Ay"
    int CurrentStock,
    List<StockMovementRowVm> Movements);

public sealed record StockMovementRowVm(
    DateTimeOffset CreatedAt,
    StockMovementType Type,    // mevcut enum
    int Quantity,              // ± gösterimi partial'da
    int StockBefore,
    int StockAfter,
    string? Reference);        // ReferenceId / Note
```
(**DB Master review şart** — StockMovement büyük tablo.)

---

## 6. HTMX Endpoint Beklentileri (SWE — `ProductActivityController`)

| Endpoint | Partial | Tetikleyici |
|---|---|---|
| `GET /products/{id}/activity?marketplace=&activityType=&status=&from=&to=&cursor=` | `_ActivityTimeline` | sekme `click once` + filtre `change` + "Daha Fazla" |
| `GET /products/{id}/orders` | `_ProductOrders` | sekme `click once` |
| `GET /products/{id}/stock-movements` | `_ProductStockMovements` | sekme `click once` |

Sekme paneli iskeleti `_Product360.cshtml` içinde; her sekme `hx-get ... hx-trigger="click once"` ile lazy.
Pazaryeri kartına tıklayınca → Aktivite sekmesi açılır + `?marketplace={id}` filtresi (spec §4.2, KK-3).
