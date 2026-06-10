# Pazarama Phase 2: Ürün Publish + Stok/Fiyat Sync — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add product publishing, batch status polling, stock update, and price update capabilities for Pazarama marketplace (MarketPlaceId=5).

**Architecture:** Mirrors the proven Trendyol event-driven pipeline: Validator → Mapper → Service → Background Service → Batch Polling. Pazarama-specific differences: GUID IDs (via ExternalId string fields), separate stock/price endpoints, variant-as-product with groupCode, 10s rate-limited event batching. New Pazarama case added to existing multi-marketplace background services (EventChannel single-reader constraint).

**Tech Stack:** .NET 8, C# 12, EF Core (PostgreSQL), xUnit + Moq + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-22-pazarama-phase2-design.md`

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductRequestModels.cs` | Product create, stock/price update request DTOs |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaMappingValidator.cs` | Pre-publish validation (category/brand/attribute matches) |
| `Application/Entegrasyon.Business/Abstract/IPazaramaProductMapper.cs` | Mapper interface |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductMapper.cs` | Product → Pazarama request transformation |
| `Application/Entegrasyon.Business/Abstract/IPazaramaProductService.cs` | Product service interface |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductService.cs` | Publish + batch status check |
| `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaProductService.cs` | Mock product service |
| `Application/Entegrasyon.Business/Abstract/IPazaramaStockPriceService.cs` | Stock/price service interface |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaStockPriceService.cs` | Separate stock + price update endpoints |
| `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaStockPriceService.cs` | Mock stock/price service |
| `Application/Entegrasyon.Business/BackgroundServices/PazaramaBatchStatusPollingService.cs` | Timer-based batch polling (60s) |
| `Test/Entegrasyon.Test/Pazarama/PazaramaMappingValidatorTests.cs` | Validator tests |
| `Test/Entegrasyon.Test/Pazarama/PazaramaProductServiceTests.cs` | Product service + mock tests |
| `Test/Entegrasyon.Test/Pazarama/PazaramaStockPriceServiceTests.cs` | Stock/price service tests |

### Modified Files
| File | Change |
|------|--------|
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs` | Add batch status response DTOs |
| `Application/Entegrasyon.Business/BackgroundServices/TrendyolProductPublishBackgroundService.cs` | Add "Pazarama" case to switch |
| `Application/Entegrasyon.Business/BackgroundServices/TrendyolStockPriceSyncService.cs` | Add Pazarama case with 10s batching |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Register new services + BG service |

### Key Reference Files (DO NOT modify, read for patterns)
- `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolMappingValidator.cs` — validator pattern
- `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductMapper.cs` — mapper pattern
- `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductService.cs` — product service pattern
- `Application/Entegrasyon.Business/BackgroundServices/TrendyolBatchStatusPollingService.cs` — batch polling pattern
- `Application/Entegrasyon.Entity/Products/ProductMarketplace.cs` — status tracking entity
- `Test/Entegrasyon.Test/N11/N11MappingValidatorTests.cs` — validator test pattern
- `Test/Entegrasyon.Test/BaseTest.cs` — test base class

---

## Task 1: Request/Response DTOs

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductRequestModels.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs`

- [ ] **Step 1: Create product request DTOs**

```csharp
// Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductRequestModels.cs
using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed record PazaramaCreateProductRequest(
    [property: JsonPropertyName("products")] List<PazaramaProductItem> Products);

public sealed record PazaramaProductItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("brandId")] string BrandId,
    [property: JsonPropertyName("desi")] int Desi,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("groupCode")] string GroupCode,
    [property: JsonPropertyName("stockCode")] string StockCode,
    [property: JsonPropertyName("stockCount")] int StockCount,
    [property: JsonPropertyName("vatRate")] int VatRate,
    [property: JsonPropertyName("listPrice")] decimal ListPrice,
    [property: JsonPropertyName("salePrice")] decimal SalePrice,
    [property: JsonPropertyName("categoryId")] string CategoryId,
    [property: JsonPropertyName("currencyType")] string CurrencyType,
    [property: JsonPropertyName("images")] List<PazaramaProductImage> Images,
    [property: JsonPropertyName("attributes")] List<PazaramaProductAttribute> Attributes);

