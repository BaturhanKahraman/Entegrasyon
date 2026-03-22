# Çiçeksepeti Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Çiçeksepeti (MarketPlaceId=8) marketplace integration with full product, order, return, and Q&A support.

**Architecture:** Pazarama pattern clone — Scoped ApiClient with static multi-tenant credential cache, `x-api-key` header auth, async batch operations with polling. All services follow primary constructor DI, Result pattern, and dual-logging.

**Tech Stack:** .NET 8, C# 12, EF Core (PostgreSQL), xUnit + Moq + FluentAssertions, Polly (retry)

**Design Spec:** `docs/superpowers/specs/2026-03-22-ciceksepeti-integration-design.md`
**API Docs:** `docs/ciceksepeti/` (10 files)

---

## File Structure

### New Files — Business/Concrete/Ciceksepeti/
| File | Responsibility |
|------|---------------|
| `CiceksepetiApiClient.cs` | HTTP client, x-api-key auth, multi-tenant credential cache |
| `MockCiceksepetiApiClient.cs` | In-memory mock for dev/test |
| `CiceksepetiResponseModels.cs` | All API response DTOs |
| `CiceksepetiRequestModels.cs` | All API request DTOs |
| `CiceksepetiCategoryService.cs` | Category & attribute fetch |
| `CiceksepetiProductService.cs` | Product CRUD + batch status |
| `CiceksepetiProductMapper.cs` | Product entity → CS DTO |
| `CiceksepetiMappingValidator.cs` | Mapping completeness check |
| `CiceksepetiStockPriceService.cs` | Stock/price batch update |
| `CiceksepetiOrderService.cs` | Orders + all cargo ops |
| `CiceksepetiInvoiceService.cs` | Invoice PDF send |
| `CiceksepetiReturnService.cs` | Return list/confirm/evaluate |
| `CiceksepetiQnAService.cs` | Q&A management |

### New Files — Business/Abstract/
| File | Responsibility |
|------|---------------|
| `ICiceksepetiApiClient.cs` | ApiClient interface |
| `ICiceksepetiCategoryService.cs` | Category service interface |
| `ICiceksepetiCategoryImporter.cs` | Category importer interface |
| `ICiceksepetiProductService.cs` | Product service interface |
| `ICiceksepetiProductMapper.cs` | Product mapper interface |
| `ICiceksepetiStockPriceService.cs` | Stock/price service interface |
| `ICiceksepetiOrderService.cs` | Order service interface |
| `ICiceksepetiInvoiceService.cs` | Invoice service interface |
| `ICiceksepetiReturnService.cs` | Return service interface |
| `ICiceksepetiQnAService.cs` | Q&A service interface |

### New Files — Business/BackgroundServices/
| File | Responsibility |
|------|---------------|
| `CiceksepetiBatchStatusPollingService.cs` | Poll batch results |
| `CiceksepetiOrderPollingService.cs` | Poll new orders |
| `CiceksepetiStockPriceSyncService.cs` | Periodic stock/price sync |

### New Files — Business/Concrete/Import/
| File | Responsibility |
|------|---------------|
| `CiceksepetiCategoryImporter.cs` | extends BaseCategoryImporterService |

### New Files — Tests
| File | Responsibility |
|------|---------------|
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiApiClientTests.cs` | ApiClient unit tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiResponseModelsTests.cs` | JSON deserialization tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiCategoryServiceTests.cs` | Category service tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiProductServiceTests.cs` | Product service tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiProductMapperTests.cs` | Product mapper tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiMappingValidatorTests.cs` | Mapping validator tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiStockPriceServiceTests.cs` | Stock/price tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiOrderServiceTests.cs` | Order service tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiInvoiceServiceTests.cs` | Invoice service tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiReturnServiceTests.cs` | Return service tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiQnAServiceTests.cs` | Q&A service tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiBatchStatusPollingServiceTests.cs` | Batch polling tests |
| `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiOrderPollingServiceTests.cs` | Order polling tests |

### Modified Files
| File | Change |
|------|--------|
| `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs` | Add `CiceksepetiMarketPlaceId = 8` |
| `Application/Entegrasyon.Entity/Categories/ImportSource.cs` | Add `PttAvm = 105, Ciceksepeti = 106` |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Add Çiçeksepeti DI registrations |

---

## Task 1: Constants & ImportSource

**Files:**
- Modify: `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs`
- Modify: `Application/Entegrasyon.Entity/Categories/ImportSource.cs`

- [ ] **Step 1: Add CiceksepetiMarketPlaceId constant**

```csharp
// In MarketPlaceConstants.cs, add after AmazonMarketPlaceId:
public const int CiceksepetiMarketPlaceId = 8;
```

- [ ] **Step 2: Add PttAvm and Ciceksepeti to ImportSource enum**

```csharp
// In ImportSource.cs, add after Amazon = 104:

/// <summary>
/// PttAVM pazaryerinden import edilmiş (reserved — MarketPlaceId=7)
/// </summary>
PttAvm = 105,

/// <summary>
/// Çiçeksepeti pazaryerinden import edilmiş
/// </summary>
Ciceksepeti = 106
```

