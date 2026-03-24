# Entegrasyon

A Turkish e-commerce marketplace integration platform built with .NET 8 Blazor Server. Manages products, orders, stock, shipments, and invoicing across 8 marketplaces from a single unified interface.

## Supported Marketplaces

| MarketPlaceId | Marketplace | Protocol | Status |
|:---:|---|---|---|
| 1 | Trendyol | REST | Full (products, orders, stock, e-invoice) |
| 2 | N11 | SOAP | Products, orders |
| 3 | Hepsiburada | REST | Full (all phases) |
| 4 | Amazon | REST + OAuth 2.0 | Listings, Feeds, Orders (FBM, TR+EU) |
| 5 | Pazarama | REST + OAuth 2.0 | Products, orders |
| 7 | PttAVM | REST + API Key | Products, orders |
| 8 | Ciceksepeti | REST + API Key | Products, orders |
| 9 | Temu | REST | Category import, products |

Each marketplace has mock implementations switchable via config (e.g., `Trendyol:UseMock`) for local development without real API credentials.

## Cargo Integrations

Three SOAP-based cargo providers, each with mock implementations:

- **Aras Kargo**
- **Surat Kargo**
- **Yurtici Kargo**

---

## Tech Stack

| Technology | Purpose |
|---|---|
| .NET 8 / C# 12 | Runtime & language (primary constructors) |
| Blazor Server | Interactive web UI |
| MudBlazor | Component library |
| EF Core 8 + Npgsql | ORM, PostgreSQL provider |
| FluentValidation | Input validation |
| Mapster | DTO mapping |
| MinIO | Object storage (product images, files) |
| SignalR | Real-time notifications |
| Redis | Caching |
| Docker | Infrastructure containers |
| Testcontainers | Ephemeral DB for integration tests |
| Playwright | E2E browser testing |

---

## Architecture

Classic layered architecture with strict dependency direction:

```
┌─────────────────────────────────────────────────────────┐
│                    Entegrasyon.Blazor                    │
│              (Blazor Server UI - MudBlazor)              │
├─────────────────────────────────────────────────────────┤
│                 Entegrasyon.Desktop                      │
│              (Photino.Blazor desktop app)                │
├─────────────────────────────────────────────────────────┤
│              Entegrasyon.ApplicationBootstrap            │
│                  (DI container setup)                    │
├─────────────────────────────────────────────────────────┤
│                  Entegrasyon.Business                    │
│     (Managers, Validators, Background Services,         │
│      API Clients, Event Channels, Cargo)                │
├─────────────────────────────────────────────────────────┤
│                 Entegrasyon.DataAccess                   │
│        (EF Core DbContext, Migrations, Configs)         │
├─────────────────────────────────────────────────────────┤
│                   Entegrasyon.Entity                     │
│            (Domain Models, DTOs, Result Types)           │
└─────────────────────────────────────────────────────────┘
```

