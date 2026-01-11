---
title: "Core Architecture Rules - Entegrasyon E-commerce Platform"
description: "Foundation rules for all AI agents and developers. Authoritative reference for layering, entity models, DI patterns, and critical constraints."
priority: "CRITICAL"
lastUpdated: "2026-01-11"
vectorDbKeywords: "layering, entity-model, dependency-injection, marketplace-integration, authentication, enum-types, composite-keys"
---

# CORE ARCHITECTURE RULES - Entegrasyon Platform

**⚠️ CRITICAL**: This document is the source of truth for all architectural decisions. If other instructions conflict with this document, this document wins. All AI agents MUST read this before any coding task.

**Last Updated**: 2026-01-11
**Format**: Vector DB optimized with clear sections and semantic tags

---

## 1. MANDATORY LAYERING ARCHITECTURE

### Layering Rules (ABSOLUTE - NO EXCEPTIONS)

```
Blazor (UI Layer)
    ↓ depends on
Business (Logic + Managers)
    ↓ depends on
DataAccess (EF Core Repositories)
    ↓ depends on
Entity (Domain Models) + Shared
```

**Critical Constraints**:
- ✅ Blazor CAN depend on: Business, DependencyResolver, Shared, Entity (interfaces only, not concrete)
- ❌ Blazor CANNOT directly reference: DataAccess concrete implementations
- ✅ Business CAN depend on: DataAccess, Entity, Shared
- ❌ Business CANNOT reference: Blazor UI code
- ✅ DataAccess CAN depend on: Entity, Shared
- ❌ DataAccess CANNOT reference: Business or Blazor

**Violations = Architectural Debt**. When violated:
1. Stop implementing
2. Propose service interface in Business layer instead
3. Open PR with design note for maintainers
4. AI agents should suggest refactor, not cross-layer access

**Vector Tag**: `layering-rules`, `cross-layer-violations`

---

## 2. DEPENDENCY INJECTION ORCHESTRATION

### DependencyResolver Responsibility

The project uses **two-part DI registration**:

```
Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
    ↓
Application/Entegrasyon.DependencyResolver/ApplicationDependencyExtension.cs
    ↓
Program.cs (orchestrator)
```

### Service Registration Pattern

**Where to register what**:

```csharp
// Program.cs - Only orchestration
var services = new ServiceCollection();
services.ApplyApplicationBootstrap();      // Calls bootstrap
services.ApplyApplicationDependency();     // Calls resolver

// ApplicationBootstrap/ApplicationDependencyExtension.cs
public static IServiceCollection ApplyApplicationBootstrap(this IServiceCollection services)
{
    services.AddScoped<IProductManager, ProductManager>();
    services.AddScoped<IProductDal, EfProductDal>();
    // Register all managers and repositories
    return services;
}

// ApplicationDependencyResolver/ApplicationDependencyExtension.cs
public static IServiceCollection ApplyApplicationDependency(this IServiceCollection services)
{
    // Configure DbContext, AutoMapper, FluentValidation
    services.AddDbContext<IntegrationDbContext>(options =>
        options.UseNpgsql(connectionString));
    services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
    return services;
}
```

**New Service Addition Checklist**:
1. Define interface in Business/Abstract/I{Entity}Manager.cs
2. Implement in Business/Concrete/{Entity}Manager.cs
3. Define DAL interface in DataAccess/Abstract/I{Entity}Dal.cs
4. Implement repository in DataAccess/Concrete/EntityFrameworkCore/Ef{Entity}Dal.cs
5. Register in ApplicationBootstrap or ApplicationDependency extension

**Vector Tag**: `dependency-injection`, `service-registration`, `di-pattern`

---

## 3. ENTITY MODEL - COMPLETE REFERENCE

### Critical Entity Properties (Must verify before coding)

#### Address (Owned Entity)
```csharp
public record Address
{
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? County { get; init; }
    public string? Street { get; init; }
    public string? ZipCode { get; init; }
    public string? FullAddress { get; init; }
}
```