> **Not:** PttAvm=105 henüz implement edilmedi ama MarketPlaceId=7 olarak planlandı. Çakışmayı önlemek için enum değeri şimdi reserve ediliyor.

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 4: Run existing tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All tests pass (no regression)

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs Application/Entegrasyon.Entity/Categories/ImportSource.cs
git commit -m "feat(ciceksepeti): add MarketPlaceId=8 constant and ImportSource.Ciceksepeti"
```

---

## Task 2: ICiceksepetiApiClient Interface & Response/Request Models

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiApiClient.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiResponseModels.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiRequestModels.cs`

**Ref docs:** `docs/ciceksepeti/BASE_KNOWLEDGE.md`, `docs/ciceksepeti/PRODUCT_API.md`, `docs/ciceksepeti/ORDER_API.md`, `docs/ciceksepeti/RETURN_API.md`, `docs/ciceksepeti/QNA_API.md`
**Ref code:** `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs`

> **Not:** Response/request DTO'ları API dökümanlarından derive edilmiştir. `HttpResponseMessage` return type kullanılır (Pazarama pattern ile tutarlı) — servis katmanı deserialization'ı yapar.

- [ ] **Step 1: Create ICiceksepetiApiClient interface**

```csharp
namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Çiçeksepeti API'ye x-api-key header ile HTTP çağrıları yapan wrapper.
/// MarketPlace tablosundan (Id=8) ApiKey çeker.
/// </summary>
public interface ICiceksepetiApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct = default);

    /// <summary>
    /// Farklı path prefix gerektiren endpoint'ler için (ör. /Branch/SendInvoiceMail).
    /// Base URL'e /api/v1/ eklemez, verilen path'i doğrudan kullanır.
    /// </summary>
    Task<HttpResponseMessage> SendRawAsync(string absolutePath, HttpMethod method, HttpContent? content = null, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create CiceksepetiResponseModels.cs**

```csharp
using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

// --- Category Models ---

public sealed record CiceksepetiCategoryResponse(
    [property: JsonPropertyName("categories")] List<CiceksepetiCategoryDto> Categories);

public sealed record CiceksepetiCategoryDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("parentCategoryId")] int? ParentCategoryId,
    [property: JsonPropertyName("subCategories")] List<CiceksepetiCategoryDto> SubCategories);

public sealed record CiceksepetiCategoryAttributeResponse(
    [property: JsonPropertyName("categoryId")] int CategoryId,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("categoryAttributes")] List<CiceksepetiAttributeDto> CategoryAttributes);

public sealed record CiceksepetiAttributeDto(
    [property: JsonPropertyName("attributeId")] int AttributeId,
    [property: JsonPropertyName("attributeName")] string AttributeName,
    [property: JsonPropertyName("required")] bool Required,
    [property: JsonPropertyName("varianter")] bool Varianter,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("attributeValues")] List<CiceksepetiAttributeValueDto> AttributeValues);

public sealed record CiceksepetiAttributeValueDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

// --- Product Models ---

public sealed record CiceksepetiBatchResponse(
    [property: JsonPropertyName("batchId")] string BatchId);

public sealed record CiceksepetiBatchStatusResponse(
    [property: JsonPropertyName("batchId")] string BatchId,
    [property: JsonPropertyName("itemCount")] int ItemCount,
    [property: JsonPropertyName("items")] List<CiceksepetiBatchItemDto> Items);

public sealed record CiceksepetiBatchItemDto(
    [property: JsonPropertyName("data")] CiceksepetiBatchItemData? Data,
    [property: JsonPropertyName("itemId")] string ItemId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("failureReasons")] List<CiceksepetiFailureReason>? FailureReasons,
    [property: JsonPropertyName("lastModificationDate")] string? LastModificationDate);

public sealed record CiceksepetiBatchItemData(
    [property: JsonPropertyName("siteCode")] string? SiteCode,
    [property: JsonPropertyName("stockCode")] string? StockCode,
    [property: JsonPropertyName("stockQuantity")] int? StockQuantity,
    [property: JsonPropertyName("listPrice")] decimal? ListPrice,
    [property: JsonPropertyName("salesPrice")] decimal? SalesPrice);

public sealed record CiceksepetiFailureReason(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("code")] string? Code);

public sealed record CiceksepetiProductListResponse(
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("products")] List<CiceksepetiProductDto> Products);

public sealed record CiceksepetiProductDto(
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("productCode")] string? ProductCode,
    [property: JsonPropertyName("categoryId")] int CategoryId,
    [property: JsonPropertyName("categoryName")] string? CategoryName,
    [property: JsonPropertyName("stockCode")] string StockCode,
    [property: JsonPropertyName("mainProductCode")] string MainProductCode,
    [property: JsonPropertyName("productStatusType")] int ProductStatusType,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("salesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("StockQuantity")] int StockQuantity,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("images")] List<string>? Images,
    [property: JsonPropertyName("attributes")] List<CiceksepetiProductAttributeDto>? Attributes);

public sealed record CiceksepetiProductAttributeDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("ValueId")] int ValueId,
    [property: JsonPropertyName("TextLength")] int TextLength);

