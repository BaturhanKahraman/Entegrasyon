# Implementation Plan — Marketplace Product Sync Pages

**Spec:** `docs/superpowers/specs/2026-03-31-marketplace-product-sync-design.md`
**Date:** 2026-03-31
**Estimated tasks:** 14

---

## Prerequisites

Read the spec before starting. The existing `ProductSyncDetailPage.razor` at `/marketplace/matching/{Id:guid}` must NOT be modified — the new page is a separate route (`/products/{id}/sync`).

---

## Task 1 — Enrich `MarketplaceSyncItemDto`

**Files:**
- `Application/Entegrasyon.Entity/Dtos/Product/ProductSyncDetailDto.cs`
- `Application/Entegrasyon.Business/Concrete/ProductSyncManager.cs` (projection query)

**TDD:**
1. Write test: `ProductSyncManager.GetProductSyncDetailAsync` returns `ExternalProductId`, `ContentId`, `IsApproved`, `IsArchived` populated from `ProductMarketplace`.
2. Extend `MarketplaceSyncItemDto` record with 4 new positional fields: `string? ExternalProductId`, `long? ContentId`, `bool? IsApproved`, `bool? IsArchived`.
3. Update all construction sites of `MarketplaceSyncItemDto` (should only be in `ProductSyncManager.cs` projection).
4. Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductSyncManager"`

---

## Task 2 — Add `DeleteProductAsync` to Trendyol service

**Files:**
- `Application/Entegrasyon.Business/Abstract/ITrendyolProductService.cs`
- `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductService.cs`

**TDD:**
1. Write test: `TrendyolProductService.DeleteProductAsync` logs a `Deleted` activity and updates `ProductMarketplace.Status` to `Failed` (or a to-be-decided tombstone state) on success.
2. Add `Task<IResult> DeleteProductAsync(Guid productId)` to the interface.
3. Implement: call the Trendyol DELETE endpoint (look up exact URL in `docs/trendyol/PRODUCT_API_V2.md`), log via `IProductActivityLogger`, set status.
4. Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolProductService"`

---

## Task 3 — `MarketplaceStatusCards` component

**Files (new):**
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/MarketplaceStatusCards.razor`
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/MarketplaceStatusCards.razor.cs`

**Spec ref:** §3.4

**Implementation notes:**
- Parameters: `IReadOnlyList<MarketplaceSyncItemDto> Marketplaces`, `bool IsLoading`, `EventCallback<int> OnMarketplaceSelected`
- One `MudCard` per marketplace; clicking raises `OnMarketplaceSelected` with the `MarketPlaceId`
- Reuse `MarketplaceLogo` and `ProductSyncPage.GetSyncStateColor/Label` static helpers
- Show `ExternalProductId` if non-null (new field from Task 1)
- No unit test required (pure presentation); add bUnit test if time permits

---

## Task 4 — `TrendyolProductDetail` component (display only)

**Files (new):**
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/TrendyolProductDetail.razor`
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/TrendyolProductDetail.razor.cs`

**Spec ref:** §3.7

**Implementation notes:**
- Parameters: `Guid ProductId`, `MarketplaceSyncItemDto SyncItem`, `MarketplaceOverrideDetailDto Overrides`, `EventCallback OnActionCompleted`
- Display status row: ContentId, ExternalProductId, BatchRequestId (with copy icon), IsApproved-derived label
- Display read-only override panel (TitleOverride, DescriptionOverride, variant price table)
- Action buttons: "İçerik Güncelle", "Fiyat/Stok Güncelle", "Sil", "Sync" — wired to dialogs/callbacks in Task 6 and Task 7
- Use `<AuthorizeView Policy="AppPermissions.Marketplace.Edit">` around action buttons
- Disable all buttons when `_isBusy`

---

## Task 5 — `N11ProductDetail` and `HepsiburadaProductDetail` (Faz 1 placeholders)

**Files (new):**
- `N11ProductDetail.razor[.cs]`
- `HepsiburadaProductDetail.razor[.cs]`

**Spec ref:** §3.10, §3.11

