---
title: Scrutor Auto-Scan DI Registration
date: 2026-03-30
status: Draft
---

# Scrutor Auto-Scan DI Registration

## Problem Statement

`ApplicationDependencyExtension.cs` currently contains **538 lines** of manual DI registration:

| Registration type | Count |
|---|---|
| `AddScoped<>` | 228 |
| `AddSingleton<>` | 11 |
| `AddHostedService<>` | 29 |
| `AddHttpClient()` | 3 |

Most of the `AddScoped<>` lines are mechanical `IXxx → Xxx` wiring with no configuration logic. Every time a new manager or service is added, a developer must touch this file as a separate step — and forgetting to do so causes a runtime `InvalidOperationException` that surfaces only at startup or first use.

Scrutor (assembly scanning + decoration for Microsoft DI) can eliminate the mechanical lines while keeping the intentional ones.

---

## Current Registration Inventory

### 1. Standard Scoped (auto-scan candidate)

Flat `IXxxManager → XxxManager` / `IXxxService → XxxService` pairs where the convention holds (strip `I`, strip `Manager`/`Service` suffix, find matching concrete). ~112 registrations fall into this bucket across core business, storefront, and marketplace-neutral services.

Examples:
- `IBranchOfficeManager → BranchOfficeManager`
- `ICustomerManager → CustomerManager`
- `IProductSyncManager → ProductSyncManager`
- `IStorefrontSettingsManager → StorefrontSettingsManager` (×25 storefront services)
- `ISellerManager → SellerManager` (×4 seller services)

### 2. Naming Convention Exceptions (must remain manual or use explicit mapping)

These interfaces do **not** follow the `IXxx → Xxx` convention and Scrutor cannot resolve them automatically:

| Interface | Implementation | Reason |
|---|---|---|
| `ICategoryService` | `CategoryManager` | "Service" → "Manager" mismatch |
| `IProductService` | `ProductManager` | "Service" → "Manager" mismatch |
| `ILabelService` | `LabelManager` | "Service" → "Manager" mismatch |
| `ILabelTemplateService` | `LabelTemplateManager` | "Service" → "Manager" mismatch |
| `ITenantContext` | `HttpTenantContext` | Completely different name |
| `IMarketplaceSearchService` | `TrendyolMarketplaceSearchService` | Marketplace-prefixed impl |
| `IMarketplaceCategoryAttributeProvider` | `TrendyolCategoryAttributeProvider` | Marketplace-prefixed impl |
| `IPaymentGatewayService` | `IyzicoPaymentService` | Provider-branded impl |
| `ITrendyolCategoryImportService` | `TrendyolCategoryImporterService` | "Import" vs "Importer" |
| `IShipmentTrackingManager` | `ShipmentTrackingManager` (in Shipping sub-ns) | Sub-namespace |
| `ISignalRNotificationSender` | `SignalRSender` | Short alias |
| `IEInvoiceIntegratorClient` | `ParasutInvoiceClient` (non-mock) | Brand-named impl |

### 3. Mock/Real Conditional Registrations (must remain manual)

13 `configuration.GetValue<bool>("X:UseMock")` guards cover ~116 `AddScoped<>` lines across:

| Config key | Interface count |
|---|---|
| `Trendyol:UseMock` | 6 interfaces |
| `Hepsiburada:UseMock` | 5 interfaces |
| `Amazon:UseMock` | 6 interfaces |
| `N11:UseMock` | 4 interfaces |
| `Pazarama:UseMock` | 5 interfaces |
| `Pttavm:UseMock` | 7 interfaces |
| `Ciceksepeti:UseMock` | 9 interfaces |
| `Temu:UseMock` | 1 interface |
| `YurticiKargo:UseMock` | 2 interfaces |
| `SuratKargo:UseMock` | 1–2 interfaces |
| `ArasKargo:UseMock` | 1 interface |
| `TrendyolEFatura:UseMock` | 2 interfaces |
| `EInvoice:UseMock` | 1 interface |

These **cannot** be auto-scanned because the correct implementation depends on runtime configuration. Scrutor would register the real implementation unconditionally, then the mock branch would double-register — and while .NET DI's last-wins rule would technically make it work, it is fragile (order-dependent) and semantically wrong (both classes are instantiated when the container is built in some DI frameworks, and double-registration is confusing).

**Decision: keep all UseMock blocks entirely manual.**

### 4. Multi-Registration (one interface, many implementations)

