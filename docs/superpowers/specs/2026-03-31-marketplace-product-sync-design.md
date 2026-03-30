# Marketplace Product Sync Pages — Design Spec

**Date:** 2026-03-31
**Status:** Approved for implementation

---

## 1. Overview

Two complementary page systems for managing marketplace product publishing:

1. **Product Sync Detail Page** (`/products/{id}/sync`) — per-product, cross-marketplace status view with Trendyol-specific actions.
2. **Bulk Product Management Page** (`/marketplace/sync/products`) — multi-product, single-marketplace operations.

### What Already Exists

Before implementing the new pages, note what is already in place:

| Existing Asset | Location | Role |
|---|---|---|
| `ProductSyncPage.razor[.cs]` | `Features/MarketplaceSync/ProductSync/` | `@page "/marketplace/matching"` — lists products with sync state for Trendyol |
| `ProductSyncDetailPage.razor[.cs]` | `Features/MarketplaceSync/ProductSync/` | `@page "/marketplace/matching/{Id:guid}"` — per-product sync cards + activity timeline |
| `ProductActivityTimeline.razor[.cs]` | same folder | Timeline component, already wired to `IProductActivityLogger` |
| `ProductSyncSummaryCards.razor[.cs]` | same folder | Top summary cards |
| `IProductSyncManager` | `Business/Abstract/` | `GetProductSyncDetailAsync`, `SyncProductAsync`, etc. |
| `IProductActivityLogger` | `Business/Abstract/` | `GetTimelineAsync(productId, limit)` |
| `IMarketplaceOverrideManager` | `Business/Abstract/` | `GetOverridesAsync`, `SaveOverridesAsync`, `SaveOverridesAndPublishAsync` |
| `ITrendyolProductService` | `Business/Abstract/` | `PublishProductAsync`, `UpdateUnapprovedProductAsync`, `UpdateApprovedContentAsync` |

The **new work** extends these foundations with:
- A dedicated, action-rich product sync page accessible from `ProductDetail`
- Trendyol-specific tab with content/price/delete/sync actions
- Faz 1 placeholder tabs for Hepsiburada and N11
- A new bulk management page reachable from NavMenu

---

## 2. Domain Model Reference

### `ProductMarketplace` (key fields)

```
ProductId, MarketPlaceId, Status (Pending/Published/Failed/Rejected)
BatchRequestId, ExternalProductId, StatusMessage, LastSyncedAt
ContentId (long?) — required by Trendyol for approved product updates
IsApproved (bool?) — Trendyol approval state
IsArchived (bool?) — Trendyol archive state
TitleOverride, DescriptionOverride
VariantOverrides → List<ProductVariantMarketplaceOverride>
```

### `ProductVariantMarketplaceOverride`

```
ProductVariantId, ListPriceOverride (money?), SalePriceOverride (money?)
```

### `ProductActivityLog`

```
ProductId, ActivityType (enum), Status (Info/Success/Warning/Error)
Message, Detail, MarketplaceName (nullable), ReferenceId
CreatedAt (from BaseEntity)
```

### Trendyol Content Status (derived logic)

| IsApproved | IsArchived | Status | Label |
|---|---|---|---|
| null | — | Pending | Beklemede |
| false | — | Rejected | Reddedildi |
| true | false/null | Approved | Onaylı |
| true | true | Archived | Arşivlendi |

A "Locked" state is not a DB field — treat any `Processing` sync state as Locked (action buttons disabled).

---

## 3. Page 1 — Product Sync Detail (`/products/{id}/sync`)

### 3.1 Route & Authorization

```
@page "/products/{Id:guid}/sync"
@rendermode InteractiveServer
@attribute [Authorize(Policy = AppPermissions.Marketplace.View)]
```

Edit-level actions (content update, price/stock update, delete, sync) require `AppPermissions.Marketplace.Edit`.

### 3.2 Data Loading

