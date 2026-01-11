# Entegrasyon E-commerce Integration Platform

> ⚠️ **READ THIS FIRST**: All instruction files are consolidated. See [INSTRUCTION-INDEX.md](.github/INSTRUCTION-INDEX.md) for priority and reference.

## Essential Documents (Read in Order)

1. **[CORE-ARCHITECTURE-RULES.md](.github/CORE-ARCHITECTURE-RULES.md)** ← **START HERE** (20 min)
   - Source of truth for layering, entities, DI, messaging, naming
   - AI agents MUST read this before any coding task

2. **[INSTRUCTION-INDEX.md](.github/INSTRUCTION-INDEX.md)** (5 min)
   - Navigation map for all documentation
   - Priority matrix and conflict resolution

3. **This file** - Repository-specific context (15 min)

4. **Specialized files** (as needed):
   - [blazor.instructions.md](.github/instructions/blazor.instructions.md) - Component patterns
   - [code-quality.instructions.md](.github/instructions/code-quality.instructions.md) - Modularity & readability
   - [csharp.instructions.md](.github/instructions/csharp.instructions.md) - C# standards
   - [mud-blazor-changelog.instructions.md](.github/instructions/mud-blazor-changelog.instructions.md) - Component API
   - [performance-optimization.instructions.md](.github/instructions/performance-optimization.instructions.md) - Optimization guide

5. **[VECTOR-DB-STRATEGY.md](.github/VECTOR-DB-STRATEGY.md)** (optional, infrastructure)
   - Vector DB setup for persistent AI agent knowledge
   - Implementation roadmap & RAG patterns

**Conflict Resolution**: If files conflict, CORE-ARCHITECTURE-RULES.md wins (see [INSTRUCTION-INDEX.md](.github/INSTRUCTION-INDEX.md) for matrix)

## Architecture Overview

⚠️ **See [CORE-ARCHITECTURE-RULES.md](CORE-ARCHITECTURE-RULES.md) Section 1-2 for complete details**

This is a **multi-tenant e-commerce integration platform** with **strict layering**:

```
Blazor (UI) → Business (Managers) → DataAccess (Repositories) → Entity (Models)
```

**Critical Rule**: No cross-layer references. Use DI + interfaces for all communication.

**Key Layers**:
- **Presentation** (`Entegrasyon.Blazor`): MudBlazor UI server
- **Business** (`Entegrasyon.Business`): Managers, services, validation
- **DataAccess** (`Entegrasyon.DataAccess`): EF Core repositories
- **Domain** (`Entegrasyon.Entity`): Models and DTOs
- **DI** (`Entegrasyon.DependencyResolver`): Service registration

See [CORE-ARCHITECTURE-RULES.md](CORE-ARCHITECTURE-RULES.md) for:

## Solution Structure

```
Application/
├── Entegrasyon.Blazor          # Blazor Server UI (port 7070)
├── Entegrasyon.Business         # Business logic, managers, services
├── Entegrasyon.DataAccess       # EF Core repositories (Repository + UnitOfWork)
├── Entegrasyon.Entity           # Domain entities and DTOs
├── Entegrasyon.DependencyResolver  # Service registration, DI configuration
├── Entegrasyon.MessageQueue     # Wolverine message handlers
└── Entegrasyon.MessageQueue.Commands  # Command/event messages

Shared/
├── Shared/                      # Common utilities, extensions, base entities
└── MarketPlace/                 # Marketplace integration interfaces

WorkerServices/
└── TrendyolBackgroundService/   # Background jobs for marketplace sync
```

## Key Architectural Patterns

### 1. Layered Architecture with Explicit Dependencies
- **See [CORE-ARCHITECTURE-RULES.md](CORE-ARCHITECTURE-RULES.md) Section 1** for rules
- **Critical**: Never reference UI from Business; use interfaces + DI

### 2. Entity Framework Core Patterns
- **Repository + UnitOfWork**: Each entity has `I{Entity}Dal` interface (e.g., `IProductDal`)
- Implementations in `DataAccess/Concrete/EntityFrameworkCore/Ef{Entity}Dal.cs`
- **DbContext**: `IntegrationDbContext` with PostgreSQL provider
- Connection string: `Host=db;Port=5432;Database=IntegrationDb;Username=Baturhan;Password=649471`

Example usage in Business layer:
```csharp
public class ProductManager
{
    private readonly IMainProductDal _productDal;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Product>> GetProduct(Guid id)
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        return Result<Product>.Success(product);
    }
}
```

### 3. Entity Model Specifics

⚠️ **CRITICAL: For complete entity model reference with ALL properties, relationships, enums, and junction tables, see [CORE-ARCHITECTURE-RULES.md](CORE-ARCHITECTURE-RULES.md) Section 3**

