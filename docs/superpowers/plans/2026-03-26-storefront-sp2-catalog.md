# SP-2: Storefront Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add product browsing, category listing, search with autocomplete, filtering, and SEO structured data to the Storefront MVC app.

**Architecture:** Extends existing business layer services with SeoSlug-based lookups and storefront-specific query methods. New controllers (Catalog, Product) wire existing services to Razor views. No new managers — only method additions to existing ICategoryService, IProductService, IBrandService.

**Tech Stack:** ASP.NET Core 8 MVC, EF Core (PostgreSQL full-text search), Tailwind CSS, vanilla JS (autocomplete), xUnit + Moq + FluentAssertions

**Security Note:** Search autocomplete dropdown renders text content from the API. The search.js builds DOM elements using textContent for user-visible strings (not innerHTML) to prevent XSS. Image URLs come from the product database (admin-uploaded via MinIO), not from user input.

**Key existing types:**
- `Product` (Guid Id, string Title, string? SeoSlug, int CategoryId, int? BrandId, NpgsqlTsVector SearchVector, ICollection ProductVariant)
- `ProductVariant` (Guid Id, decimal ListPrice, decimal SalePrice, decimal ECommercePrice, ICollection BranchOfficeStock, ICollection Image)
- `Category` (int Id, string Name, string? SeoSlug, int? SuperCategoryId, IEnumerable Category SubCategories)
- `Brand` (int Id, string Name, string? SeoSlug)
- `Pageable<T>` (IReadOnlyList T Items, int CurrentPageIndex, int PageSize, int TotalItemCount)
- `IDbContextFactory<IntegrationDbContext>` — all DB access via factory
- Result types: `SuccessDataResult<T>`, `ErrorDataResult<T>`, `SuccessResult`, `ErrorResult`

---

### Task 1: Storefront DTOs

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Storefront/StorefrontProductCardDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Storefront/StorefrontProductDetailDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Storefront/StorefrontCatalogQuery.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Storefront/CategoryTreeDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Storefront/StorefrontSearchSuggestionDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Storefront/BreadcrumbItemDto.cs`

- [ ] **Step 1: Create all DTO files**

```csharp
// StorefrontProductCardDto.cs
namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontProductCardDto(
    Guid Id, string Title, string? SeoSlug, string? ImageUrl,
    decimal MinPrice, decimal MaxPrice, decimal? OldPrice,
    int TotalStock, string? BrandName, string CategoryName, bool IsNew);
```

```csharp
// StorefrontProductDetailDto.cs
namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontProductDetailDto(
    Guid Id, string Title, string? Description, string? StockCode,
    string? SeoSlug, string? SeoTitle, string? SeoDescription,
    string? BrandName, string? BrandSlug,
    string CategoryName, string? CategorySlug, int CategoryId,
    decimal MinPrice, decimal MaxPrice,
    List<StorefrontVariantDto> Variants,
    List<StorefrontAttributeDto> Attributes,
    List<BreadcrumbItemDto> Breadcrumbs);

public record StorefrontVariantDto(
    Guid Id, string? Barcode, decimal ListPrice, decimal SalePrice, int Stock,
    List<StorefrontVariantAttributeDto> Attributes, List<string> ImageUrls);

public record StorefrontVariantAttributeDto(string AttributeKey, string AttributeValue);
public record StorefrontAttributeDto(string Key, string HumanizedKey, string Value);
```

```csharp
// StorefrontCatalogQuery.cs
namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontCatalogQuery(
    int? CategoryId = null, int? BrandId = null,
    string? SearchQuery = null,
    decimal? MinPrice = null, decimal? MaxPrice = null,
    string? SortBy = null, int Page = 1, int PageSize = 24);
```

```csharp
// CategoryTreeDto.cs
namespace Entegrasyon.Entity.Dtos.Storefront;

public record CategoryTreeDto(
    int Id, string Name, string? SeoSlug, int ProductCount,
    List<CategoryTreeDto> Children);
```

```csharp
// StorefrontSearchSuggestionDto.cs
namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontSearchSuggestionDto(
    string Text, string Url, string Type, string? ImageUrl);
```

```csharp
// BreadcrumbItemDto.cs
namespace Entegrasyon.Entity.Dtos.Storefront;

public record BreadcrumbItemDto(string Name, string Url);
```

- [ ] **Step 2: Verify build**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Storefront/
git commit -m "feat(storefront): add catalog DTOs for SP-2"
```

---