On `OnInitializedAsync`:

1. `IProductSyncManager.GetProductSyncDetailAsync(Id)` → `ProductSyncDetailDto` (has Title, StockCode, BrandName, all marketplace `MarketplaceSyncItemDto`s)
2. `IProductActivityLogger.GetTimelineAsync(Id, limit: 50)` → `List<ProductActivityLog>`
3. For Trendyol tab (MarketPlaceId = 1): `IMarketplaceOverrideManager.GetOverridesAsync(Id, 1)` → `MarketplaceOverrideDetailDto`
4. Load `ProductMarketplace` records as needed for ContentId/IsApproved — this requires either enriching `ProductSyncDetailDto` or a direct DB query via a new method `IProductSyncManager.GetMarketplaceStatusAsync(productId, marketPlaceId)` (see §6.1).

### 3.3 Layout

```
[Back button] [Product title] [StockCode chip] [Brand chip] [Category chip]

[MarketplaceStatusCards component — one card per marketplace]

[MudTabs — one tab per marketplace that has a record]
    [Trendyol] [N11] [Hepsiburada] [Amazon — future]

[ProductActivityTimeline component]
```

The top back button navigates to `/products/{Id}`.

### 3.4 `MarketplaceStatusCards` Component

**Input parameters:**
- `IReadOnlyList<MarketplaceSyncItemDto> Marketplaces`
- `bool IsLoading`

**Per-card display:**
- Marketplace logo (`MarketplaceLogo` component, already exists)
- Status chip: color-coded by `MarketplaceSyncState` (use `ProductSyncPage.GetSyncStateColor/Label` static helpers, already exist)
- External ID (if available — comes from `ExternalProductId` on `ProductMarketplace`)
- Last sync time
- Clicking the card activates the corresponding tab

**Note:** `MarketplaceSyncItemDto` currently lacks `ExternalProductId`. This field needs to be added to the record (see §6.1).

### 3.5 `ProductActivityTimeline` Component

Already implemented. Reuse as-is. Parameters: `ProductId` (Guid).

### 3.6 Marketplace Tabs

Tabs are built from the list of `ProductMarketplace` records. Only show a tab if a `ProductMarketplace` record exists for that marketplace (Published, Pending, Failed, or Rejected — any non-null record).

Supported tab components:
- `TrendyolProductDetail` (Faz 1: full actions)
- `N11ProductDetail` (Faz 1: status only)
- `HepsiburadaProductDetail` (Faz 1: status only)

### 3.7 `TrendyolProductDetail` Component

**Parameters:**
- `Guid ProductId`
- `MarketplaceSyncItemDto SyncItem`
- `MarketplaceOverrideDetailDto Overrides`
- `EventCallback OnSyncCompleted` (refreshes parent)

**Display sections:**

**Status Row:**
```
[Status chip: Approved / Pending / Rejected / Archived]
[ContentId label + value]
[ExternalProductId label + value]
[BatchRequestId label + value (truncated, copy button)]
[LastSyncedAt formatted]
```

**Override Panel (read-only display):**
```
Title override: [value or "—" if not set] vs Original: [product.Title]
Description override: [value or "—"] vs Original: [first 100 chars]
Variant price overrides: table — Barcode | List Price Override | Sale Price Override
```

**Action Buttons** (require `AppPermissions.Marketplace.Edit`):

| Button | Label | Behavior | Condition |
|---|---|---|---|
| İçerik Güncelle | Edit content | Opens `ProductContentUpdateDialog` | Always shown (disabled if Processing) |
| Fiyat/Stok Güncelle | Edit price/stock | Opens `ProductPriceStockDialog` | Always shown (disabled if Processing) |
| Sil | Delete | Confirmation → calls Trendyol DELETE API via new method | Only if IsApproved == true or has ExternalProductId |
| Sync | Re-publish | Calls `IProductSyncManager.SyncProductAsync(productId, 1)` | Shows if any state |

