# N11 SOAP → REST Migration Plan

## Status: IN PROGRESS

## Task 1: Request/Response DTOs
Create `Application/Entegrasyon.Entity/Dtos/N11/N11RestDtos.cs`:
- Product create/update request records
- Price/stock update request records
- Task response + task detail records
- Order (shipment packages) response records
- Category tree + attribute response records

## Task 2: N11RestClient
- `Abstract/IN11RestClient.cs` — GetAsync, PostAsync, PutAsync
- `Concrete/N11/N11RestClient.cs` — reads credentials from MarketPlace table, adds appkey/appsecret headers
- Register `"N11Rest"` named HttpClient in `AddClients()`

## Task 3: N11RestProductService
- Implements `IN11ProductService`
- SaveProductAsync → POST /ms/product/tasks/product-create, stores taskId in BatchRequestId, Status=Pending
- DeleteProductAsync → SOAP fallback (IN11SoapClient)
- UpdateProductBasicAsync → POST /ms/product/tasks/product-update
- StartSellingAsync → POST /ms/product/tasks/product-update (status=Active)
- StopSellingAsync → POST /ms/product/tasks/product-update (status=Suspended)

## Task 4: N11RestStockPriceService
- Implements `IN11StockPriceService`
- UpdatePriceAsync → POST /ms/product/tasks/price-stock-update (listPrice + salePrice, stockCode from variants)
- UpdateStockAsync → POST /ms/product/tasks/price-stock-update (quantity only, stockCode from variants)

## Task 5: N11RestOrderService
- Implements `IN11OrderService`
- FetchOrdersAsync → GET /rest/delivery/v1/shipmentPackages
- GetOrderDetailAsync → GET /rest/delivery/v1/shipmentPackages?orderNumber={id}
- AcceptOrderItemAsync → PUT /rest/order/v1/update (status=Picking)
- ShipOrderItemAsync → SOAP fallback
- RejectOrderItemAsync → SOAP fallback

## Task 6: N11RestCategoryImporter
- Extends `BaseCategoryImporterService`
- GetExternalCategoriesAsync → GET /cdn/categories (recursive tree flatten)
- ImportCategoryAttributesAsync → GET /cdn/category/{id}/attribute

## Task 7: N11TaskPollingService
- Background service, 2-minute interval
- Polls pending N11 ProductMarketplace records
- POST /ms/product/task-details/page-query
- PROCESSED → update Status (SUCCESS→Published, FAIL→Failed)
- IN_QUEUE → skip

## Task 8: DI Registration
Update `ApplicationDependencyExtension.cs`:
- `N11:UseSoap` flag (default false)
- false (REST): register REST implementations
- true (SOAP): register SOAP implementations
- `IN11SoapClient` always registered (REST services need it for delete/ship/reject)
- `N11TaskPollingService` added to background services
- `N11RestCategoryImporter` registered when UseSoap=false

## Task 9: Tests
- `N11RestClientTests` — mock HttpClient, verify headers
- `N11RestProductServiceTests` — verify request format, taskId storage
- `N11RestStockPriceServiceTests` — verify price/stock request
- Run all unit tests

## Commit Order
1. DTOs
2. RestClient
3. RestProductService
4. RestStockPriceService + RestOrderService
5. RestCategoryImporter + TaskPollingService
6. DI registration
7. Tests