These intentionally register multiple implementations against the same interface for `IEnumerable<T>` injection. Auto-scan would handle them incorrectly (it would pick only one or create duplicates unpredictably):

- `ICargoTrackingAdapter` → `ArasTrackingAdapter`, `SuratTrackingAdapter`, `YurticiTrackingAdapter`
- `INotificationSender` → `SignalRSender`, `EmailSender`, `SmsSender`

Also note the dual-registration pattern for notification senders:
```csharp
services.AddScoped<ISignalRNotificationSender, SignalRSender>();  // typed access
services.AddScoped<INotificationSender, SignalRSender>();         // collection
```

**Decision: keep these manual.**

### 5. Concrete-Only Registrations (no interface)

Several concrete classes are registered without an interface and are injected directly:

- `TenantMemoryCache`
- `ApplicationLifetimeManager`
- `ExcelParser`, `CsvParser`, `ProductImportValidator`
- `TrendyolCategoryImporter`, `N11CategoryImporter`, `TrendyolMappingValidator`
- `TrendyolSupplierAddressCache`
- `HepsiburadaCategoryImporter`, `HepsiburadaMappingValidator`
- `AmazonMappingValidator`
- `N11MappingValidator`
- `PazaramaMappingValidator`, `PazaramaCategoryImporter`
- `PttavmCategoryImporter`, `PttavmMappingValidator`
- `CiceksepetiCategoryImporter`, `CiceksepetiMappingValidator`
- `TemuCategoryImporter`
- `ArasKargoClient` (conditional, real-only branch)
- `TrendyolEFaturaInvoiceBuilder`

Scrutor can auto-register these as `RegisterAsSelf()`, but since they are already structurally grouped (all `*CategoryImporter`, all `*MappingValidator`) they can also be handled with a targeted scan filter. Whether to scan or leave manual is a judgment call — see Strategy below.

### 6. Singleton Registrations (must remain manual — lifetime is explicit)

```csharp
services.AddSingleton<ITenantRegistryDataSource, AdminPanelTenantDataSource>();
services.AddSingleton<ITenantRegistry, TenantRegistryService>();
services.AddSingleton<IFeatureDataSource, AdminPanelFeatureDataSource>();
services.AddSingleton<IRandomGenerator, RandomGenerator>();
services.AddSingleton<ILabelGenerator, ZplLabelGenerator>();       // Labels
services.AddSingleton<IReceiptGenerator, EscPosReceiptGenerator>(); // POS
services.AddSingleton<IAmazonTokenManager, AmazonTokenManager>();  // OAuth token cache
services.AddSingleton<IMinioFileStorage, MinioFileStorage>();      // Storage
services.AddSingleton<IImageProcessingService, ImageProcessingService>();
services.AddSingleton<IStorefrontTenantResolver, StorefrontTenantResolver>();
services.AddSingleton<IUserIdProvider, ApplicationUserIdProvider>(); // SignalR
```

`IAmazonTokenManager` is explicitly Singleton because it holds a `ConcurrentDictionary` token cache (multi-tenant per CLAUDE.md requirements). Auto-scan would default to Scoped and break this.

### 7. Background Services (must remain manual)

29 `AddHostedService<>` calls. There is no Scrutor convention for `IHostedService` — these must stay explicit.

### 8. HttpClient Factory (must remain manual)

Named client registrations with base addresses and timeouts are configuration, not DI wiring:
```csharp
services.AddHttpClient("TrendyolApi", x => { x.BaseAddress = ... });
services.AddHttpClient("HepsiburadaApi", x => { x.BaseAddress = ... });
services.AddHttpClient("Ollama", x => { x.BaseAddress = ...; x.Timeout = ...; });
```

### 9. DbContext Factory (must remain manual)

Custom `IDbContextFactory<IntegrationDbContext>` registration via factory lambda (tenant-aware connection string provider). Not scannable.

### 10. FluentValidation Validators (`ServiceDependencyExtension.AddValidators`)

Currently: 35 explicit `AddScoped<IValidator<TDto>, TValidator>()` registrations in a separate file.

Scrutor alternative: scan the Business assembly for all classes implementing `IValidator<>` and register as `IValidator<T>` automatically. This is a well-established pattern and **is a good auto-scan candidate**.

However, FluentValidation also ships `services.AddValidatorsFromAssembly(assembly)` (via the `FluentValidation.DependencyInjectionExtensions` package) which does the same thing more idiomatically. If that package is already available or easy to add, prefer it over Scrutor for validators.

