# Scrutor Auto-Scan DI Registration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace ~100+ manual DI registrations with Scrutor convention-based scanning.
**Architecture:** Add Scrutor `Scan()` block at the top of `AddApplicationDependencies()` targeting `Entegrasyon.Business.Concrete` namespaces. Naming-exception and conditional (mock/real) registrations remain explicit after the scan block and act as last-wins overrides.
**Tech Stack:** .NET 8, Scrutor 7.x, xUnit

---

## Context

`ApplicationDependencyExtension.cs` (538 lines) contains ~112 standard `IXxx → Xxx` scoped registrations that can be eliminated by Scrutor convention scanning. The file also contains:
- 13 `UseMock` conditional blocks (~116 lines) — must remain manual
- Naming mismatches (e.g. `ICategoryService → CategoryManager`) — must remain explicit
- Multi-registration interfaces (`INotificationSender`, `ICargoTrackingAdapter`) — must remain explicit
- Concrete-only registrations (`ExcelParser`, `TrendyolCategoryImporter`, …) — must remain explicit
- Singletons, `AddHostedService<>`, `AddHttpClient` — must remain manual

The scan block uses `.AsMatchingInterface()` which strips the `I` prefix and matches by convention (e.g. `BranchOfficeManager` → `IBranchOfficeManager`). Explicit registrations placed **after** the scan block act as last-wins overrides for any naming-exception case that was also accidentally caught by the scan.

---

## Files Affected

| File | Change |
|---|---|
| `Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj` | Add Scrutor NuGet |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Add scan block, remove ~100 manual lines |
| `Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs` | Phase 4 (optional): replace with scan |
| `Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj` | No change (project already references ApplicationBootstrap) |
| `Test/Entegrasyon.Test/DI/ScrutorScanTests.cs` | New: DI verification test (safety net) |

---

## Task 1: Add Scrutor NuGet + Write DI Verification Test (RED first)

### 1a. Add Scrutor package

Edit `Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj` — add inside the existing `<ItemGroup>` with other `<PackageReference>` entries:

```xml
<PackageReference Include="Scrutor" Version="7.0.0" />
```

Run `dotnet restore Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj` to verify the package resolves.

### 1b. Write the verification test

Create `Test/Entegrasyon.Test/DI/ScrutorScanTests.cs`:

```csharp
using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Notifications;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace Entegrasyon.UnitTest.DI;

/// <summary>
/// Safety-net: verifies that all standard-convention services resolve after
/// the Scrutor scan block is in place. Run RED before Task 2, GREEN after.
/// </summary>
public class ScrutorScanTests
{
    private static IServiceProvider BuildProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Force all UseMock=true so real HTTP clients are never created
                ["Trendyol:UseMock"] = "true",
                ["Hepsiburada:UseMock"] = "true",
                ["Amazon:UseMock"] = "true",
                ["N11:UseMock"] = "true",
                ["Pazarama:UseMock"] = "true",
                ["Pttavm:UseMock"] = "true",
                ["Ciceksepeti:UseMock"] = "true",
                ["Temu:UseMock"] = "true",
                ["YurticiKargo:UseMock"] = "true",
                ["SuratKargo:UseMock"] = "true",
                ["ArasKargo:UseMock"] = "true",
                ["TrendyolEFatura:UseMock"] = "true",
                ["EInvoice:UseMock"] = "true",
                ["ConnectionStrings:Main"] = "Host=localhost;Database=test;",
                ["Minio:Endpoint"] = "localhost:9000",
                ["Minio:AccessKey"] = "test",
                ["Minio:SecretKey"] = "test",
            })
            .Build();

        var services = new ServiceCollection();

        // Minimal ASP.NET Core services needed by the DI registrations
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        // Register a stub DbContext so the factory lambda resolves
        services.AddDbContext<IntegrationDbContext>(opts =>
            opts.UseInMemoryDatabase("ScrutorTest"), ServiceLifetime.Scoped);

        // The method under test
        services.AddApplicationDependencies(config);
        services.AddNotification();
        services.AddStorefrontServices();

        return services.BuildServiceProvider();
    }

    // --- Core business services (standard IXxx → Xxx convention) ---

    [Fact] public void IBranchOfficeManager_Resolves() =>
        BuildProvider().GetRequiredService<IBranchOfficeManager>().Should().NotBeNull();

    [Fact] public void ICustomerManager_Resolves() =>
        BuildProvider().GetRequiredService<ICustomerManager>().Should().NotBeNull();

    [Fact] public void IProductVariantManager_Resolves() =>
        BuildProvider().GetRequiredService<IProductVariantManager>().Should().NotBeNull();

    [Fact] public void IOfficeStockManager_Resolves() =>
        BuildProvider().GetRequiredService<IOfficeStockManager>().Should().NotBeNull();

    [Fact] public void IAttributeKeyValueManager_Resolves() =>
        BuildProvider().GetRequiredService<IAttributeKeyValueManager>().Should().NotBeNull();

    [Fact] public void ICategoryAttributeManager_Resolves() =>
        BuildProvider().GetRequiredService<ICategoryAttributeManager>().Should().NotBeNull();

    [Fact] public void ICategoryMatchService_Resolves() =>
        BuildProvider().GetRequiredService<ICategoryMatchService>().Should().NotBeNull();

    [Fact] public void IBrandMatchService_Resolves() =>
        BuildProvider().GetRequiredService<IBrandMatchService>().Should().NotBeNull();

    [Fact] public void INotificationManager_Resolves() =>
        BuildProvider().GetRequiredService<INotificationManager>().Should().NotBeNull();

    [Fact] public void IChatManager_Resolves() =>
        BuildProvider().GetRequiredService<IChatManager>().Should().NotBeNull();

    [Fact] public void IReportManager_Resolves() =>
        BuildProvider().GetRequiredService<IReportManager>().Should().NotBeNull();

    [Fact] public void IDashboardManager_Resolves() =>
        BuildProvider().GetRequiredService<IDashboardManager>().Should().NotBeNull();

    [Fact] public void IOrderManager_Resolves() =>
        BuildProvider().GetRequiredService<IOrderManager>().Should().NotBeNull();

    [Fact] public void IBulkOperationManager_Resolves() =>
        BuildProvider().GetRequiredService<IBulkOperationManager>().Should().NotBeNull();

    [Fact] public void IEInvoiceManager_Resolves() =>
        BuildProvider().GetRequiredService<IEInvoiceManager>().Should().NotBeNull();

    // --- Naming-exception services (must resolve via explicit override) ---

    [Fact] public void ICategoryService_Resolves_As_CategoryManager() =>
        BuildProvider().GetRequiredService<ICategoryService>().Should().BeOfType<CategoryManager>();

    [Fact] public void IProductService_Resolves_As_ProductManager() =>
        BuildProvider().GetRequiredService<IProductService>().Should().BeOfType<ProductManager>();

    [Fact] public void ILabelService_Resolves_As_LabelManager() =>
        BuildProvider().GetRequiredService<ILabelService>().Should().BeOfType<LabelManager>();

    [Fact] public void ILabelTemplateService_Resolves_As_LabelTemplateManager() =>
        BuildProvider().GetRequiredService<ILabelTemplateService>().Should().BeOfType<LabelTemplateManager>();

    [Fact] public void ITenantContext_Resolves() =>
        BuildProvider().GetRequiredService<ITenantContext>().Should().NotBeNull();

    // --- Storefront services ---

    [Fact] public void IStorefrontSettingsManager_Resolves() =>
        BuildProvider().GetRequiredService<IStorefrontSettingsManager>().Should().NotBeNull();

    [Fact] public void ICartManager_Resolves() =>
        BuildProvider().GetRequiredService<ICartManager>().Should().NotBeNull();

    [Fact] public void ICheckoutManager_Resolves() =>
        BuildProvider().GetRequiredService<ICheckoutManager>().Should().NotBeNull();

    // --- Mock-mode marketplace services ---

    [Fact] public void ITrendyolProductService_Resolves_Mock() =>
        BuildProvider().GetRequiredService<ITrendyolProductService>().Should().NotBeNull();

    [Fact] public void IHepsiburadaProductService_Resolves_Mock() =>
        BuildProvider().GetRequiredService<IHepsiburadaProductService>().Should().NotBeNull();

    [Fact] public void IAmazonListingService_Resolves_Mock() =>
        BuildProvider().GetRequiredService<IAmazonListingService>().Should().NotBeNull();
}
```