**Trendyol-specific sync logic (buttons map to existing service):**

- If `IsApproved == true`: use `UpdateApprovedContentAsync` for content changes
- If `IsApproved == false` or null: use `UpdateUnapprovedProductAsync`
- For "Sync" button: `IProductSyncManager.SyncProductAsync` (already handles routing)

### 3.8 `ProductContentUpdateDialog`

**Purpose:** Edit title override, description override, and images for a specific marketplace.

**Parameters:**
- `Guid ProductId`
- `int MarketPlaceId`
- `MarketplaceOverrideDetailDto CurrentOverrides`

**Fields:**
- Title Override (MudTextField, max 200)
- Description Override (MudTextField, lines=5, max 2000)
- Images — note: image management is handled separately via `ProductImageUploadDialog`. This dialog does not include image editing. Add a note/link to the product detail image section.

**On Save:** calls `IMarketplaceOverrideManager.SaveOverridesAsync(dto)`. Returns a boolean result to indicate if the user also wants to sync immediately (checkbox: "Kaydet ve Senkronize Et"). If checked, calls `SaveOverridesAndPublishAsync` instead.

**Validation:**
- Title override: if provided, must be 3–200 chars
- Description override: if provided, must be 20–2000 chars

### 3.9 `ProductPriceStockDialog`

**Purpose:** Edit per-variant price overrides for a specific marketplace.

**Parameters:**
- `Guid ProductId`
- `int MarketPlaceId`
- `MarketplaceOverrideDetailDto CurrentOverrides`

**Display:** Table of variants with:

| Varyant | Orijinal Liste | Orijinal Satış | Override Liste | Override Satış |
|---|---|---|---|---|
| [barcode/label] | [original] | [original] | [editable] | [editable] |

Overrides are optional. Empty = use original. Show original value as placeholder text.

**On Save:** calls `IMarketplaceOverrideManager.SaveOverridesAsync(dto)`. Same "Kaydet ve Senkronize Et" checkbox pattern.

### 3.10 `N11ProductDetail` — Faz 1

Status-only placeholder:
- Status chip (from `MarketplaceSyncItemDto.SyncState`)
- Last sync time
- StatusMessage if present
- "Sync" button (calls `IProductSyncManager.SyncProductAsync(productId, 2)`)
- "İçerik yönetimi yakında..." info banner

### 3.11 `HepsiburadaProductDetail` — Faz 1

Same as N11 pattern but for MarketPlaceId=3. "Sync" button calls `SyncProductAsync(productId, 3)`.

### 3.12 Entry Point from `ProductDetail`

In `ProductDetail.razor`, the existing sync chip currently calls `SyncToMarketplace` directly. Add a navigation chip/button:

```razor
@if (_syncStatus is not null && _syncStatus.State != MarketplaceSyncState.NeverSynced)
{
    // Existing chip remains for quick sync
    // Add new button in the İşlemler menu:
    <MudMenuItem OnClick="GoToSyncPage" Icon="@Icons.Material.Filled.Hub">
        Pazaryeri Detayı
    </MudMenuItem>
}
```

`GoToSyncPage` navigates to `/products/{Id}/sync`.

---

## 4. Page 2 — Bulk Product Management (`/marketplace/sync/products`)

### 4.1 Route & Authorization

```
@page "/marketplace/sync/products"
@rendermode InteractiveServer
@attribute [Authorize(Policy = AppPermissions.Marketplace.View)]
```

Bulk action buttons (update, delete, sync) require `AppPermissions.Marketplace.Edit`.

### 4.2 Relationship to Existing `ProductSyncPage`

The existing `ProductSyncPage` at `/marketplace/matching` serves a similar function but:
- Is hardcoded to Trendyol (filtered by single marketplace context)
- Has no multi-select or bulk actions
- Navigates to the existing detail page at `/marketplace/matching/{id}`, not the new sync page

