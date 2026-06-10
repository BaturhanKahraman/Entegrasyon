# Multi-Tenant Faz 1: Tenant Core Infrastructure

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Single-tenant Entegrasyon platformunu database-per-tenant multi-tenant mimariye tasiyan core altyapiyi kurmak.

**Architecture:** Subdomain-based tenant resolution (middleware) + database-per-tenant isolation (TenantDbContextFactory) + AdminPanel uzerinden otomatik DB provisioning. Mevcut 30+ manager sifir degisiklikle calisir cunku hepsi IDbContextFactory kullaniyor.

**Tech Stack:** .NET 8, EF Core (PostgreSQL + Npgsql), Blazor Server, IMemoryCache, SQLite (AdminPanel), xUnit + Moq + FluentAssertions

---

## File Structure

### New Files
| File | Responsibility |
|---|---|
| `Business/Tenants/TenantRegistryEntry.cs` | Immutable read model — tenant cache verisi |
| `Business/Tenants/ITenantRegistry.cs` | Interface — tenant lookup + cache |
| `Business/Tenants/TenantRegistryService.cs` | Impl — AdminPanel SQLite'tan okur, IMemoryCache ile cache'ler |
| `Business/Concrete/HttpTenantContext.cs` | Scoped ITenantContext — middleware tarafindan initialize edilir |
| `DataAccess/TenantDbContextFactory.cs` | Scoped IDbContextFactory — ITenantContext.ConnectionString'den DbContext olusturur |
| `Blazor/Middleware/BlazorTenantResolutionMiddleware.cs` | HTTP middleware — subdomain'den tenant cozumler |
| `Blazor/Utility/TenantCircuitHandler.cs` | CircuitHandler — Blazor circuit reconnect'te tenant context'i yeniden initialize eder |
| `AdminPanel/Infrastructure/TenantProvisioningService.cs` | DB provisioning — CREATE DATABASE + migration + seed |
| `Test/Entegrasyon.Test/Tenants/TenantRegistryServiceTests.cs` | Unit test — registry service |
| `Test/Entegrasyon.Test/Tenants/HttpTenantContextTests.cs` | Unit test — tenant context |
| `Test/Entegrasyon.Test/Tenants/TenantDbContextFactoryTests.cs` | Unit test — factory |
| `Test/Entegrasyon.Test/Tenants/BlazorTenantResolutionMiddlewareTests.cs` | Unit test — middleware |

### Modified Files
| File | Change |
|---|---|
| `Business/Abstract/ITenantContext.cs` | `ConnectionString`, `IsInitialized`, `Initialize()` eklenir |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs:45` | `DefaultTenantContext` -> `HttpTenantContext` |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs:384-404` | `AddCustomDbContext` tenant-aware factory'e donusur |
| `Blazor/Program.cs:65,110-115` | TenantResolutionMiddleware + AdminPanel DB connection |
| `Blazor/Services/CustomAuthenticationStateProvider.cs:75,85-90` | TenantId claim + UserSession.TenantId |
| `AdminPanel/Features/Tenants/TenantsController.cs:92-120` | Create aksiyonuna provisioning eklenir |
| `AdminPanel/Infrastructure/Data/AdminPanelDbContext.cs` | Gerekli genis/letmeler |

---

### Task 1: TenantRegistryEntry Read Model

**Files:**
- Create: `Application/Entegrasyon.Business/Tenants/TenantRegistryEntry.cs`

- [ ] **Step 1: Create TenantRegistryEntry record**

```csharp
// Application/Entegrasyon.Business/Tenants/TenantRegistryEntry.cs
namespace Entegrasyon.Business.Tenants;

/// <summary>
/// AdminPanel DB'den okunan tenant bilgisinin inmemory temsili.
/// Singleton ITenantRegistry tarafindan cache'lenir.
/// </summary>
public record TenantRegistryEntry(
    int TenantId,
    string Subdomain,
    string CompanyName,
    string ConnectionString,
    bool IsActive,
    string? LicenseType);
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Tenants/TenantRegistryEntry.cs
git commit -m "feat(tenant): add TenantRegistryEntry read model record"
```

---

### Task 2: ITenantContext Interface Genisletme

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/ITenantContext.cs`
- Create: `Application/Entegrasyon.Business/Concrete/HttpTenantContext.cs`
- Test: `Test/Entegrasyon.Test/Tenants/HttpTenantContextTests.cs`

- [ ] **Step 1: Write failing tests for HttpTenantContext**

```csharp
// Test/Entegrasyon.Test/Tenants/HttpTenantContextTests.cs
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.UnitTest.Tenants;