### 1c. Run the test (expect GREEN — all currently resolve before migration)

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~ScrutorScanTests"
```

All tests must pass before proceeding — this proves the baseline is correct. Any failure now means the test itself has a setup bug; fix before moving on.

### Commit after Task 1

```
git add Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj \
        Test/Entegrasyon.Test/DI/ScrutorScanTests.cs
git commit -m "feat(di): add Scrutor package + DI verification safety-net tests"
```

---

## Task 2: Add Scrutor Scan Block + Remove Auto-Scannable Registrations

### 2a. Add `using Scrutor;` to `ApplicationDependencyExtension.cs`

Add to the top of the file (after the last existing `using` line):

```csharp
using Scrutor;
```

### 2b. Add the scan block at the very TOP of `AddApplicationDependencies()`

Insert immediately after `public static IServiceCollection AddApplicationDependencies(this IServiceCollection services, IConfiguration configuration)` opens, before any other line:

```csharp
// ── Scrutor convention scan ──────────────────────────────────────────────
// Automatically registers all standard IXxx → Xxx Scoped pairs from the
// Business assembly. Naming-exception and conditional (UseMock) services
// are registered explicitly below and override via last-wins.
services.Scan(scan => scan
    .FromAssemblyOf<NotificationManager>()      // Entegrasyon.Business assembly
    .AddClasses(classes => classes
        .InNamespaces(
            "Entegrasyon.Business.Concrete",
            "Entegrasyon.Business.Concrete.Auth",
            "Entegrasyon.Business.Concrete.BulkOperations",
            "Entegrasyon.Business.Concrete.Import",
            "Entegrasyon.Business.Concrete.Invoicing",
            "Entegrasyon.Business.Concrete.Kargo",
            "Entegrasyon.Business.Concrete.POS",
            "Entegrasyon.Business.Concrete.Shipping",
            "Entegrasyon.Business.Concrete.Storefront",
            "Entegrasyon.Business.Notifications"
        )
        .Where(t =>
            !t.Name.StartsWith("Mock")                   &&  // handled by UseMock blocks
            !t.Name.EndsWith("CategoryImporter")         &&  // concrete-only, no interface
            !t.Name.EndsWith("MappingValidator")         &&  // concrete-only, no interface
            !t.Name.EndsWith("Cache")                    &&  // may need explicit lifetime
            !t.Name.EndsWith("InvoiceBuilder")           &&  // concrete-only
            !t.Name.EndsWith("InvoiceClient")            &&  // brand-named, keep manual
            !typeof(IHostedService).IsAssignableFrom(t)      // never auto-register hosted
        )
    )
    .AsMatchingInterface()      // strips I-prefix: BranchOfficeManager → IBranchOfficeManager
    .WithScopedLifetime()
);
// ─────────────────────────────────────────────────────────────────────────
```

### 2c. Remove the auto-scannable registrations

Remove the following lines from `AddApplicationDependencies()`. These are now covered by the scan block above.

**Core business services to remove (lines 64–106 in the original):**
```csharp
services.AddScoped<IBranchOfficeManager, BranchOfficeManager>();
services.AddScoped<IBrandService, BrandService>();
services.AddScoped<ICategoryAttributeManager, CategoryAttributeManager>();
services.AddScoped<IRoleService, RoleService>();
services.AddScoped<IApplicationLogManager, ApplicationLogManager>();
services.AddScoped<IApplicationUserManager, ApplicationUserManager>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<ICargoCompaniesManager, CargoCompaniesManager>();
services.AddScoped<ICustomerManager, CustomerManager>();
services.AddScoped<IOfficeStockManager, OfficeStockManager>();
services.AddScoped<IProductVariantManager, ProductVariantManager>();
services.AddScoped<IImageManager, ImageManager>();
services.AddScoped<IDiscountVoucherManager, DiscountVoucherManager>();
services.AddScoped<IAttributeKeyValueManager, AttributeKeyValueManager>();
services.AddScoped<ISaleManager, SaleManager>();
services.AddScoped<ITrendyolBrandImporterService, TrendyolBrandImporterService>();
services.AddScoped<IBarcodeService, BarcodeService>();
services.AddScoped<IMatchedEntityImportManager, MatchedEntityImportManager>();
services.AddScoped<IBarcodeScannerService, BarcodeScannerService>();
services.AddScoped<ICommissionCalculator, CommissionCalculator>();
services.AddScoped<IBrandMatchService, BrandMatchService>();
services.AddScoped<ICategoryMatchService, CategoryMatchService>();
services.AddScoped<IAttributeMatchManager, AttributeMatchManager>();
services.AddScoped<ICategoryMatchValidationService, CategoryMatchValidationService>();
services.AddScoped<ICategoryAutoMatchService, CategoryAutoMatchService>();
services.AddScoped<IAttributeAutoMatchService, AttributeAutoMatchService>();
services.AddScoped<ICategoryAttributeCategoryManager, CategoryAttributeCategoryManager>();
services.AddScoped<ICategoryAttributeValueManager, CategoryAttributeValueManager>();
services.AddScoped<INotificationManager, NotificationManager>();
services.AddScoped<IChatManager, ChatManager>();
services.AddScoped<IProductSyncManager, ProductSyncManager>();
services.AddScoped<IDiscountManager, DiscountManager>();
services.AddScoped<IMarketPlaceManager, MarketPlaceManager>();
services.AddScoped<IProductActivityLogger, ProductActivityLogger>();
services.AddScoped<IMarketplaceOverrideManager, MarketplaceOverrideManager>();
services.AddScoped<INotificationSettingManager, NotificationSettingManager>();
services.AddScoped<IReportManager, ReportManager>();
services.AddScoped<IApplicationSettingManager, ApplicationSettingManager>();
services.AddScoped<IPOSSessionManager, POSSessionManager>();
services.AddScoped<IBulkOperationManager, BulkOperationManager>();
services.AddScoped<IOrderManager, OrderManager>();
services.AddScoped<IDashboardManager, DashboardManager>();
services.AddScoped<IEInvoiceManager, EInvoiceManager>();
```

**Trendyol services to remove** (the ones that match convention; keep the explicit ones):
```csharp
services.AddScoped<ITrendyolApiClient, TrendyolApiClient>();
services.AddScoped<ITrendyolProductMapper, TrendyolProductMapper>();
```

**Hepsiburada services to remove:**
```csharp
services.AddScoped<IHepsiburadaApiClient, HepsiburadaApiClient>();
services.AddScoped<IHepsiburadaProductMapper, HepsiburadaProductMapper>();
```

**Amazon services to remove:**
```csharp
services.AddScoped<IAmazonApiClient, AmazonApiClient>();
services.AddScoped<IAmazonProductMapper, AmazonProductMapper>();
```

**N11 services to remove:**
```csharp
services.AddScoped<IN11SoapClient, N11SoapClient>();
services.AddScoped<IN11ProductMapper, N11ProductMapper>();
```

**Pazarama services to remove:**
```csharp
services.AddScoped<IPazaramaProductMapper, PazaramaProductMapper>();
services.AddScoped<IPazaramaBrandService, PazaramaBrandService>();
```

**PttAVM services to remove:**
```csharp
services.AddScoped<IPttavmProductMapper, PttavmProductMapper>();
```

**Shipping tracker to remove** (already covered by `Concrete.Shipping` namespace in scan):
```csharp
services.AddScoped<IShipmentTrackingManager, Entegrasyon.Business.Concrete.Shipping.ShipmentTrackingManager>();
```

**Storefront services to remove** from `AddStorefrontServices()` (all standard-convention ones):
```csharp
services.AddScoped<IStorefrontTenantContext, StorefrontTenantContext>();
services.AddScoped<IStorefrontSettingsManager, StorefrontSettingsManager>();
services.AddScoped<IStorefrontPageManager, StorefrontPageManager>();
services.AddScoped<IStorefrontBannerManager, StorefrontBannerManager>();
services.AddScoped<IStorefrontAuthManager, StorefrontAuthManager>();
services.AddScoped<ICartManager, CartManager>();
services.AddScoped<IStorefrontCouponManager, StorefrontCouponManager>();
services.AddScoped<ICheckoutManager, CheckoutManager>();
services.AddScoped<IStorefrontEmailService, StorefrontEmailService>();
services.AddScoped<IStorefrontReviewManager, StorefrontReviewManager>();
services.AddScoped<IStorefrontWishlistManager, StorefrontWishlistManager>();
services.AddScoped<IStorefrontContactManager, StorefrontContactManager>();
services.AddScoped<IStorefrontNewsletterManager, StorefrontNewsletterManager>();
services.AddScoped<IStorefrontReturnManager, StorefrontReturnManager>();
services.AddScoped<IStorefrontSearchHistoryManager, StorefrontSearchHistoryManager>();
services.AddScoped<IStorefrontStockNotificationManager, StorefrontStockNotificationManager>();
services.AddScoped<IStorefrontSizeGuideManager, StorefrontSizeGuideManager>();
services.AddScoped<IStorefrontGiftCardManager, StorefrontGiftCardManager>();
services.AddScoped<IStorefrontLoyaltyManager, StorefrontLoyaltyManager>();
services.AddScoped<IStorefrontReferralManager, StorefrontReferralManager>();
services.AddScoped<IStorefrontPushManager, StorefrontPushManager>();
services.AddScoped<IStorefrontCampaignManager, StorefrontCampaignManager>();
services.AddScoped<ISellerManager, SellerManager>();
services.AddScoped<ISellerOrderManager, SellerOrderManager>();
services.AddScoped<ISellerCommissionManager, SellerCommissionManager>();
services.AddScoped<ISellerPayoutManager, SellerPayoutManager>();
services.AddScoped<IStorefrontAbandonedCartManager, StorefrontAbandonedCartManager>();
services.AddScoped<IStorefrontQnAManager, StorefrontQnAManager>();
services.AddScoped<IStorefrontWalletManager, StorefrontWalletManager>();
```

**Notification services to remove** from `AddNotification()`:
```csharp
services.AddScoped<INotificationRecipientResolver, NotificationRecipientResolver>();
```

> NOTE: `INotificationRecipientResolver` follows the exact naming convention and will be picked up by the scan automatically. The multi-registration lines for `INotificationSender` and the typed `ISignalRNotificationSender`, `IEmailSender`, `ISmsSender` registrations MUST be kept.

### 2d. Lines that MUST be kept explicit (do not remove these)

Keep all of the following exactly as they are:

```csharp
// Tenant + feature (naming exception or singleton)
services.AddScoped<ITenantContext, HttpTenantContext>();          // naming exception
services.AddScoped<TenantMemoryCache>();                          // concrete-only
services.AddSingleton<ITenantRegistryDataSource, AdminPanelTenantDataSource>();
services.AddSingleton<ITenantRegistry, TenantRegistryService>();
services.AddSingleton<IFeatureDataSource, AdminPanelFeatureDataSource>();
services.AddScoped<IFeatureService, FeatureService>();
services.AddScoped<ApplicationLifetimeManager>();                 // concrete-only