#### Product Domain
```csharp
public class Product : BaseEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string StockCode { get; set; }
    public string? Season { get; set; }      // STRING, not int
    public string? Year { get; set; }        // STRING, not int
    public int? BrandId { get; set; }
    public int CategoryId { get; set; }

    public ICollection<ProductVariant> ProductVariants { get; set; }
    public ICollection<AttributeKeyValue> AttributeKeyValues { get; set; }
    public NpgsqlTsVector SearchVector { get; set; }
}

public class ProductVariant : BaseEntity
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? Barcode { get; set; }
    public decimal? DimensionalWeight { get; set; }
    public string CurrencyType { get; set; } = "TRY";

    // Money columns (important for pricing)
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal ECommercePrice { get; set; }
    public decimal VatRate { get; set; }

    public ICollection<ProductVariantAttribute> ProductVariantAttributes { get; set; }
    public ICollection<BranchOfficeStock> BranchOfficeStocks { get; set; }
    public ICollection<Image> Images { get; set; }
}

public class Image : BaseEntity
{
    public int Id { get; set; }
    public string Src { get; set; }
    public string? AlternativeText { get; set; }
    public string? Description { get; set; }
    public bool IsCoverImage { get; set; }
    public FileStorageType FileStorageType { get; set; }
    public Guid? ProductVariantId { get; set; }
}

public class BranchOfficeStock
{
    public int BranchOfficeId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int CurrentStock { get; set; }    // Computed
    public int SoldQuantity { get; set; }
    public int FirstTotalStock { get; set; }
}
```

#### Category Domain (Complex - Read Carefully)
```csharp
public class Category : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsImported { get; set; }
    public ImportSource ImportSource { get; set; }      // CRITICAL: Enum values
    public string? ExternalCategoryId { get; set; }
    public int? SuperCategoryId { get; set; }

    public ICollection<Category> SubCategories { get; set; }
    public ICollection<CategoryAttributeCategory> CategoryAttributes { get; set; }
    public ICollection<CategoryMarketplace> MarketplaceLinks { get; set; }
}

// ImportSource Enum - CRITICAL VALUES
public enum ImportSource
{
    Manual = 0,
    Csv = 1,
    Api = 2,
    Excel = 3,
    Trendyol = 100      // Marketplace-specific
}

public class CategoryAttribute : BaseEntity
{
    public int Id { get; set; }
    public string CategoryAttributeKey { get; set; }
    public string? CategoryAttributeHumanized { get; set; }  // Use for display
    public bool AllowCustom { get; set; }
    public int ImportId { get; set; }

    public ICollection<CategoryAttributeCategory> Categories { get; set; }
    public ICollection<CategoryAttributeValue> CategoryAttributeValues { get; set; }
}

// Junction table - Required
public class CategoryAttributeCategory
{
    public int CategoryId { get; set; }
    public int CategoryAttributeId { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSlicer { get; set; }
    public bool IsVarianter { get; set; }
}

// Junction table - Required
public class CategoryMarketplace
{
    public int CategoryId { get; set; }
    public int MarketPlaceId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public bool IsActive { get; set; }
}
```

#### Customer Domain (Inheritance)
```csharp
public abstract class Customer : BaseEntity
{
    public int Id { get; set; }
    public string? PhoneNumber { get; set; }
    public CustomerType CustomerType { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string FullName { get; set; }

    public Address Address { get; set; }    // Owned
    public ICollection<Sale> Sales { get; set; }
}

public class CorporateCustomer : Customer
{
    public string TaxNumber { get; set; }
    public string CorporateName { get; set; }
    public NpgsqlTsVector CorporateSearchVector { get; set; }
}

public class RetailCustomer : Customer
{
    public string NationalIdentity { get; set; }
    public NpgsqlTsVector RetailSearchVector { get; set; }
}
```