public sealed record PazaramaProductImage(
    [property: JsonPropertyName("imageurl")] string ImageUrl);

public sealed record PazaramaProductAttribute(
    [property: JsonPropertyName("attributeId")] string AttributeId,
    [property: JsonPropertyName("attributeValueId")] string AttributeValueId);

public sealed record PazaramaStockUpdateRequest(
    [property: JsonPropertyName("items")] List<PazaramaStockUpdateItem> Items);

public sealed record PazaramaStockUpdateItem(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("stockCount")] int StockCount);

public sealed record PazaramaPriceUpdateRequest(
    [property: JsonPropertyName("items")] List<PazaramaPriceUpdateItem> Items);

public sealed record PazaramaPriceUpdateItem(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("listPrice")] decimal ListPrice,
    [property: JsonPropertyName("salePrice")] decimal SalePrice);
```

- [ ] **Step 2: Add batch status response DTOs to PazaramaResponseModels.cs**

Append to the existing `PazaramaResponseModels.cs`:

```csharp
// Batch status for product create
public sealed record PazaramaBatchStatusResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("batchRequestId")] string BatchRequestId,
    [property: JsonPropertyName("batchResult")] List<object>? BatchResult,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("successfulCount")] int SuccessfulCount,
    [property: JsonPropertyName("isExcel")] bool IsExcel,
    [property: JsonPropertyName("failedCount")] int FailedCount,
    [property: JsonPropertyName("failedProducts")] List<PazaramaFailedProduct>? FailedProducts,
    [property: JsonPropertyName("creationDate")] string? CreationDate);

public sealed record PazaramaFailedProduct(
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("productCode")] string ProductCode,
    [property: JsonPropertyName("errorReason")] string ErrorReason);

// Batch status for stock/price updates
public sealed record PazaramaStockPriceBatchResponse(
    [property: JsonPropertyName("pageIndex")] int PageIndex,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("data")] List<PazaramaStockPriceBatchItem>? Data,
    [property: JsonPropertyName("successCount")] int SuccessCount,
    [property: JsonPropertyName("notCompletedCount")] int NotCompletedCount,
    [property: JsonPropertyName("failedCount")] int FailedCount,
    [property: JsonPropertyName("processingCount")] int ProcessingCount);

public sealed record PazaramaStockPriceBatchItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("price")] PazaramaPriceBatchDetail? Price,
    [property: JsonPropertyName("stock")] PazaramaStockBatchDetail? Stock,
    [property: JsonPropertyName("operationStatusText")] string OperationStatusText);

public sealed record PazaramaPriceBatchDetail(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("operationDetail")] string? OperationDetail,
    [property: JsonPropertyName("salePrice")] decimal SalePrice,
    [property: JsonPropertyName("listPrice")] decimal ListPrice);

public sealed record PazaramaStockBatchDetail(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("operationDetail")] string? OperationDetail,
    [property: JsonPropertyName("count")] int Count);
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductRequestModels.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs
git commit -m "feat(pazarama): add product request and batch status response DTOs"
```

---

## Task 2: PazaramaMappingValidator

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaMappingValidator.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaMappingValidatorTests.cs`

**Reference:** `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolMappingValidator.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/Pazarama/PazaramaMappingValidatorTests.cs
using Entegrasyon.Business.Concrete.Pazarama;
using FluentAssertions;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaMappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private PazaramaMappingValidator CreateSut() => new(mockContextFactory.Object);

    [Fact]
    public async Task ValidateAsync_WhenProductNotFound_ShouldReturnError()
    {
        // Arrange: mock empty MainProducts
        // The implementor should set up mockIntegrationDbContext.MainProducts
        // to return no matching product for the given productId
        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(Guid.NewGuid());
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    // Additional tests the implementor should write:
    // - WhenCategoryMatchMissing_ShouldReturnError
    // - WhenBrandMatchMissing_ShouldReturnError
    // - WhenRequiredAttributeMissing_ShouldReturnError
    // - WhenAllMappingsExist_ShouldReturnSuccess
    // - WhenBrandIdNull_ShouldReturnError (Pazarama requires brand)
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaMappingValidatorTests" -v n`

