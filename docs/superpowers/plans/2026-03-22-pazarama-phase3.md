# Pazarama Phase 3: Sipariş Yönetimi + İade/İptal — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add order fetching (polling), order status updates, cargo tracking, refund management, and cancellation management for Pazarama marketplace (MarketPlaceId=5).

**Architecture:** Follows the N11 order import pattern: polling service → fetch from API → OrderManager.ImportPazaramaOrdersAsync (dedup + stock reduction + advisory lock). Separate refund/cancel services with their own polling. All marketplace-specific DTOs in a single models file.

**Tech Stack:** .NET 8, C# 12, EF Core (PostgreSQL), xUnit + Moq + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-22-pazarama-phase3-design.md`

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaOrderModels.cs` | Order/refund/cancel DTOs |
| `Application/Entegrasyon.Business/Abstract/IPazaramaOrderService.cs` | Order service interface |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaOrderService.cs` | Order fetch + status update |
| `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaOrderService.cs` | Mock order service |
| `Application/Entegrasyon.Business/Abstract/IPazaramaRefundService.cs` | Refund/cancel service interface |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaRefundService.cs` | Refund/cancel management |
| `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaRefundService.cs` | Mock refund service |
| `Application/Entegrasyon.Business/BackgroundServices/PazaramaOrderPollingService.cs` | 2-min order polling |
| `Application/Entegrasyon.Business/BackgroundServices/PazaramaRefundPollingService.cs` | 5-min refund/cancel polling |
| `Test/Entegrasyon.Test/Pazarama/PazaramaOrderServiceTests.cs` | Order service tests |
| `Test/Entegrasyon.Test/Pazarama/PazaramaOrderImportTests.cs` | Order import tests |
| `Test/Entegrasyon.Test/Pazarama/PazaramaRefundServiceTests.cs` | Refund service tests |

### Modified Files
| File | Change |
|------|--------|
| `Application/Entegrasyon.Business/Abstract/IOrderManager.cs` | Add `ImportPazaramaOrdersAsync` |
| `Application/Entegrasyon.Business/Concrete/OrderManager.cs` | Implement Pazarama import |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Register services + BG services |

### Key Reference Files (DO NOT modify, read for patterns)
- `Application/Entegrasyon.Business/Concrete/OrderManager.cs` — N11 import pattern (advisory lock, dedup, stock reduction)
- `Application/Entegrasyon.Business/BackgroundServices/N11OrderPollingService.cs` — polling pattern (ConcurrentDictionary, mock check)
- `Application/Entegrasyon.Business/Abstract/IN11OrderService.cs` — order service interface pattern
- `Application/Entegrasyon.Entity/Orders/Order.cs` — Order entity fields
- `Application/Entegrasyon.Entity/Orders/OrderItem.cs` — OrderItem entity fields
- `Test/Entegrasyon.Test/N11/N11OrderImportTests.cs` — import test pattern

---

## Task 1: Order/Refund/Cancel DTOs

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaOrderModels.cs`

- [ ] **Step 1: Create DTO file**

All DTOs for Pazarama order lifecycle in one file. Use `System.Text.Json.Serialization` with `JsonPropertyName` attributes. Key records:

- `PazaramaMoneyDto` — `value` (decimal), `valueString`, `currency`
- `PazaramaOrderDto` — orderId, orderNumber (long), orderDate, orderAmount, shipmentAmount, discountAmount, currency, paymentType, orderStatus, customerId, customerName, customerEmail, shipmentAddress, billingAddress, items
- `PazaramaOrderAddressDto` — addressId, title, nameSurname, cityName, districtName, addressDetail, phoneNumber, identityNumber, invoiceType, companyName, taxNumber, taxOffice, isEInvoiceObliged
- `PazaramaOrderItemDto` — orderItemId (string GUID), orderItemStatus (int), shipmentCode, shipmentCost (money), deliveryType, quantity, listPrice (money), salePrice (money), taxAmount (money), shipmentAmount (money), totalPrice (money), discountAmount (money), taxIncluded, cargo (company/tracking), product (productId, name, code, vatRate, url, imageUrl, variantOptionDisplay)
- `PazaramaCargoDto` — companyName, trackingNumber, trackingUrl
- `PazaramaOrderProductDto` — productId, name, title, url, imageURL, variantOptionDisplay, stockCode, code, vatRate
- `PazaramaOrderItemUpdate` — orderItemId (string), status (int), deliveryType (int?), shippingTrackingNumber (string?), trackingUrl (string?), cargoCompanyId (string?)
- `PazaramaOrderFetchRequest` — orderNumber (long?), startDate (string), endDate (string), pageSize (int), pageNumber (int)
- `PazaramaRefundDto` — refundId (string), orderNumber (long), orderDate (string), refundNumber (long), refundType (string), refundStatus (int), refundStatusName (string), paymentType (string), refundDate (string), totalAmount (money), refundAmount (money), customerId (string), customerName, customerEmail, customerPhoneNumber, productName, productCode, productStockCode, shipmentCompanyName, shipmentCode, description
- `PazaramaRefundListResponse` — responsePage, pageReport, refundList
- `PazaramaRefundPageInfo` — pageSize, pageIndex, totalCount, totalPages
- `PazaramaRefundPageReport` — totalRefundCount, totalWaitingRefundCount, totalApprovedRefundCount, totalRejectedRefundCount
- `PazaramaRefundUpdateRequest` — refundId (string), status (int), RefundRejectType (int?)
- `PazaramaCancelUpdateRequest` — refundId (string), status (int)

- [ ] **Step 2: Build to verify**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaOrderModels.cs
git commit -m "feat(pazarama): add order, refund, and cancellation DTOs"
```

---

## Task 2: IPazaramaOrderService + PazaramaOrderService + Mock + Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPazaramaOrderService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaOrderService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaOrderService.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaOrderServiceTests.cs`

**Reference:** `IN11OrderService.cs`, `N11OrderService.cs`, `N11OrderServiceTests.cs`

- [ ] **Step 1: Create interface**

```csharp
interface IPazaramaOrderService
{
    Task<IDataResult<List<PazaramaOrderDto>>> FetchOrdersAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int pageSize = 500, int pageNumber = 1);
    Task<IResult> UpdateOrderItemStatusAsync(long orderNumber, PazaramaOrderItemUpdate item);
    Task<IResult> BulkUpdateOrderStatusAsync(long orderNumber, int status);
}
```

- [ ] **Step 2: Write failing tests**

Mock `IPazaramaApiClient`. Tests:
- FetchOrdersAsync: calls POST /order/getOrdersForApi, parses response
- FetchOrdersAsync: API failure returns error
- UpdateOrderItemStatusAsync: calls PUT /order/updateOrderStatus
- BulkUpdateOrderStatusAsync: calls PUT /order/updateOrderStatusList
- Mock service: returns empty order list

- [ ] **Step 3: Implement PazaramaOrderService**

Primary constructor: `IPazaramaApiClient`, `ILogger<PazaramaOrderService>`

- `FetchOrdersAsync`: POST `/order/getOrdersForApi` with `PazaramaOrderFetchRequest` body. Format dates as `yyyy-MM-dd`. Parse `PazaramaResponse<List<PazaramaOrderDto>>`.
- `UpdateOrderItemStatusAsync`: PUT `/order/updateOrderStatus` with `{ orderNumber, item }`.
- `BulkUpdateOrderStatusAsync`: PUT `/order/updateOrderStatusList` with `{ orderNumber, status }`.

- [ ] **Step 4: Implement MockPazaramaOrderService**

Returns empty order list, logs calls.

- [ ] **Step 5: Run tests, commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPazaramaOrderService.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaOrderService.cs Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaOrderService.cs Test/Entegrasyon.Test/Pazarama/PazaramaOrderServiceTests.cs
git commit -m "feat(pazarama): add PazaramaOrderService with fetch and status update"
```

---

## Task 3: OrderManager.ImportPazaramaOrdersAsync

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IOrderManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/OrderManager.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaOrderImportTests.cs`