#### Order & Sales Domains
```csharp
public class Order : BaseEntity
{
    public Guid Id { get; set; }
    public int TotalQuantity { get; set; }        // Computed
    public decimal TotalPrice { get; set; }       // Computed

    public Address BillingAddress { get; set; }  // Owned
    public Address ShippingAddress { get; set; } // Owned
    public ICollection<OrderItem> OrderItems { get; set; }
}

public class Sale : BaseEntity
{
    public Guid Id { get; set; }
    public int? DiscountVoucherId { get; set; }
    public Guid SalePersonId { get; set; }
    public int BranchOfficeId { get; set; }
    public double GeneralDiscount { get; set; }
    public int? CustomerId { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; }
    public ApplicationUser SalePerson { get; set; }
}
```

#### Marketplace Domain
```csharp
public class MarketPlace : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }         // Required, max 50
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public bool IsBasicAuth { get; set; }
    public string? BasicAuthUserName { get; set; }
    public string? BasicAuthPassword { get; set; }
}
```

#### Brand Domain
```csharp
public class Brand : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }         // Max 55

    public ICollection<Product> Products { get; set; }
}
```

#### BranchOffice Domain
```csharp
public class BranchOffice : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsDefaultMarketPlaceStock { get; set; }

    public ICollection<ApplicationUser> Users { get; set; }
}
```

#### Matches Domain (Marketplace Mapping)
```csharp
// Junction tables for mapping application entities to marketplace entities
public class CategoryMarketPlaceMatch
{
    public int ApplicationCategoryId { get; set; }
    public int MarketPlaceId { get; set; }
    public int MarketPlaceCategoryId { get; set; }
}

public class BrandMarketPlaceMatch
{
    public int ApplicationBrandId { get; set; }
    public int MarketPlaceId { get; set; }
    public int MarketPlaceBrandId { get; set; }
}
```

#### Logs Domain
```csharp
public class ApplicationLog : BaseEntity
{
    public long Id { get; set; }
    public string Content { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public LogType LogType { get; set; }
    public LogAction LogAction { get; set; }
    public string? IpAddress { get; set; }
    public string? Object { get; set; }  // JSON
}

public enum LogType
{
    User = 0,
    Auth = 1,
    Order = 2,
    Product = 3,
    Branch = 4,
    Sale = 5,
    Matching = 6,
    Category = 7,
    Role = 8,
    Brand = 9,
    Customer = 10,
    DiscountVoucher = 11,
    Error = 999
}

public enum LogAction
{
    None = 0,
    Add = 1,
    Update = 2,
    Delete = 3,
    List = 4
}
```

#### Tenant Domain (Multi-tenancy)
```csharp
public class Tenant : BaseEntity
{
    public int Id { get; set; }
    public string ConnectionString { get; set; }
    public int? MainCustomerId { get; set; }

    public ConnectionInfo ConnectionInfo { get; set; }
    public MainCustomer? MainCustomer { get; set; }
}

public class MainCustomer : BaseEntity
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string? Info { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
}

public class ConnectionInfo
{
    public string Database { get; set; }
    public string Host { get; set; }
    public int? Port { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }
    public DatabaseType DatabaseType { get; set; }
}

public enum DatabaseType
{
    PostgreSql = 0,
    MsSql = 1,
    MySql = 2,
    Sqlite = 3
}
```

**CRITICAL REMINDERS**:
- Product.Season and Product.Year are **STRING, NOT INT**
- CategoryMarketplace is a **JUNCTION TABLE** (composite key)
- CategoryAttributeCategory is a **JUNCTION TABLE** with required/slicer/varianter flags
- Use CategoryAttributeHumanized for **DISPLAY**, not CategoryAttributeKey
- BranchOfficeStock.CurrentStock is **COMPUTED**
- All prices on ProductVariant are **DECIMAL** (money type)
- Search vectors (NpgsqlTsVector) used for PostgreSQL full-text search
- Owned entities: Address (used in Customer, Order, Sale)

**Vector Tag**: `entity-model`, `properties`, `composite-keys`, `owned-entities`, `junction-tables`, `enum-types`

---

## 4. MARKETPLACE INTEGRATION - TWO WORKFLOWS

### Critical Distinction: Import vs Matching

**DO NOT CONFUSE THESE TWO WORKFLOWS**