public class HttpTenantContextTests
{
    [Fact]
    public void TenantId_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        var context = new HttpTenantContext();
        var act = () => context.TenantId;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void ConnectionString_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        var context = new HttpTenantContext();
        var act = () => context.ConnectionString;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void IsInitialized_WhenNotInitialized_ReturnsFalse()
    {
        var context = new HttpTenantContext();
        context.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Initialize_SetsTenantIdAndConnectionString()
    {
        var context = new HttpTenantContext();
        var entry = new TenantRegistryEntry(
            TenantId: 5,
            Subdomain: "acme",
            CompanyName: "Acme Corp",
            ConnectionString: "Host=localhost;Database=tenant_5",
            IsActive: true,
            LicenseType: "Standard");

        context.Initialize(entry);

        context.TenantId.Should().Be(5);
        context.ConnectionString.Should().Be("Host=localhost;Database=tenant_5");
        context.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Initialize_WithNull_ThrowsArgumentNullException()
    {
        var context = new HttpTenantContext();
        var act = () => context.Initialize(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMarketPlaceId_DelegatesToTenantDb()
    {
        // GetMarketPlaceId simdilik InvalidOperationException firlatir
        // cunku marketplace ID'ler tenant DB'sinden cozumlenecek
        var context = new HttpTenantContext();
        var entry = new TenantRegistryEntry(1, "test", "Test", "conn", true, null);
        context.Initialize(entry);

        var act = () => context.GetMarketPlaceId("Trendyol");
        act.Should().Throw<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~HttpTenantContextTests" -v minimal`
Expected: FAIL — HttpTenantContext does not exist yet

- [ ] **Step 3: Extend ITenantContext interface**

```csharp
// Application/Entegrasyon.Business/Abstract/ITenantContext.cs
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Mevcut tenant baglamini saglar.
/// HTTP request'ten subdomain uzerinden cozumlenir.
/// Background service'lerde manuel olarak initialize edilir.
/// </summary>
public interface ITenantContext
{
    /// <summary>Mevcut tenant ID.</summary>
    int TenantId { get; }

    /// <summary>Tenant'in veritabani connection string'i.</summary>
    string ConnectionString { get; }

    /// <summary>Tenant context basariyla initialize edildi mi?</summary>
    bool IsInitialized { get; }

    /// <summary>Tenant bilgisini set eder. Request basina bir kez cagirilir.</summary>
    void Initialize(TenantRegistryEntry entry);

    /// <summary>
    /// Belirtilen marketplace turu icin bu tenant'in marketplace ID'sini doner.
    /// </summary>
    int GetMarketPlaceId(string marketplaceName);
}
```

- [ ] **Step 4: Implement HttpTenantContext**

```csharp
// Application/Entegrasyon.Business/Concrete/HttpTenantContext.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Scoped tenant context — middleware veya background service tarafindan initialize edilir.
/// StorefrontTenantContext pattern'ini takip eder.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private TenantRegistryEntry? _entry;

    public int TenantId => _entry?.TenantId
        ?? throw new InvalidOperationException("Tenant context is not initialized.");

    public string ConnectionString => _entry?.ConnectionString
        ?? throw new InvalidOperationException("Tenant context is not initialized.");

    public bool IsInitialized => _entry is not null;

    public void Initialize(TenantRegistryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
    }

    public int GetMarketPlaceId(string marketplaceName)
    {
        // Multi-tenant'ta marketplace ID'ler tenant DB'sindeki MarketPlace tablosundan
        // cozumlenecek. Bu metod scoped IMarketPlaceManager uzerinden kullanilmali.
        throw new InvalidOperationException(
            "Multi-tenant modda GetMarketPlaceId() yerine scoped IMarketPlaceManager kullanin.");
    }
}
```

- [ ] **Step 5: Fix DefaultTenantContext compilation (ITenantContext degisti)**

`DefaultTenantContext`'e yeni member'lari ekle ama single-tenant davranisini koru. Bu sinif hala integration test'lerde kullanilabilir:

```csharp
// Application/Entegrasyon.Business/Concrete/DefaultTenantContext.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Single-tenant varsayilan implementasyon.
/// Sadece gelistirme/test ortaminda kullanilir.
/// Uretimde HttpTenantContext ile Değiştirilir.
/// </summary>
public sealed class DefaultTenantContext : ITenantContext
{
    public int TenantId => 1;

    public string ConnectionString =>
        throw new InvalidOperationException("DefaultTenantContext does not support ConnectionString. Use HttpTenantContext.");

    public bool IsInitialized => true;

    public void Initialize(TenantRegistryEntry entry)
    {
        // Single-tenant modda no-op
    }

    public int GetMarketPlaceId(string marketplaceName) => marketplaceName switch
    {
        "Trendyol" => TrendyolMarketPlaceId,
        "N11" => N11MarketPlaceId,
        "Hepsiburada" => HepsiburadaMarketPlaceId,
        "Pazarama" => PazaramaMarketPlaceId,
        "Amazon" => AmazonMarketPlaceId,
        _ => throw new ArgumentException($"Bilinmeyen marketplace: {marketplaceName}")
    };
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~HttpTenantContextTests" -v minimal`
Expected: 6 passed

- [ ] **Step 7: Run all existing tests to verify no regression**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All tests pass (DefaultTenantContext ITenantContext'i hala implement eder)

- [ ] **Step 8: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ITenantContext.cs \
      Application/Entegrasyon.Business/Concrete/HttpTenantContext.cs \
      Application/Entegrasyon.Business/Concrete/DefaultTenantContext.cs \
      Test/Entegrasyon.Test/Tenants/HttpTenantContextTests.cs
git commit -m "feat(tenant): extend ITenantContext + add HttpTenantContext implementation"
```

---

### Task 3: ITenantRegistry + TenantRegistryService

**Files:**
- Create: `Application/Entegrasyon.Business/Tenants/ITenantRegistry.cs`
- Create: `Application/Entegrasyon.Business/Tenants/TenantRegistryService.cs`
- Test: `Test/Entegrasyon.Test/Tenants/TenantRegistryServiceTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/Tenants/TenantRegistryServiceTests.cs
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.UnitTest.Tenants;

public class TenantRegistryServiceTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task GetBySubdomainAsync_WhenTenantExists_ReturnsTenant()
    {
        // TenantRegistryService AdminPanel DB'den okur.
        // Unit test'te mock ITenantRegistryDataSource kullanacagiz.
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme Corp", "Host=localhost;Database=tenant_1", true, "Standard"),
                new(2, "beta", "Beta Inc", "Host=localhost;Database=tenant_2", true, "Pro"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);

        var result = await service.GetBySubdomainAsync("acme");

        result.Should().NotBeNull();
        result!.TenantId.Should().Be(1);
        result.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetBySubdomainAsync_WhenNotFound_ReturnsNull()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>());

        var service = new TenantRegistryService(mockDataSource.Object, _cache);

        var result = await service.GetBySubdomainAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTenantExists_ReturnsTenant()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(3, "gamma", "Gamma LLC", "Host=localhost;Database=tenant_3", true, "Enterprise"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);

        var result = await service.GetByIdAsync(3);

        result.Should().NotBeNull();
        result!.Subdomain.Should().Be("gamma");
    }

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveTenants()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme", "conn1", true, "Standard"),
                new(2, "beta", "Beta", "conn2", false, "Standard"),
                new(3, "gamma", "Gamma", "conn3", true, "Pro"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);

        var result = await service.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Select(t => t.Subdomain).Should().Contain("acme", "gamma");
    }

    [Fact]
    public async Task GetBySubdomainAsync_UsesCacheOnSecondCall()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme", "conn1", true, "Standard"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);

        await service.GetBySubdomainAsync("acme");
        await service.GetBySubdomainAsync("acme");

        // DataSource sadece 1 kez cagirilmali (cache'den donmeli)
        mockDataSource.Verify(x => x.GetAllTenantsAsync(), Times.Once);
    }

    [Fact]
    public void InvalidateCache_ClearsCache()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme", "conn1", true, "Standard"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);

        service.InvalidateCache();

        // Cache temizlendikten sonra yeni cagri DataSource'a gitmeli
        _ = service.GetBySubdomainAsync("acme").Result;
        _ = service.GetBySubdomainAsync("acme").Result;
        mockDataSource.Verify(x => x.GetAllTenantsAsync(), Times.Once);

        service.InvalidateCache();
        _ = service.GetBySubdomainAsync("acme").Result;
        mockDataSource.Verify(x => x.GetAllTenantsAsync(), Times.Exactly(2));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TenantRegistryServiceTests" -v minimal`
Expected: FAIL — classes don't exist

- [ ] **Step 3: Create ITenantRegistry + ITenantRegistryDataSource interfaces**

```csharp
// Application/Entegrasyon.Business/Tenants/ITenantRegistry.cs
namespace Entegrasyon.Business.Tenants;

/// <summary>
/// Tenant kayit defteri — singleton, tum aktif tenant'lari cache'ler.
/// Background service'ler ve middleware tarafindan kullanilir.
/// </summary>
public interface ITenantRegistry
{
    Task<TenantRegistryEntry?> GetBySubdomainAsync(string subdomain);
    Task<TenantRegistryEntry?> GetByIdAsync(int tenantId);
    Task<IReadOnlyList<TenantRegistryEntry>> GetAllActiveAsync();
    void InvalidateCache();
}

/// <summary>
/// Tenant verisini AdminPanel DB'den okuyan data source.
/// ITenantRegistry bunu kullanarak cache doldurur.
/// Test'te mocklanir.
/// </summary>
public interface ITenantRegistryDataSource
{
    Task<IReadOnlyList<TenantRegistryEntry>> GetAllTenantsAsync();
}
```

- [ ] **Step 4: Implement TenantRegistryService**

```csharp
// Application/Entegrasyon.Business/Tenants/TenantRegistryService.cs
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.Business.Tenants;

public sealed class TenantRegistryService(
    ITenantRegistryDataSource dataSource,
    IMemoryCache cache) : ITenantRegistry
{
    private const string CacheKey = "tenant:registry:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<TenantRegistryEntry?> GetBySubdomainAsync(string subdomain)
    {
        var tenants = await GetCachedTenantsAsync();
        return tenants.FirstOrDefault(t =>
            string.Equals(t.Subdomain, subdomain, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TenantRegistryEntry?> GetByIdAsync(int tenantId)
    {
        var tenants = await GetCachedTenantsAsync();
        return tenants.FirstOrDefault(t => t.TenantId == tenantId);
    }

    public async Task<IReadOnlyList<TenantRegistryEntry>> GetAllActiveAsync()
    {
        var tenants = await GetCachedTenantsAsync();
        return tenants.Where(t => t.IsActive).ToList().AsReadOnly();
    }

    public void InvalidateCache()
    {
        cache.Remove(CacheKey);
    }

    private async Task<IReadOnlyList<TenantRegistryEntry>> GetCachedTenantsAsync()
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<TenantRegistryEntry>? cached) && cached is not null)
            return cached;

        var tenants = await dataSource.GetAllTenantsAsync();
        cache.Set(CacheKey, tenants, CacheDuration);
        return tenants;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TenantRegistryServiceTests" -v minimal`
Expected: 6 passed

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Tenants/ITenantRegistry.cs \
      Application/Entegrasyon.Business/Tenants/TenantRegistryService.cs \
      Test/Entegrasyon.Test/Tenants/TenantRegistryServiceTests.cs
git commit -m "feat(tenant): add ITenantRegistry + TenantRegistryService with cache"
```

---

### Task 4: TenantDbContextFactory

**Files:**
- Create: `Application/Entegrasyon.DataAccess/TenantDbContextFactory.cs`
- Test: `Test/Entegrasyon.Test/Tenants/TenantDbContextFactoryTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/Tenants/TenantDbContextFactoryTests.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Tenants;

public class TenantDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WhenTenantInitialized_CreatesContextWithTenantConnection()
    {
        var tenantContext = new HttpTenantContext();
        tenantContext.Initialize(new TenantRegistryEntry(
            1, "acme", "Acme", "Host=localhost;Database=tenant_test_1;Username=test;Password=test",
            true, "Standard"));

        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(tenantContext, loggerFactory);

        using var dbContext = factory.CreateDbContext();

        dbContext.Should().NotBeNull();
        dbContext.Should().BeOfType<IntegrationDbContext>();
        dbContext.Database.GetConnectionString()
            .Should().Contain("tenant_test_1");
    }

    [Fact]
    public void CreateDbContext_WhenTenantNotInitialized_ThrowsInvalidOperationException()
    {
        var tenantContext = new HttpTenantContext();
        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(tenantContext, loggerFactory);

        var act = () => factory.CreateDbContext();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TenantDbContextFactoryTests" -v minimal`
Expected: FAIL — TenantDbContextFactory does not exist

- [ ] **Step 3: Implement TenantDbContextFactory**

`Func<string>` pattern kullaniyoruz — dongusel referansi (DataAccess -> Business) onler:

```csharp
// Application/Entegrasyon.DataAccess/TenantDbContextFactory.cs
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.DataAccess;

/// <summary>
/// Scoped IDbContextFactory — connection string provider'dan
/// tenant-specific DbContext olusturur.
/// Func{string} kullanarak DataAccess -> Business dongusel referansini onler.
/// DI'da bridge: () => tenantContext.ConnectionString
/// </summary>
public sealed class TenantDbContextFactory(
    Func<string> connectionStringProvider,
    ILoggerFactory loggerFactory) : IDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext()
    {
        var connectionString = connectionStringProvider();

        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.UseLoggerFactory(loggerFactory);

        return new IntegrationDbContext(optionsBuilder.Options);
    }
}
```

```csharp
// Test/Entegrasyon.Test/Tenants/TenantDbContextFactoryTests.cs
using Entegrasyon.DataAccess;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Tenants;

public class TenantDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WhenConnectionStringProvided_CreatesContextWithCorrectConnection()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(
            () => "Host=localhost;Database=tenant_test_1;Username=test;Password=test",
            loggerFactory);

        using var dbContext = factory.CreateDbContext();

        dbContext.Should().NotBeNull();
        dbContext.Should().BeOfType<IntegrationDbContext>();
        dbContext.Database.GetConnectionString().Should().Contain("tenant_test_1");
    }

    [Fact]
    public void CreateDbContext_WhenProviderThrows_PropagatesException()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var factory = new TenantDbContextFactory(
            () => throw new InvalidOperationException("Tenant context is not initialized."),
            loggerFactory);

        var act = () => factory.CreateDbContext();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TenantDbContextFactoryTests" -v minimal`
Expected: 2 passed

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.DataAccess/TenantDbContextFactory.cs \
      Test/Entegrasyon.Test/Tenants/TenantDbContextFactoryTests.cs
git commit -m "feat(tenant): add TenantDbContextFactory with Func<string> connection provider"
```

---

### Task 5: BlazorTenantResolutionMiddleware

**Files:**
- Create: `Application/Entegrasyon.Blazor/Middleware/BlazorTenantResolutionMiddleware.cs`
- Test: `Test/Entegrasyon.Test/Tenants/BlazorTenantResolutionMiddlewareTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/Tenants/BlazorTenantResolutionMiddlewareTests.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Http;

namespace Entegrasyon.UnitTest.Tenants;

public class BlazorTenantResolutionMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext(string host, string path = "/")
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_InitializesTenantContext()
    {
        var tenantEntry = new TenantRegistryEntry(
            5, "acme", "Acme Corp", "Host=localhost;Database=tenant_5", true, "Standard");

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync("acme"))
            .ReturnsAsync(tenantEntry);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("acme.app.entegrasyon.com");

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        tenantContext.IsInitialized.Should().BeTrue();
        tenantContext.TenantId.Should().Be(5);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownSubdomain_Returns404()
    {
        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync(It.IsAny<string>()))
            .ReturnsAsync((TenantRegistryEntry?)null);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("unknown.app.entegrasyon.com");

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        httpContext.Response.StatusCode.Should().Be(404);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithInactiveTenant_Returns404()
    {
        var tenantEntry = new TenantRegistryEntry(
            5, "acme", "Acme Corp", "conn", false, "Standard");

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync("acme"))
            .ReturnsAsync(tenantEntry);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("acme.app.entegrasyon.com");

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        httpContext.Response.StatusCode.Should().Be(404);
        nextCalled.Should().BeFalse();
    }

    [Theory]
    [InlineData("/_blazor")]
    [InlineData("/_framework/blazor.server.js")]
    [InlineData("/css/site.css")]
    [InlineData("/js/app.js")]
    [InlineData("/favicon.ico")]
    public async Task InvokeAsync_WithStaticPath_SkipsResolution(string path)
    {
        var mockRegistry = new Mock<ITenantRegistry>();
        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("acme.app.entegrasyon.com", path);

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        nextCalled.Should().BeTrue();
        mockRegistry.Verify(x => x.GetBySubdomainAsync(It.IsAny<string>()), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~BlazorTenantResolutionMiddlewareTests" -v minimal`
Expected: FAIL — class does not exist

- [ ] **Step 3: Implement BlazorTenantResolutionMiddleware**

```csharp
// Application/Entegrasyon.Blazor/Middleware/BlazorTenantResolutionMiddleware.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Blazor.Middleware;

/// <summary>
/// Blazor Server icin subdomain-based tenant resolution middleware.
/// Storefront TenantResolutionMiddleware pattern'ini takip eder.
/// </summary>
public class BlazorTenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SkipPrefixes =
        ["/_blazor", "/_framework", "/css", "/js", "/images", "/lib", "/favicon.ico"];

    public async Task InvokeAsync(
        HttpContext context,
        ITenantRegistry tenantRegistry,
        ITenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? "";
        if (SkipPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);

        if (string.IsNullOrEmpty(subdomain))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Tenant bulunamadı.");
            return;
        }

        var tenant = await tenantRegistry.GetBySubdomainAsync(subdomain);

        if (tenant is null || !tenant.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Tenant bulunamadı veya pasif.");
            return;
        }

        tenantContext.Initialize(tenant);
        await next(context);
    }

    /// <summary>
    /// "acme.app.entegrasyon.com" -> "acme"
    /// "localhost" -> null (gelistirme ortami icin ozel islem gerekir)
    /// </summary>
    internal static string? ExtractSubdomain(string host)
    {
        // IP adresi veya localhost kontrolu
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            System.Net.IPAddress.TryParse(host, out _))
            return null;

        var parts = host.Split('.');
        // En az 3 parca olmali: subdomain.domain.tld
        if (parts.Length < 3)
            return null;

        return parts[0];
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~BlazorTenantResolutionMiddlewareTests" -v minimal`
Expected: 4 passed (4 test methods — Theory counts as 1)

- [ ] **Step 5: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Blazor/Middleware/BlazorTenantResolutionMiddleware.cs \
      Test/Entegrasyon.Test/Tenants/BlazorTenantResolutionMiddlewareTests.cs
git commit -m "feat(tenant): add BlazorTenantResolutionMiddleware with subdomain extraction"
```

---

### Task 6: UserSession'a TenantId Ekleme

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Services/CustomAuthenticationStateProvider.cs:75,85-90`

- [ ] **Step 1: Add TenantId to UserSession and claims**

`UserSession` sinifina `TenantId` property'si ekle ve `BuildClaimsPrincipal`'da claim olarak yaz:

```csharp
// Application/Entegrasyon.Blazor/Services/CustomAuthenticationStateProvider.cs
// UserSession sinifina ekle (line ~85):
public class UserSession
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }
    public int TenantId { get; set; }  // YENi
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

// BuildClaimsPrincipal metodunda (line ~70):
private static ClaimsPrincipal BuildClaimsPrincipal(UserSession session)
{
    return new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, session.UserName),
        new Claim(ClaimTypes.Email, session.Email),
        new Claim(ClaimTypes.NameIdentifier, session.UserId),
        new Claim("Token", session.Token),
        new Claim("TenantId", session.TenantId.ToString())  // YENi
    }
    .Concat(session.Roles.Select(r => new Claim(ClaimTypes.Role, r)))
    .Concat(session.Permissions.Select(p => new Claim("Permission", p))),
    "CustomAuth"));
}
```

- [ ] **Step 2: Build to verify no compilation errors**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj`
Expected: Build succeeded

- [ ] **Step 3: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All pass

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Services/CustomAuthenticationStateProvider.cs
git commit -m "feat(tenant): add TenantId to UserSession and auth claims"
```

---

### Task 7: TenantCircuitHandler (Blazor Circuit Reconnect)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Utility/TenantCircuitHandler.cs`

- [ ] **Step 1: Implement TenantCircuitHandler**

Blazor Server'da circuit reconnect oldugunda middleware tekrar calismaz. Bu handler, claims'teki TenantId'yi okuyarak ITenantContext'i yeniden initialize eder.

```csharp
// Application/Entegrasyon.Blazor/Utility/TenantCircuitHandler.cs
using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Entegrasyon.Blazor.Utility;

/// <summary>
/// Blazor circuit reconnect'te ITenantContext'i yeniden initialize eder.
/// Claims'teki TenantId kullanilarak ITenantRegistry'den tenant bilgisi cekilir.
/// </summary>
public sealed class TenantCircuitHandler(
    ITenantContext tenantContext,
    ITenantRegistry tenantRegistry,
    AuthenticationStateProvider authStateProvider) : CircuitHandler
{
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        if (tenantContext.IsInitialized)
            return;

        try
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            var tenantIdClaim = authState.User.FindFirst("TenantId")?.Value;

            if (tenantIdClaim is not null && int.TryParse(tenantIdClaim, out var tenantId))
            {
                var tenant = await tenantRegistry.GetByIdAsync(tenantId);
                if (tenant is not null && tenant.IsActive)
                {
                    tenantContext.Initialize(tenant);
                }
            }
        }
        catch
        {
            // Circuit handler'da hata yutulmali — tenant context bos kalirsa
            // sayfa zaten login'e yonlendirilecek
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Utility/TenantCircuitHandler.cs
git commit -m "feat(tenant): add TenantCircuitHandler for circuit reconnect"
```

---

### Task 8: DI Registration + Pipeline Wiring

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- Modify: `Application/Entegrasyon.Blazor/Program.cs`

- [ ] **Step 1: Update ApplicationDependencyExtension — ITenantContext**

`ApplicationDependencyExtension.cs` line 45'teki kaydi guncelle:

```csharp
// Line 45: DefaultTenantContext -> HttpTenantContext
services.AddScoped<ITenantContext, HttpTenantContext>();
```

Using ekle:
```csharp
using Entegrasyon.Business.Tenants;
```

- [ ] **Step 2: Update AddCustomDbContext — TenantDbContextFactory**

`AddCustomDbContext` metodunu (line 384-404) guncelle:

```csharp
public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
{
    // Fallback connection string — development/single-tenant modu icin
    var fallbackConnectionString = configuration.GetConnectionString("Main")
        ?? configuration.GetConnectionString("DefaultConnection");

    services.AddScoped<IDbContextFactory<IntegrationDbContext>>(sp =>
    {
        var tenantContext = sp.GetRequiredService<ITenantContext>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        Func<string> connectionStringProvider = tenantContext.IsInitialized
            ? () => tenantContext.ConnectionString
            : () => fallbackConnectionString
                ?? throw new InvalidOperationException(
                    "Tenant context is not initialized and no fallback ConnectionString configured.");

        return new TenantDbContextFactory(connectionStringProvider, loggerFactory);
    });

    return services;
}
```

Using ekle:
```csharp
using Entegrasyon.DataAccess;
```

- [ ] **Step 3: Register ITenantRegistry as Singleton**

`AddApplicationDependencies` metoduna ekle (line 45 civarinda):

```csharp
services.AddScoped<ITenantContext, HttpTenantContext>();
services.AddSingleton<ITenantRegistry, TenantRegistryService>();
```

- [ ] **Step 4: Update Blazor Program.cs — Middleware + CircuitHandler**

`Program.cs`'e middleware ve circuit handler ekle:

```csharp
// Line 64 civarinda (AddCustomDbContext'ten sonra):
builder.Services.AddScoped<CircuitHandler, TenantCircuitHandler>();

// Pipeline'da (line 113, UseRouting()'den ONCE):
app.UseMiddleware<BlazorTenantResolutionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

Using ekle:
```csharp
using Entegrasyon.Blazor.Middleware;
using Entegrasyon.Blazor.Utility;
using Microsoft.AspNetCore.Components.Server.Circuits;
```

- [ ] **Step 5: Build entire solution**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 6: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
      Application/Entegrasyon.Blazor/Program.cs
git commit -m "feat(tenant): wire DI registrations and middleware pipeline for multi-tenant"
```

---

### Task 9: TenantProvisioningService (AdminPanel)

**Files:**
- Create: `Application/Entegrasyon.AdminPanel/Infrastructure/TenantProvisioningService.cs`
- Modify: `Application/Entegrasyon.AdminPanel/Features/Tenants/TenantsController.cs`

- [ ] **Step 1: Add DataAccess project reference to AdminPanel**

AdminPanel.csproj'a referans ekle:
```xml
<ProjectReference Include="..\..\Application\Entegrasyon.DataAccess\Entegrasyon.DataAccess.csproj" />
```

- [ ] **Step 2: Implement TenantProvisioningService**

```csharp
// Application/Entegrasyon.AdminPanel/Infrastructure/TenantProvisioningService.cs
using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Entegrasyon.AdminPanel.Infrastructure;

public record ProvisionResult(bool Success, string? ErrorMessage = null);

public class TenantProvisioningService(ILogger<TenantProvisioningService> logger)
{
    /// <summary>
    /// Yeni tenant icin PostgreSQL veritabani olusturur, migration uygular ve seed data yukler.
    /// </summary>
    public async Task<ProvisionResult> ProvisionAsync(string connectionString, CancellationToken ct = default)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database
            ?? throw new ArgumentException("Connection string must contain a Database name.");

        // 1. postgres DB'ye baglan ve yeni DB olustur
        var adminConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        }.ConnectionString;

        try
        {
            await using var adminConnection = new NpgsqlConnection(adminConnectionString);
            await adminConnection.OpenAsync(ct);

            // Veritabaninin zaten var olup olmadigini kontrol et
            await using var checkCmd = adminConnection.CreateCommand();
            checkCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
            var exists = await checkCmd.ExecuteScalarAsync(ct);

            if (exists is null)
            {
                await using var createCmd = adminConnection.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await createCmd.ExecuteNonQueryAsync(ct);
                logger.LogInformation("Database {DatabaseName} created.", databaseName);
            }
            else
            {
                logger.LogInformation("Database {DatabaseName} already exists.", databaseName);
            }

            // 2. Migration uygula
            var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();
            optionsBuilder.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            await using var dbContext = new IntegrationDbContext(optionsBuilder.Options);
            await dbContext.Database.MigrateAsync(ct);
            logger.LogInformation("Migrations applied to {DatabaseName}.", databaseName);

            return new ProvisionResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision database {DatabaseName}.", databaseName);

            // Rollback: DB olusturulduysa sil
            try
            {
                await using var adminConnection = new NpgsqlConnection(adminConnectionString);
                await adminConnection.OpenAsync(ct);
                await using var dropCmd = adminConnection.CreateCommand();
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
                await dropCmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception rollbackEx)
            {
                logger.LogError(rollbackEx, "Failed to rollback database {DatabaseName}.", databaseName);
            }

            return new ProvisionResult(false, ex.Message);
        }
    }
}
```

- [ ] **Step 3: Register TenantProvisioningService in AdminPanel DI**

AdminPanel `Program.cs`'e ekle:
```csharp
builder.Services.AddScoped<TenantProvisioningService>();
```

- [ ] **Step 4: Update TenantsController.Create to call provisioning**

`TenantsController.cs` Create POST aksiyonunu guncelle — tenant kaydedildikten sonra provisioning calistir:

```csharp
// TenantsController constructor'a TenantProvisioningService ekle:
public class TenantsController(
    AdminPanelDbContext dbContext,
    TenantProvisioningService provisioningService) : Controller

// Create POST'ta, tenant kaydedildikten sonra:
var provisionResult = await provisioningService.ProvisionAsync(tenant.ConnectionString, cancellationToken);
if (!provisionResult.Success)
{
    ModelState.AddModelError("", $"Veritabani olusturulamadi: {provisionResult.ErrorMessage}");
    // Tenant kaydini da sil (geri al)
    dbContext.Tenants.Remove(tenant);
    await dbContext.SaveChangesAsync();
    return View($"{ViewBase}/Create.cshtml", model);
}
```

- [ ] **Step 5: Build AdminPanel**

Run: `dotnet build Application/Entegrasyon.AdminPanel/Entegrasyon.AdminPanel.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.AdminPanel/Infrastructure/TenantProvisioningService.cs \
      Application/Entegrasyon.AdminPanel/Features/Tenants/TenantsController.cs \
      Application/Entegrasyon.AdminPanel/Entegrasyon.AdminPanel.csproj \
      Application/Entegrasyon.AdminPanel/Program.cs
git commit -m "feat(tenant): add TenantProvisioningService with auto DB creation and migration"
```

---

### Task 10: ITenantRegistryDataSource — AdminPanel SQLite Implementation

**Files:**
- Create: `Application/Entegrasyon.ApplicationBootstrap/Tenants/AdminPanelTenantDataSource.cs`

- [ ] **Step 1: Implement SQLite data source**

Blazor uygulamasinin AdminPanel SQLite DB'den tenant listesini okumasini saglayan data source:

```csharp
// Application/Entegrasyon.ApplicationBootstrap/Tenants/AdminPanelTenantDataSource.cs
using Entegrasyon.Business.Tenants;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Entegrasyon.ApplicationBootstrap.Tenants;

/// <summary>
/// AdminPanel SQLite DB'den tenant listesini okur.
/// ITenantRegistryDataSource implementasyonu.
/// appsettings.json'dan ConnectionStrings:AdminPanel kullanir.
/// </summary>
public sealed class AdminPanelTenantDataSource(IConfiguration configuration) : ITenantRegistryDataSource
{
    public async Task<IReadOnlyList<TenantRegistryEntry>> GetAllTenantsAsync()
    {
        var connectionString = configuration.GetConnectionString("AdminPanel")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:AdminPanel is not configured in appsettings.json.");

        var tenants = new List<TenantRegistryEntry>();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.Id, t.Subdomain, t.CompanyName, t.ConnectionString, t.IsActive,
                   (SELECT l.Type FROM TenantLicenses l
                    WHERE l.TenantId = t.Id
                      AND datetime('now') BETWEEN datetime(l.StartDate) AND datetime(l.EndDate)
                    ORDER BY l.Id DESC LIMIT 1) as LicenseType
            FROM Tenants t
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var licenseTypeValue = reader.IsDBNull(5) ? null : reader.GetString(5);

            tenants.Add(new TenantRegistryEntry(
                TenantId: reader.GetInt32(0),
                Subdomain: reader.GetString(1),
                CompanyName: reader.GetString(2),
                ConnectionString: reader.GetString(3),
                IsActive: reader.GetBoolean(4),
                LicenseType: licenseTypeValue));
        }

        return tenants.AsReadOnly();
    }
}
```

- [ ] **Step 2: Register in DI**

`ApplicationDependencyExtension.cs`'e ekle:
```csharp
services.AddSingleton<ITenantRegistryDataSource, AdminPanelTenantDataSource>();
```

- [ ] **Step 3: Add AdminPanel connection string to Blazor appsettings**

`Application/Entegrasyon.Blazor/appsettings.Development.json`'a ekle:
```json
"ConnectionStrings": {
    "Main": "...",
    "Redis": "...",
    "AdminPanel": "Data Source=/path/to/adminpanel.db;Mode=ReadOnly"
}
```

- [ ] **Step 4: Add Microsoft.Data.Sqlite NuGet package to ApplicationBootstrap**

Run: `dotnet add Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj package Microsoft.Data.Sqlite`

- [ ] **Step 5: Build solution**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/Tenants/AdminPanelTenantDataSource.cs \
      Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
      Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj \
      Application/Entegrasyon.Blazor/appsettings.Development.json
git commit -m "feat(tenant): add AdminPanelTenantDataSource for SQLite tenant registry"
```

---

### Task 11: End-to-End Verification

- [ ] **Step 1: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All pass including new tenant tests

- [ ] **Step 2: Run integration tests (Docker must be running)**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj -v minimal`
Expected: All pass (integration tests use their own DB, unaffected by tenant changes)

- [ ] **Step 3: Build full solution**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 errors, 0 warnings

- [ ] **Step 4: Manual smoke test**

1. AdminPanel'den yeni tenant olustur (subdomain: "test1", connection string ile)
2. `test1.app.localhost:7001` adresine git
3. Tenant'in kendi DB'sindeki verileri gordugunuzu dogrulayin

- [ ] **Step 5: Final commit (eger eksik dosya varsa)**

```bash
git status
git add -A  # Sadece tenant-related dosyalar eklenmeli
git commit -m "feat(tenant): Faz 1 complete — multi-tenant core infrastructure"
```