// --- Order Models ---

public sealed record CiceksepetiOrderListResponse(
    [property: JsonPropertyName("orderListCount")] int OrderListCount,
    [property: JsonPropertyName("supplierOrderListWithBranch")] List<CiceksepetiOrderItemDto> SupplierOrderListWithBranch);

public sealed record CiceksepetiOrderItemDto(
    [property: JsonPropertyName("branchId")] int BranchId,
    [property: JsonPropertyName("orderId")] long OrderId,
    [property: JsonPropertyName("orderItemId")] long OrderItemId,
    [property: JsonPropertyName("orderItemStatusId")] int OrderItemStatusId,
    [property: JsonPropertyName("orderDate")] string? OrderDate,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("productCode")] string? ProductCode,
    [property: JsonPropertyName("stockCode")] string? StockCode,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("salesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("listPrice")] decimal ListPrice,
    [property: JsonPropertyName("invoicePrice")] decimal InvoicePrice,
    [property: JsonPropertyName("allowanceRate")] decimal AllowanceRate,
    [property: JsonPropertyName("receiverName")] string? ReceiverName,
    [property: JsonPropertyName("receiverAddress")] string? ReceiverAddress,
    [property: JsonPropertyName("receiverCity")] string? ReceiverCity,
    [property: JsonPropertyName("receiverDistrict")] string? ReceiverDistrict,
    [property: JsonPropertyName("receiverPhone")] string? ReceiverPhone,
    [property: JsonPropertyName("senderName")] string? SenderName,
    [property: JsonPropertyName("cargoCompany")] string? CargoCompany,
    [property: JsonPropertyName("cargoTrackingNumber")] string? CargoTrackingNumber,
    [property: JsonPropertyName("cargoTrackingUrl")] string? CargoTrackingUrl,
    [property: JsonPropertyName("deliveryType")] int DeliveryType,
    [property: JsonPropertyName("deliveryMessageType")] int DeliveryMessageType,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("cancellationResult")] int? CancellationResult,
    [property: JsonPropertyName("note")] string? Note);

// --- Return Models ---

public sealed record CiceksepetiReturnListResponse(
    [property: JsonPropertyName("orderItemList")] List<CiceksepetiReturnItemDto> OrderItemList);

public sealed record CiceksepetiReturnItemDto(
    [property: JsonPropertyName("orderId")] long OrderId,
    [property: JsonPropertyName("orderItemId")] long OrderItemId,
    [property: JsonPropertyName("orderItemStatusId")] int OrderItemStatusId,
    [property: JsonPropertyName("customerName")] string? CustomerName,
    [property: JsonPropertyName("salesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("cancelReason")] string? CancelReason,
    [property: JsonPropertyName("cancelStatusId")] int? CancelStatusId,
    [property: JsonPropertyName("cargoCompany")] string? CargoCompany,
    [property: JsonPropertyName("cargoTrackingNumber")] string? CargoTrackingNumber,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("stockCode")] string? StockCode);

// --- Q&A Models ---

public sealed record CiceksepetiQuestionListResponse(
    [property: JsonPropertyName("items")] List<CiceksepetiQuestionDto> Items,
    [property: JsonPropertyName("hasNextPage")] bool HasNextPage);

public sealed record CiceksepetiQuestionDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("answer")] string? Answer,
    [property: JsonPropertyName("answered")] bool Answered,
    [property: JsonPropertyName("createdDate")] string? CreatedDate,
    [property: JsonPropertyName("product")] CiceksepetiQuestionProductDto? Product,
    [property: JsonPropertyName("branchActionId")] int? BranchActionId,
    [property: JsonPropertyName("approve")] bool? Approve);

public sealed record CiceksepetiQuestionProductDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl);

public sealed record CiceksepetiActionListResponse(
    [property: JsonPropertyName("actions")] List<CiceksepetiActionDto> Actions);

public sealed record CiceksepetiActionDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("details")] List<CiceksepetiActionDetailDto> Details);

public sealed record CiceksepetiActionDetailDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);
```

- [ ] **Step 3: Create CiceksepetiRequestModels.cs**

```csharp
using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

// --- Product Create/Update ---

public sealed record CiceksepetiCreateProductsRequest(
    [property: JsonPropertyName("products")] List<CiceksepetiProductRequest> Products);

public sealed record CiceksepetiProductRequest(
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("mainProductCode")] string MainProductCode,
    [property: JsonPropertyName("stockCode")] string StockCode,
    [property: JsonPropertyName("categoryId")] int CategoryId,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("deliveryType")] int DeliveryType,
    [property: JsonPropertyName("deliveryMessageType")] int DeliveryMessageType,
    [property: JsonPropertyName("stockQuantity")] int StockQuantity,
    [property: JsonPropertyName("salesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("listPrice")] decimal? ListPrice,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("isActive")] bool? IsActive,
    [property: JsonPropertyName("images")] List<string> Images,
    [property: JsonPropertyName("Attributes")] List<CiceksepetiAttributeRequest>? Attributes);

public sealed record CiceksepetiAttributeRequest(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("ValueId")] int ValueId,
    [property: JsonPropertyName("TextLength")] int TextLength);