---

## Migration Strategy

### Phase 1 — Add Scrutor

Add to `Entegrasyon.ApplicationBootstrap.csproj`:

```xml
<PackageReference Include="Scrutor" Version="7.0.0" />
```

### Phase 2 — Auto-scan standard business services

Inside `AddApplicationDependencies()`, **before** all existing manual registrations, add:

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<NotificationManager>()  // Entegrasyon.Business assembly
    .AddClasses(classes => classes
        .InNamespaces(
            "Entegrasyon.Business.Concrete",
            "Entegrasyon.Business.Concrete.Storefront",
            "Entegrasyon.Business.Concrete.Auth",
            "Entegrasyon.Business.Notifications"
        )
        .Where(t =>
            !t.Name.StartsWith("Mock") &&       // exclude mocks — handled by UseMock blocks
            !t.Name.EndsWith("CategoryImporter") && // concrete-only, keep manual
            !t.Name.EndsWith("MappingValidator") && // concrete-only, keep manual
            !t.Name.EndsWith("Cache") &&            // caches may need specific lifetime
            !t.Name.EndsWith("InvoiceBuilder") &&   // concrete-only
            !typeof(IHostedService).IsAssignableFrom(t) // never auto-register hosted services
        )
    )
    .AsMatchingInterface()   // IXxx → Xxx where names match after stripping I
    .WithScopedLifetime()
);
```

`AsMatchingInterface()` in Scrutor strips the `I` prefix and matches by convention. This will correctly handle ~100+ standard registrations.

### Phase 3 — Keep explicit registrations for exceptions

After the scan block, keep explicit registrations for:

1. **Naming mismatch cases** (all 12 from the table above)
2. **UseMock conditional blocks** (all 13 blocks, unchanged)
3. **Multi-registration interfaces** (`ICargoTrackingAdapter`, `INotificationSender`)
4. **Concrete-only registrations** (unchanged)
5. **All singletons** (unchanged)
6. **All `AddHostedService<>`** (unchanged)
7. **HttpClient/DbContext/SignalR** (unchanged)

Because .NET DI is **last-wins for single registrations**, the explicit registrations that follow the scan block will correctly override any auto-scanned registration for the same interface. This is the safe override strategy.

### Phase 4 — Auto-scan validators (optional, separate PR)

Replace `ServiceDependencyExtension.AddValidators()` with:

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<NotificationManager>()
    .AddClasses(classes => classes
        .AssignableTo(typeof(AbstractValidator<>))
    )
    .AsImplementedInterfaces()
    .WithScopedLifetime()
);
services.AddScoped<IFluentValidator, FluentValidator>(); // keep manual (no generic T)
```

Alternatively, use `services.AddValidatorsFromAssemblyContaining<NotificationManager>()` if `FluentValidation.DependencyInjectionExtensions` is added.

---

## What the Resulting File Looks Like

The auto-scan block replaces ~100 manual `AddScoped<>` lines. The remaining explicit section retains:
- ~116 lines for mock/real conditional blocks (unchanged, essential)
- ~20 lines for naming-exception explicit registrations
- ~15 lines for multi-registration and concrete-only
- ~11 lines for singletons
- ~29 lines for hosted services
- ~10 lines for HttpClient/DbContext/SignalR/storage

Estimated line reduction: **~100–110 lines** removed from `AddApplicationDependencies()`, with the scan block adding ~15 lines. Net: ~90 lines saved, file shrinks from 538 to ~450 lines. The primary benefit is not line count — it is that new services in the standard naming convention are registered automatically without touching this file.

---

## Risk Assessment

### Risk 1 — Wrong lifetime (Scoped vs Singleton)

**Scenario:** A class that should be Singleton gets auto-scanned as Scoped.

**Affected classes:** `AmazonTokenManager`, `ZplLabelGenerator`, `EscPosReceiptGenerator`, `MinioFileStorage`, `ImageProcessingService`, `TenantRegistryService` etc.

**Mitigation:** The scan filter uses `InNamespaces()` targeting only `Concrete` sub-namespaces. Singleton classes live in other namespaces (`Business.Labels`, `ApplicationBootstrap.*`, `Business.FileStorage`) or are explicitly excluded. Additionally, singletons are registered **after** the scan, so the explicit `AddSingleton<>` would override any accidental Scoped scan — but this is not reliable if no scan hit occurs. Better: ensure singleton classes are in excluded namespaces or add them to the `Where()` exclusion list.