// Naming exceptions (IXxxService → XxxManager)
services.AddScoped<ICategoryService, CategoryManager>();
services.AddScoped<IProductService, ProductManager>();
services.AddScoped<ILabelService, LabelManager>();
services.AddScoped<ILabelTemplateService, LabelTemplateManager>();
services.AddScoped<ITrendyolCategoryImportService, TrendyolCategoryImporterService>(); // "Import" vs "Importer"

// Concrete-only bulk operation helpers
services.AddScoped<ExcelParser>();
services.AddScoped<CsvParser>();
services.AddScoped<ProductImportValidator>();

// Singletons
services.AddSingleton<IRandomGenerator, RandomGenerator>();
services.AddSingleton<ILabelGenerator, ZplLabelGenerator>();
services.AddSingleton<IReceiptGenerator, EscPosReceiptGenerator>();
services.AddSingleton<IAmazonTokenManager, AmazonTokenManager>();   // MUST be Singleton (multi-tenant cache)

// Trendyol concrete-only helpers
services.AddScoped<TrendyolCategoryImporter>();
services.AddScoped<N11CategoryImporter>();
services.AddScoped<TrendyolMappingValidator>();
services.AddScoped<TrendyolSupplierAddressCache>();
services.AddScoped<TrendyolEFaturaInvoiceBuilder>();

