# N11 Kategori Import — Sprint 2 Design Spec

## Problem

N11 pazaryerine urun publish edebilmek icin once N11 kategori agacinin ve attribute/value tanimlarinin local DB'ye import edilmesi gerekiyor. Trendyol icin bu altyapi mevcut (BaseCategoryImporterService + TrendyolCategoryImporter + UI). N11 icin ayni pipeline'in SOAP/XML versiyonu olusturulacak.

## Scope

- N11 kategori agacini SOAP API'den cekip DB'ye import etmek
- Kategori attribute ve value'larini import etmek (match tablolari dahil)
- Blazor UI'da N11 tab'ini aktif edip lazy-load tree view eklemek
- Background service ile async import destegi (marketplace routing dahil)

## Out of Scope

- Urun publish (Sprint 3)
- Stok/fiyat sync (Sprint 4)
- Sipariş/iade (gelecek faz)

---

## Architecture

### Existing Pattern (Trendyol)

```
BaseCategoryImporterService (abstract)
    ├── GetExternalCategoriesAsync(CancellationToken) [abstract]
    ├── ImportCategoryAttributesAsync(dbContext, category, isNewCategory, ct) [virtual]
    ├── ImportCategoryInternalAsync() [concrete — shared logic]
    └── ImportCategoriesAsync() [concrete — transaction + loop]

TrendyolCategoryImporter : BaseCategoryImporterService
    ├── GetExternalCategoriesAsync() → REST GET /product/product-categories
    └── ImportCategoryAttributesAsync() → REST GET /product-categories/{id}/attributes
```

### N11 Extension

```
N11CategoryImporter : BaseCategoryImporterService
    ├── GetExternalCategoriesAsync() → SOAP GetTopLevelCategories
    ├── GetSubCategoriesAsync(categoryId) → SOAP GetSubCategories [NEW — lazy-load, not on base]
    └── ImportCategoryAttributesAsync() → SOAP GetCategoryAttributes
```

### Key Difference: Lazy-Load

Trendyol returns the entire category tree in one REST call. N11 requires multiple SOAP calls:

1. `GetTopLevelCategories` — returns ~15-20 root categories (id + name only)
2. `GetSubCategories(categoryId)` — returns direct children of a category
3. Must be called recursively to build the full tree

**Decision:** Lazy-load in UI. Load only top-level categories initially. When user expands a node, fetch its children via GetSubCategories SOAP call.

---

## Components

### 1. N11CategoryImporter

**File:** `Application/Entegrasyon.Business/Concrete/Import/N11CategoryImporter.cs`

**Extends:** `BaseCategoryImporterService`

**Dependencies:**
- `IN11SoapClient` (from Sprint 1)
- `IDbContextFactory<IntegrationDbContext>`
- `ILogger<N11CategoryImporter>`

**Methods:**

```csharp
// Property override
public override ImportSource Source => ImportSource.N11;

// Fetches top-level categories from N11 (override from BaseCategoryImporterService)
public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
    CancellationToken cancellationToken = default)
// → SOAP: GetTopLevelCategories → maps to ExternalCategoryDto (HasChildren=true for all top-level)

// NEW method — NOT on base class. Called directly by UI for lazy-load.
public async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetSubCategoriesAsync(
    long categoryId, CancellationToken cancellationToken = default)
// → SOAP: GetSubCategories(categoryId) → maps to ExternalCategoryDto
// → HasChildren detection: call GetSubCategories for each child, check if response has subCategoryList

// Override from BaseCategoryImporterService — correct signature
protected override async Task ImportCategoryAttributesAsync(
    IntegrationDbContext dbContext,
    Category category,
    bool isNewCategory,
    CancellationToken cancellationToken = default)
// → SOAP: GetCategoryAttributes(category.ExternalCategoryId, pagingData)
// → Creates CategoryAttribute + CategoryAttributeValue + match records
// → Deduplication: marketplace-aware — checks ImportId + MarketPlaceId=2 (not just ImportId alone)
```