#### 1️⃣ Category Import (One-time bulk operation)
- **Purpose**: Create NEW Category entities in our system by importing from external source
- **When**: Initial setup, data migration, periodic bulk import
- **Sources**:
  - Marketplace APIs (currently: Trendyol)
  - CSV files (future)
  - Excel files (future)
  - Generic APIs (future)

**Process**:
1. User navigates to `/categories/import` or uses "İçe Aktar" button
2. Selects import source
3. For marketplace: API loads category tree with IDs
4. User selects categories to import (multi-select checkboxes)
5. System creates NEW Category records with:
   - `ImportSource = Trendyol` (or Csv, Excel, etc)
   - `ExternalCategoryId = Trendyol's category ID`
   - `IsImported = true`
6. Automatically creates CategoryMarketplace junction records
7. Imports category attributes from API

**Result**: Categories become "ours" - appear in regular category list

**Implementation**:
- Trendyol: `Business/Concrete/Import/BaseCategoryImporterService`, `TrendyolCategoryImporter`
- UI: `Blazor/Pages/Categories/CategoryImport.razor`

**Trendyol API**:
```
GET https://apigw.trendyol.com/integration/product/product-categories
GET https://apigw.trendyol.com/integration/product/product-categories/{categoryId}/attributes

Response:
{
  "id": 123,
  "name": "Electronics",
  "parentId": 0,
  "subCategories": [ {...} ]
}
```

#### 2️⃣ Marketplace Matching (Manual mapping - Future Feature)
- **Purpose**: Link EXISTING manually-created categories to marketplace categories
- **When**: After manual category creation, ongoing
- **Process**:
  1. User creates category manually (ImportSource = Manual)
  2. Later, user selects this category and matches to marketplace
  3. Creates CategoryMarketplace junction record
- **Result**: Manual category gets linked to marketplace for syncing

**Comparison Table**:
| Aspect | Import | Matching |
|--------|--------|----------|
| Creates Categories? | YES | NO |
| Frequency | Once/rarely | Ongoing |
| Source | Marketplace API | User selection |
| Attributes | Imported auto | Not applicable |
| ImportSource | Set to marketplace | Remains Manual |
| Use Case | Bulk setup | Manual fine-tuning |

**Vector Tag**: `marketplace-integration`, `category-import`, `category-matching`, `trendyol-api`, `import-workflow`

---

## 5. MESSAGING STRATEGY - TWO APPROACHES

### Dual Messaging for Different Scenarios

#### Scenario 1: Single-Instance Blazor Server
**Use**: .NET Channels (in-process events)

```csharp
// Service definition
public class ProductEventChannel
{
    private readonly Channel<ProductUpdatedEvent> _channel =
        Channel.CreateUnbounded<ProductUpdatedEvent>();

    public async Task PublishAsync(ProductUpdatedEvent evt)
    {
        await _channel.Writer.WriteAsync(evt);
    }

    public IAsyncEnumerable<ProductUpdatedEvent> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}

// Registration in Program.cs
services.AddScoped<ProductEventChannel>();

// Publishing from Business layer
public class ProductManager
{
    private readonly ProductEventChannel _channel;

    public async Task UpdateProduct(Product product)
    {
        // ... business logic
        await _channel.PublishAsync(new ProductUpdatedEvent(product.Id, "Updated"));
    }
}

// Consuming in Blazor
@inject ProductEventChannel _productEventChannel

protected override async Task OnInitializedAsync()
{
    _ = Task.Run(async () =>
    {
        await foreach (var evt in _productEventChannel.ReadAllAsync(stoppingToken))
        {
            await LoadProducts();
            StateHasChanged();
        }
    });
}
```

**When to use**: Development, small deployments, single server

#### Scenario 2: Multi-Instance / Distributed
**Use**: RabbitMQ via Wolverine framework