// Hepsiburada concrete-only helpers
services.AddScoped<HepsiburadaCategoryImporter>();
services.AddScoped<HepsiburadaMappingValidator>();

// Amazon concrete-only helpers
services.AddScoped<AmazonMappingValidator>();

// N11 concrete-only helpers
services.AddScoped<N11MappingValidator>();

// Pazarama concrete-only helpers
services.AddScoped<PazaramaMappingValidator>();
services.AddScoped<PazaramaCategoryImporter>();

// PttAVM concrete-only helpers
services.AddScoped<PttavmCategoryImporter>();
services.AddScoped<PttavmMappingValidator>();

// Çiçeksepeti concrete-only helpers + dual-registration
services.AddScoped<ICiceksepetiCategoryImporter, CiceksepetiCategoryImporter>(); // typed access
services.AddScoped<CiceksepetiCategoryImporter>();                                // concrete-only
services.AddScoped<CiceksepetiMappingValidator>();

// Temu concrete-only helpers
services.AddScoped<TemuCategoryImporter>();

// Aras Kargo — concrete-only in real branch
// (ArasKargoClient has no interface, registered as concrete in real branch only)

// Multi-registration shipping trackers
services.AddScoped<ICargoTrackingAdapter, Entegrasyon.Business.Concrete.Shipping.ArasTrackingAdapter>();
services.AddScoped<ICargoTrackingAdapter, Entegrasyon.Business.Concrete.Shipping.SuratTrackingAdapter>();
services.AddScoped<ICargoTrackingAdapter, Entegrasyon.Business.Concrete.Shipping.YurticiTrackingAdapter>();