### 2. Background Service: Marketplace Routing

**Problem:** Mevcut `TrendyolCategoryImportBackgroundService`, `EventChannel<CategoryImportRequestedEvent>`'i `ReadAllAsync` ile consume eder. Bu destructive read — ikinci bir consumer eklersek event'ler kaybolur.

**Solution:** Ayri background service OLUSTURMA. Mevcut `TrendyolCategoryImportBackgroundService`'i marketplace-aware bir `CategoryImportBackgroundService`'e refactor et. MarketplaceName'e gore dogru importer'i resolve et.

**File:** `Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs` (rename from Trendyol-specific)

```csharp
public class CategoryImportBackgroundService : BackgroundService
{
    // Same EventChannel<CategoryImportRequestedEvent> — single consumer
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var importEvent in _importRequestedChannel.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();

            // Route by MarketplaceName
            BaseCategoryImporterService importer = importEvent.MarketplaceName switch
            {
                "Trendyol" => scope.ServiceProvider.GetRequiredService<TrendyolCategoryImporter>(),
                "N11" => scope.ServiceProvider.GetRequiredService<N11CategoryImporter>(),
                _ => throw new InvalidOperationException($"Unknown marketplace: {importEvent.MarketplaceName}")
            };

            // ... rest of import flow (notification, import, completed event)
        }
    }
}
```

**DI Change:** Replace `AddHostedService<TrendyolCategoryImportBackgroundService>()` with `AddHostedService<CategoryImportBackgroundService>()`.

### 3. UI: CategoryImport.razor Changes

**File:** `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor` + `.razor.cs`

- Activate N11 tab (currently disabled)
- **Separate state per tab:** Introduce `_n11Categories`, `_n11SelectedNodes`, `_n11Loading`, `_n11SearchQuery` (prefix with `_n11`) alongside existing Trendyol state. Tab switch does NOT clear other tab's state.
- Add N11 load/import handlers: `LoadN11CategoriesAsync()`, `ImportN11CategoriesAsync()`
- `[Inject] N11CategoryImporter` for N11 operations (concrete class injection, same pattern as `TrendyolCategoryImporter`)

### 4. UI: N11CategoryTreeView Component

**File:** `Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor` + `.razor.cs`

**[Inject]:** `N11CategoryImporter` (concrete class — `GetSubCategoriesAsync` is not on any interface)