// --- Stock & Price ---

public sealed record CiceksepetiStockPriceUpdateRequest(
    [property: JsonPropertyName("items")] List<CiceksepetiStockPriceItem> Items);

public sealed record CiceksepetiStockPriceItem(
    [property: JsonPropertyName("stockCode")] string StockCode,
    [property: JsonPropertyName("StockQuantity")] int? StockQuantity,
    [property: JsonPropertyName("salesPrice")] decimal? SalesPrice,
    [property: JsonPropertyName("listPrice")] decimal? ListPrice);

// --- Order ---

public sealed record CiceksepetiGetOrdersRequest(
    [property: JsonPropertyName("startDate")] string? StartDate,
    [property: JsonPropertyName("endDate")] string? EndDate,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("statusId")] int? StatusId,
    [property: JsonPropertyName("orderNo")] long? OrderNo,
    [property: JsonPropertyName("orderItemNo")] long? OrderItemNo);

// --- Cargo ---

public sealed record CiceksepetiCsCargoRequest(
    [property: JsonPropertyName("orderItemsGroup")] List<CiceksepetiCargoGroup> OrderItemsGroup);

public sealed record CiceksepetiCargoGroup(
    [property: JsonPropertyName("orderItemIds")] List<int> OrderItemIds);

public sealed record CiceksepetiOwnCargoRequest(
    [property: JsonPropertyName("orderItems")] List<CiceksepetiOwnCargoItem> OrderItems);

public sealed record CiceksepetiOwnCargoItem(
    [property: JsonPropertyName("orderItemId")] int OrderItemId,
    [property: JsonPropertyName("orderItemStatusId")] int OrderItemStatusId,
    [property: JsonPropertyName("cargoBusinessId")] int? CargoBusinessId,
    [property: JsonPropertyName("shipmentNumber")] string? ShipmentNumber,
    [property: JsonPropertyName("shipmentTrackingUrl")] string? ShipmentTrackingUrl,
    [property: JsonPropertyName("receiverName")] string? ReceiverName,
    [property: JsonPropertyName("deliveryTime")] string? DeliveryTime);

public sealed record CiceksepetiChangeCargoRequest(
    [property: JsonPropertyName("items")] List<CiceksepetiChangeCargoItem> Items);

public sealed record CiceksepetiChangeCargoItem(
    [property: JsonPropertyName("orderProductId")] int OrderProductId,
    [property: JsonPropertyName("cargoId")] int CargoId);

public sealed record CiceksepetiCargoMeasurementRequest(
    [property: JsonPropertyName("items")] List<CiceksepetiCargoMeasurementItem> Items);

public sealed record CiceksepetiCargoMeasurementItem(
    [property: JsonPropertyName("orderProductId")] int OrderProductId,
    [property: JsonPropertyName("desi")] decimal Desi,
    [property: JsonPropertyName("quantity")] int Quantity);

public sealed record CiceksepetiDigitalCodeRequest(
    [property: JsonPropertyName("items")] List<CiceksepetiDigitalCodeItem> Items);

public sealed record CiceksepetiDigitalCodeItem(
    [property: JsonPropertyName("orderProductId")] int OrderProductId,
    [property: JsonPropertyName("receiverName")] string ReceiverName,
    [property: JsonPropertyName("deliveryTime")] string DeliveryTime);

public sealed record CiceksepetiLaborCostRequest(
    [property: JsonPropertyName("items")] List<CiceksepetiLaborCostItem> Items);

public sealed record CiceksepetiLaborCostItem(
    [property: JsonPropertyName("orderProductId")] int OrderProductId,
    [property: JsonPropertyName("laborCost")] double LaborCost);

// --- Invoice ---

public sealed record CiceksepetiInvoiceRequest(
    [property: JsonPropertyName("items")] List<CiceksepetiInvoiceItem> Items);

public sealed record CiceksepetiInvoiceItem(
    [property: JsonPropertyName("orderItemId")] int OrderItemId,
    [property: JsonPropertyName("document")] string? Document,
    [property: JsonPropertyName("documentUrl")] string? DocumentUrl);

// --- Return ---

public sealed record CiceksepetiGetReturnsRequest(
    [property: JsonPropertyName("orderItemStatusId")] int? OrderItemStatusId,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("startDate")] string? StartDate,
    [property: JsonPropertyName("endDate")] string? EndDate);

public sealed record CiceksepetiReturnReceivedRequest(
    [property: JsonPropertyName("orderItemIds")] List<int> OrderItemIds);

public sealed record CiceksepetiReturnEvaluationRequest(
    [property: JsonPropertyName("orderItemId")] int OrderItemId,
    [property: JsonPropertyName("process")] int Process);

// --- Q&A ---

public sealed record CiceksepetiAnswerQuestionRequest(
    [property: JsonPropertyName("answer")] string? Answer,
    [property: JsonPropertyName("branchActionId")] int BranchActionId,
    [property: JsonPropertyName("branchActionDetailId")] int? BranchActionDetailId,
    [property: JsonPropertyName("branchDescription")] string? BranchDescription);
