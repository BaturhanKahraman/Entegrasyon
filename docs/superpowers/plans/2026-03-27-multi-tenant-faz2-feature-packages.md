# Multi-Tenant Faz 2: Permission-Based Feature Packages

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tenant bazinda ozellik paketleri (Starter/Pro/Enterprise) tanimlayip, permission gruplariyla esleyerek iki katmanli yetkilendirme (tenant paketi + kullanici yetkisi) saglamak.

**Architecture:** AdminPanel DB'de FeaturePackage + TenantSubscription entity'leri, Blazor'da IFeatureService + TenantFeatureAuthorizationHandler ile iki katmanli kontrol, FeatureGate component ile UI gating.

**Tech Stack:** .NET 8, EF Core (SQLite - AdminPanel), Blazor Server, IAuthorizationHandler, xUnit + Moq + FluentAssertions

---

## File Structure

### New Files
| File | Responsibility |
|---|---|
| `AdminPanel/Infrastructure/Data/FeaturePackageEntities.cs` | FeaturePackage, FeaturePackagePermission, TenantSubscription entity'leri |
| `Business/Tenants/IFeatureService.cs` | Interface — tenant feature kontrolu |
| `Business/Tenants/FeatureService.cs` | Impl — AdminPanel DB'den paket permission'lari okur, cache'ler |
| `ApplicationBootstrap/Security/TenantFeatureAuthorizationHandler.cs` | IAuthorizationHandler — iki katmanli yetki kontrolu |
| `Blazor/Components/Shared/FeatureGate.razor` | UI gating component |
| `Blazor/Components/Shared/FeatureGate.razor.cs` | Code-behind |
| `Test/Entegrasyon.Test/Tenants/FeatureServiceTests.cs` | Unit test |
| `Test/Entegrasyon.Test/Tenants/TenantFeatureAuthorizationHandlerTests.cs` | Unit test |

### Modified Files
| File | Change |
|---|---|
| `AdminPanel/Infrastructure/Data/AdminPanelDbContext.cs` | DbSet'ler + OnModelCreating |
| `AdminPanel/Program.cs` | Migration seed |
| `Blazor/Program.cs` | Authorization handler kaydi |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs` | IFeatureService DI |

---

### Task 1: FeaturePackage Entity'leri + Migration

**Files:**
- Create: `Application/Entegrasyon.AdminPanel/Infrastructure/Data/FeaturePackageEntities.cs`
- Modify: `Application/Entegrasyon.AdminPanel/Infrastructure/Data/AdminPanelDbContext.cs`

- [ ] **Step 1: Create entity classes**

```csharp
// Application/Entegrasyon.AdminPanel/Infrastructure/Data/FeaturePackageEntities.cs
namespace Entegrasyon.AdminPanel.Infrastructure.Data;

public class FeaturePackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FeaturePackagePermission> Permissions { get; set; } = [];
    public ICollection<TenantSubscription> Subscriptions { get; set; } = [];
}

public class FeaturePackagePermission
{
    public int Id { get; set; }
    public int FeaturePackageId { get; set; }
    public FeaturePackage Package { get; set; } = null!;
    public string PermissionKey { get; set; } = string.Empty;
}

