# Entegrasyon E-commerce Integration Platform

## Architecture Overview

This is a **multi-tenant e-commerce integration platform** connecting retail management systems with marketplaces (Trendyol, Hepsiburada, N11). Built with:
- **Frontend**: Blazor Server with MudBlazor UI components
- **Backend**: Layered architecture (.NET 8.0)
- **Infrastructure**: PostgreSQL, Redis (cache), RabbitMQ (messaging), Docker/Podman deployment
- **Messaging**: Wolverine (RabbitMQ) for distributed messaging + .NET Channels for in-process events

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
- **Blazor** → Business → DataAccess → Entity
- **DependencyResolver** orchestrates all registrations (see `ApplicationDependencyExtension.cs`)
- Never reference UI from Business layer; use interfaces + DI

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

### 3. Entity Model Specifics (Critical!)
These differ from typical conventions:
- **Category**: `Id` is `int`, `SuperCategoryId` (not ParentCategoryId) is `int?`
- **Product**: `Id` is `Guid`, `BrandId` is `int?`, `CategoryId` is `int`
- **Product**: `Season` and `Year` are `string` types (not int)
- **Product**: No `Gender` property exists
- **CategoryAttribute**: Use `CategoryAttributeHumanized` for display names (not `Name`)
- **CategoryAttributeCategory**: Junction table with `IsRequired`, `IsSlicer`, `IsVarianter`

### 4. Dual Messaging Strategy
**For distributed scenarios (multi-instance)**:
- RabbitMQ via Wolverine framework
- Commands/Events in `Entegrasyon.MessageQueue.Commands`
- Handlers in `Entegrasyon.MessageQueue`

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
- **MudBlazor** for all components (v6.11.2)
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
- **Interfaces**: Prefix with `I` (e.g., `IProductDal`)
- **Managers**: Business logic classes end with `Manager` (e.g., `ProductManager`)
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

Each marketplace has its own namespace:
- `Business/Concrete/Trendyol/`
- Implement `IMarketPlaceProductService` and `IMarketPlaceOrderService`
- Background sync via `BackgroundServices/` (Hosted Services)

## Testing
- **Unit Tests**: `Test/Entegrasyon.Test/`
- Use xUnit + Moq + FluentAssertions
- No "Arrange/Act/Assert" comments in tests
- Test naming: `MethodName_Scenario_ExpectedResult`

## Critical Notes for AI Agents

1. **Never assume entity property names** - always check the actual C# file
2. **Async is mandatory** - Blazor UI operations must be async
3. **Connection strings in docker-compose** override appsettings.json
4. **Event channels are per-type** - `EventChannel<ProductUpdatedEvent>` ≠ `EventChannel<CategoryUpdatedEvent>`
5. **MudBlazor components** - refer to https://mudblazor.com/components for API
6. **Database migrations** - Use EF Core migrations, not manual SQL
7. **Fedora/Podman specific** - Use `podman-compose` not `docker-compose`

## Next Steps for New Features

1. Create entity in `Entegrasyon.Entity/`
2. Add DAL interface in `DataAccess/Abstract/I{Entity}Dal.cs`
3. Implement repository in `DataAccess/Concrete/EntityFrameworkCore/`
4. Register in `DependencyResolver/ApplicationDependencyExtension.cs`
5. Create Manager in `Business/Concrete/`
6. Build Blazor UI in `Blazor/Pages/` or `Blazor/Components/`
7. Add event channel if real-time updates needed

## Related Documentation
- C# conventions: `.github/instructions/csharp.instructions.md`
- Blazor patterns: `.github/instructions/blazor.instructions.md`
- Performance: Global instruction file for optimization techniques