```csharp
// Command definition
public class UpdateProductCommand
{
    public Guid ProductId { get; set; }
    public string UpdatedBy { get; set; }
}

// Handler
public class UpdateProductCommandHandler
{
    public async Task Handle(UpdateProductCommand command)
    {
        // ... business logic
    }
}

// Publishing
public class ProductManager
{
    private readonly IMessageBus _bus;

    public async Task UpdateProduct(Product product)
    {
        await _bus.PublishAsync(new UpdateProductCommand { ProductId = product.Id });
    }
}
```

**When to use**: Production, multi-server, scalable deployments

**Vector Tag**: `messaging-strategy`, `event-channels`, `wolverine`, `rabbitmq`

---

## 6. NAMING & CODE CONVENTIONS

### Strict Naming Rules

**Classes & Types**:
- ✅ `ProductManager`, `ProductDal`, `IProductService` (PascalCase)
- ❌ `product_manager`, `ProductManager_v2`

**Methods & Properties**:
- ✅ `GetProductAsync()`, `ProductId`, `IsActive` (PascalCase)
- ❌ `get_product()`, `productid`

**Private Fields & Local Variables**:
- ✅ `private string _productName; var productCount = 0;` (camelCase)
- ❌ `private string ProductName; var ProductCount = 0;`

**Interfaces**:
- ✅ `IProductManager`, `IProductDal`, `IMarketplaceService` (I prefix + PascalCase)
- ❌ `ProductManagerInterface`, `productManager`

**Business Layer Classes**:
- ✅ Manager classes end with `Manager` and implement `IManager` interface
  - `ProductManager : IProductManager`
  - `CategoryManager : ICategoryManager`
- ❌ `ProductService`, `ProductLogic`, `ProductHandler`

**DataAccess Layer Classes**:
- ✅ DAL classes follow `Ef{Entity}Dal : I{Entity}Dal`
  - `EfProductDal : IProductDal`
  - `EfCategoryDal : ICategoryDal`
- ❌ `ProductRepository`, `ProductData`

**File Organization**:
- Business Managers: `Business/Concrete/{EntityName}Manager.cs`
- Repository Interfaces: `DataAccess/Abstract/I{EntityName}Dal.cs`
- Repository Implementations: `DataAccess/Concrete/EntityFrameworkCore/Ef{EntityName}Dal.cs`
- Blazor Pages: `Blazor/Pages/{Feature}.razor` (+ `.razor.cs`, `.razor.css`)
- Blazor Components: `Blazor/Components/{Shared|Dialogs}/{Name}.razor`
- Entities: `Entity/{Feature}/{EntityName}.cs`

**Enums**:
- ✅ `ImportSource.Trendyol`, `LogType.Product`, `CustomerType.Corporate` (PascalCase values)
- ❌ `importSource.trendyol`, `TRENDYOL`

**Vector Tag**: `naming-conventions`, `file-organization`, `interface-naming`

---

## 7. AUTHENTICATION & AUTHORIZATION

### Current Implementation

**Framework**: ASP.NET Core Identity with Cookie Authentication

```csharp
// Program.cs setup
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    });

services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});
```

**Usage in Blazor**:
```razor
@attribute [Authorize]
@attribute [Authorize(Roles = "Admin")]

<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity.Name</p>
    </Authorized>
    <NotAuthorized>
        <p>You are not authorized</p>
    </NotAuthorized>
</AuthorizeView>
```

**Roles** (typically in database or claims):
- Admin
- Manager
- User
- Viewer

**Vector Tag**: `authentication`, `authorization`, `identity`, `roles`

---

## 8. BLAZOR PATTERNS & LIFECYCLE

### Component File Structure (MANDATORY)

**3-File Separation Rule**:

For components with >30 lines of logic:
1. `Component.razor` - Markup only
2. `Component.razor.cs` - Code-behind
3. `Component.razor.css` - Scoped CSS (if needed)

**Component.razor**:
```razor
@page "/products"
@using Entegrasyon.Blazor.Models
@inject IProductManager ProductManager
@inject ISnackbar Snackbar

<MudContainer>
    <MudDataGrid T="Product" Items="@_products" />
</MudContainer>

@code {
    // This should be EMPTY or just @inherits if using code-behind
}
```