**Key Reminders When Coding**:
- ❌ Product.Season and Product.Year are **STRING, NOT INT**
- ❌ CategoryMarketplace is a **JUNCTION TABLE** (composite key: CategoryId + MarketPlaceId)
- ✅ Use CategoryAttributeHumanized for **DISPLAY**, not CategoryAttributeKey
- ✅ All prices on ProductVariant are **DECIMAL** (money type)
- ✅ Owned entities: Address (used in Customer, Order, Sale)
- ✅ Search vectors: NpgsqlTsVector for PostgreSQL full-text search

**Always verify entity properties** before coding UI:
- Read actual C# file: `Application/Entegrasyon.Entity/{EntityName}.cs`
- Check junction tables for many-to-many relationships
- Validate enum values match the definition

### 4. Dual Messaging Strategy

**For in-process scenarios (single-instance Blazor)**:
- .NET Channels via `EventChannel<T>` (see `Blazor/Services/Channels/`)
- Register: `services.AddEventChannels()`
- Usage pattern:
```csharp
// Publish
await _productEventChannel.PublishAsync(new ProductUpdatedEvent(productId, "Updated"));

// Consume (in BackgroundService)
await foreach (var evt in _channel.Reader.ReadAllAsync(stoppingToken))
{
    // Handle event
}
```

### 5. Blazor UI Patterns
- **EventCallbacks** for component communication
- **Channels** for triggering UI updates from background services
- **Authentication**: Cookie-based (`CookieAuthenticationDefaults`)
- **State Management**: Scoped services + EventChannels (not Fluxor/Redux)

Component structure:
```razor
@inject ProductManager ProductManager
@inject EventChannel<ProductUpdatedEvent> ProductEventChannel
@inject ISnackbar Snackbar

<MudDataGrid T="Product" Items="@_products" />

@code {
    protected override async Task OnInitializedAsync()
    {
        await LoadProducts();
        // Subscribe to updates
        _ = Task.Run(async () => await ListenForUpdates());
    }
}
```

## Development Workflows

### Build & Run (Docker/Podman)
```bash
# Build all containers
podman-compose build

# Start infrastructure + app
podman-compose up -d

# Check logs
podman-compose logs entegrasyon.blazor -f

# Access UI
http://localhost:7070

# Access services
# PostgreSQL: localhost:5432
# Redis: localhost:6379
# RabbitMQ Admin: http://localhost:15672 (guest/guest)
# pgAdmin: http://localhost:5050 (admin@admin.com/root)
```

### Common Issues & Solutions
**Entity model mismatches**: Always verify actual entity properties before coding UI:
- Read `Application/Entegrasyon.Entity/{EntityName}.cs`
- Check junction tables for many-to-many relationships

**Async method warnings**: Blazor lifecycle methods (`OnInitializedAsync`, button clicks) should use `await`

**NuGet version conflicts**: Avoid mixing `Microsoft.CodeAnalysis.*` versions
- Remove `Microsoft.VisualStudio.Web.CodeGeneration.Design` if present
- Use `Microsoft.CodeAnalysis.Common 4.13.0` explicitly

## Project Conventions

### Naming & Style
- **PascalCase**: Classes, methods, public properties
- **camelCase**: Private fields (`_fieldName`), local variables
- **Interfaces**: Prefix with `I` (e.g., `IProductDal`, `IProductManager`)
- **Managers**: Business logic classes end with `Manager` and implement `IManager` interfaces (e.g., `ProductManager : IProductManager`)
- **DAL**: Data access layer interfaces end with `Dal`