**Severity:** High if triggered; mitigation makes it Low.

### Risk 2 — Double registration of conditional (mock/real) services

**Scenario:** Scrutor scans `TrendyolProductService` and registers it as `ITrendyolProductService`, then the mock block also registers `MockTrendyolProductService` — but in real mode, the explicit registration follows the scan and overrides correctly.

**Mitigation:** The scan filter excludes `Mock*` classes by name. Real implementations are still scanned. The explicit mock-block registrations then override with the mock class. In real mode, the scanned registration and the explicit real registration agree, so last-wins produces the correct result.

**However**, this means real implementations are registered **twice** (once by scan, once explicitly). This wastes a tiny amount of memory but does not cause runtime errors for Scoped services.

**Alternative to avoid double registration:** Move all mock-gated interfaces to the `Where()` exclusion list. Then the scan block only covers services that are never conditional. This is cleaner but requires maintaining the exclusion list.

**Recommended approach:** Exclude all mock-gated interfaces from the scan. See the exclusion list strategy in Phase 2 above.

### Risk 3 — Missing registration for new interface

**Scenario:** Developer adds `IFooManager` in `Abstract/` and `FooManager` in `Concrete/` but forgets the scan will only work if the naming convention is followed exactly.

**Mitigation:** Document the convention clearly. Add a startup assertion or integration test that verifies all `IXxxManager`/`IXxxService` interfaces in `Abstract/` have a registered implementation (see verification below).

**Severity:** Low — failure mode is identical to the current manual approach (runtime error on first use), but now it surfaces as "the auto-scan didn't find it" rather than "you forgot to add a line."

### Risk 4 — Test project DI setups

**Scenario:** Integration tests and unit tests that build `WebApplicationFactory` or construct the DI container manually may pick up the Scrutor scan and register classes they did not intend to register in the test context.

**Mitigation:** `WebApplicationFactory` calls the real `AddApplicationDependencies()`. This is already the case today — no change. Unit tests that mock interfaces explicitly are not affected (they don't call `AddApplicationDependencies()`).

**Severity:** None.

### Risk 5 — Scrutor `AsMatchingInterface()` mismatches

**Scenario:** `AsMatchingInterface()` convention fails for a class because the interface is in a different assembly or the naming does not follow the expected pattern.

**Affected:** Any class in `Concrete/` that implements an interface from `Abstract/` where the name after stripping `I` does not match the class name. See the naming exception table above — these are already known and will be registered manually.

**Mitigation:** Run `dotnet build` and the integration test suite after migration. Any missed registration will fail at startup.

**Severity:** Low — easily caught.

---

## Verification Plan

1. **Build check:** `dotnet build Entegrasyon.sln` — no compile errors.

2. **Startup smoke test:** Start the Blazor app in development mode. If any interface resolution fails, ASP.NET Core fails fast at startup with a clear `InvalidOperationException: Unable to resolve service for type 'IXxx'`.

3. **Integration tests:** `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj` — these use `WebApplicationFactory` and will exercise the real DI container with real registrations.

4. **Registration assertion test (new, optional):** Add a unit test that builds the service provider using `AddApplicationDependencies()` with a test configuration (all `UseMock=true`) and verifies that `GetRequiredService<IXxx>()` resolves without error for every interface in `Abstract/`. This would catch regressions from future additions.

---

## Implementation Checklist

- [ ] Add `<PackageReference Include="Scrutor" Version="7.0.0" />` to `Entegrasyon.ApplicationBootstrap.csproj`
- [ ] Add `Scan()` block at the top of `AddApplicationDependencies()` (before manual registrations)
- [ ] Build and verify no compile errors
- [ ] Start app in dev mode, verify startup
- [ ] Run unit tests: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
- [ ] Run integration tests: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
- [ ] Remove redundant manual `AddScoped<>` lines that are now covered by the scan
- [ ] Repeat verification after removals
- [ ] (Optional, Phase 4) Replace `AddValidators()` with `Scan()` or `AddValidatorsFromAssemblyContaining<>()`
- [ ] Update `CLAUDE.md` to document the auto-scan convention

---

## Files Affected

| File | Change |
|---|---|
| `Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj` | Add Scrutor NuGet |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Add scan block, remove ~100 manual lines |
| `Application/Entegrasyon.Business/Validation/FluentValidation/ServiceDependencyExtension.cs` | Optional: replace with scan |
| `CLAUDE.md` | Document auto-scan convention |