**Implementation notes:**
- Parameters: `Guid ProductId`, `MarketplaceSyncItemDto SyncItem`, `EventCallback OnSyncCompleted`
- Status chip, last sync time, optional StatusMessage alert
- "Sync" button calls `IProductSyncManager.SyncProductAsync(ProductId, marketPlaceId)` and raises `OnSyncCompleted`
- "İçerik yönetimi yakında..." `MudAlert` with `Severity.Info`

---

## Task 6 — `ProductContentUpdateDialog`

**Files (new):**
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/ProductContentUpdateDialog.razor`
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/ProductContentUpdateDialog.razor.cs`

**Spec ref:** §3.8

**Implementation notes:**
- `[CascadingParameter] MudDialogInstance MudDialog { get; set; }`
- `[Parameter] Guid ProductId`
- `[Parameter] int MarketPlaceId`
- `[Parameter] MarketplaceOverrideDetailDto CurrentOverrides`
- Form: TitleOverride (MudTextField), DescriptionOverride (MudTextField multiline), "Kaydet ve Senkronize Et" checkbox
- On submit: call `IMarketplaceOverrideManager.SaveOverridesAsync` or `SaveOverridesAndPublishAsync`
- Client-side validation: title 3–200 chars, description 20–2000 chars (if non-empty)
- On success: snackbar "İçerik güncellendi", close dialog with `MudDialog.Close(DialogResult.Ok(true))`

---

## Task 7 — `ProductPriceStockDialog`

**Files (new):**
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/ProductPriceStockDialog.razor`
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/ProductPriceStockDialog.razor.cs`

**Spec ref:** §3.9

**Implementation notes:**
- Same parameter pattern as `ProductContentUpdateDialog`
- Render `MudSimpleTable` with variant rows: VariantLabel, Barcode, OriginalListPrice, OriginalSalePrice, ListPriceOverride (editable), SalePriceOverride (editable)
- Bind a `List<VariantPriceOverrideDetailDto>` cloned from `CurrentOverrides.VariantOverrides` to allow editing
- On submit: build `SaveMarketplaceOverridesDto` and call appropriate override manager method
- "Kaydet ve Senkronize Et" checkbox same as content dialog

---

## Task 8 — `ProductSyncDetailPage` (new route `/products/{id}/sync`)

**Files (new):**
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/ProductSyncDetailPage.razor` — **note: this filename already exists** at the existing route `/marketplace/matching/{Id:guid}`. Rename the existing file to `MarketplaceMatchingDetailPage.razor[.cs]` and update its `@page` directive, or create the new file with a different name (e.g., `ProductSyncFullDetailPage.razor[.cs]`). Pick the cleanest name during implementation.
- Corresponding `.razor.cs`

**Spec ref:** §3.1 – §3.6

**Implementation notes:**
- `@page "/products/{Id:guid}/sync"`
- `OnInitializedAsync`: parallel-safe sequential loads (no `Task.WhenAll` with shared DbContext)
  1. `IProductSyncManager.GetProductSyncDetailAsync(Id)`
  2. `IProductActivityLogger.GetTimelineAsync(Id, 50)`
  3. `IMarketplaceOverrideManager.GetOverridesAsync(Id, 1)` (Trendyol)
- Top header row: back button (`/products/{Id}`), title, chips
- `<MarketplaceStatusCards>` bound to `_detail.Marketplaces`, `OnMarketplaceSelected` sets `_activeTab`
- `<MudTabs>` with `ActivePanelIndex="@_activeTab"`: iterate marketplaces that have records, render appropriate tab component
- `<ProductActivityTimeline ProductId="Id" />`
- On `OnActionCompleted` callback from tab components: reload all data

---

## Task 9 — Entry point in `ProductDetail`

**Files (modified):**
- `Application/Entegrasyon.Blazor/Features/Products/ProductDetail.razor`
- `Application/Entegrasyon.Blazor/Features/Products/ProductDetail.razor.cs`

**Spec ref:** §3.12

**Implementation notes:**
- In `ProductDetail.razor`, inside the `<MudMenu Label="İşlemler" ...>`, add after existing "Pazaryeri Senkronizasyonu" item:
  ```razor
  <MudMenuItem OnClick="GoToSyncPage" Icon="@Icons.Material.Filled.Hub">
      Pazaryeri Detayı
  </MudMenuItem>
  ```
- In `ProductDetail.razor.cs`, add:
  ```csharp
  private void GoToSyncPage() => NavigationManager.NavigateTo($"/products/{Id}/sync");
  ```
- No test required (navigation only).

---

## Task 10 — `BulkProductSyncPage`

**Files (new):**
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/BulkProductSyncPage.razor`
- `Application/Entegrasyon.Blazor/Features/MarketplaceSync/ProductSync/BulkProductSyncPage.razor.cs`