```

- [ ] **Step 4: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ICiceksepetiApiClient.cs Application/Entegrasyon.Business/Concrete/Ciceksepeti/
git commit -m "feat(ciceksepeti): add API client interface and request/response models"
```

---

## Task 3: CiceksepetiApiClient Implementation & Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiApiClient.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/MockCiceksepetiApiClient.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiApiClientTests.cs`

**Ref docs:** `docs/ciceksepeti/BASE_KNOWLEDGE.md` (auth, rate limits, base URL)
**Ref code:** `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaApiClient.cs` (pattern to follow)

- [ ] **Step 1: Write failing tests for ApiClient**

Tests must cover:
1. `GetAsync` injects `x-api-key` header from MarketPlace.ApiKey
2. `PostAsync` sends JSON body with correct content-type
3. `SendRawAsync` uses absolute path (no `/api/v1/` prefix)
4. Credential cache hit — doesn't query DB on second call
5. Handles missing MarketPlace record → throws

Use `MockHttpMessageHandler` to intercept HTTP requests and verify headers.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CiceksepetiApiClient"`
Expected: FAIL — classes don't exist yet

- [ ] **Step 3: Implement CiceksepetiApiClient**

Key implementation details:
- Primary constructor: `(IDbContextFactory<IntegrationDbContext> contextFactory, IHttpClientFactory httpClientFactory, ILogger<CiceksepetiApiClient> logger)`
- `static ConcurrentDictionary<int, (string ApiKey, string BaseUrl)> _credentialCache` — survives scoped lifetime
- `static ConcurrentDictionary<int, SemaphoreSlim> _credentialLocks` — per-tenant lock
- Default base URL: `https://apis.ciceksepeti.com`
- All requests: `x-api-key` header
- `SendRawAsync`: skips `/api/v1/` prefix, uses path directly relative to base URL
- MarketPlace lookup: `dbContext.MarketPlaces.AsNoTracking().FirstOrDefaultAsync(m => m.Id == CiceksepetiMarketPlaceId)`

- [ ] **Step 4: Implement MockCiceksepetiApiClient**

Returns canned `HttpResponseMessage` with configurable response bodies. Implements `ICiceksepetiApiClient`.

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CiceksepetiApiClient"`
Expected: All PASS

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiApiClient.cs Application/Entegrasyon.Business/Concrete/Ciceksepeti/MockCiceksepetiApiClient.cs Test/Entegrasyon.Test/Ciceksepeti/
git commit -m "feat(ciceksepeti): implement CiceksepetiApiClient with multi-tenant credential cache"
```

---

## Task 4: Seed Data & DI Registration

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- DB seed: MarketPlace tablosuna Id=8 kaydı

**Ref code:** Lines 185-203 (Pazarama DI pattern)

- [ ] **Step 1: Add MarketPlace seed data**

MarketPlace tablosuna kayıt ekle (migration veya HasData seed):
```
Id=8, Name="Çiçeksepeti", BaseUrl="https://apis.ciceksepeti.com"
```

> **Not:** Mevcut seed/migration pattern'ine göre uygun yöntem seçilecek. Eğer `HasData` kullanılıyorsa `MarketPlaceEntityConfiguration.cs`'e, yoksa SQL migration'a eklenecek.

- [ ] **Step 2: Add Ciceksepeti using directives**

```csharp
using Entegrasyon.Business.Concrete.Ciceksepeti;
```

- [ ] **Step 3: Add Çiçeksepeti service registrations in AddApplicationDependencies**

After the Pazarama block (line ~203), add initial registrations:

```csharp
// Çiçeksepeti servisleri
services.AddScoped<CiceksepetiMappingValidator>();
services.AddScoped<CiceksepetiCategoryImporter>();

var useCiceksepetiMock = configuration.GetValue<bool>("Ciceksepeti:UseMock", true);
if (useCiceksepetiMock)
{
    services.AddScoped<ICiceksepetiApiClient, MockCiceksepetiApiClient>();
}
else
{
    services.AddScoped<ICiceksepetiApiClient, CiceksepetiApiClient>();
}
```

> **IMPORTANT:** Her sonraki Task'ta yeni servis oluşturulduğunda, bu bloğa ilgili DI kaydı eklenecek. Task 13'te tam DI bloğunun spec ile uyumlu olduğu doğrulanacak. Nihai tam blok:
> ```csharp
> services.AddScoped<ICiceksepetiCategoryService, CiceksepetiCategoryService>();
> services.AddScoped<ICiceksepetiProductService, CiceksepetiProductService>();
> services.AddScoped<ICiceksepetiProductMapper, CiceksepetiProductMapper>();
> services.AddScoped<ICiceksepetiStockPriceService, CiceksepetiStockPriceService>();
> services.AddScoped<ICiceksepetiOrderService, CiceksepetiOrderService>();
> services.AddScoped<ICiceksepetiInvoiceService, CiceksepetiInvoiceService>();
> services.AddScoped<ICiceksepetiReturnService, CiceksepetiReturnService>();
> services.AddScoped<ICiceksepetiQnAService, CiceksepetiQnAService>();
> services.AddScoped<ICiceksepetiCategoryImporter, CiceksepetiCategoryImporter>();
> ```

- [ ] **Step 4: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(ciceksepeti): add MarketPlace seed data and register DI services"
```