**Reference:** `OrderManager.cs` lines 115-234 (ExecuteN11ImportAsync pattern)

- [ ] **Step 1: Add to IOrderManager interface**

```csharp
Task<IResult> ImportPazaramaOrdersAsync(List<PazaramaOrderDto> orders);
```

Add using: `using Entegrasyon.Business.Concrete.Pazarama;`

- [ ] **Step 2: Write failing tests**

Following `N11OrderImportTests.cs` pattern. Tests:
- Import creates Order with MarketPlaceId=5
- Duplicate OrderNumber is skipped (dedup)
- Barcode maps to ProductVariant
- Missing barcode sets ProductId to null

- [ ] **Step 3: Implement ImportPazaramaOrdersAsync**

Follow N11 import pattern exactly:
1. `ImportPazaramaOrdersAsync` calls `ExecutePazaramaImportAsync` inside advisory lock wrapper
2. Advisory lock key: `2005`
3. Dedup: batch load existing `OrderNumber` values for MarketPlaceId=5
4. Barcode lookup: batch load all product variants' barcodes into dictionary
5. Warehouse IDs: load from `MarketPlaceWarehouses` for PazaramaMarketPlaceId
6. For each order DTO:
   - Skip if OrderNumber already exists (dedup)
   - Create `Order` entity with MarketPlaceId=5, OrderNumber, CustomerName/Email
   - Map addresses: Pazarama `shipmentAddress` → `Order.ShippingAddress`, `billingAddress` → `Order.BillingAddress`
   - For each item: create `OrderItem` with barcode → ProductVariant lookup
   - Stock reduction: `DecreaseStockAtomicAsync` per warehouse
7. SaveChangesAsync

Key mapping notes:
- `order.orderNumber.ToString()` → `Order.OrderNumber` (string field)
- `order.customerName` → `Order.CustomerFirstName` (split by space if needed)
- `item.product.code` → `OrderItem.Barcode` (for product variant lookup)
- `item.salePrice.value` → `OrderItem.UnitPrice` (decimal from money object)
- `order.orderStatus.ToString()` → `Order.MarketplaceOrderStatus`

- [ ] **Step 4: Run tests, commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IOrderManager.cs Application/Entegrasyon.Business/Concrete/OrderManager.cs Test/Entegrasyon.Test/Pazarama/PazaramaOrderImportTests.cs
git commit -m "feat(pazarama): add ImportPazaramaOrdersAsync to OrderManager"
```

---

## Task 4: PazaramaOrderPollingService

**Files:**
- Create: `Application/Entegrasyon.Business/BackgroundServices/PazaramaOrderPollingService.cs`

**Reference:** `N11OrderPollingService.cs` — follow this pattern exactly

- [ ] **Step 1: Implement polling service**

```csharp
public class PazaramaOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<PazaramaOrderPollingService> logger,
    IConfiguration configuration) : BackgroundService
```

Key implementation:
- 2-min interval, 20s startup delay
- Mock check: `if (configuration.GetValue<bool>("Pazarama:UseMock"))` → return (disable polling)
- `ConcurrentDictionary<int, DateTimeOffset>` for lastPollTime (multi-tenant ready)
- MarketPlaceId = 5 (hardcoded with TODO for multi-tenant iteration)
- Initial look-back: 1 day (`DateTimeOffset.UtcNow.AddDays(-1)`)
- Calls `IPazaramaOrderService.FetchOrdersAsync(lastPoll, now)`
- On success with data: calls `IOrderManager.ImportPazaramaOrdersAsync()`
- Updates lastPollTime on success

- [ ] **Step 2: Build and test**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/BackgroundServices/PazaramaOrderPollingService.cs
git commit -m "feat(pazarama): add PazaramaOrderPollingService with 2-min polling"
```

---

## Task 5: IPazaramaRefundService + PazaramaRefundService + Mock + Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPazaramaRefundService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaRefundService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaRefundService.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaRefundServiceTests.cs`

- [ ] **Step 1: Create interface**