**Spec ref:** §4

**Implementation notes:**
- `@page "/marketplace/sync/products"`
- `OnInitializedAsync`: load marketplace list (use available service or direct `IDbContextFactory` query), then load product list for default marketplace (Trendyol, Id=1)
- Marketplace selector `MudSelect<int>` — on change: reset page, reload grid
- Status filter `MudSelect<MarketplaceSyncState?>` — on change: reload grid
- Search `MudTextField` with debounce 400ms
- `MudDataGrid<ProductSyncListItemDto>` with `MultiSelection="true"` and `SelectedItemsChanged`
- Bulk actions toolbar: visible only when `_selectedItems.Count > 0`
- Grid `RowClick` → `NavigationManager.NavigateTo($"/products/{item.ProductId}/sync")`
- Bulk sync/update: sequential loop (not parallel) with `MudProgressLinear` and item counter
- "Seçilenleri Sil" for non-Trendyol marketplaces: `ISnackbar.Add("Bu pazaryeri için silme henüz desteklenmiyor", Severity.Warning)`

---

## Task 11 — NavMenu update

**Files (modified):**
- `Application/Entegrasyon.Blazor/Components/Shared/NavMenu.razor`

**Spec ref:** §5

**Implementation notes:**
- Add inside the "Senkronizasyon" `MudNavGroup`, after the "Marka Eslestirme" link:
  ```razor
  <PermissionGuardedNavLink Permission="@AppPermissions.Marketplace.View"
                            Href="/marketplace/sync/products"
                            Icon="@Icons.Material.Filled.Inventory2"
                            Title="Urun Senkronizasyon" />
  ```
- No changes to `NavMenu.razor.cs` needed — `_syncExpanded` already covers `marketplace/sync/*` routes.

---

## Task 12 — Unit Tests

**Files (new):**
- `Test/Entegrasyon.Test/Business/TrendyolProductServiceDeleteTests.cs`
- `Test/Entegrasyon.Test/Business/ProductSyncManagerEnrichedDtoTests.cs`

**Coverage targets:**
- `TrendyolProductService.DeleteProductAsync`: happy path (product found, API call succeeds, activity logged, status updated), product not found, API failure
- `ProductSyncManager.GetProductSyncDetailAsync`: new fields present in returned DTO (`ExternalProductId`, `ContentId`, `IsApproved`, `IsArchived`)

Run all unit tests: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`

---

## Task 13 — Integration Test

**Files (modified or new):**
- `Test/Entegrasyon.IntegrationTest/` — add a test class for the new page route

**Minimal integration test:**
- Authenticated request to `GET /products/{existingProductId}/sync` returns 200
- Authenticated request to `GET /marketplace/sync/products` returns 200

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`

---

## Task 14 — Final Verification

Run all test suites:
```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
dotnet build Entegrasyon.sln
```

Manual smoke test (app must be running):
1. Open `/products` → click a product with Trendyol data → click "İşlemler" → "Pazaryeri Detayı" → lands on `/products/{id}/sync`
2. Verify status cards show correct data
3. Verify Trendyol tab loads overrides
4. Open "İçerik Güncelle" dialog — fill title — save — timeline updates
5. Navigate to `/marketplace/sync/products` from NavMenu → select 2 products → click "Toplu Sync" → progress shown → success snackbar

---

## Execution Order

```
Task 1  → Task 2  (business layer foundations)
    ↓
Task 3  → Task 4  → Task 5  (sub-components, can be parallel)
    ↓
Task 6  → Task 7  (dialogs, can be parallel)
    ↓
Task 8             (main detail page — depends on 3,4,5,6,7)
    ↓
Task 9  → Task 10 → Task 11  (entry points and nav, can be parallel)
    ↓
Task 12 → Task 13 → Task 14  (test and verify)
```

Tasks 3/4/5 and Tasks 6/7 within their groups can be parallelized if using subagents.