The new `BulkProductSyncPage` is **a new page** — it does not replace `ProductSyncPage`. Both will coexist. The new page adds:
- Marketplace selector dropdown (multi-marketplace awareness)
- Multi-select checkboxes
- Bulk actions toolbar
- Date range filter
- Row click navigates to `/products/{id}/sync`

### 4.3 Data Loading

Uses `IProductSyncManager.GetProductSyncListAsync(marketPlaceId, stateFilter, searchKey, pageIndex, pageSize)` — already exists.

Additionally needs date range filter support — the existing method signature does not include date range. This is a Faz 2 enhancement (see §6.2). Faz 1: skip date range or add it as client-side filter on loaded page.

### 4.4 Layout

```
[Page title: "Toplu Ürün Yönetimi"]

[Filter Row]
  [Marketplace selector: Trendyol / N11 / Hepsiburada dropdown]
  [Status filter: Tümü / Beklemede / Onaylı / Reddedildi / Başarısız]
  [Search: ürün adı veya barkod]
  [Date range: başlangıç — bitiş — Faz 2]

[Bulk Actions Toolbar — visible when rows selected]
  [N seçili] [Seçilenleri Güncelle] [Seçilenleri Sil] [Toplu Sync]

[MudDataGrid with multi-select]
  Columns: checkbox | Stok Kodu | Ürün Adı | Marka | Kategori | Varyant | Durum | Son Sync | Aksiyonlar

[Pagination]
```

### 4.5 Marketplace Selector

Loads all `MarketPlace` entities from DB at page init. Default selection: Trendyol (Id=1).

Changing marketplace reloads the grid.

### 4.6 Grid Columns Detail

| Column | Source | Notes |
|---|---|---|
| (checkbox) | MudDataGrid multiselect | `MultiSelection="true"` |
| Stok Kodu | `ProductSyncListItemDto.StockCode` | |
| Ürün Adı | `ProductSyncListItemDto.Title` | |
| Marka | `ProductSyncListItemDto.BrandName` | |
| Kategori | `ProductSyncListItemDto.CategoryName` | |
| Varyant | `ProductSyncListItemDto.VariantCount` | chip |
| Durum | `ProductSyncListItemDto.SyncState` | color chip |
| Son Sync | `ProductSyncListItemDto.LastSyncedAt` | formatted |
| Aksiyonlar | inline | Sync / Retry / Detail buttons |

Row click navigates to `/products/{ProductId}/sync`.

### 4.7 Bulk Actions

**Seçilenleri Güncelle:** iterates selected product IDs, calls `IProductSyncManager.SyncProductAsync(id, selectedMarketPlaceId)` for each. Shows progress `MudProgressLinear`. Reports per-item result in a summary snackbar.

**Seçilenleri Sil:** confirmation dialog → calls a new `ITrendyolProductService.DeleteProductAsync(productId)` or equivalent per-marketplace delete (Faz 2 for non-Trendyol). For Faz 1, only Trendyol delete is wired; others show "not supported" toast.

**Toplu Sync:** same as "Seçilenleri Güncelle" — calls `SyncProductAsync` for all selected. Functionally identical in Faz 1 (may diverge in Faz 2 when "Güncelle" means content-only and "Sync" means full re-publish).

**Execution pattern:** do NOT use `Task.WhenAll` (shared DbContext risk per project rules). Execute sequentially with a small progress counter shown to the user.

### 4.8 Status Filter Values

| Label | `MarketplaceSyncState` value |
|---|---|
| Tümü | null (no filter) |
| Beklemede | `Waiting` |
| Onaylı/Yayında | `Synced` |
| Güncelleme Gerekiyor | `OutOfSync` |
| Başarısız | `Failed` |
| Reddedildi | `Rejected` |
| Senkronize Edilmedi | `NeverSynced` |

These map directly to `MarketplaceSyncState` enum which already drives the existing `ProductSyncPage`.