```csharp
interface IPazaramaRefundService
{
    Task<IDataResult<PazaramaRefundListResponse>> GetRefundsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1);
    Task<IResult> UpdateRefundAsync(string refundId, int status, int? refundRejectType = null);
    Task<IDataResult<PazaramaRefundListResponse>> GetCancellationsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1);
    Task<IResult> UpdateCancellationAsync(string refundId, int status);
}
```

- [ ] **Step 2: Write tests**

- GetRefundsAsync: calls POST /order/getRefund, parses response
- UpdateRefundAsync: calls POST /order/updateRefund with status
- UpdateRefundAsync with reject: includes RefundRejectType
- GetCancellationsAsync: calls POST /order/api/cancel/items
- UpdateCancellationAsync: calls PUT /order/api/cancel
- Mock service tests

- [ ] **Step 3: Implement PazaramaRefundService**

Primary constructor: `IPazaramaApiClient`, `ILogger<PazaramaRefundService>`

- `GetRefundsAsync`: POST `/order/getRefund` with `{ pageSize, pageNumber, refundStatus, requestStartDate: "yyyy-MM-dd", requestEndDate: "yyyy-MM-dd" }`
- `UpdateRefundAsync`: POST `/order/updateRefund` with `PazaramaRefundUpdateRequest { refundId, status, RefundRejectType }`
- `GetCancellationsAsync`: POST `/order/api/cancel/items` with same request format
- `UpdateCancellationAsync`: PUT `/order/api/cancel` with `PazaramaCancelUpdateRequest { refundId, status }`

- [ ] **Step 4: Implement MockPazaramaRefundService**

Returns empty refund/cancel lists.

- [ ] **Step 5: Run tests, commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPazaramaRefundService.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaRefundService.cs Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaRefundService.cs Test/Entegrasyon.Test/Pazarama/PazaramaRefundServiceTests.cs
git commit -m "feat(pazarama): add PazaramaRefundService for return and cancellation management"
```

---

## Task 6: PazaramaRefundPollingService

**Files:**
- Create: `Application/Entegrasyon.Business/BackgroundServices/PazaramaRefundPollingService.cs`

- [ ] **Step 1: Implement refund polling service**

Follow N11OrderPollingService pattern:
- 5-min interval, 25s startup delay
- Mock check: `Pazarama:UseMock` → disable
- `ConcurrentDictionary<int, DateTimeOffset>` for lastPollTime
- Calls `GetRefundsAsync(lastPoll, now, refundStatus: 1)` for pending refunds
- Calls `GetCancellationsAsync(lastPoll, now, refundStatus: 1)` for pending cancellations
- Logs count of new refunds/cancellations
- Updates lastPollTime

- [ ] **Step 2: Build and test**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/BackgroundServices/PazaramaRefundPollingService.cs
git commit -m "feat(pazarama): add PazaramaRefundPollingService with 5-min polling"
```

---

## Task 7: DI Registration

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Add Pazarama order/refund services to DI**

In the Pazarama DI block:

```csharp
// Inside mock toggle:
if (usePazaramaMock)
{
    // existing mock registrations...
    services.AddScoped<IPazaramaOrderService, MockPazaramaOrderService>();
    services.AddScoped<IPazaramaRefundService, MockPazaramaRefundService>();
}
else
{
    // existing real registrations...
    services.AddScoped<IPazaramaOrderService, PazaramaOrderService>();
    services.AddScoped<IPazaramaRefundService, PazaramaRefundService>();
}
```

In `AddBackgroundServices`:
```csharp
services.AddHostedService<PazaramaOrderPollingService>();
services.AddHostedService<PazaramaRefundPollingService>();
```

- [ ] **Step 2: Build and run all tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(pazarama): register Phase 3 order and refund services in DI"
```

---

## Task 8: Final Verification

- [ ] **Step 1: Full build**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 errors

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`
Expected: All pass

- [ ] **Step 3: Run Pazarama tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Pazarama" -v n`
Expected: All Pazarama tests pass (Phase 1 + 2 + 3)

- [ ] **Step 4: Verify git log**

Run: `git log --oneline -10`
