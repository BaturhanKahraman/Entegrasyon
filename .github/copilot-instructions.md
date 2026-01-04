# Entegrasyon E-commerce Integration Platform

> IMPORTANT: Load these repository instruction files as well

- [.github/instructions/blazor.instructions.md](.github/instructions/blazor.instructions.md)
- [.github/instructions/code-quality.instructions.md](.github/instructions/code-quality.instructions.md)
- [.github/instructions/csharp.instructions.md](.github/instructions/csharp.instructions.md)
 - [.github/instructions/performance-optimization.instructions.md](.github/instructions/performance-optimization.instructions.md)

AI agents MUST read and apply the rules in the files above in addition to this document. When processing repository instructions, treat them with equal priority and merge their constraints with this file's guidance.

## Architecture Overview

This is a **multi-tenant e-commerce integration platform** connecting retail management systems with marketplaces (Trendyol, Hepsiburada, N11). Built with:
- **Frontend**: Blazor Server with MudBlazor UI components
- **Backend**: Layered architecture (.NET 8.0)
- **Infrastructure**: PostgreSQL, Redis (cache), RabbitMQ (messaging), Docker/Podman deployment
- **Messaging**: .NET Channels for in-process events

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

#### Core Entities
- **Address** (Owned Entity): City, Country, County, Street, ZipCode, FullAddress
- **BranchOffice**: Id (int), Name, IsDefaultMarketPlaceStock (bool), Users (IEnumerable<ApplicationUser>)
- **CargoCompany**: Id (int), Name, Code, TaxNumber, SearchVector (NpgsqlTsVector)
- **Image**: Id (int), Src (varchar(450)), AlternativeText, Description, IsCoverImage (bool), FileStorageType, ProductVariantId (Guid?), ProductVariant
- **MarketPlace**: Id (int), Name (required, max 50), ApiKey, ApiSecret, IsBasicAuth (bool), BasicAuthUserName, BasicAuthPassword

#### Categories Domain
- **Category**: Id (int), Name, IsFavorite (bool), IsImported (bool), ImportSource (enum), ExternalCategoryId (string?), ImportId (int?) [Deprecated], SuperCategoryId (int?), SubCategories, CategoryAttributes (List<CategoryAttributeCategory>), MarketplaceLinks (ICollection<CategoryMarketplace>), Products
- **CategoryAttribute**: Id (int), CategoryAttributeKey, CategoryAttributeHumanized (display name), AllowCustom (bool), ImportId (int), CategoryAttributeValues, Categories
- **CategoryAttributeCategory** (Junction): CategoryId, CategoryAttributeId, IsRequired (bool), IsSlicer (bool), IsVarianter (bool)
- **CategoryAttributeValue**: Id (int), Name, CategoryAttributeId, CategoryAttribute
- **CategoryMarketplace** (Junction): CategoryId, MarketPlaceId (composite key), ExternalCategoryId (string?), MarketPlaceCategoryName (string?), LastSyncedAt (DateTimeOffset?), IsActive (bool)
- **ImportSource** (enum): General sources (Manual=0, Csv=1, Api=2, Excel=3) and Marketplace sources (Trendyol=100)
- **AttributeKeyValue**: CategoryAttributeId, CustomValue, AttributeValueId (int?), ProductId (Guid), Product

#### Products Domain
- **Product**: Id (Guid), Title, Description, StockCode, Season (string), Year (string), BrandId (int?), CategoryId (int), ProductVariants, AttributeKeyValues, SearchVector
- **ProductVariant**: Id (Guid), ProductId (Guid), ProductVariantAttributes (List), Barcode, DimensionalWeight, CurrencyType ("TRY"), ListPrice (money), SalePrice (money), CostPrice (money), ECommercePrice (money), VatRate, BranchOfficeStocks, Images
- **ProductVariantAttribute**: CategoryAttributeValueId (int?), CategoryAttributeValue (string), CustomValue, IsVarianter (bool), IsSlicer (bool)
- **BranchOfficeStock**: BranchOfficeId (int), ProductVariantId (Guid?), CurrentStock (computed int), SoldQuantity (int), FirstTotalStock (int)

#### Brands Domain
- **Brand**: Id (int), Name (max 55), Products

#### Customers Domain
- **Customer** (Base): Id (int), PhoneNumber, CustomerType, Name, Surname, FullName, Address (owned), Sales, DiscountVouchers
- **CorporateCustomer** (inherits Customer): TaxNumber, CorporateName, CorporateSearchVector
- **RetailCustomer** (inherits Customer): NationalIdentity, RetailSearchVector

#### Orders Domain
- **Order**: Id (Guid), TotalQuantity (computed int), TotalPrice (computed decimal), OrderItems, BillingAddress (owned), ShippingAddress (owned)
- **OrderItem**: Id (long), OrderId (Guid), ProductId (Guid?), Product (ProductVariant), Quantity (int), UnitPrice (decimal)

#### Sales Domain
- **Sale**: Id (Guid), DiscountVoucherId (int?), SaleItems, SalePersonId (Guid), SalePerson (ApplicationUser), BranchOfficeId (int), GeneralDiscount (double), CustomerId (int?)
- **SaleItem**: Id (Guid), ProductVariantId (Guid), BranchOfficeId (int?), TaxPercentage (double), DiscountPercent (double), UnitPrice (decimal), Quantity (int), UsedDiscountVoucherCode
- **ChangeProduct**: Id (int) - Minimal entity
- **ReturnProduct**: Id (int), ProductId (Guid), Product (ProductVariant)