Differs from `TrendyolCategoryTreeView`:
- **Lazy-load on expand:** When user clicks expand on a node, calls `N11CategoryImporter.GetSubCategoriesAsync(n11CategoryId)` and adds children dynamically
- Loading spinner per node during fetch (bool `IsLoading` on `CategoryTreeNode`)
- Caches already-fetched subtrees via `_fetchedNodes` HashSet (don't refetch on collapse/re-expand)
- Same selection logic: parent select → auto-select children (recursive)
- Reuses `CategoryTreeNodeTemplate` and `SelectedCategoriesPanel`

---

## N11 SOAP Calls

### GetTopLevelCategories

```xml
<!-- Request -->
<sch:GetTopLevelCategoriesRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <!-- auth injected by N11SoapClient -->
</sch:GetTopLevelCategoriesRequest>

<!-- Response (full wrapper) -->
<ns3:GetTopLevelCategoriesResponse xmlns:ns3="http://www.n11.com/ws/schemas">
    <result><status>success</status></result>
    <categories>
        <category><id>1</id><name>Kategori 1</name></category>
        <category><id>2</id><name>Kategori 2</name></category>
    </categories>
</ns3:GetTopLevelCategoriesResponse>
```

**Mapping:** Her top-level category `HasChildren=true` olarak isaretlenir (ust kategorilerin hepsinin alt kategorisi vardir).

### GetSubCategories(categoryId)

```xml
<!-- Request -->
<sch:GetSubCategoriesRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <!-- auth injected by N11SoapClient -->
    <categoryId>1002720</categoryId>
</sch:GetSubCategoriesRequest>

<!-- Response (full wrapper) -->
<ns3:GetSubCategoriesResponse xmlns:ns3="http://www.n11.com/ws/schemas">
    <result><status>success</status></result>
    <category>
        <id>1002720</id>
        <name>Tasiz Bileklik</name>
        <!-- subCategoryList is ABSENT if leaf node (no children) -->
    </category>
</ns3:GetSubCategoriesResponse>

<!-- Response with children -->
<ns3:GetSubCategoriesResponse xmlns:ns3="http://www.n11.com/ws/schemas">
    <result><status>success</status></result>
    <category>
        <id>1002720</id>
        <name>Parent Category</name>
    </category>
    <subCategoryList>
        <subCategory><id>123</id><name>Alt Kat 1</name></subCategory>
        <subCategory><id>124</id><name>Alt Kat 2</name></subCategory>
    </subCategoryList>
</ns3:GetSubCategoriesResponse>
```

**HasChildren detection:** `subCategoryList` elementi varsa `HasChildren=true`, yoksa `HasChildren=false` (leaf node). Eger response'ta `subCategoryList` elementi yoksa veya bossa, kategori leaf'tir.

### GetCategoryAttributes(categoryId, pagingData)

```xml
<!-- Request -->
<sch:GetCategoryAttributesRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <!-- auth injected by N11SoapClient -->
    <categoryId>1002306</categoryId>
    <pagingData>
        <currentPage>0</currentPage>
        <pageSize>100</pageSize>
    </pagingData>
</sch:GetCategoryAttributesRequest>

<!-- Response -->
<ns3:GetCategoryAttributesResponse xmlns:ns3="http://www.n11.com/ws/schemas">
    <result><status>success</status></result>
    <category>
        <metadata>
            <currentPage>1</currentPage>
            <pageSize>100</pageSize>
            <totalCount>2695</totalCount>
            <pageCount>27</pageCount>
        </metadata>
        <attributeList>
            <attribute>
                <id>354189900</id>
                <mandatory>true</mandatory>
                <multipleSelect>false</multipleSelect>
                <name>Marka</name>
                <priority>4.0</priority>
                <valueList>
                    <value><id>7915478</id><name>Axcess</name></value>
                    <value><id>7561463</id><name>Microsoft</name></value>
                </valueList>
            </attribute>
        </attributeList>
        <id>1002306</id>
        <name>Video Oyun &amp; Konsol</name>
    </category>
</ns3:GetCategoryAttributesResponse>
```

**Pagination:** `metadata.pageCount` kontrol edilir. `currentPage < pageCount` oldugu surece sonraki sayfa cekilir.

---

## Data Mapping

### N11 Category → ExternalCategoryDto

| N11 Field | ExternalCategoryDto Field |
|-----------|--------------------------|
| `category.id` | `ExternalId` (string, e.g. "1002306") |
| `category.name` | `Name` |
| parent category id | `ParentExternalId` |
| `subCategoryList` element exists? | `HasChildren` |

### N11 Attribute → CategoryAttribute + Matches

| N11 Field | Local Entity/Field |
|-----------|-------------------|
| `attribute.id` | `CategoryAttribute.ImportId` + `CategoryAttributeMarketPlaceMatch.MarketPlaceCategoryAttributeId` (MarketPlaceId=2) |
| `attribute.name` | `CategoryAttribute.CategoryAttributeHumanized` + `CategoryAttributeKey` |
| `attribute.mandatory` | `CategoryAttributeCategory.IsRequired` |
| `attribute.multipleSelect` | `CategoryAttributeCategory.IsSlicer` (reuse: N11'de multiSelect ≈ slicer kavrami) |
| `attribute.valueList.value.id` | `CategoryAttributeValue` + `CategoryAttributeValueMarketPlaceMatch.MarketPlaceCategoryAttributeValueId` (MarketPlaceId=2) |
| `attribute.valueList.value.name` | `CategoryAttributeValue.Name` |

**multipleSelect mapping notu:** `CategoryAttributeCategory.IsSlicer` field'i reuse edilir. Bu field Trendyol'da da benzer semantikte kullanilir (birden fazla deger secimi). Yeni migration gerekmez.

---

## Attribute Deduplication (Cross-Marketplace Safety)

**Problem:** `CategoryAttribute.ImportId` N11 ve Trendyol'da bagimsiz integer sequence'lardir. Ayni `ImportId` degeri farkli marketplace'lerde farkli attribute'lari temsil edebilir.

**Solution:** Deduplication kontrolu marketplace-aware olmalidir:

```csharp
// YANLIS — cross-marketplace collision riski
_savedCategoryAttributes.Any(x => x.ImportId == n11AttributeId)

// DOGRU — marketplace-specific kontrol
var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
    .AnyAsync(x => x.MarketPlaceCategoryAttributeId == n11AttributeId
                && x.MarketPlaceId == MarketPlaceConstants.N11MarketPlaceId);
```

Her N11 attribute icin once match tablosunda MarketPlaceId=2 ile kontrol yapilir. Yoksa yeni `CategoryAttribute` + match olusturulur.

---

## Error Handling

- SOAP call failures → log error, throw (background service catches and notifies user)
- Empty category response → return empty list (not an error)
- Attribute pagination → loop until all pages fetched (`currentPage < pageCount`)
- Marketplace-aware attribute deduplication (see above)
- Transaction rollback on any import failure
- UI: lazy-load failure → show error snackbar per node, allow retry

## DI Registration

```csharp
// In AddApplicationDependencies:
services.AddScoped<N11CategoryImporter>();

// In AddBackgroundServices:
// REMOVE: services.AddHostedService<TrendyolCategoryImportBackgroundService>();
// ADD:
services.AddHostedService<CategoryImportBackgroundService>(); // marketplace-aware router
```

## Testing

### Unit Tests (`Test/Entegrasyon.Test/N11/`)

**N11CategoryImporterTests.cs:**
1. `GetExternalCategoriesAsync_ShouldReturnTopLevelCategories` — mock SOAP response with 3 categories, verify ExternalCategoryDto mapping
2. `GetExternalCategoriesAsync_WhenSoapFails_ShouldReturnError` — mock SOAP exception, verify error result
3. `GetSubCategoriesAsync_ShouldReturnChildCategories` — mock SOAP GetSubCategories response
4. `GetSubCategoriesAsync_WhenLeafNode_ShouldReturnEmptyWithHasChildrenFalse` — no subCategoryList in response
5. `ImportCategoryAttributesAsync_ShouldCreateAttributeAndValueRecords` — mock SOAP GetCategoryAttributes, verify DB records created
6. `ImportCategoryAttributesAsync_ShouldCreateMarketPlaceMatchWithN11Id` — verify match records use MarketPlaceId=2
7. `ImportCategoryAttributesAsync_WhenAttributeAlreadyExists_ShouldNotDuplicate` — marketplace-aware dedup test
8. `ImportCategoryAttributesAsync_ShouldHandlePagination` — mock multi-page response

**CategoryImportBackgroundServiceTests.cs:**
1. `ExecuteAsync_WhenN11Event_ShouldResolveN11Importer` — verify routing by MarketplaceName
2. `ExecuteAsync_WhenTrendyolEvent_ShouldResolveTrendyolImporter` — verify backward compat
3. `ExecuteAsync_WhenImportFails_ShouldSendErrorNotification` — verify notification flow
4. `ExecuteAsync_WhenImportSucceeds_ShouldPublishCompletedEvent` — verify event publishing

### bUnit Tests (`Test/Entegrasyon.BunitTest/`)

**N11CategoryTreeViewTests.cs:**
1. `TreeView_ShouldRenderTopLevelCategories` — verify initial render
2. `TreeView_WhenExpand_ShouldFetchSubCategories` — verify lazy-load triggers API call
3. `TreeView_WhenExpandCached_ShouldNotRefetch` — verify cache behavior