// Notification: multi-registration + typed sender interfaces
services.AddScoped<ISignalRNotificationSender, SignalRSender>();
services.AddScoped<IEmailSender, EmailSender>();
services.AddScoped<ISmsSender, SmsSender>();
services.AddScoped<INotificationSender, SignalRSender>();   // collection
services.AddScoped<INotificationSender, EmailSender>();     // collection
services.AddScoped<INotificationSender, SmsSender>();       // collection

// Storefront naming exception
services.AddScoped<IPaymentGatewayService, IyzicoPaymentService>(); // provider-branded impl
services.AddSingleton<IStorefrontTenantResolver, StorefrontTenantResolver>();

// All 13 UseMock conditional blocks — unchanged

// All AddHostedService<> calls — unchanged

// HttpClient, DbContext factory, SignalR, Storage — unchanged
```

### 2e. Verify — build and run the safety-net tests

```bash
# Build
dotnet build Entegrasyon.sln

# Safety-net tests must still be GREEN
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~ScrutorScanTests"

# Full unit test suite
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

All tests must pass. If a service fails to resolve, the scan filter is excluding it — add the namespace to `InNamespaces()` or add an explicit registration.

### Commit after Task 2

```
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(di): add Scrutor scan block and remove ~100 manual AddScoped registrations"
```

---

## Task 3: Handle Naming Exceptions — Verify Explicit Overrides