| Project | Responsibility |
|---|---|
| `Entegrasyon.Entity` | Domain models, DTOs, enums, Result pattern types |
| `Entegrasyon.DataAccess` | EF Core `IntegrationDbContext`, migrations, entity configurations |
| `Entegrasyon.Business` | Business managers (Abstract + Concrete), FluentValidation validators, marketplace API clients, background services, event channels, cargo integrations |
| `Entegrasyon.ApplicationBootstrap` | DI registration via extension methods in `ApplicationDependencyExtension.cs` |
| `Entegrasyon.Blazor` | Blazor Server UI with MudBlazor, feature-based folder structure |
| `Entegrasyon.Desktop` | Photino.Blazor desktop application with Velopack auto-update |
| `Entegrasyon.AdminPanel` | MVC + SQLite tenant/customer management panel (vertical slice) |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) (for PostgreSQL, Redis, and integration tests)
- [Node.js](https://nodejs.org/) (only if running Playwright E2E tests)

### 1. Start Infrastructure

```bash
docker-compose up -d
```

This starts:
- **PostgreSQL 16** on port `5432`
- **Redis** on port `6379`
- **pgAdmin** on port `5050`

### 2. Apply Database Migrations

```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor
```

### 3. Run the Application

```bash
cd Application/Entegrasyon.Blazor && dotnet run
```

The app will be available at `http://localhost:5099`.

### 4. Run Tests

```bash
# Unit tests
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj

# Integration tests (requires Docker — Testcontainers auto-creates PostgreSQL)
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj

# bUnit component tests
dotnet test Test/Entegrasyon.BunitTest/Entegrasyon.BunitTest.csproj

# Admin panel tests
dotnet test Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj

# E2E tests (app must be running)
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj
```

---

## Commands Reference

```bash
# Build the entire solution
dotnet build Entegrasyon.sln

# Run a specific test by name
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TestClassName"

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor
```

---

## Key Patterns

### 1. Business Layer 3-Step Pipeline (Strict)

Every business manager method follows this exact sequence:

```
┌─────────────┐     ┌────────────────┐     ┌───────────┐
│  Validation  │────>│ Business Rules │────>│ Execution │
│ (FluentVal.) │     │  (LogicRunner) │     │  (Action) │
└─────────────┘     └────────────────┘     └───────────┘
     Fail?                Fail?
       │                    │
       v                    v
   ErrorResult          ErrorResult
```

1. **Validation** -- Validate the DTO/input with FluentValidation. Return `ErrorResult` on failure.
2. **Business Rules** -- Check domain rules (stock availability, uniqueness, etc.) via `LogicRunner`. Return `ErrorResult` on failure.
3. **Execution** -- Only runs if both steps above pass. Perform the actual operation and return `SuccessResult` / `SuccessDataResult<T>`.

### 2. Result Pattern

All business methods return `IResult` or `IDataResult<T>`:

```csharp
public interface IResult { bool Success { get; } string Message { get; } }
public interface IDataResult<T> : IResult { T Data { get; } }

// Implementations: SuccessResult, ErrorResult, SuccessDataResult<T>, ErrorDataResult<T>
```

### 3. Primary Constructor DI (C# 12)

```csharp
public class ProductManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IValidator<CreateProductDto> validator
) : IProductManager
```

### 4. Manager/Interface Pattern

Every business service has:
- `Abstract/IXxxManager.cs` -- interface
- `Concrete/XxxManager.cs` -- implementation

All registered in `ApplicationDependencyExtension.cs`.

### 5. EventChannel Pattern

Bounded channels (capacity 1000, `BoundedChannelFullMode.DropOldest`) for async background communication:

```csharp
EventChannel<StockPriceChangedEvent>  // → consumed by 7 marketplace sync services
EventChannel<ProductAddedEvent>       // → triggers marketplace publishing
EventChannel<CategoryUpdatedEvent>    // → triggers category re-sync
EventChannel<ProductCreatedForMarketplaceEvent>
```

When a product's stock/price changes, the event fans out to all active marketplace sync background services simultaneously.

### 6. Code-Behind (Strict)

Every `.razor` file **must** have a `.razor.cs` companion. No C# logic in razor markup.

### 7. Feature-Based Folder Structure

```
Features/
├── Attributes/
├── Auth/
├── Brands/
├── Categories/
├── CategoryImport/
├── Customers/
├── Dashboard/
├── Invoicing/
├── MarketplaceSync/
├── MatchedEntityImport/
├── Notifications/
├── Orders/
├── Printing/
├── Products/
├── Profile/
├── Reports/
├── Sales/
├── Settings/
└── Users/
```

Only shared components (layouts, dialogs, navmenu) live in `Components/Shared/`.

### 8. Dual Logging

| Logger | Audience | Language | Purpose |
|---|---|---|---|
| `IApplicationLogManager` | Admin users | Turkish | Clean, user-facing action logs (stored in DB) |
| `ILogger<T>` | Developers | Technical | Diagnostic logs (structured, Serilog-compatible) |

Both should be used in business methods; they serve different purposes.

### 9. Multi-Tenant Design

The system is designed for future multi-tenancy:
- Singleton services use `ConcurrentDictionary<int, T>` for per-tenant state (especially OAuth token caches)
- Per-tenant `SemaphoreSlim` for lock isolation
- All DB queries must support tenant filtering
- Tenant-specific configuration is stored in DB, not `appsettings.json`

### 10. EF Core Conventions

- Default **no-tracking** queries
- All entities inherit from `BaseEntity` (`Id`, `IsDeleted`, `DeletedAt`, `CreatedAt`, `UpdatedAt`)
- `SaveChangesAsync()` auto-converts timestamps to UTC
- Soft delete via `IsDeleted` flag with global query filters

---

## Domain Model

### Core Entities

```
Category ──< CategoryAttributeCategory >── CategoryAttribute ──< CategoryAttributeValue
                                                    │
Product ──< ProductVariant ──< AttributeKeyValue ───┘
    │              │
    │              └──< BranchOfficeStock (CurrentStock = FirstTotalStock - SoldQuantity)
    │
    └──< Image (stored in MinIO)

Customer ─┬─ RetailCustomer
           └─ CorporateCustomer

Sale ──< SaleItem
Order ──< OrderItem
StockMovement (full audit trail)
```

### Marketplace Matching

```
CategoryAttribute ──< CategoryAttributeMarketPlaceMatch (per marketplace)
CategoryAttributeValue ──< CategoryAttributeValueMarketPlaceMatch (per marketplace)
CargoCompany ──< CargoCompanyMarketPlaceMatch
```

These junction tables map internal attributes/values to their marketplace-specific equivalents.

---

## Background Services

27 background services handle marketplace synchronization:

| Category | Services |
|---|---|
| **Stock & Price Sync** | Trendyol, Hepsiburada, N11, Amazon, Pazarama, PttAVM, Ciceksepeti (7 services, consume `StockPriceChangedEvent`) |
| **Order Polling** | One per marketplace (7 services) |
| **Batch Status Polling** | Trendyol, Hepsiburada, Ciceksepeti, Pazarama, Amazon (listing + feed) |
| **Product Publishing** | `TrendyolProductPublishBackgroundService` |
| **Category Import** | `CategoryImportBackgroundService` |
| **Dashboard** | `DashboardRefreshService` |
| **E-Fatura** | `TrendyolEFaturaStatusPollingService` |

---

## DI Registration

All registrations are in `ApplicationDependencyExtension.cs`:

| Method | Registers |
|---|---|
| `AddApplicationDependencies()` | All managers, event channels, validators, Mapster profiles |
| `AddCustomDbContext()` | PostgreSQL with `IDbContextFactory`, no-tracking |
| `AddBackgroundServices()` | All 27 background services |
| `AddStorageServices()` | MinIO client + ImageSharp |
| `AddSignalRSettings()` | SignalR hub (`/NotificationHub`) |
| `AddNotification()` | Notification services |

---

## Testing Strategy

| Layer | Framework | Project | Count |
|---|---|---|---|
| Unit | xUnit + Moq + FluentAssertions | `Test/Entegrasyon.Test/` | ~97 |
| Integration | xUnit + Testcontainers + Respawn + WebApplicationFactory | `Test/Entegrasyon.IntegrationTest/` | -- |
| bUnit | bUnit + MudBlazor | `Test/Entegrasyon.BunitTest/` | 7 |
| Admin Panel | xUnit (MVC controllers) | `Test/Entegrasyon.AdminPanel.Test/` | 39 |
| E2E | NUnit + Playwright | `Test/Entegrasyon.E2E/` | 25 |

**Integration tests** use Testcontainers to auto-create an ephemeral PostgreSQL container. Respawn cleans the DB between tests while preserving seed data.

**E2E tests** expect the app running at `http://localhost:5099` (configurable via `E2E_BASE_URL` env var).

---

## Development Workflow

This project follows **TDD-First** development. Every feature and bug fix must follow:

1. Write the test (RED)
2. Run test, confirm it fails
3. Write minimum code to pass (GREEN)
4. Run test, confirm it passes
5. Refactor if needed
6. Run full test suite

**A feature without tests is not considered complete.**

---

## Project Structure

```
Entegrasyon/
├── Application/
│   ├── Entegrasyon.AdminPanel/          # MVC admin panel (SQLite)
│   ├── Entegrasyon.ApplicationBootstrap/ # DI registration
│   ├── Entegrasyon.Blazor/              # Blazor Server UI
│   │   ├── Components/Shared/           # Shared layouts, nav, dialogs
│   │   └── Features/                    # Feature-based pages (19 features)
│   ├── Entegrasyon.Business/
│   │   ├── Abstract/                    # Interfaces (IXxxManager)
│   │   ├── Concrete/                    # Implementations + marketplace clients
│   │   │   ├── Amazon/
│   │   │   ├── Ciceksepeti/
│   │   │   ├── Hepsiburada/
│   │   │   ├── Kargo/                   # Aras, Surat, Yurtici + mocks
│   │   │   ├── N11/
│   │   │   ├── Pazarama/
│   │   │   ├── Pttavm/
│   │   │   ├── Temu/
│   │   │   └── Trendyol/
│   │   ├── MapperProfiles/              # Mapster configurations
│   │   ├── Utilities/                   # Helpers (commission calculator, etc.)
│   │   └── Validation/FluentValidation/ # All validators
│   ├── Entegrasyon.DataAccess/
│   │   └── Concrete/EntityFrameworkCore/
│   │       ├── Contexts/                # IntegrationDbContext
│   │       ├── EntityConfigurations/    # EF Core fluent configs
│   │       └── Migrations/
│   ├── Entegrasyon.Desktop/             # Photino.Blazor desktop app
│   ├── Entegrasyon.DependencyResolver/
│   └── Entegrasyon.Entity/
│       ├── Categories/
│       ├── Customers/
│       ├── Dtos/                        # Request/response DTOs
│       ├── Marketplace/
│       ├── Orders/
│       ├── Products/
│       └── Sales/
├── Test/
│   ├── Entegrasyon.Test/                # Unit tests
│   ├── Entegrasyon.IntegrationTest/     # Integration tests
│   ├── Entegrasyon.BunitTest/           # Blazor component tests
│   ├── Entegrasyon.AdminPanel.Test/     # Admin panel tests
│   └── Entegrasyon.E2E/                 # Playwright E2E tests
├── docs/                                # Marketplace API documentation
│   ├── trendyol/
│   ├── n11/
│   ├── hepsiburada/
│   ├── amazon/
│   ├── pazarama/
│   ├── pttavm/
│   ├── ciceksepeti/
│   ├── temu/
│   └── kargo/
├── docker-compose.yml                   # Dev infrastructure
├── docker-compose.e2e.yml               # E2E test environment
├── docker-compose.stage.yml             # Staging
├── docker-compose.prod.yml              # Production
├── docker-compose.monitoring.yml        # Grafana + Loki + Uptime Kuma
└── Entegrasyon.sln
```

---

## Environment Configuration

Infrastructure services are configured via environment variables or `appsettings.json`:

| Variable | Default | Description |
|---|---|---|
| `DB_USER` | `baturhan` | PostgreSQL username |
| `DB_PASSWORD` | -- | PostgreSQL password |
| `E2E_BASE_URL` | `http://localhost:5099` | E2E test target URL |
| `Trendyol:UseMock` | `false` | Use mock Trendyol API client |

Marketplace API credentials and tenant-specific configurations are stored in the database, not in config files.

---

## Known Gotchas

- **MudBlazor namespace conflict:** `MudBlazor.CategoryAttribute` conflicts with `Entegrasyon.Entity.Categories.CategoryAttribute`. Use an alias in Blazor files:
  ```csharp
  using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;
  ```
- **MudDataGrid:** `Items` parameter requires `IEnumerable<T>` -- call `.AsEnumerable()` on `List<T>`.
- **No `Task.WhenAll` with DbContext:** Never parallelize service calls that share the same scoped `DbContext`.
- **Layout components:** Use `IServiceScopeFactory` for DB access in layout components to avoid scoped `DbContext` conflicts.
- **No exceptions in utilities:** Utility/calculator classes must not throw exceptions; validation belongs in the business layer with FluentValidation.

---

## License

Proprietary. All rights reserved.