---

## 5. NavMenu Change

Add "Ürün Senkronizasyon" link to the existing "Senkronizasyon" nested group in the "Pazaryeri" section:

```razor
<MudNavGroup Title="Senkronizasyon" Icon="@Icons.Material.Filled.Sync" Expanded="@_syncExpanded">
    <PermissionGuardedNavLink ... Href="/marketplace/sync" ... Title="Genel Bakis" />
    <PermissionGuardedNavLink ... Href="/marketplace/sync/categories" ... Title="Kategori Eslestirme" />
    <PermissionGuardedNavLink ... Href="/marketplace/sync/attributes" ... Title="Ozellik Eslestirme" />
    <PermissionGuardedNavLink ... Href="/marketplace/sync/brands" ... Title="Marka Eslestirme" />
    <!-- NEW: -->
    <PermissionGuardedNavLink Permission="@AppPermissions.Marketplace.View"
                              Href="/marketplace/sync/products"
                              Icon="@Icons.Material.Filled.Inventory2"
                              Title="Urun Senkronizasyon" />
</MudNavGroup>
```

The `_syncExpanded` logic in `NavMenu.razor.cs` already triggers for all `marketplace/sync` routes via `relativePath.StartsWith("marketplace/sync")`, so no code change needed in the `.cs` file.

---

## 6. Business Layer Gaps (New Methods Needed)

### 6.1 `IProductSyncManager` — Add `GetMarketplaceStatusAsync`

The existing `ProductSyncDetailDto` and `MarketplaceSyncItemDto` lack:
- `ExternalProductId`
- `ContentId`
- `IsApproved`
- `IsArchived`

Options:
1. Enrich `MarketplaceSyncItemDto` to include these fields (preferred — avoids a new method)
2. Add `Task<IDataResult<ProductMarketplace>> GetMarketplaceStatusAsync(Guid productId, int marketPlaceId)` to the interface

**Decision:** Option 1 — extend `MarketplaceSyncItemDto` with the 4 missing fields. This is a non-breaking record change since it's a `sealed record` with positional parameters — use named params at all call sites.

### 6.2 `IProductSyncManager` — Date Range Filter (Faz 2)

`GetProductSyncListAsync` signature:
```csharp
Task<DataResult<Pageable<ProductSyncListItemDto>>> GetProductSyncListAsync(
    int marketPlaceId, MarketplaceSyncState? stateFilter, string searchKey, int pageIndex, int pageSize);
```

For Faz 2, extend to:
```csharp
Task<DataResult<Pageable<ProductSyncListItemDto>>> GetProductSyncListAsync(
    int marketPlaceId, MarketplaceSyncState? stateFilter, string searchKey,
    DateTimeOffset? from, DateTimeOffset? to,
    int pageIndex, int pageSize);
```

For Faz 1: implement `BulkProductSyncPage` with the existing 5-parameter signature. Date filter inputs are hidden/disabled in Faz 1.

### 6.3 Trendyol Delete — New Method

The `ITrendyolProductService` does not have a delete method. Trendyol API supports delete via batch update with items removed (or a dedicated DELETE endpoint — see `PRODUCT_API_V2.md`). Need:

```csharp
// In ITrendyolProductService:
Task<IResult> DeleteProductAsync(Guid productId);
```

Implementation: calls Trendyol DELETE endpoint, logs to `IProductActivityLogger`, updates `ProductMarketplace.Status`.

This is needed for the "Sil" button in `TrendyolProductDetail`. If not implemented, the button shows with a "coming soon" disabled state for Faz 1.

**Decision for Faz 1:** implement `DeleteProductAsync` as part of this work.

### 6.4 Marketplace Loader for BulkProductSyncPage