### Task 2: Service Interface Extensions + Implementations + Unit Tests

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IProductManager.cs` (contains IProductService)
- Modify: `Application/Entegrasyon.Business/Abstract/ICategoryManager.cs` (contains ICategoryService)
- Modify: `Application/Entegrasyon.Business/Abstract/IBrandService.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/ProductManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/BrandService.cs`
- Create: `Test/Entegrasyon.Test/Storefront/StorefrontCatalogQueryTests.cs`

- [ ] **Step 1: Write and run unit test for DTO defaults**

Create `Test/Entegrasyon.Test/Storefront/StorefrontCatalogQueryTests.cs` with tests verifying default Page=1, PageSize=24, null filters. Run to confirm pass.

- [ ] **Step 2: Add storefront methods to IProductService**

Add to the interface (using Entegrasyon.Entity.Dtos.Storefront):
- `Task<IDataResult<Product>> GetProductBySeoSlugAsync(string slug);`
- `Task<IDataResult<Pageable<StorefrontProductCardDto>>> GetStorefrontProductsAsync(StorefrontCatalogQuery query);`
- `Task<IDataResult<List<StorefrontProductCardDto>>> GetNewProductsAsync(int count);`
- `Task<IDataResult<List<StorefrontProductCardDto>>> GetBestSellersAsync(int count);`
- `Task<IDataResult<List<StorefrontSearchSuggestionDto>>> GetSearchSuggestionsAsync(string query, int maxResults = 8);`
- `Task<IDataResult<StorefrontProductDetailDto>> GetStorefrontProductDetailAsync(string seoSlug);`

- [ ] **Step 3: Add storefront methods to ICategoryService**

- `Task<IDataResult<Category>> GetCategoryBySeoSlugAsync(string slug);`
- `Task<IDataResult<List<CategoryTreeDto>>> GetCategoryTreeAsync();`

- [ ] **Step 4: Add storefront method to IBrandService**

- `Task<IDataResult<Brand>> GetBrandBySeoSlugAsync(string slug);`

- [ ] **Step 5: Implement ProductManager methods**

`GetProductBySeoSlugAsync`: Query by SeoSlug with Include(Brand, Category, ProductVariants.ProductVariantAttributes, ProductVariants.Images, ProductVariants.BranchOfficeStocks, AttributeKeyValues).

`GetStorefrontProductsAsync`: Build IQueryable with optional filters (CategoryId, BrandId, SearchQuery via SearchVector.Matches, MinPrice, MaxPrice). Sort by SortBy parameter (newest/price-asc/price-desc/bestseller). Paginate. Project to StorefrontProductCardDto.

`GetNewProductsAsync` and `GetBestSellersAsync`: Delegate to GetStorefrontProductsAsync with appropriate SortBy.

`GetSearchSuggestionsAsync`: ILike search on Product.Title (5), Category.Name (2), Brand.Name (1). Max 8 results. Min 2 chars.

`GetStorefrontProductDetailAsync`: Use GetProductBySeoSlugAsync, then map to StorefrontProductDetailDto with breadcrumbs, variants, attributes.

Note: Check actual property names on BranchOfficeStock (Quantity/SoldQuantity), Image (Url/DisplayOrder), ProductVariantAttribute (AttributeKey/AttributeValue), AttributeKeyValue (CategoryAttribute/CategoryAttributeValue/CustomValue) — adjust implementation to match actual entity definitions.

- [ ] **Step 6: Implement CategoryManager methods**

`GetCategoryBySeoSlugAsync`: FirstOrDefault by SeoSlug, Include SubCategories and SuperCategory.
`GetCategoryTreeAsync`: Top-level categories (SuperCategoryId == null) with 2 levels of SubCategories, project to CategoryTreeDto with ProductCount.

- [ ] **Step 7: Implement BrandService method**

`GetBrandBySeoSlugAsync`: FirstOrDefault by SeoSlug.

- [ ] **Step 8: Verify build and tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Build succeeded, all tests pass.

- [ ] **Step 9: Commit**

```bash
git add Application/Entegrasyon.Business/ Test/Entegrasyon.Test/Storefront/
git commit -m "feat(storefront): add catalog service methods for category, product, brand lookups"
```

---

### Task 3: JsonLdBuilder Extensions + Unit Tests

**Files:**
- Modify: `Application/Entegrasyon.Storefront/Infrastructure/JsonLdBuilder.cs`
- Modify: `Test/Entegrasyon.Test/Storefront/JsonLdBuilderTests.cs`

- [ ] **Step 1: Write failing tests for BuildProduct and BuildCollectionPage**

Add tests to JsonLdBuilderTests.cs:
- `BuildProduct_ValidProduct_ReturnsProductSchema`: Create StorefrontProductDetailDto, call BuildProduct, parse JSON, verify @type=Product, name, brand.name, offers.lowPrice, offers.priceCurrency=TRY.
- `BuildCollectionPage_ValidCategory_ReturnsCollectionPageSchema`: Call BuildCollectionPage with name/description/url, verify @type=CollectionPage, name.

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Implement BuildProduct and BuildCollectionPage**

`BuildProduct(StorefrontProductDetailDto p, string baseUrl)`: Returns Product schema JSON-LD with name, description, sku, image array, brand, AggregateOffer (lowPrice/highPrice/priceCurrency=TRY/availability based on stock).

`BuildCollectionPage(string name, string? description, string url)`: Returns CollectionPage schema JSON-LD.

Add using: `using Entegrasyon.Entity.Dtos.Storefront;`

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "JsonLdBuilder"`
Expected: All tests PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Storefront/Infrastructure/ Test/Entegrasyon.Test/Storefront/
git commit -m "feat(storefront): add Product and CollectionPage JSON-LD builders"
```

---

### Task 4: Routes + CatalogController + ProductController

**Files:**
- Modify: `Application/Entegrasyon.Storefront/Program.cs`
- Create: `Application/Entegrasyon.Storefront/Controllers/CatalogController.cs`
- Create: `Application/Entegrasyon.Storefront/Controllers/ProductController.cs`

- [ ] **Step 1: Add routes to Program.cs**

Add BEFORE the existing "legal" catch-all route:
- `/kategoriler` -> Catalog.Categories
- `/kategori/{slug}` -> Catalog.Category
- `/kategori/{parentSlug}/{slug}` -> Catalog.Category (subcategory)
- `/urun/{slug}` -> Product.Detail
- `/marka/{slug}` -> Catalog.Brand
- `/urunler` -> Catalog.AllProducts
- `/arama` -> Catalog.Search
- `/api/arama/oneri` -> Catalog.SearchSuggest
- `/yeni-urunler` -> Catalog.NewProducts
- `/cok-satanlar` -> Catalog.BestSellers

- [ ] **Step 2: Create CatalogController**

Primary constructor DI: `(IStorefrontTenantContext tenant, IProductService productService, ICategoryService categoryService, IBrandService brandService)`

Actions:
- `Categories()` — category tree grid, ResponseCache 300s
- `Category(string slug, string? parentSlug, [FromQuery] StorefrontCatalogQuery query)` — category + products, ResponseCache 300s VaryByQueryKeys
- `AllProducts([FromQuery] StorefrontCatalogQuery query)` — all products, uses Category view
- `Search(string? q, [FromQuery] StorefrontCatalogQuery query)` — search results, ResponseCache 60s
- `Brand(string slug, [FromQuery] StorefrontCatalogQuery query)` — brand products, uses Category view
- `NewProducts()` — newest 24 products
- `BestSellers()` — top 24 bestsellers
- `SearchSuggest(string? q)` — JSON endpoint for autocomplete, returns suggestions

Each action sets ViewBag with: Products (Pageable), Query, SeoTitle, Breadcrumbs, JsonLd where appropriate.

- [ ] **Step 3: Create ProductController**

Primary constructor DI: `(IStorefrontTenantContext tenant, IProductService productService)`

Actions:
- `Detail(string slug)` — product detail, ResponseCache 300s, sets JsonLd Product schema, fetches related products (same category, max 8)

- [ ] **Step 4: Verify build**

Run: `dotnet build Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Storefront/
git commit -m "feat(storefront): add CatalogController and ProductController with routes"
```

---

### Task 5: Shared View Partials

**Files:**
- Create: `Application/Entegrasyon.Storefront/Views/Shared/_ProductCard.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Shared/_Pagination.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Shared/_Breadcrumb.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Shared/_FilterSidebar.cshtml`

- [ ] **Step 1: Create _ProductCard.cshtml**

Model: `StorefrontProductCardDto`. Card with: lazy-loaded image, badges (YENI green if IsNew, -%X red if OldPrice set, SON STOK orange if TotalStock <= 5, TUKENDI red if TotalStock == 0), brand name (small gray), title (2 lines bold), old/new price, stock status. Links to `/urun/{SeoSlug}`. Tailwind responsive grid item.

- [ ] **Step 2: Create _Pagination.cshtml**

Receives page info from ViewBag (CurrentPage, TotalPages). Renders prev/next buttons + page numbers (max 5 around current). Preserves query string parameters via `Context.Request.QueryString`.

- [ ] **Step 3: Create _Breadcrumb.cshtml**

Receives `List<BreadcrumbItemDto>` from ViewBag.Breadcrumbs. Renders semantic nav with `>` separators. Last item is non-linked.

- [ ] **Step 4: Create _FilterSidebar.cshtml**

Form with GET method. Contains: price range (min/max inputs + apply button), sort dropdown (Oneri/Yeni/Fiyat artan/Fiyat azalan/Cok Satan). Submits as query parameters.

- [ ] **Step 5: Verify build and commit**

```bash
git add Application/Entegrasyon.Storefront/Views/Shared/
git commit -m "feat(storefront): add shared view partials (product card, pagination, breadcrumb, filters)"
```

---

### Task 6: Catalog + Product Views

**Files:**
- Create: `Application/Entegrasyon.Storefront/Views/Catalog/Categories.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Catalog/Category.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Catalog/Search.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Product/Detail.cshtml`

- [ ] **Step 1: Create Categories.cshtml**

Model: `List<CategoryTreeDto>`. Grid of category cards, each showing name + product count + subcategory links. Links to `/kategori/{SeoSlug}`.

- [ ] **Step 2: Create Category.cshtml (shared listing view)**

Used by Category, AllProducts, Brand, NewProducts, BestSellers. Layout: breadcrumb at top, page title, 2-column desktop (filter sidebar left, product grid right), sort dropdown, product cards via _ProductCard partial, pagination, empty state message if no products.

- [ ] **Step 3: Create Search.cshtml**

Similar to Category but shows search query and result count. Empty state with search prompt if no query.

- [ ] **Step 4: Create Product Detail.cshtml**

Model: `StorefrontProductDetailDto`. 2-column layout (image gallery left, info right). Shows: brand link, title, price, stock, variant attributes, quantity selector, "Sepete Ekle" button (disabled placeholder for SP-4). Tabs: description HTML, attributes table, delivery info, return info. Related products at bottom via _ProductCard partials.

- [ ] **Step 5: Verify build and commit**

```bash
git add Application/Entegrasyon.Storefront/Views/
git commit -m "feat(storefront): add catalog and product detail views"
```

---

### Task 7: HomeController Update + MegaMenu + Sitemap

**Files:**
- Modify: `Application/Entegrasyon.Storefront/Controllers/HomeController.cs`
- Modify: `Application/Entegrasyon.Storefront/Views/Home/Index.cshtml`
- Create: `Application/Entegrasyon.Storefront/ViewComponents/MegaMenuViewComponent.cs`
- Create: `Application/Entegrasyon.Storefront/Views/Shared/Components/MegaMenu/Default.cshtml`
- Modify: `Application/Entegrasyon.Storefront/Controllers/SeoController.cs`
- Modify: `Application/Entegrasyon.Storefront/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Update HomeController**