**Component.razor.cs**:
```csharp
namespace Entegrasyon.Blazor.Pages;

public partial class Products
{
    [Inject] private IProductManager? ProductManager { get; set; }
    [Inject] private ISnackbar? Snackbar { get; set; }

    private List<Product> _products = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _products = await ProductManager!.GetAllProductsAsync();
        }
        catch (Exception ex)
        {
            Snackbar?.Add("Error loading products", Severity.Error);
        }
    }
}
```

**Component.razor.css**:
```css
::deep .product-row {
    background-color: #f5f5f5;
}
```

### Blazor Lifecycle Methods

```csharp
protected override async Task OnInitializedAsync()
{
    // Called once when component initialized
    // Use for: Initial data loading, setup
    await LoadData();
}

protected override async Task OnParametersSetAsync()
{
    // Called when parameters change
    // Use for: Reload data based on parameter changes
}

protected override bool ShouldRender()
{
    // Called before every render
    // Use for: Performance optimization, prevent unnecessary renders
    return _needsRender;
}

public async Task HandleEventAsync()
{
    // Event handlers MUST be async
    // Always use await in Blazor
    await UpdateProductAsync();
}
```

**CRITICAL**: All Blazor async methods MUST use await. Never fire-and-forget.

### EventCallback Pattern

```csharp
// Parent component
<ChildComponent OnProductSelected="HandleProductSelected" />

private async Task HandleProductSelected(Product product)
{
    await ProcessProduct(product);
}

// Child component
@code {
    [Parameter]
    public EventCallback<Product> OnProductSelected { get; set; }

    private async Task SelectProduct(Product product)
    {
        await OnProductSelected.InvokeAsync(product);
    }
}
```

**Vector Tag**: `blazor-lifecycle`, `component-structure`, `event-callbacks`

---

## 9. DATABASE & REPOSITORY PATTERNS

### Entity Framework Core Setup

```csharp
// DbContext
public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
        : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    // ... other sets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Owned entities
        modelBuilder.Entity<Order>()
            .OwnsOne(o => o.BillingAddress);

        // Composite keys
        modelBuilder.Entity<CategoryMarketplace>()
            .HasKey(cm => new { cm.CategoryId, cm.MarketPlaceId });

        // Full-text search vectors
        modelBuilder.HasPostgresExtension("uuid-ossp");
    }
}
```

### Repository + UnitOfWork Pattern

**Repository Interface**:
```csharp
public interface IProductDal : IRepository<Product>
{
    Task<List<Product>> GetProductsByCategory(int categoryId);
    Task<Product?> GetProductWithVariantsAsync(Guid productId);
}
```

**Repository Implementation**:
```csharp
public class EfProductDal : EfEntityRepositoryBase<Product>, IProductDal
{
    public EfProductDal(IntegrationDbContext context) : base(context) { }

    public async Task<List<Product>> GetProductsByCategory(int categoryId)
    {
        return await _context.Products
            .Where(p => p.CategoryId == categoryId)
            .Include(p => p.ProductVariants)
            .ToListAsync();
    }
}
```

**UnitOfWork**:
```csharp
public interface IUnitOfWork
{
    IProductDal Products { get; }
    ICategoryDal Categories { get; }
    // ... other repositories

    Task<int> SaveAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly IntegrationDbContext _context;

    public IProductDal Products => new EfProductDal(_context);

    public async Task<int> SaveAsync() => await _context.SaveChangesAsync();
}
```

**Usage in Manager**:
```csharp
public class ProductManager : IProductManager
{
    private readonly IProductDal _productDal;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Product>> CreateProductAsync(Product product)
    {
        await _productDal.AddAsync(product);
        await _unitOfWork.SaveAsync();
        return Result<Product>.Success(product);
    }
}
```

**Vector Tag**: `entity-framework`, `repository-pattern`, `unit-of-work`, `dbcontext`

---

## 10. ERROR HANDLING & VALIDATION

### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static Result<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    public static Result<T> Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}
```

**Usage**:
```csharp
public async Task<Result<Product>> GetProductAsync(Guid id)
{
    try
    {
        var product = await _productDal.GetAsync(p => p.Id == id);
        return product is null
            ? Result<Product>.Failure("Product not found")
            : Result<Product>.Success(product);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching product");
        return Result<Product>.Failure("An error occurred");
    }
}
```

### FluentValidation

```csharp
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(p => p.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title max 255 chars");

        RuleFor(p => p.CategoryId)
            .NotEmpty().WithMessage("Category is required");
    }
}
```

**In Blazor**:
```razor
<MudForm Model="@_product" @ref="@_form">
    <MudTextField @bind-Value="_product.Title"
                  Label="Title"
                  HelperText="Product title" />
    <MudButton OnClick="@HandleSave">Save</MudButton>
</MudForm>

@code {
    private async Task HandleSave()
    {
        if (await _form.Validate())
        {
            // Valid
        }
    }
}
```

**Vector Tag**: `error-handling`, `result-pattern`, `validation`, `fluentvalidation`

---

## 11. VERSION CONSTRAINTS

### Required Versions

- **.NET Target**: `net8.0` (minimum)
- **C# Version**: 13 (or latest compatible with net8.0)
- **Entity Framework Core**: Latest stable for net8.0
- **PostgreSQL**: 13+ (preferred for full-text search)

### C# Features to Use

- ✅ Records (primary constructors, init-only properties)
- ✅ Pattern matching (is/switch expressions)
- ✅ Global usings in `GlobalUsings.cs`
- ✅ File-scoped namespaces
- ✅ Nullable reference types (`string?`, `string!`)
- ✅ Async/await (mandatory for I/O)

### Deprecated / Avoid

- ❌ Non-nullable reference type warnings without `?` or `!`
- ❌ Old-style `#nullable enable` pragmas (use file-scoped instead)
- ❌ Synchronous I/O (blocking operations)
- ❌ `new T()` for classes (use `new()` with target-typing)

**Vector Tag**: `version-constraints`, `net8.0`, `csharp-13`, `mudblazor-6`

---

## 12. QUICK REFERENCE MATRIX

| Task | Layer | Pattern | Example |
|------|-------|---------|---------|
| Create entity | DataAccess/Concrete | Repository | `EfProductDal : IProductDal` |
| Business logic | Business/Concrete | Manager | `ProductManager : IProductManager` |
| UI component | Blazor | 3-file separation | `Products.razor` + `.razor.cs` + `.razor.css` |
| Validation | Business/Validation | FluentValidation | `CreateProductValidator` |
| API endpoint | Business | HTTP endpoint | `ProductEndpoints.cs` |
| Background job | Business/BackgroundServices | HostedService | `ProductSyncService : BackgroundService` |
| Event | Messaging | EventChannel or Wolverine | `ProductUpdatedEvent` |
| Logging | Business | Serilog | `_logger.LogInformation(...)` |
| Cache | Business | MemoryCache | `IMemoryCache` injected |
| Async operation | Any | async/await | `await MethodAsync()` |

**Vector Tag**: `quick-reference`, `patterns-matrix`

---

## 13. COMMON MISTAKES (RED FLAGS)

### DO NOT DO THIS

❌ **Cross-layer references**:
```csharp
// In Blazor component - WRONG
var context = new IntegrationDbContext();  // Never!
var product = context.Products.First();    // Never!
```

✅ **Correct approach**:
```csharp
// In Blazor component - RIGHT
@inject IProductManager ProductManager
var product = await ProductManager.GetProductAsync(id);
```

---

❌ **Blocking async**:
```csharp
// In Blazor - WRONG
public async Task LoadProducts()
{
    _products = _productManager.GetProductsAsync().Result;  // Deadlock!
}
```

✅ **Correct**:
```csharp
protected override async Task OnInitializedAsync()
{
    _products = await _productManager.GetProductsAsync();
}
```

---