---

## Task 5: CiceksepetiCategoryService & Importer

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiCategoryService.cs`
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiCategoryImporter.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiCategoryService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Import/CiceksepetiCategoryImporter.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiCategoryServiceTests.cs`

**Ref docs:** `docs/ciceksepeti/CATEGORY_API.md`
**Ref code:** `Application/Entegrasyon.Business/Concrete/Import/BaseCategoryImporterService.cs`

- [ ] **Step 1: Write failing tests**

Tests:
1. `GetCategoriesAsync_ReturnsRecursiveCategoryTree`
2. `GetCategoryAttributesAsync_ReturnsAttributesWithTypes`
3. `CategoryImporter_MapsVariantOzellik_ToIsVarianterTrue`
4. `CategoryImporter_MapsKisisellestirilebilir_ToAllowCustomTrue`

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Create ICiceksepetiCategoryService interface**

```csharp
namespace Entegrasyon.Business.Abstract;

public interface ICiceksepetiCategoryService
{
    Task<IDataResult<CiceksepetiCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default);
    Task<IDataResult<CiceksepetiCategoryAttributeResponse>> GetCategoryAttributesAsync(int categoryId, CancellationToken ct = default);
}
```

- [ ] **Step 4: Implement CiceksepetiCategoryService**

Primary constructor: `(ICiceksepetiApiClient apiClient, ILogger<CiceksepetiCategoryService> logger)`
- `GetCategoriesAsync` → GET `/api/v1/Categories`
- `GetCategoryAttributesAsync(categoryId)` → GET `/api/v1/Categories/{categoryId}/attributes`

- [ ] **Step 5: Create ICiceksepetiCategoryImporter**

- [ ] **Step 6: Implement CiceksepetiCategoryImporter extending BaseCategoryImporterService**

- `Source = ImportSource.Ciceksepeti`
- Override `GetExternalCategoriesAsync` → call `ICiceksepetiCategoryService.GetCategoriesAsync()` → flatten tree
- Override `ImportCategoryAttributesAsync` → call `GetCategoryAttributesAsync(categoryId)` → map types:
  - `"Variant Ozellik"` → IsVarianter=true
  - `"Urun Ozellik"` → IsVarianter=false
  - `"Kisisellestirilebilir Ozellik"` → IsVarianter=false, AllowCustom=true

- [ ] **Step 7: Register in DI** — add `ICiceksepetiCategoryService` and `ICiceksepetiCategoryImporter` to DI block:

```csharp
services.AddScoped<ICiceksepetiCategoryService, CiceksepetiCategoryService>();
services.AddScoped<ICiceksepetiCategoryImporter, CiceksepetiCategoryImporter>();
```

> **Not:** Spec, `ICiceksepetiCategoryImporter` interface'ini zorunlu tutuyor (Pazarama'dan farklı olarak).

- [ ] **Step 8: Run tests — expect pass**

- [ ] **Step 9: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`

- [ ] **Step 10: Commit**

```bash
git commit -m "feat(ciceksepeti): add category service and importer with attribute type mapping"
```

---

## Task 6: CiceksepetiProductService, Mapper & Validator

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiProductService.cs`
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiProductMapper.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiProductService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiProductMapper.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiMappingValidator.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiProductServiceTests.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiProductMapperTests.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiMappingValidatorTests.cs`

**Ref docs:** `docs/ciceksepeti/PRODUCT_API.md`
**Ref code:** `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolProductService.cs`

- [ ] **Step 1: Write failing tests (3 separate test files)**

`CiceksepetiProductMapperTests.cs`:
1. `MapsAttributes_WithIdValueIdTextLength`
2. `MapsMainProductCode_And_StockCode`
3. `MapsImages_InOrder`

`CiceksepetiMappingValidatorTests.cs`:
4. `MissingCategoryMatch_ReturnsError`
5. `MissingRequiredAttribute_ReturnsError`
6. `AllMappingsComplete_ReturnsSuccess`

`CiceksepetiProductServiceTests.cs`:
7. `PublishProductAsync_ValidProduct_ReturnsBatchId`
8. `PublishProductAsync_MissingCategory_ReturnsValidationError`
9. `UpdateProductAsync_SetsIsActiveTrue`
10. `CheckBatchStatusAsync_ReturnsItemStatuses`
11. `GetProductsAsync_Uses1BasedPagination`

- [ ] **Step 2: Run — expect fail**

- [ ] **Step 3: Implement interfaces, services, mapper, validator**

Key `CiceksepetiProductService` details:
- Dependencies: `ICiceksepetiApiClient`, `ICiceksepetiProductMapper`, `CiceksepetiMappingValidator`, `IProductActivityLogger`, `IApplicationLogManager`, `ILogger<T>`, `IDbContextFactory<IntegrationDbContext>`
- `PublishProductAsync`: validate → map → POST `/api/v1/Products` → return batchId
- `UpdateProductAsync`: validate → map (isActive=true) → PUT `/api/v1/Products`
  - **WARNING:** omitting operatorContacts/safetyInfo DELETES existing data!
- `CheckBatchStatusAsync`: GET `/api/v1/Products/batch-status/{batchId}`
- `GetProductsAsync`: GET `/api/v1/Products?Page={page}&PageSize={pageSize}` (page is **1-based**)

- [ ] **Step 4: Register in DI**

- [ ] **Step 5: Run tests — expect pass**

- [ ] **Step 6: Run all tests**

- [ ] **Step 7: Commit**

```bash
git commit -m "feat(ciceksepeti): add product service, mapper, and mapping validator"
```

---

## Task 7: CiceksepetiStockPriceService

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiStockPriceService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiStockPriceService.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiStockPriceServiceTests.cs`