public class TenantSubscription : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int FeaturePackageId { get; set; }
    public FeaturePackage Package { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 2: Add DbSets and relationships to AdminPanelDbContext**

```csharp
// DbSet'ler ekle
public DbSet<FeaturePackage> FeaturePackages => Set<FeaturePackage>();
public DbSet<FeaturePackagePermission> FeaturePackagePermissions => Set<FeaturePackagePermission>();
public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
```

OnModelCreating'e ekle:
```csharp
modelBuilder.Entity<FeaturePackage>(e =>
{
    e.HasIndex(p => p.Name).IsUnique();
    e.HasMany(p => p.Permissions).WithOne(pp => pp.Package).HasForeignKey(pp => pp.FeaturePackageId);
    e.HasMany(p => p.Subscriptions).WithOne(s => s.Package).HasForeignKey(s => s.FeaturePackageId);
});

modelBuilder.Entity<TenantSubscription>(e =>
{
    e.HasOne(s => s.Tenant).WithMany().HasForeignKey(s => s.TenantId);
});
```

- [ ] **Step 3: Create and apply migration**

Run:
```bash
cd Application/Entegrasyon.AdminPanel
dotnet ef migrations add AddFeaturePackages
dotnet ef database update
```

- [ ] **Step 4: Build**
Run: `dotnet build Application/Entegrasyon.AdminPanel/Entegrasyon.AdminPanel.csproj`

- [ ] **Step 5: Commit**

---

### Task 2: Seed Predefined Packages (Starter/Pro/Enterprise)

**Files:**
- Modify: `Application/Entegrasyon.AdminPanel/Program.cs` (or a seed method)

- [ ] **Step 1: Add seed logic after migration**

Create a static seed method or add to startup. The 44 permissions from AppPermissions.cs:

**Starter:** Products.*, Categories.*, Brands.*, Customers.*, BranchOffices.*, Sales.*, Orders.View, Settings.*, Users.*, Roles.*, Notifications.View, Logs.View (28 permissions)

**Pro:** Starter + Orders.Create/Edit/Delete, Cargo.*, Reports.*, Integrations.*, Marketplace.* (44 permissions — all)

**Enterprise:** Pro + future premium features (same as Pro for now, higher price)

```csharp
// Seed in Program.cs after db.Database.Migrate()
if (!db.FeaturePackages.Any())
{
    var starter = new FeaturePackage { Name = "Starter", Description = "Temel ozellikler", MonthlyPrice = 499, Permissions = starterPermissions };
    var pro = new FeaturePackage { Name = "Pro", Description = "Tum marketplace entegrasyonlari", MonthlyPrice = 999, Permissions = proPermissions };
    var enterprise = new FeaturePackage { Name = "Enterprise", Description = "Tam ozellik + storefront", MonthlyPrice = 1999, Permissions = enterprisePermissions };
    db.FeaturePackages.AddRange(starter, pro, enterprise);
    db.SaveChanges();
}
```

- [ ] **Step 2: Build and verify**
- [ ] **Step 3: Commit**

---

### Task 3: IFeatureService + FeatureService

**Files:**
- Create: `Application/Entegrasyon.Business/Tenants/IFeatureService.cs`
- Create: `Application/Entegrasyon.Business/Tenants/FeatureService.cs`
- Test: `Test/Entegrasyon.Test/Tenants/FeatureServiceTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
public class FeatureServiceTests
{
    [Fact]
    public async Task IsFeatureEnabledAsync_WhenTenantHasPackageWithPermission_ReturnsTrue()

    [Fact]
    public async Task IsFeatureEnabledAsync_WhenTenantHasNoSubscription_ReturnsFalse()

    [Fact]
    public async Task IsFeatureEnabledAsync_WhenPermissionNotInPackage_ReturnsFalse()

    [Fact]
    public async Task GetEnabledFeaturesAsync_ReturnsAllPermissionsFromActiveSubscription()
}
```

- [ ] **Step 2: Create IFeatureService interface**

```csharp
public interface IFeatureService
{
    Task<bool> IsFeatureEnabledAsync(string permissionKey);
    Task<IReadOnlySet<string>> GetEnabledFeaturesAsync();
}
```

- [ ] **Step 3: Implement FeatureService**

IFeatureDataSource pattern (same as ITenantRegistryDataSource) — AdminPanel SQLite'dan tenant subscription + package permissions okur, cache'ler.

- [ ] **Step 4: Run tests**
- [ ] **Step 5: Commit**

---

### Task 4: TenantFeatureAuthorizationHandler

**Files:**
- Create: `Application/Entegrasyon.ApplicationBootstrap/Security/TenantFeatureAuthorizationHandler.cs`
- Modify: `Application/Entegrasyon.Blazor/Program.cs`
- Test: `Test/Entegrasyon.Test/Tenants/TenantFeatureAuthorizationHandlerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Feature kapali + user yetkili = DENY
// Feature acik + user yetkisiz = DENY
// Feature acik + user yetkili = ALLOW
// Feature acik + Admin role = ALLOW
```

- [ ] **Step 2: Implement handler**

```csharp
public class TenantFeatureAuthorizationHandler : IAuthorizationHandler
{
    // Her PermissionRequirement icin:
    // 1. IFeatureService.IsFeatureEnabledAsync(permission) kontrol et
    // 2. User.IsInRole("Admin") || User.HasClaim("Permission", permission) kontrol et
}
```

- [ ] **Step 3: Update Program.cs authorization**

Mevcut inline RequireAssertion yerine handler-based approach.

- [ ] **Step 4: Run tests**
- [ ] **Step 5: Commit**

---

### Task 5: FeatureGate Blazor Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/Components/Shared/FeatureGate.razor`
- Create: `Application/Entegrasyon.Blazor/Components/Shared/FeatureGate.razor.cs`

- [ ] **Step 1: Create FeatureGate component**

```razor
@if (_isEnabled)
{
    @ChildContent
}
else if (FallbackContent is not null)
{
    @FallbackContent
}
```

Code-behind:
```csharp
public partial class FeatureGate : ComponentBase
{
    [Inject] private IFeatureService FeatureService { get; set; } = null!;
    [Parameter] public string Permission { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? FallbackContent { get; set; }
    private bool _isEnabled;

    protected override async Task OnInitializedAsync()
    {
        _isEnabled = await FeatureService.IsFeatureEnabledAsync(Permission);
    }
}
```

- [ ] **Step 2: Build and commit**

---

### Task 6: DI Wiring + End-to-End Verification

- [ ] **Step 1: Register IFeatureService in DI**
- [ ] **Step 2: Register TenantFeatureAuthorizationHandler**
- [ ] **Step 3: Build full solution**
- [ ] **Step 4: Run all tests**
- [ ] **Step 5: Commit**