❌ **N+1 queries**:
```csharp
// In repository - WRONG
var products = _context.Products.ToList();
foreach (var p in products)
{
    var variants = _context.ProductVariants
        .Where(v => v.ProductId == p.Id)
        .ToList();  // One query per product!
}
```

✅ **Correct**:
```csharp
var products = await _context.Products
    .Include(p => p.ProductVariants)
    .ToListAsync();  // One query total
```

---

❌ **Wrong file location**:
```
❌ Blazor/Services/ProductRepository.cs        // Services shouldn't be in Blazor
❌ Business/ProductComponent.cs                 // Components shouldn't be in Business
❌ Entity/ProductManager.cs                     // Managers shouldn't be in Entity
```

✅ **Correct locations**:
```
✅ Business/Concrete/ProductManager.cs
✅ DataAccess/Concrete/EfProductDal.cs
✅ Blazor/Pages/Products.razor
✅ Blazor/Components/ProductCard.razor
```

---

❌ **Blazor component too large**:
```razor
@* Products.razor with 300+ lines - WRONG *@
<MudDataGrid Items="@_products">
    @* All logic inline, no separation *@
</MudDataGrid>

@code {
    // 250 lines of code here - REFACTOR!
}
```

✅ **Split into smaller pieces**:
```
Products.razor (50 lines markup)
Products.razor.cs (100 lines logic)
Products.razor.css (scoped styles)
+ ProductCard.razor (sub-component)
```

**Vector Tag**: `antipatterns`, `mistakes`, `red-flags`

---

## 14. AI AGENT RESPONSIBILITIES

When any AI agent works on this codebase:

1. **ALWAYS read this document first** - Before any coding task
2. **Verify entity models** - Check actual C# files, don't assume
3. **Respect layering** - Never cross layer boundaries
4. **Async is mandatory** - All I/O must be async
5. **Use DI patterns** - Never instantiate services directly
6. **Follow naming** - Interfaces start with I, managers end with Manager
7. **Propose refactors** - If layering violation needed, suggest service instead
8. **Document decisions** - Leave comments on complex logic
9. **Run validation** - Check compilation, no warnings
10. **Update instructions** - If discovering major issues, propose doc updates

**When conflicted**: This document is the source of truth. If another instruction conflicts, this wins.

**Vector Tag**: `ai-agent-guidelines`, `responsibilities`

---

## 15. VECTOR DATABASE OPTIMIZATION NOTES

This document is structured for **semantic search & vector embedding**:

- **Section headers** use single `#` or `##` for clear hierarchy
- **Code examples** are tagged with ✅/❌ for contrast
- **"CRITICAL"** and **"DO NOT"** markers are high-signal
- **Comparison tables** appear in section 4 and 12
- **Vector tags** at end of each section for semantic tagging
- **Enums** and **Entity properties** listed explicitly (searchable)
- **File paths** and **class names** are concrete (not abstract)

For embedding:
- Chunk by section (split on `##`)
- Tag each chunk with section number + vector keywords
- Include code examples as separate chunks with context
- Mark "CRITICAL" sections with higher weight

**Vector Tag**: `vector-db-optimization`, `chunking-strategy`

---

## 16. DOCUMENT MAINTENANCE POLICY

**When to update this document**:

✅ **DO UPDATE** for:
- Major architectural misunderstandings (confusing patterns)
- New layering rules or constraints
- Entity model changes or additions
- Marketplace integration updates
- Critical anti-patterns discovered

❌ **DON'T UPDATE** for:
- Code style preferences (use code-quality.instructions.md)
- Library version changes (use mud-blazor-changelog.instructions.md)
- Performance tips (use performance-optimization.instructions.md)
- Testing specifics (use csharp.instructions.md)

**Process**:
1. AI agent identifies issue/gap
2. Proposes update in PR or issue
3. Gets review from maintainers
4. Updates document with date and context
5. Links related files

**Vector Tag**: `maintenance`, `update-policy`

---

**Last Modified**: 2026-01-11
**Format Version**: 1.0 (Vector DB Optimized)
**Maintainer**: Baturhan
**Status**: Active & Authoritative