- [ ] **Step 3: Implement PazaramaMappingValidator**

Follow `TrendyolMappingValidator` pattern exactly but with these Pazarama-specific changes:
- Use `PazaramaMarketPlaceId` (5) instead of `TrendyolMarketPlaceId` (1)
- For category check: use `CategoryMarketplaces` table with `ExternalCategoryId != null` (not `CategoryMarketPlaceMatches`)
- For brand check: use `BrandMarketPlaceMatches` with `MarketPlaceBrandExternalId != null`
- For attribute check: use `MarketPlaceCategoryAttributeExternalId != null`
- Error messages in Turkish with "Pazarama" marketplace name

```csharp
// Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaMappingValidator.cs
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed class PazaramaMappingValidator(IDbContextFactory<IntegrationDbContext> contextFactory)
{
    public async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.BrandId, p.Title })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorResult("Urun bulunamadı.");

        var errors = new List<string>();

        // 1. Kategori eslestirmesi (CategoryMarketplace with ExternalCategoryId)
        var categoryMapped = await dbContext.CategoryMarketplaces
            .AnyAsync(m => m.Category.Id == product.CategoryId
                        && m.MarketPlaceId == PazaramaMarketPlaceId
                        && m.ExternalCategoryId != null);
        if (!categoryMapped)
            errors.Add("Urunun kategorisi Pazarama'ya eslestirilmemis.");

        // 2. Marka eslestirmesi
        if (product.BrandId is null)
        {
            errors.Add("Urunun markasi belirlenmemis.");
        }
        else
        {
            var brandMapped = await dbContext.BrandMarketPlaceMatches
                .AnyAsync(m => m.ApplicationBrandId == product.BrandId.Value
                            && m.MarketPlaceId == PazaramaMarketPlaceId
                            && m.MarketPlaceBrandExternalId != null);
            if (!brandMapped)
                errors.Add("Urunun markasi Pazarama'ya eslestirilmemis.");
        }

        // 3. Zorunlu ozellik eslestirmeleri
        var requiredAttrIds = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
            .Select(cac => cac.CategoryAttributeId)
            .ToListAsync();

        if (requiredAttrIds.Count > 0)
        {
            var mappedAttrIds = await dbContext.CategoryAttributeMarketPlaceMatches
                .Where(m => requiredAttrIds.Contains(m.ApplicationCategoryAttributeId)
                         && m.MarketPlaceId == PazaramaMarketPlaceId
                         && m.MarketPlaceCategoryAttributeExternalId != null)
                .Select(m => m.ApplicationCategoryAttributeId)
                .ToListAsync();

            var unmappedCount = requiredAttrIds.Count - mappedAttrIds.Count;
            if (unmappedCount > 0)
                errors.Add($"{unmappedCount} zorunlu ozellik Pazarama'ya eslestirilmemis.");
        }

        if (errors.Count > 0)
            return new ErrorResult($"'{product.Title}' gonderilemez: {string.Join(" ", errors)}");

        return new SuccessResult();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaMappingValidatorTests" -v n`

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaMappingValidator.cs Test/Entegrasyon.Test/Pazarama/PazaramaMappingValidatorTests.cs
git commit -m "feat(pazarama): add PazaramaMappingValidator for pre-publish validation"
```

---

## Task 3: IPazaramaProductMapper + PazaramaProductMapper

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPazaramaProductMapper.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductMapper.cs`

**Reference:** `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductMapper.cs`

