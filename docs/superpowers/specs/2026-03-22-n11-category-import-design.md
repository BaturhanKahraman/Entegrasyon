# N11 Kategori Import — Sprint 2 Design Spec

## Problem

N11 pazaryerine urun publish edebilmek icin once N11 kategori agacinin ve attribute/value tanimlarinin local DB'ye import edilmesi gerekiyor. Trendyol icin bu altyapi mevcut (BaseCategoryImporterService + TrendyolCategoryImporter + UI). N11 icin ayni pipeline'in SOAP/XML versiyonu olusturulacak.

## Scope

- N11 kategori agacini SOAP API'den cekip DB'ye import etmek
- Kategori attribute ve value'larini import etmek (match tablolari dahil)
- Blazor UI'da N11 tab'ini aktif edip lazy-load tree view eklemek
- Background service ile async import destegi

## Out of Scope

- Urun publish (Sprint 3)
- Stok/fiyat sync (Sprint 4)
- Siparis/iade (gelecek faz)

---

## Architecture

### Existing Pattern (Trendyol)

```
BaseCategoryImporterService (abstract)
    ├── GetExternalCategoriesAsync() [abstract]
    ├── ImportCategoryAttributesAsync() [abstract]
    └── ImportCategoryInternalAsync() [concrete — shared logic]

TrendyolCategoryImporter : BaseCategoryImporterService
    ├── GetExternalCategoriesAsync() → REST GET /product/product-categories
    └── ImportCategoryAttributesAsync() → REST GET /product-categories/{id}/attributes
```

### N11 Extension

```
N11CategoryImporter : BaseCategoryImporterService
    ├── GetExternalCategoriesAsync() → SOAP GetTopLevelCategories
    ├── GetSubCategoriesAsync(categoryId) → SOAP GetSubCategories [NEW — lazy-load]
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
// Override from BaseCategoryImporterService
protected override ImportSource Source => ImportSource.N11;

// Fetches top-level categories from N11
public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync()
// → SOAP: GetTopLevelCategories → maps to ExternalCategoryDto (HasChildren=true)

// Fetches subcategories for a given N11 category ID (NEW method for lazy-load)
public async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetSubCategoriesAsync(long categoryId)
// → SOAP: GetSubCategories(categoryId) → maps to ExternalCategoryDto

// Imports attributes for a leaf category
protected override async Task ImportCategoryAttributesAsync(
    IntegrationDbContext dbContext, int localCategoryId, string externalCategoryId)
// → SOAP: GetCategoryAttributes(categoryId, pagingData)
// → Creates CategoryAttribute + CategoryAttributeValue + match records
```

### 2. N11CategoryImportBackgroundService

**File:** `Application/Entegrasyon.Business/BackgroundServices/N11CategoryImportBackgroundService.cs`

Follows exact Trendyol pattern:
- Listens on `EventChannel<CategoryImportRequestedEvent>` where `MarketplaceName == "N11"`
- Resolves `N11CategoryImporter` from DI scope
- Calls `ImportCategoriesAsync()`
- Sends notifications via `INotificationManager`
- Publishes `CategoryImportCompletedEvent`

### 3. UI: CategoryImport.razor Changes

**File:** `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor`

- Activate N11 tab (currently disabled)
- Add N11 load/import handlers (mirror Trendyol handlers)
- Use `N11CategoryImporter` for N11 tab operations

### 4. UI: N11CategoryTreeView Component

**File:** `Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor` + `.razor.cs`

Differs from `TrendyolCategoryTreeView`:
- **Lazy-load on expand:** When user clicks expand on a node, calls `N11CategoryImporter.GetSubCategoriesAsync(n11CategoryId)` and adds children dynamically
- Loading spinner per node during fetch
- Caches already-fetched subtrees (don't refetch on collapse/re-expand)
- Same selection logic: parent select → auto-select children
- Reuses `CategoryTreeNodeTemplate` and `SelectedCategoriesPanel`

---

## N11 SOAP Calls

### GetTopLevelCategories

```xml
<!-- Request -->
<sch:GetTopLevelCategoriesRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <auth><appKey>***</appKey><appSecret>***</appSecret></auth>
</sch:GetTopLevelCategoriesRequest>

<!-- Response -->
<categories>
    <category><id>1</id><name>Kategori 1</name></category>
    <category><id>2</id><name>Kategori 2</name></category>
    ...
</categories>
```

### GetSubCategories(categoryId)

```xml
<!-- Request -->
<sch:GetSubCategoriesRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <auth>...</auth>
    <categoryId>1002720</categoryId>
</sch:GetSubCategoriesRequest>

<!-- Response -->
<category>
    <id>1002720</id>
    <name>Tasiz Bileklik</name>
</category>
<!-- OR with subcategories: -->
<subCategoryList>
    <subCategory><id>123</id><name>Alt Kat</name></subCategory>
</subCategoryList>
```

### GetCategoryAttributes(categoryId, pagingData)

```xml
<!-- Request -->
<sch:GetCategoryAttributesRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <auth>...</auth>
    <categoryId>1002306</categoryId>
    <pagingData>
        <currentPage>0</currentPage>
        <pageSize>100</pageSize>
    </pagingData>
</sch:GetCategoryAttributesRequest>

<!-- Response -->
<category>
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
```

---

## Data Mapping

### N11 Category → ExternalCategoryDto

| N11 Field | ExternalCategoryDto Field |
|-----------|--------------------------|
| `category.id` | `ExternalId` (string) |
| `category.name` | `Name` |
| parent category id | `ParentExternalId` |
| has subcategories? | `HasChildren` |

### N11 Attribute → CategoryAttribute + Matches

| N11 Field | Local Entity/Field |
|-----------|-------------------|
| `attribute.id` | `CategoryAttribute.ImportId` + `CategoryAttributeMarketPlaceMatch.MarketPlaceCategoryAttributeId` |
| `attribute.name` | `CategoryAttribute.CategoryAttributeHumanized` + `CategoryAttributeKey` |
| `attribute.mandatory` | `CategoryAttributeCategory.IsRequired` |
| `attribute.multipleSelect` | (stored but not directly mapped — used during product publish) |
| `attribute.valueList.value.id` | `CategoryAttributeValue` (new record) + `CategoryAttributeValueMarketPlaceMatch.MarketPlaceCategoryAttributeValueId` |
| `attribute.valueList.value.name` | `CategoryAttributeValue.Name` |

---

## Error Handling

- SOAP call failures → log error, throw (background service catches and notifies user)
- Empty category response → return empty list (not an error)
- Attribute pagination → loop until all pages fetched (pageSize=100)
- Duplicate attribute prevention → check `_savedCategoryAttributes` list (same as Trendyol)
- Transaction rollback on any import failure

## DI Registration

```csharp
// In AddApplicationDependencies:
services.AddScoped<N11CategoryImporter>();

// In AddBackgroundServices:
services.AddHostedService<N11CategoryImportBackgroundService>();
```

## Testing

- `N11CategoryImporterTests` — mock IN11SoapClient, verify SOAP XML construction and response parsing
- `N11CategoryTreeViewTests` (bUnit) — verify lazy-load behavior, expand triggers API call
- Background service: verify event handling and notification flow