**Ref docs:** `docs/ciceksepeti/STOCK_PRICE_API.md`

- [ ] **Step 1: Write failing tests**

Tests:
1. `UpdateStockAndPriceAsync_ValidItems_ReturnsBatchId`
2. `UpdateStockAndPriceAsync_Max200PerBatch_SplitsIntoMultiple`
3. `UpdateStockAndPriceAsync_ListPriceWithoutSalesPrice_ReturnsError`
4. `UpdateStockAndPriceAsync_PriceDrop50Percent_ReturnsError`
5. `UpdateStockAndPriceAsync_PriceSpreadOutOfRange_ReturnsError` (listPrice-salesPrice must be >1% and <80%)

- [ ] **Step 2-6: Implement, register DI, test, commit**

```bash
git commit -m "feat(ciceksepeti): add stock/price service with business rule validation"
```

---

## Task 8: CiceksepetiOrderService

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiOrderService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiOrderService.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiOrderServiceTests.cs`

**Ref docs:** `docs/ciceksepeti/ORDER_API.md`, `docs/ciceksepeti/CARGO_API.md`, `docs/ciceksepeti/LABOR_COST_API.md`

- [ ] **Step 1: Write failing tests**

Tests:
1. `GetOrdersAsync_Uses0BasedPagination`
2. `GetOrdersAsync_DateRangeMax2Weeks_ThrowsIfExceeded`
3. `ReadyForCargoWithCsAsync_SendsGroupedOrderItemIds`
4. `UpdateStatusWithOwnCargoAsync_IncludesCargoBusinessId`
5. `ChangeCargoCompanyAsync_SendsItems`
6. `SendCargoMeasurementAsync_SendsDesiAndQuantity`
7. `SendDigitalCodeAsync_SendsReceiverAndDeliveryTime`
8. `UpdateLaborCostAsync_SendsItems`

- [ ] **Step 2-6: Implement, register DI, test, commit**

```bash
git commit -m "feat(ciceksepeti): add order service with all cargo operations"
```

---

## Task 9: CiceksepetiInvoiceService

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiInvoiceService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiInvoiceService.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiInvoiceServiceTests.cs`

**Ref docs:** `docs/ciceksepeti/INVOICE_API.md`

- [ ] **Step 1: Write failing tests**

Tests:
1. `SendInvoiceAsync_UsesRawPath_NotApiV1Prefix` — verifies `/Branch/SendInvoiceMail` path
2. `SendInvoiceAsync_WithBase64Document_SendsCorrectPayload`
3. `SendInvoiceAsync_WithDocumentUrl_SendsCorrectPayload`

- [ ] **Step 2: Run — expect fail**

- [ ] **Step 3: Implement** — uses `apiClient.SendRawAsync("/Branch/SendInvoiceMail", ...)` since this endpoint does NOT use `/api/v1/` prefix

- [ ] **Step 4: Register DI, run tests, commit**

```bash
git commit -m "feat(ciceksepeti): add invoice service with /Branch/ path handling"
```

---

## Task 10: CiceksepetiReturnService

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiReturnService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiReturnService.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiReturnServiceTests.cs`

**Ref docs:** `docs/ciceksepeti/RETURN_API.md`

- [ ] **Step 1: Write failing tests**

Tests:
1. `GetReturnOrdersAsync_DateRangeMax1Month`
2. `ConfirmReturnReceivedAsync_SendsOrderItemIds`
3. `EvaluateReturnAsync_ApproveProcess1_RejectProcess3`

- [ ] **Step 2-5: Implement, register DI, test, commit**

```bash
git commit -m "feat(ciceksepeti): add return service (list/confirm/evaluate)"
```

---

## Task 11: CiceksepetiQnAService

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ICiceksepetiQnAService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Ciceksepeti/CiceksepetiQnAService.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiQnAServiceTests.cs`

**Ref docs:** `docs/ciceksepeti/QNA_API.md`

- [ ] **Step 1: Write failing tests**

Tests:
1. `GetQuestionsAsync_Uses1BasedPagination`
2. `GetQuestionsAsync_DateRangeMax32Days`
3. `AnswerQuestionAsync_ActionId1_RequiresAnswer`
4. `GetActionsAsync_ReturnsActionList`

- [ ] **Step 2-5: Implement, register DI, test, commit**

```bash
git commit -m "feat(ciceksepeti): add Q&A service (questions/answers/actions)"
```

---

## Task 12: Background Services (TDD)