The page needs a list of all available marketplaces. Load directly from `IDbContextFactory<IntegrationDbContext>` via a new thin method, or via an existing service. Check: `IMarketplaceSyncManager` or a utility. For simplicity, inject `IDbContextFactory` into a new `IMarketplaceService.GetAllAsync()` method, or reuse any existing interface that returns marketplace entities. Inspect actual services during implementation.

---

## 7. File Structure

### New Files

All under `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/`:

| File | Route / Role |
|---|---|
| `ProductSyncDetailPage.razor` (rename/replace) | `@page "/products/{id}/sync"` — main detail page |
| `ProductSyncDetailPage.razor.cs` | code-behind |
| `MarketplaceStatusCards.razor` | top status cards |
| `MarketplaceStatusCards.razor.cs` | code-behind |
| `TrendyolProductDetail.razor` | Trendyol tab component |
| `TrendyolProductDetail.razor.cs` | code-behind |
| `N11ProductDetail.razor` | N11 tab (Faz 1 placeholder) |
| `N11ProductDetail.razor.cs` | code-behind |
| `HepsiburadaProductDetail.razor` | Hepsiburada tab (Faz 1 placeholder) |
| `HepsiburadaProductDetail.razor.cs` | code-behind |
| `ProductContentUpdateDialog.razor` | Content override dialog |
| `ProductContentUpdateDialog.razor.cs` | code-behind |
| `ProductPriceStockDialog.razor` | Price/stock override dialog |
| `ProductPriceStockDialog.razor.cs` | code-behind |
| `BulkProductSyncPage.razor` | `@page "/marketplace/sync/products"` |
| `BulkProductSyncPage.razor.cs` | code-behind |

**Note on existing files:** `ProductSyncDetailPage.razor[.cs]` currently serves `/marketplace/matching/{Id:guid}`. The new page is at `/products/{id}/sync` — these are **different routes**. The existing file can remain unchanged. Create the new files separately.

### Modified Files

| File | Change |
|---|---|
| `ProductDetail.razor` | Add "Pazaryeri Detayı" menu item |
| `ProductDetail.razor.cs` | Add `GoToSyncPage()` navigation method |
| `NavMenu.razor` | Add "Ürün Senkronizasyon" nav link |
| `ITrendyolProductService.cs` | Add `DeleteProductAsync` |
| `TrendyolProductService.cs` | Implement `DeleteProductAsync` |
| `MarketplaceSyncItemDto` | Add `ExternalProductId`, `ContentId`, `IsApproved`, `IsArchived` fields |
| `IProductSyncManager.cs` | Update to return enriched DTO (no new methods unless §6.1 option 2 chosen) |
| `ProductSyncManager.cs` | Update query to include new fields in DTO projection |

---

## 8. Permissions Summary

| Action | Permission |
|---|---|
| View both pages | `AppPermissions.Marketplace.View` |
| Content update dialog | `AppPermissions.Marketplace.Edit` |
| Price/stock update dialog | `AppPermissions.Marketplace.Edit` |
| Delete button | `AppPermissions.Marketplace.Delete` |
| Sync button | `AppPermissions.Marketplace.Edit` |
| Bulk actions | `AppPermissions.Marketplace.Edit` |

Use `<AuthorizeView Policy="...">` wrappers around action buttons in Razor.

---

## 9. UX Notes

- **No loading spinners on action buttons** — use `MudButton`'s `Disabled` parameter bound to an `_isBusy` field during async calls.
- **After sync/update** — refresh the detail data (call `GetProductSyncDetailAsync` and `GetTimelineAsync` again). Do not navigate away.
- **Error display** — show failures via `ISnackbar` (Error severity). Do not use try-catch in data loading methods (ErrorBoundary handles those per project rules).
- **Pagination** — use server-side paging for the grid in `BulkProductSyncPage`. The existing `GetProductSyncListAsync` already returns `Pageable<T>`.
- **MarketplaceLogo** — the shared `MarketplaceLogo` component is already used in `ProductSyncDetailPage.razor`. Reuse throughout.