### 3a. Confirm the naming-exception registrations are in place

After Task 2 the following registrations already exist explicitly (either never removed or added back). Verify each one is present in `ApplicationDependencyExtension.cs` and annotated:

```csharp
// NAMING EXCEPTION: IXxxService → XxxManager (Scrutor cannot match)
services.AddScoped<ICategoryService, CategoryManager>();
services.AddScoped<IProductService, ProductManager>();
services.AddScoped<ILabelService, LabelManager>();
services.AddScoped<ILabelTemplateService, LabelTemplateManager>();

// NAMING EXCEPTION: "Import" vs "Importer" suffix
services.AddScoped<ITrendyolCategoryImportService, TrendyolCategoryImporterService>();

// NAMING EXCEPTION: different class name entirely
services.AddScoped<ITenantContext, HttpTenantContext>();

// NAMING EXCEPTION: marketplace-prefixed impl
// IMarketplaceSearchService and IMarketplaceCategoryAttributeProvider are inside the
// UseMock blocks — both mock and real branches register these explicitly, so no action needed.

// NAMING EXCEPTION: payment provider brand
services.AddScoped<IPaymentGatewayService, IyzicoPaymentService>(); // in AddStorefrontServices()

// NAMING EXCEPTION: SignalR short alias
services.AddScoped<ISignalRNotificationSender, SignalRSender>(); // in AddNotification()

// LIFETIME EXCEPTION: must be Singleton (multi-tenant token cache)
services.AddSingleton<IAmazonTokenManager, AmazonTokenManager>();
```

### 3b. Verify each exception resolves to the correct concrete type

Add a dedicated block of assertions to the `ScrutorScanTests.cs` file you created in Task 1 (these tests should already be there — confirm they pass):

```csharp
[Fact] public void ICategoryService_Resolves_As_CategoryManager() =>
    BuildProvider().GetRequiredService<ICategoryService>().Should().BeOfType<CategoryManager>();

[Fact] public void IProductService_Resolves_As_ProductManager() =>
    BuildProvider().GetRequiredService<IProductService>().Should().BeOfType<ProductManager>();

[Fact] public void ILabelService_Resolves_As_LabelManager() =>
    BuildProvider().GetRequiredService<ILabelService>().Should().BeOfType<LabelManager>();

[Fact] public void ILabelTemplateService_Resolves_As_LabelTemplateManager() =>
    BuildProvider().GetRequiredService<ILabelTemplateService>().Should().BeOfType<LabelTemplateManager>();

[Fact] public void ITenantContext_Resolves_As_HttpTenantContext() =>
    BuildProvider().GetRequiredService<ITenantContext>().Should().BeOfType<HttpTenantContext>();
```

Run:

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj \
  --filter "FullyQualifiedName~ScrutorScanTests"
```

All must be GREEN.

### 3c. Run integration tests as final gate

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```

Integration tests use `WebApplicationFactory` and exercise the real DI container. Any missed registration will fail with a clear `InvalidOperationException`. Fix by adding the missing explicit registration in the appropriate section of `ApplicationDependencyExtension.cs`.

### Commit after Task 3

```
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
        Test/Entegrasyon.Test/DI/ScrutorScanTests.cs
git commit -m "fix(di): verify and annotate all naming-exception explicit overrides"
```

---

## Task 4: Verify, Cleanup, and Update CLAUDE.md

### 4a. Run all test suites

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```

Both must be green.

### 4b. Verify line reduction

```bash
wc -l Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
```

Expect the file to shrink from 538 lines to approximately 430–450 lines. The `AddApplicationDependencies()` method specifically should lose ~90–100 lines.

### 4c. Update CLAUDE.md — document the auto-scan convention

Add the following block to the **Key Patterns** section in `CLAUDE.md`:

```markdown
**Scrutor Auto-Scan (Strict Rule):**
Business layer services in `Entegrasyon.Business.Concrete` and sub-namespaces are registered automatically via Scrutor convention scanning in `AddApplicationDependencies()`. The convention:
- Interface name = `I` + class name (e.g. `FooManager` → `IFooManager`, `FooService` → `IFooService`)
- Lifetime: Scoped (default)
- Assembly: `Entegrasyon.Business`