#### DiscountVouchers Domain
- **DiscountVoucher**: Id (int), Percentage (double), Amount (decimal), Code, ExpiringDate, IsActive (bool), CustomerId (int?)

#### Tenants Domain (Multi-tenancy)
- **Tenant**: Id (int), ConnectionString, MainCustomerId (int?), ConnectionInfo
- **MainCustomer**: Id (int), TenantId (int?), Info, StartedAt, ValidUntil
- **ConnectionInfo**: Database, Host, Port (int?), Password, Username, DatabaseType (enum: PostgreSql, MsSql, MySql, Sqlite)

#### Logs Domain
- **ApplicationLog**: Id (long), Content, ApplicationUserId (Guid?), LogType (enum), LogAction (enum: None, Add, Update, Delete, List), IpAddress, Object (jsonb)
- **LogType** enum: User, Auth, Order, Product, Branch, Sale, Matching, Category, Role, Brand, Customer, DiscountVoucher, Error=999

#### Matches Domain (Marketplace Integration)
- **CategoryMarketPlaceMatch**: ApplicationCategoryId (int), MarketPlaceId (int), MarketPlaceCategoryId (int)
- **BrandMarketPlaceMatch**: ApplicationBrandId (int), MarketPlaceId (int), MarketPlaceBrandId (int)
- **CargoCompanyMarketPlaceMatch**: ApplicationCargoCompanyId (int), MarketPlaceId (int), MarketPlaceCargoCompanyId (int)
- **CategoryAttributeMarketPlaceMatch**: ApplicationCategoryAttributeId (int), MarketPlaceId (int), MarketPlaceCategoryAttributeId (int)
- **CategoryAttributeValueMarketPlaceMatch**: ApplicationCategoryAttributeValueId (int), MarketPlaceId (int), MarketPlaceCategoryAttributeValueId (int)

#### Notifications Domain
- **Notification**: Id (long), Header, Content, IsRead (bool), ReadAt, NotificationsUsers, Users, NotificationClaims, Claims
- **NotificationsUsers** (Junction)
- **NotificationsClaims** (Junction)

#### Barcode Domain
- **TempBarcode**: Temporary barcode handling

#### Key Notes
- Use `CategoryAttributeHumanized` for display names (not `Name`)
- Product `Season` and `Year` are strings, not integers
- No `Gender` property on Product
- Junction tables: CategoryAttributeCategory, CategoryMarketplace, NotificationsUsers, NotificationsClaims
- Owned entities: Address (used in Customer, Order)
- Search vectors: NpgsqlTsVector for full-text search on CargoCompany, Product, CorporateCustomer, RetailCustomer
- Computed fields: Order.TotalQuantity, Order.TotalPrice, BranchOfficeStock.CurrentStock
- Money type columns: ProductVariant prices

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

### Category Import vs Marketplace Matching (CRITICAL!)

**Two DISTINCT workflows - DO NOT confuse these:**

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

1. **Never assume entity property names** - always check the actual C# file
2. **Async is mandatory** - Blazor UI operations must be async
3. **Connection strings in docker-compose** override appsettings.json
4. **Event channels are per-type** - `EventChannel<ProductUpdatedEvent>` ≠ `EventChannel<CategoryUpdatedEvent>`
5. **MudBlazor components** - refer to https://mudblazor.com/components for API
6. **Database migrations** - Use EF Core migrations, not manual SQL
7. **Fedora/Podman specific** - Use `podman-compose` not `docker-compose`
8. **Category Import ≠ Category Sync** - Import creates new categories from marketplace (one-time bulk), Matching links existing categories (ongoing manual)
9. **Instructions are living documentation** - When resolving major misunderstandings or implementing complex features, propose updates to this file
10. **Business logic in instructions** - Document non-obvious domain rules, integration patterns, and architectural decisions here for future AI agents

## Next Steps for New Features

1. Create entity in `Entegrasyon.Entity/`
2. Add DAL interface in `DataAccess/Abstract/I{Entity}Dal.cs`
3. Implement repository in `DataAccess/Concrete/EntityFrameworkCore/`
4. Register in `DependencyResolver/ApplicationDependencyExtension.cs`
5. Create Manager in `Business/Concrete/`
6. Build Blazor UI in `Blazor/Pages/` or `Blazor/Components/`
7. Add event channel if real-time updates needed

## Instructions Maintenance Policy

**When to update `.github/copilot-instructions.md`:**

1. **After major misunderstandings** - Document what was misunderstood, why, and the correct interpretation with examples
2. **Complex business logic** - Domain-specific rules that aren't obvious from code (e.g., Import vs Matching workflows)
3. **Integration patterns** - External API usage, data flow between systems, marketplace-specific behaviors
4. **Architectural decisions** - Why certain patterns were chosen over alternatives
5. **Common pitfalls** - Issues that caused bugs, confusion, or require special attention
6. **Critical data relationships** - Non-obvious entity relationships and their business meaning

**Format guidelines:**
- Use clear headings with **(CRITICAL!)** suffix for must-know information
- Include code examples, API endpoints, and response structures
- Add comparison tables for easily confused concepts
- Keep it concise but complete - prefer tables and bullet points
- Update existing sections rather than adding duplicate information

**Responsibility:**
- AI agents should propose instructions updates when encountering/resolving significant issues
- Always verify proposed changes don't contradict existing patterns
- User can disable file editing; in such cases, provide update suggestions in conversation

## Related Documentation
- C# conventions: `.github/instructions/csharp.instructions.md`
- Blazor patterns: `.github/instructions/blazor.instructions.md`
- Performance: Global instruction file for optimization techniques
