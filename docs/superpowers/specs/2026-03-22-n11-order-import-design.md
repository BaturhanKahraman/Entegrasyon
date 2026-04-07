# N11 Sipariş Cekme + Import — Sprint 6 Design Spec

## Problem

N11 pazaryerinden Siparişleri periyodik olarak cekip local DB'ye import etmek. Trendyol icin bu altyapi mevcut (TrendyolOrderPollingService + OrderManager). N11 icin ayni pipeline'in SOAP/XML versiyonu.

## Scope

- IN11OrderService: N11 SOAP cagirilari (DetailedOrderList, OrderDetail)
- N11OrderService: gercek implementasyon
- MockN11OrderService: mock (N11:UseMock=true)
- N11OrderPollingService: 2 dk periyodik polling (BackgroundService)
- IOrderManager genisletme: ImportN11OrdersAsync
- N11OrderDto: SOAP response parse icin DTO'lar
- UI: MarketplaceOrders sayfasinda N11 filtresi

## Out of Scope

- Sipariş aksiyonlari (Accept, Reject, Ship — Sprint 7)
- Iptal/iade/degisim (Sprint 8)

---

## Components

### 1. N11OrderDto Records

**File:** `Application/Entegrasyon.Entity/Dtos/N11/N11OrderResponse.cs`

```csharp
public record N11OrderDto
{
    public long Id { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public string? PaymentType { get; init; }
    public DateTimeOffset CreateDate { get; init; }
    public string? CitizenshipId { get; init; }
    public N11BuyerDto? Buyer { get; init; }
    public N11AddressDto? BillingAddress { get; init; }
    public N11AddressDto? ShippingAddress { get; init; }
    public List<N11OrderItemDto> OrderItems { get; init; } = new();
}

public record N11OrderItemDto
{
    public long Id { get; init; }
    public long ProductId { get; init; }
    public string? ProductSellerCode { get; init; }
    public string? ProductName { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal? Discount { get; init; }
    public decimal? VatRate { get; init; }
    public string? Status { get; init; }
    public N11ShipmentDto? Shipment { get; init; }
}

public record N11BuyerDto(string? FirstName, string? LastName, string? Email);
public record N11AddressDto(string? City, string? District, string? FullAddress, string? PostalCode);
public record N11ShipmentDto(string? CompanyName, string? TrackingNumber, string? ShipmentCode);
```

### 2. IN11OrderService + Implementations

**Interface File:** `Application/Entegrasyon.Business/Abstract/IN11OrderService.cs`

```csharp
public interface IN11OrderService
{
    Task<IDataResult<List<N11OrderDto>>> FetchOrdersAsync(
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null,
        string? status = null, int page = 0, int pageSize = 50);
    Task<IDataResult<N11OrderDto>> GetOrderDetailAsync(long orderId);
}
```

**Real Implementation:** `Application/Entegrasyon.Business/Concrete/N11/N11OrderService.cs`

- Uses `IN11SoapClient.SendAsync("OrderService", "", request)`
- `FetchOrdersAsync`: SOAP `DetailedOrderListRequest` with search criteria + pagination
- `GetOrderDetailAsync`: SOAP `OrderDetailRequest` with orderId
- XML response parsing: XElement → N11OrderDto mapping
- `CheckN11ResponseStatus` pattern (same as N11ProductService)
- Pagination: loop until all pages fetched if needed

**Mock Implementation:** `Application/Entegrasyon.Business/Concrete/N11/MockN11OrderService.cs`

- Returns empty list or sample mock orders
- Used when `N11:UseMock=true`

### 3. IOrderManager Genisletme

**File:** `Application/Entegrasyon.Business/Abstract/IOrderManager.cs` (modify)

Add: `Task<IResult> ImportN11OrdersAsync(List<N11OrderDto> orders);`

**File:** `Application/Entegrasyon.Business/Concrete/OrderManager.cs` (modify)

`ImportN11OrdersAsync` follows `ImportTrendyolOrdersAsync` pattern:

1. Advisory lock (prevent concurrent imports)
2. Pre-load: warehouse IDs (MarketPlaceId=2), barcode→ProductVariant dict
3. For each N11OrderDto:
   - Dedup: check `OrderNumber + MarketPlaceId=2` exists
   - Create `Order` entity: MarketPlaceId=2, OrderNumber, Status, CustomerInfo, Addresses
   - For each N11OrderItemDto:
     - Barcode lookup: `ProductSellerCode` → ProductVariant by barcode
     - Create `OrderItem`: Quantity, UnitPrice, Barcode, MerchantSku
   - Auto stock decrease (same pattern as Trendyol)
4. SaveChanges

### 4. N11OrderPollingService

**File:** `Application/Entegrasyon.Business/BackgroundServices/N11OrderPollingService.cs`

Same pattern as `TrendyolOrderPollingService`:
- Polling interval: 2 minutes (configurable)
- Maintains `_lastPollTime` (default: 1 day ago on startup)
- Flow: `IN11OrderService.FetchOrdersAsync(startDate: _lastPollTime)` → `IOrderManager.ImportN11OrdersAsync(orders)`
- Error handling: log + continue polling
- Only runs when `N11:UseMock=false` (check config in constructor)

### 5. UI Genisletme

**File:** `Application/Entegrasyon.Blazor/Features/Orders/MarketplaceOrders.razor.cs` (modify)

- Add marketplace filter (dropdown: All, Trendyol, N11)
- Pass `MarketPlaceId` to `IOrderManager.GetOrdersAsync(marketPlaceId: selectedId)`
- N11 status color mapping (same colors as Trendyol)

---

## SOAP Calls

### DetailedOrderList

**WSDL:** `OrderService`

```xml
<sch:DetailedOrderListRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <searchData>
        <buyerName></buyerName>
        <orderNumber></orderNumber>
        <productId></productId>
        <status></status>
        <startDate>{dd/MM/yyyy}</startDate>
        <endDate>{dd/MM/yyyy}</endDate>
        <sortForUpdateDate>true</sortForUpdateDate>
    </searchData>
    <pagingData>
        <currentPage>0</currentPage>
        <pageSize>50</pageSize>
    </pagingData>
</sch:DetailedOrderListRequest>
```

Response: `<orderList><order>...<orderItemList><orderItem>...</orderItem></orderItemList></order></orderList>`

### OrderDetail

```xml
<sch:OrderDetailRequest xmlns:sch="http://www.n11.com/ws/schemas">
    <orderRequest>
        <id>{n11OrderId}</id>
    </orderRequest>
</sch:OrderDetailRequest>
```

---

## DI Registration

```csharp
// AddApplicationDependencies — inside N11:UseMock block:
if (useN11Mock)
{
    services.AddScoped<IN11OrderService, MockN11OrderService>();
}
else
{
    services.AddScoped<IN11OrderService, N11OrderService>();
}

// AddBackgroundServices:
services.AddHostedService<N11OrderPollingService>();
```

## Error Handling

- SOAP failure → log, skip batch, retry next poll
- Dedup collision → skip order (already imported)
- Barcode not found → create OrderItem without ProductVariant link (ProductId=null)
- Stock insufficient → force decrease + critical notification (same as Trendyol)

## Testing

**N11OrderServiceTests.cs:** (4-5 tests)
1. FetchOrdersAsync_ShouldParseSoapResponse
2. FetchOrdersAsync_WhenEmpty_ShouldReturnEmptyList
3. GetOrderDetailAsync_ShouldReturnOrder
4. FetchOrdersAsync_WhenSoapFails_ShouldReturnError

**OrderManagerN11ImportTests.cs:** (3-4 tests)
1. ImportN11OrdersAsync_ShouldCreateOrderWithMarketPlaceId2
2. ImportN11OrdersAsync_WhenDuplicate_ShouldSkip
3. ImportN11OrdersAsync_ShouldMapBarcodeToProductVariant