- [ ] **Step 1: Create interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IPazaramaProductMapper.cs
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaProductMapper
{
    Task<IDataResult<PazaramaCreateProductRequest>> MapProductAsync(Guid productId);
}
```

- [ ] **Step 2: Implement PazaramaProductMapper**

Key differences from TrendyolProductMapper:
- **GUID IDs:** `brandId`, `categoryId`, `attributeId`, `attributeValueId` are string GUIDs from ExternalId fields
- **Category lookup:** Use `CategoryMarketplaces.ExternalCategoryId` (not `CategoryMarketPlaceMatches.MarketPlaceCategoryId`)
- **Brand lookup:** Use `BrandMarketPlaceMatches.MarketPlaceBrandExternalId`
- **Attribute lookup:** Use `MarketPlaceCategoryAttributeExternalId` and `MarketPlaceCategoryAttributeValueExternalId`
- **Each variant = separate product:** Each variant becomes a `PazaramaProductItem` with its own name/description
- **groupCode:** `product.StockCode?.Substring(0, Math.Min(10, product.StockCode.Length)) ?? product.Id.ToString("N")[..10]`
- **currencyType:** Always `"TRY"`
- **desi:** From product (default 1 if not set)

The implementor should follow the TrendyolProductMapper pattern closely:
1. Load product with Include chains (variants, images, attributes, branchOfficeStocks)
2. Load ProductMarketplace overrides
3. Batch-load all mappings into dictionaries
4. Determine warehouse IDs (MarketPlaceWarehouse → fallback to IsDefaultMarketPlaceStock)
5. For each variant: build PazaramaProductItem with GUID string IDs

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPazaramaProductMapper.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductMapper.cs
git commit -m "feat(pazarama): add PazaramaProductMapper with variant-as-product and GUID mapping"
```

---

## Task 4: IPazaramaProductService + Mock + Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPazaramaProductService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaProductService.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaProductServiceTests.cs`