**Files:**
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiBatchStatusPollingServiceTests.cs`
- Create: `Test/Entegrasyon.Test/Ciceksepeti/CiceksepetiOrderPollingServiceTests.cs`
- Create: `Application/Entegrasyon.Business/BackgroundServices/CiceksepetiBatchStatusPollingService.cs`
- Create: `Application/Entegrasyon.Business/BackgroundServices/CiceksepetiOrderPollingService.cs`
- Create: `Application/Entegrasyon.Business/BackgroundServices/CiceksepetiStockPriceSyncService.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` (AddBackgroundServices)

**Ref code:** `Application/Entegrasyon.Business/BackgroundServices/TrendyolBatchStatusPollingService.cs`

- [ ] **Step 1: Write failing tests for batch polling**

`CiceksepetiBatchStatusPollingServiceTests.cs`:
1. `PollAsync_PendingBatch_CallsCheckBatchStatus`
2. `PollAsync_BatchOlderThan24Hours_MarksFailed`
3. `PollAsync_StockPriceBatchOlderThan4Hours_MarksFailed`
4. `MultiTenant_IsolatesPollingPerTenant`

- [ ] **Step 2: Write failing tests for order polling**

`CiceksepetiOrderPollingServiceTests.cs`:
1. `PollAsync_UsesLast2HourWindow`
2. `PollAsync_UpdatesLastPollTimestamp`
3. `MultiTenant_IsolatesTimestampPerTenant`

- [ ] **Step 3: Run tests — expect fail**

- [ ] **Step 4: Implement CiceksepetiBatchStatusPollingService**

- `IHostedService` with `IServiceScopeFactory`
- Poll interval: configurable (default 30s)
- Multi-tenant: `ConcurrentDictionary<int, DateTime>` last poll per tenant
- Max polling: 24h for product batches, 4h for stock/price — mark as Failed after timeout

- [ ] **Step 5: Implement CiceksepetiOrderPollingService**

- Poll interval: configurable (default 60s)
- Date range: last 2-hour window
- `ConcurrentDictionary<int, DateTime>` last poll timestamp

- [ ] **Step 6: Implement CiceksepetiStockPriceSyncService**

- EventChannel-driven or periodic
- Detect changed stock/prices → batch update

- [ ] **Step 7: Run tests — expect pass**

- [ ] **Step 8: Register in AddBackgroundServices**

```csharp
// In AddBackgroundServices(), add:
services.AddHostedService<CiceksepetiBatchStatusPollingService>();
services.AddHostedService<CiceksepetiOrderPollingService>();
services.AddHostedService<CiceksepetiStockPriceSyncService>();
```

- [ ] **Step 9: Build and run all tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`

- [ ] **Step 10: Commit**

```bash
git commit -m "feat(ciceksepeti): add background services with TDD (batch poll, order poll, stock sync)"
```

---

## Task 13: DI Verification & Final Integration Test

- [ ] **Step 1: Verify complete DI registration**

Cross-check `ApplicationDependencyExtension.cs` against the spec's Section 8. Ensure ALL of these are registered:
```
ICiceksepetiApiClient, ICiceksepetiCategoryService, ICiceksepetiCategoryImporter,
ICiceksepetiProductService, ICiceksepetiProductMapper, CiceksepetiMappingValidator,
ICiceksepetiStockPriceService, ICiceksepetiOrderService, ICiceksepetiInvoiceService,
ICiceksepetiReturnService, ICiceksepetiQnAService
```
And in AddBackgroundServices:
```
CiceksepetiBatchStatusPollingService, CiceksepetiOrderPollingService, CiceksepetiStockPriceSyncService
```

- [ ] **Step 2: Run full build**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 3: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`

- [ ] **Step 4: Verify DI registrations compile and resolve**

Start app briefly to check DI: `cd Application/Entegrasyon.Blazor && dotnet run` (Ctrl+C after startup)

- [ ] **Step 5: Final commit if any cleanup needed**

```bash
git commit -m "chore(ciceksepeti): DI verification and integration test"
```

---

## Task 14: E2E Tests (Sandbox)

**Files:**
- Create: `Test/Entegrasyon.E2E/Ciceksepeti/CiceksepetiSmokeTests.cs`

**Ref docs:** `docs/ciceksepeti/BASE_KNOWLEDGE.md` (sandbox URL)

> **Prerequisite:** Çiçeksepeti sandbox API key mevcut olmalı. Key yoksa bu task atlanır.

- [ ] **Step 1: Write E2E smoke tests**

Tests (sandbox API'ye gerçek HTTP request'ler):
1. `CategoryList_ReturnsNonEmptyTree` — GET /api/v1/Categories
2. `ProductList_ReturnsPaginatedProducts` — GET /api/v1/Products
3. `BatchStatus_WithInvalidId_ReturnsError` — GET /api/v1/Products/batch-status/{fake-id}

- [ ] **Step 2: Run E2E tests**

Run: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --filter "FullyQualifiedName~Ciceksepeti"`

- [ ] **Step 3: Commit**

```bash
git commit -m "test(ciceksepeti): add E2E smoke tests for sandbox API"
```
