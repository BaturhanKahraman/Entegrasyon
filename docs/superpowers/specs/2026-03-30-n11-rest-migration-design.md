# N11 SOAP → REST Migration Design

## Background

N11 released a REST API alongside the existing SOAP API. The REST API is task-based (async processing) vs SOAP which is synchronous. Some operations (delete, ship, reject, claim) have no REST equivalent and must keep using SOAP.

## Goals

1. Replace SOAP calls with REST for: product create, product update, price/stock update, order list, order accept, category import.
2. Keep SOAP for: delete, ship, reject claim operations.
3. Zero interface changes — consumers do not change.
4. Feature flag `N11:UseSoap` (default `false` = REST) for safe rollback.

## Key Differences: SOAP vs REST

| Operation | SOAP | REST |
|---|---|---|
| Product create | Sync, returns N11 product ID | Task-based, returns taskId |
| Price/stock update | Sync | Task-based, returns taskId |
| Product update | Sync | Task-based, returns taskId |
| Order list | SOAP DetailedOrderList | GET /rest/delivery/v1/shipmentPackages |
| Order accept | SOAP OrderItemAccept | PUT /rest/order/v1/update |
| Category tree | Multiple SOAP calls (paginated top-level + sub-categories) | Single GET /cdn/categories (full tree) |
| Category attributes | Paginated SOAP | Single GET /cdn/category/{id}/attribute |
| Delete product | SOAP only | No REST endpoint |
| Ship order | SOAP only | No REST endpoint |
| Reject order | SOAP only | No REST endpoint |

## REST Auth

Headers: `appkey` + `appsecret` (no Authorization header).

## Task-Based Flow

Product/price operations return a `taskId`. A new background service (`N11TaskPollingService`) polls `/ms/product/task-details/page-query` every 2 minutes to resolve task outcomes and update `ProductMarketplace.Status`.

## Status Mapping (Orders)

REST status strings: Created, Picking, Shipped, Delivered, Cancelled, UnSupplied
These map to existing `N11OrderDto.Status` values (currently SOAP string statuses).

## Components

- `IN11RestClient` / `N11RestClient` — HTTP wrapper (analogous to `IN11SoapClient`)
- `N11RestProductService` — implements `IN11ProductService` via REST
- `N11RestStockPriceService` — implements `IN11StockPriceService` via REST
- `N11RestOrderService` — implements `IN11OrderService` via REST (SOAP fallback for ship/reject)
- `N11RestCategoryImporter` — REST-based category + attribute import
- `N11TaskPollingService` — background task status resolver
- DTOs in `Entity/Dtos/N11/` — request/response records

## Non-Goals

- N11ClaimService stays SOAP (no REST equivalent)
- No UI changes
- No DB schema changes