Add IProductService and ICategoryService to constructor. In Index(), fetch NewProducts(8), BestSellers(8), CategoryTree. Pass to ViewBag.

- [ ] **Step 2: Update Index.cshtml**

Add sections after trust badges: "One Cikan Kategoriler" (category cards), "Yeni Urunler" (product cards horizontal scroll + "Tumunu Gor" link), "Cok Satanlar" (same pattern).

- [ ] **Step 3: Create MegaMenuViewComponent**

Primary constructor DI: `(ICategoryService categoryService, IStorefrontTenantContext tenant, IMemoryCache cache)`. InvokeAsync: cache category tree for 30 min per tenant, return View with `List<CategoryTreeDto>`.

- [ ] **Step 4: Create MegaMenu Default.cshtml**

Horizontal nav bar with top-level categories. Hover shows dropdown with subcategories. Responsive hamburger on mobile.

- [ ] **Step 5: Add MegaMenu to _Layout.cshtml**

After header section: `@await Component.InvokeAsync("MegaMenu")`

- [ ] **Step 6: Update SeoController sitemap**

Add IProductService, ICategoryService to constructor. Replace hardcoded sitemap with dynamic: query all products with SeoSlug, all categories with SeoSlug, add as url entries. Keep legal pages.

- [ ] **Step 7: Verify build and commit**