### File Organization
- **Blazor Pages**: `Application/Entegrasyon.Blazor/Pages/{Feature}.razor`
- **Blazor Components**: `Application/Entegrasyon.Blazor/Components/{Shared|Dialogs}/{Name}.razor`
- **Business Managers**: `Application/Entegrasyon.Business/Concrete/{EntityName}Manager.cs`
- **Repositories**: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Ef{EntityName}Dal.cs`
- **Entities**: `Application/Entegrasyon.Entity/{Feature}/{EntityName}.cs`

### Validation
- FluentValidation for complex rules (see `Business/Validation/FluentValidation/`)
- DataAnnotations for simple validation (`[Required]`, `[EmailAddress]`)
- Add `@using System.ComponentModel.DataAnnotations` to `_Imports.razor`

### Error Handling
- Return `Result<T>` pattern from Business layer (success/failure with messages)
- Use `ISnackbar` for user notifications in Blazor
- Log errors with Serilog (configured in `Program.cs`)

## Marketplace Integration

### Critical Distinction: Category Import vs Category Matching

⚠️ **See [CORE-ARCHITECTURE-RULES.md](CORE-ARCHITECTURE-RULES.md) Section 4 for detailed workflow documentation**

**DO NOT CONFUSE THESE TWO**:
- **Import**: Creates NEW Category entities (one-time bulk operation from API)
- **Matching**: Links EXISTING categories to marketplace (ongoing manual mapping)

#### 1. Category Import (One-time or periodic bulk operation)
- **Purpose**: Import categories from external sources into our system as our own categories
- **When**: Done once or periodically, typically during initial setup or data migration
- **Source**: Multiple sources supported:
  - **Marketplace APIs** (Trendyol)
  - **CSV files** (bulk upload from spreadsheets)
  - **Excel files** (structured data import)
  - **Generic APIs** (other e-commerce platforms)
- **Process**:
  1. User navigates to `/categories/import` (via "İçe Aktar" button on Categories page)
  2. User selects import source (currently only Trendyol marketplace available)
  3. For marketplace: Load categories from Trendyol API in tree structure
  4. For files: Upload and parse CSV/Excel file (future feature)
  5. User browses category tree and selects which categories to import (multi-select with checkboxes)
  6. Import creates NEW Category entities in our database with:
     - ImportSource = Trendyol (or Csv, Api, Excel for file imports)
     - ExternalCategoryId = Trendyol's category ID
     - IsImported = true
  7. For marketplace imports: Automatically creates CategoryMarketplace junction records
  8. Import includes category attributes from Trendyol API
- **Result**: Categories become "ours" - they appear in regular category list alongside manually created ones
- **Implementation**:
  - Marketplace: `BaseCategoryImporterService`, `TrendyolCategoryImporter`, `CategoryImport.razor`
  - File-based: CSV/Excel parsers (to be implemented)
- **API Example** (Trendyol):
  - Categories: `GET https://apigw.trendyol.com/integration/product/product-categories`
  - Attributes: `GET https://apigw.trendyol.com/integration/product/product-categories/{categoryId}/attributes`
  - Response structure: `{ id, name, parentId, subCategories[] }`
  - All categories returned if no parameters provided

#### 2. Marketplace Matching (Manual mapping - Future Feature)
- **Purpose**: Link existing manually-created categories to marketplace categories
- **When**: After manual category creation, ongoing as needed
- **Process**:
  1. User creates category manually via Categories page (ImportSource = Manual)
  2. Later, user can match this category to a marketplace category
  3. Creates CategoryMarketplace junction record
- **Result**: Manual category gets linked to marketplace for sync/product listing
- **Implementation**: Part of category management UI (to be implemented)

### Key Differences
| Aspect | Category Import | Marketplace Matching |
|--------|-----------------|---------------------|
| Frequency | Once/rarely | Ongoing, as needed |
| Creates Categories? | YES - creates new Category entities | NO - uses existing categories |
| Source | Marketplace API | User selection |
| Attributes | Imported automatically | Not applicable |
| Use Case | Bulk setup, get marketplace's full catalog | Manual fine-tuning, custom categories |
| ImportSource | Set to marketplace | Remains Manual |

### Marketplace-Specific Implementations
Each marketplace has its own namespace:
- `Business/Concrete/Trendyol/` - Trendyol-specific services
- `Business/Concrete/Import/` - Category importers (BaseCategoryImporterService, TrendyolCategoryImporter)
- Implement `IMarketPlaceProductService` and `IMarketPlaceOrderService`
- Background sync via `BackgroundServices/` (Hosted Services)

## Testing
- **Unit Tests**: `Test/Entegrasyon.Test/`
- Use xUnit + Moq + FluentAssertions
- No "Arrange/Act/Assert" comments in tests
- Test naming: `MethodName_Scenario_ExpectedResult`

## Critical Notes for AI Agents

1. **Read documentation first** - Start with [CORE-ARCHITECTURE-RULES.md](CORE-ARCHITECTURE-RULES.md), then [INSTRUCTION-INDEX.md](INSTRUCTION-INDEX.md)
2. **Never assume entity property names** - Always check the actual C# file first
3. **Async is mandatory** - Blazor UI operations must be async
4. **Connection strings in docker-compose** override appsettings.json
5. **Event channels are per-type** - `EventChannel<ProductUpdatedEvent>` ≠ `EventChannel<CategoryUpdatedEvent>`
6. **MudBlazor components** - Refer to [mud-blazor-changelog.instructions.md](./instructions/mud-blazor-changelog.instructions.md) for version-correct API
7. **Database migrations** - Use EF Core migrations, not manual SQL
8. **Podman specific** - Use `podman-compose` (not `docker-compose` on Fedora)
9. **Layering violations** - If cross-layer access needed, stop and propose service interface instead (see CORE-ARCHITECTURE-RULES Section 1)
10. **Instructions are authoritative** - Conflict resolution: CORE-ARCHITECTURE-RULES.md wins (see INSTRUCTION-INDEX.md matrix)

## Related Documentation
- C# conventions: `.github/instructions/csharp.instructions.md`
- Blazor patterns: `.github/instructions/blazor.instructions.md`
- Performance: Global instruction file for optimization techniques