- [ ] **Step 1: Create interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IPazaramaProductService.cs
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaProductService
{
    Task<IDataResult<string>> PublishProductAsync(Guid productId);
    Task<IDataResult<PazaramaBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId);
}
```

- [ ] **Step 2: Write failing tests**

Tests should cover:
- `PublishProductAsync` — validation fails → returns error
- `PublishProductAsync` — mapping fails → returns error
- `PublishProductAsync` — API success → returns batchRequestId
- `CheckBatchStatusAsync` — returns parsed batch status
- Mock service tests: returns mock batchRequestId, increments

- [ ] **Step 3: Implement PazaramaProductService**

Follow `TrendyolProductService` pattern:
1. `PublishProductAsync`:
   - Call `PazaramaMappingValidator.ValidateProductMappingsAsync()` → error if fails
   - Call `IPazaramaProductMapper.MapProductAsync()` → error if fails
   - `IPazaramaApiClient.PostAsync("product/create", request)` → parse `PazaramaResponse<PazaramaBatchResponse>` with `batchRequestId`
   - Log via `IProductActivityLogger`
   - Return batchRequestId

2. `CheckBatchStatusAsync`:
   - `IPazaramaApiClient.GetAsync($"product/getProductBatchResult?BatchRequestId={batchRequestId}")`
   - Parse `PazaramaResponse<PazaramaBatchStatusResponse>`
   - Status: 1=InProgress, 2=Done, 3=Error

- [ ] **Step 4: Implement MockPazaramaProductService**

```csharp
// Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaProductService.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed class MockPazaramaProductService(
    ILogger<MockPazaramaProductService> logger) : IPazaramaProductService
{
    private int _counter;

    public Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        var batchId = $"mock-pazarama-batch-{Interlocked.Increment(ref _counter)}";
        logger.LogInformation("MockPazarama: PublishProduct {ProductId} → {BatchId}", productId, batchId);
        return Task.FromResult<IDataResult<string>>(new SuccessDataResult<string>(batchId));
    }

    public Task<IDataResult<PazaramaBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId)
    {
        var response = new PazaramaBatchStatusResponse(2, batchRequestId, null, 1, 1, false, 0, null, null);
        return Task.FromResult<IDataResult<PazaramaBatchStatusResponse>>(
            new SuccessDataResult<PazaramaBatchStatusResponse>(response));
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaProductServiceTests" -v n`

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPazaramaProductService.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaProductService.cs Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaProductService.cs Test/Entegrasyon.Test/Pazarama/PazaramaProductServiceTests.cs
git commit -m "feat(pazarama): add PazaramaProductService with publish and batch status check"
```

---

## Task 5: IPazaramaStockPriceService + Mock + Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPazaramaStockPriceService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaStockPriceService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaStockPriceService.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaStockPriceServiceTests.cs`

- [ ] **Step 1: Create interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IPazaramaStockPriceService.cs
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaStockPriceService
{
    Task<IDataResult<string>> UpdateStockAsync(List<PazaramaStockUpdateItem> items);
    Task<IDataResult<string>> UpdatePriceAsync(List<PazaramaPriceUpdateItem> items);
}
```

- [ ] **Step 2: Write failing tests**

Tests:
- `UpdateStockAsync` — sends correct request to `/product/updateStock-v2`, returns dataId
- `UpdatePriceAsync` — sends correct request to `/product/updatePrice-v2`, returns dataId
- API failure → returns error
- Mock service tests

- [ ] **Step 3: Implement PazaramaStockPriceService**

```csharp
// Key implementation — two separate API calls
public async Task<IDataResult<string>> UpdateStockAsync(List<PazaramaStockUpdateItem> items)
{
    var request = new PazaramaStockUpdateRequest(items);
    var response = await apiClient.PostAsync("product/updateStock-v2", request);
    // Parse PazaramaResponse<string> — data field is the dataId
    ...
}

public async Task<IDataResult<string>> UpdatePriceAsync(List<PazaramaPriceUpdateItem> items)
{
    var request = new PazaramaPriceUpdateRequest(items);
    var response = await apiClient.PostAsync("product/updatePrice-v2", request);
    ...
}
```

- [ ] **Step 4: Implement MockPazaramaStockPriceService**

Returns mock dataId strings.

- [ ] **Step 5: Run tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaStockPriceServiceTests" -v n`

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPazaramaStockPriceService.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaStockPriceService.cs Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaStockPriceService.cs Test/Entegrasyon.Test/Pazarama/PazaramaStockPriceServiceTests.cs
git commit -m "feat(pazarama): add PazaramaStockPriceService with separate stock and price endpoints"
```

---

## Task 6: Add Pazarama Case to ProductPublishBackgroundService

**Files:**
- Modify: `Application/Entegrasyon.Business/BackgroundServices/TrendyolProductPublishBackgroundService.cs`

**IMPORTANT:** This service already has switch-case routing for Trendyol and Hepsiburada. Add "Pazarama" case. Do NOT create a separate background service (EventChannel single-reader constraint).

- [ ] **Step 1: Read the existing file**

Read `TrendyolProductPublishBackgroundService.cs` to understand the switch-case structure.

- [ ] **Step 2: Add Pazarama case**

In `ExecuteAsync` method, add after the `"Hepsiburada"` case:
```csharp
case "Pazarama":
    await HandlePazaramaAsync(scope.ServiceProvider, evt.ProductId, stoppingToken);
    break;
```

Add the handler method:
```csharp
private async Task HandlePazaramaAsync(IServiceProvider services, Guid productId, CancellationToken ct)
{
    var dbContext = services.GetRequiredService<IntegrationDbContext>();
    var pazaramaService = services.GetRequiredService<IPazaramaProductService>();

    var record = await dbContext.ProductMarketplaces
        .FirstOrDefaultAsync(pm => pm.ProductId == productId &&
                                   pm.MarketPlace.Name == "Pazarama", ct);

    if (record is null)
    {
        logger.LogWarning("ProductMarketplace record not found for ProductId={ProductId}, Pazarama", productId);
        return;
    }

    try
    {
        var result = await pazaramaService.PublishProductAsync(productId);

        if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
        {
            record.BatchRequestId = result.Data;
            logger.LogInformation("Product {ProductId} published to Pazarama. BatchId={BatchId}", productId, result.Data);
        }
        else if (result.Success)
        {
            record.Status = MarketplaceProductStatus.Failed;
            record.StatusMessage = "Pazarama API basarili dondu ancak BatchRequestId bos geldi.";
        }
        else
        {
            record.Status = MarketplaceProductStatus.Failed;
            record.StatusMessage = result.Message;
            logger.LogWarning("Product {ProductId} failed to publish to Pazarama: {Message}", productId, result.Message);
        }
    }
    catch (Exception ex)
    {
        record.Status = MarketplaceProductStatus.Failed;
        record.StatusMessage = $"Publish istegi sirasinda hata: {ex.Message}";
        logger.LogError(ex, "Exception during Pazarama publish for product {ProductId}", productId);
    }

    await dbContext.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/BackgroundServices/TrendyolProductPublishBackgroundService.cs
git commit -m "feat(pazarama): add Pazarama case to ProductPublishBackgroundService"
```

---

## Task 7: PazaramaBatchStatusPollingService

**Files:**
- Create: `Application/Entegrasyon.Business/BackgroundServices/PazaramaBatchStatusPollingService.cs`

**Reference:** `TrendyolBatchStatusPollingService.cs` — follow same pattern but with `MarketPlaceId=5` filter (HB pattern, not Trendyol's unfiltered bug)

- [ ] **Step 1: Implement batch polling service**

Key implementation:
- Timer-based: 60s interval, 10s startup delay
- Query: `ProductMarketplaces.Where(pm => pm.MarketPlaceId == PazaramaMarketPlaceId && pm.Status == Pending && pm.BatchRequestId != null)`
- For each record: call `IPazaramaProductService.CheckBatchStatusAsync(batchRequestId)`
- Pazarama batch status: 1=InProgress, 2=Done, 3=Error
- Done + failedCount==0 → Published; Error or failedCount>0 → Failed (with error messages)
- 24h timeout → Failed
- Activity logging via `IProductActivityLogger`

- [ ] **Step 2: Build and run all tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/BackgroundServices/PazaramaBatchStatusPollingService.cs
git commit -m "feat(pazarama): add PazaramaBatchStatusPollingService with 60s polling"
```

---

## Task 8: Add Pazarama Case to StockPriceSyncService

**Files:**
- Modify: `Application/Entegrasyon.Business/BackgroundServices/TrendyolStockPriceSyncService.cs`

**IMPORTANT:** Do NOT create separate service. Add Pazarama handling to existing service. Pazarama needs 10s event batching due to rate limit.

- [ ] **Step 1: Read current service**

Read `TrendyolStockPriceSyncService.cs` to understand the current flow.

- [ ] **Step 2: Add Pazarama handling**

After the Trendyol stock/price update in the event loop, add Pazarama check:
- Check if product is Published on Pazarama (MarketPlaceId=5)
- If yes: aggregate stock from Pazarama warehouses, prepare stock+price items
- Call `IPazaramaStockPriceService.UpdateStockAsync()` and `UpdatePriceAsync()`

For 10s batching, the implementor should consider a simple approach: since the event loop already processes events sequentially, and the rate limit is per-client (not per-request), adding a `Task.Delay(TimeSpan.FromSeconds(10))` after Pazarama API calls is the simplest solution that respects the rate limit. A more sophisticated Channel-based buffer can be added later if needed.

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/BackgroundServices/TrendyolStockPriceSyncService.cs
git commit -m "feat(pazarama): add Pazarama stock/price sync to existing background service"
```

---

## Task 9: DI Registration

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Add Pazarama service registrations**

In the existing Pazarama DI block, add:
```csharp
// Pazarama Faz 2 servisleri
services.AddScoped<PazaramaMappingValidator>();
services.AddScoped<IPazaramaProductMapper, PazaramaProductMapper>();

if (usePazaramaMock)
{
    // Mock block'a ekle:
    services.AddScoped<IPazaramaProductService, MockPazaramaProductService>();
    services.AddScoped<IPazaramaStockPriceService, MockPazaramaStockPriceService>();
}
else
{
    // Real block'a ekle:
    services.AddScoped<IPazaramaProductService, PazaramaProductService>();
    services.AddScoped<IPazaramaStockPriceService, PazaramaStockPriceService>();
}
```

In `AddBackgroundServices`:
```csharp
services.AddHostedService<PazaramaBatchStatusPollingService>();
```

- [ ] **Step 2: Build and run all tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(pazarama): register Phase 2 services and background service in DI"
```

---

## Task 10: Final Verification

- [ ] **Step 1: Full build**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 errors

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`
Expected: All pass

- [ ] **Step 3: Run Pazarama tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Pazarama" -v n`
Expected: All Pazarama tests pass (Phase 1 + Phase 2)

- [ ] **Step 4: Verify git log**

Run: `git log --oneline -15`
Verify all Phase 2 commits are present.