```bash
git add Application/Entegrasyon.Storefront/
git commit -m "feat(storefront): update home vitrines, add mega menu, dynamic sitemap"
```

---

### Task 8: Search Autocomplete JS

**Files:**
- Create: `Application/Entegrasyon.Storefront/wwwroot/js/search.js`
- Modify: `Application/Entegrasyon.Storefront/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Create search.js**

Vanilla JS, no dependencies. Targets `[data-search-input]` elements.
- On input: 300ms debounce, fetch `/api/arama/oneri?q=...`, build dropdown with safe DOM methods (createElement, textContent — not innerHTML)
- Each suggestion: link with icon (product/category/brand), image thumbnail if available, text
- Enter key: navigate to `/arama?q=...`
- Click outside: close dropdown
- Min 2 characters to trigger

- [ ] **Step 2: Update _Layout.cshtml**

Add `data-search-input` attribute to header search input. Add `<script src="~/js/search.js"></script>` before closing body.

- [ ] **Step 3: Verify build and commit**

```bash
git add Application/Entegrasyon.Storefront/
git commit -m "feat(storefront): add search autocomplete with debounced suggestions"
```

---

### Task 9: Full Build + Test Verification

**Files:** None (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All tests pass

- [ ] **Step 3: Run storefront-specific tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Storefront"`
Expected: All storefront tests pass

- [ ] **Step 4: Run integration tests**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: All existing tests pass (no regression)

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "feat(storefront): SP-2 catalog complete"
```