If a new service follows the naming convention, it is **automatically registered** — no change to `ApplicationDependencyExtension.cs` needed.

If a service is a **naming exception** (e.g. `ICategoryService → CategoryManager`), it must be registered explicitly after the scan block with a `// NAMING EXCEPTION:` comment.

If a service needs **Singleton** lifetime, it must be registered explicitly with `AddSingleton<>` — the scan block always uses Scoped.

Services in the `Where(t => ...)` exclusion filter (CategoryImporter, MappingValidator, Cache, etc.) are concrete-only and must be registered explicitly with `AddScoped<ConcreteType>()`.
```

### 4d. Final commit

```bash
git add CLAUDE.md
git commit -m "docs(di): document Scrutor auto-scan convention in CLAUDE.md"
```

---

## Task 5 (Optional): Auto-Scan Validators — Phase 4

> This task is independent of Tasks 1–4 and can be done in a separate session or PR.

### 5a. Replace `ServiceDependencyExtension.AddValidators()` with Scrutor scan

Replace the 35 explicit `AddScoped<IValidator<T>, TValidator>()` lines in `ServiceDependencyExtension.cs` with:

```csharp
public static IServiceCollection AddValidators(this IServiceCollection services)
{
    services.Scan(scan => scan
        .FromAssemblyOf<FluentValidator>()   // Entegrasyon.Business assembly
        .AddClasses(classes => classes
            .AssignableTo(typeof(AbstractValidator<>))
        )
        .AsImplementedInterfaces()
        .WithScopedLifetime()
    );

    // Keep manual: IFluentValidator has no generic type parameter, must stay explicit
    services.AddScoped<IFluentValidator, FluentValidator>();
    return services;
}
```

### 5b. Write a verification test first

Add to `ScrutorScanTests.cs`:

```csharp
[Fact]
public void IFluentValidator_Resolves()
{
    var provider = BuildProvider();
    using var scope = provider.CreateScope();
    var validator = scope.ServiceProvider.GetRequiredService<IFluentValidator>();
    validator.Should().BeOfType<FluentValidator>();
}

[Fact]
public void AddProductValidator_Resolves_Via_IValidator()
{
    using var scope = BuildProvider().CreateScope();
    var validator = scope.ServiceProvider
        .GetRequiredService<IValidator<AddProductDto>>();
    validator.Should().NotBeNull();
}
```

Run RED → implement → run GREEN.

### 5c. Run full test suite

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

### Commit after Task 5

```
git add Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs \
        Test/Entegrasyon.Test/DI/ScrutorScanTests.cs
git commit -m "feat(di): replace manual validator registrations with Scrutor scan"
```

---

## Summary of What is Removed vs Kept

| Category | Before | After | Action |
|---|---|---|---|
| Standard `IXxx → Xxx` scoped (core) | ~50 lines | 0 | Removed — scan covers |
| Standard `IXxx → Xxx` scoped (storefront) | ~30 lines | 0 | Removed — scan covers |
| Standard `IXxx → Xxx` scoped (marketplace API clients/mappers) | ~15 lines | 0 | Removed — scan covers |
| Naming exceptions | ~5 lines | ~12 lines | Kept + annotated |
| UseMock conditional blocks | ~116 lines | ~116 lines | Unchanged |
| Concrete-only registrations | ~20 lines | ~20 lines | Unchanged |
| Singletons | ~11 lines | ~11 lines | Unchanged |
| Multi-registration (INotificationSender, ICargoTrackingAdapter) | ~8 lines | ~8 lines | Unchanged |
| AddHostedService | ~29 lines | ~29 lines | Unchanged |
| HttpClient / DbContext / SignalR | ~15 lines | ~15 lines | Unchanged |
| Scrutor scan block (new) | 0 | ~20 lines | Added |
| **Total** | **~538 lines** | **~445 lines** | **~90 lines removed** |
