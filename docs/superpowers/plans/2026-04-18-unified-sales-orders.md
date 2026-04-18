# Birleşik Satış & Sipariş Sayfası — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** POS + Manuel satışları ile Marketplace + Storefront siparişlerini tek `/sales` sayfasında birleştirerek, satır-tıklama ile tipe özel detay sayfasına götüren yeni bir liste deneyimi kurmak.

**Architecture:** PostgreSQL view (`vw_unified_sales`) iki tabloyu (Sales + Orders) UNION ALL ile birleştirir; EF Core keyless entity (`UnifiedSaleView`) üzerinden tek sorguda filtre/pagination yapılır. Liste UI'si Tabler + HTMX, sekmeli kaynak filtresi + KPI kartları + iki-katlı satır tasarımı. Detay sayfaları tip-prefix'li route (`/sales/sale/{id}`, `/sales/order/{id}`) ile ayrışır ama ortak header partial'ı paylaşır.

**Tech Stack:** .NET 10 / C# 13, ASP.NET Core MVC, EF Core 10 (Npgsql), HTMX, Tabler UI, Mapperly, FluentValidation, xUnit + Moq (unit), Testcontainers PostgreSQL (integration), Playwright + NUnit (E2E).

**Spec:** `docs/superpowers/specs/2026-04-18-unified-sales-orders-design.md`

**DB-per-tenant notu:** Sale ve Order entity'lerinde `TenantId` kolonu YOK — sistem DB-per-tenant olarak çalışıyor, tenant izolasyonu database seviyesinde. Bu plan da view'a TenantId eklemiyor.

---

## File Structure

**Yeni dosyalar:**
- `Application/Entegrasyon.Entity/Sales/UnifiedSaleSource.cs`
- `Application/Entegrasyon.Entity/Sales/UnifiedSaleStatus.cs`
- `Application/Entegrasyon.Entity/Sales/Views/UnifiedSaleView.cs`
- `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleListItemDto.cs`
- `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleFilterDto.cs`
- `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleSummaryDto.cs`
- `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleSourceCountDto.cs`
- `Application/Entegrasyon.Business/Abstract/IUnifiedSaleManager.cs`
- `Application/Entegrasyon.Business/Concrete/UnifiedSaleManager.cs`
- `Application/Entegrasyon.Business/Mappers/UnifiedSaleStatusMapper.cs`
- `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/UnifiedSaleViewConfiguration.cs`
- Migration: `<timestamp>_CreateUnifiedSalesView.cs` (otomatik üretim)
- `Application/Entegrasyon.MVC/Features/Sales/ViewModels/UnifiedSaleIndexViewModel.cs`
- `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleTable.cshtml`
- `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSourceTabs.cshtml`
- `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedFilters.cshtml`
- `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedKpiCards.cshtml`
- `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleDetailHeader.cshtml`
- `Test/Entegrasyon.Test/Business/UnifiedSaleManagerTests.cs`
- `Test/Entegrasyon.Test/Business/UnifiedSaleStatusMapperTests.cs`
- `Test/Entegrasyon.IntegrationTest/Sales/UnifiedSalesViewTests.cs`
- `Test/Entegrasyon.E2E/Tests/UnifiedSalesPageTests.cs`

**Değiştirilen dosyalar:**
- `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs` — `DbSet<UnifiedSaleView>` ekle
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` — `IUnifiedSaleManager` DI kaydı
- `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs` — `Index` yeniden yazılır, `SaleDetail` `/sales/sale/{id}`'e taşınır, yeni `OrderDetail` action'ı eklenir
- `Application/Entegrasyon.MVC/Features/Sales/Views/Index.cshtml` — komple yeniden yazılır
- `Application/Entegrasyon.MVC/Features/Sales/Views/Detail.cshtml` → adı `SaleDetail.cshtml` olur, ortak header partial'ını include eder
- `Application/Entegrasyon.MVC/Features/Orders/Views/OrderDetail.cshtml` → `Features/Sales/Views/OrderDetail.cshtml`'e taşınır, ortak header partial'ını include eder
- `Application/Entegrasyon.MVC/Features/Orders/OrderController.cs` — sadece redirect'ler kalır (ya da silinir, route tablosu `SaleController`'da yeniden tanımlanır)
- `Application/Entegrasyon.MVC/Shared/Views/_Sidebar.cshtml` — "Satış Gecmisi" ve "Satışlar ve Siparişler" iki öğesi tek "Satışlar"a indirgenir; `marketplace-orders` nav öğesi kaldırılır (veya `/sales?source=...`'a yönlendirilir)

**Silinen dosyalar:**
- `Application/Entegrasyon.MVC/Features/Orders/Views/Index.cshtml`
- `Application/Entegrasyon.MVC/Features/Orders/Views/MarketplaceOrders.cshtml`
- `Application/Entegrasyon.MVC/Features/Orders/Views/Partials/_OrderTable.cshtml`
- `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleTable.cshtml` (yerine `_UnifiedSaleTable.cshtml`)
- `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleSummaryCards.cshtml` (yerine `_UnifiedKpiCards.cshtml`)

---

## Task 1: UnifiedSaleSource ve UnifiedSaleStatus Enum'ları

**Files:**
- Create: `Application/Entegrasyon.Entity/Sales/UnifiedSaleSource.cs`
- Create: `Application/Entegrasyon.Entity/Sales/UnifiedSaleStatus.cs`

- [ ] **Step 1: `UnifiedSaleSource.cs` yaz**

```csharp
namespace Entegrasyon.Entity.Sales;

/// <summary>
/// Birleşik satış kaynağı. Sale.SaleSource + Order.MarketPlaceId + Storefront'u tek enum'da toplar.
/// Marketplace değerleri (10 + MarketPlaceId) offset'i kullanır.
/// </summary>
public enum UnifiedSaleSource
{
    POS         = 1,
    Manual      = 2,
    Storefront  = 3,
    Trendyol    = 11,   // 10 + MarketPlaceId 1
    N11         = 12,   // 10 + MarketPlaceId 2
    Hepsiburada = 13,   // 10 + MarketPlaceId 3
    Amazon      = 14,   // 10 + MarketPlaceId 4
    Pazarama    = 15,   // 10 + MarketPlaceId 5
    PttAvm      = 17,   // 10 + MarketPlaceId 7
    Ciceksepeti = 18    // 10 + MarketPlaceId 8
}
```

- [ ] **Step 2: `UnifiedSaleStatus.cs` yaz**

```csharp
namespace Entegrasyon.Entity.Sales;

/// <summary>
/// Liste UI'sinde gösterilecek 6 normalize durum.
/// Sale.SaleStatus, Order.StorefrontOrderStatus ve MarketplaceOrderStatus
/// bu enum'a indirgenerek gösterilir.
/// </summary>
public enum UnifiedSaleStatus
{
    Completed    = 1,
    Pending      = 2,
    Shipping     = 3,
    PartialReturn = 4,
    FullReturn   = 5,
    Cancelled    = 6
}
```

- [ ] **Step 3: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/UnifiedSaleSource.cs Application/Entegrasyon.Entity/Sales/UnifiedSaleStatus.cs
git commit -m "feat(sales): UnifiedSaleSource ve UnifiedSaleStatus enum'larını ekle"
```

---

## Task 2: UnifiedSaleView Keyless Entity

**Files:**
- Create: `Application/Entegrasyon.Entity/Sales/Views/UnifiedSaleView.cs`

- [ ] **Step 1: Dizin yapısını hazırla**

Run: `mkdir -p Application/Entegrasyon.Entity/Sales/Views`
Expected: no output (exit 0).

- [ ] **Step 2: `UnifiedSaleView.cs` yaz**

```csharp
namespace Entegrasyon.Entity.Sales.Views;

/// <summary>
/// vw_unified_sales view'ına bağlı keyless entity.
/// Sale ve Order tablolarını birleşik okuma için kullanılır.
/// </summary>
public sealed class UnifiedSaleView
{
    public Guid Id { get; init; }

    /// <summary>0 = Sale, 1 = Order</summary>
    public int EntityType { get; init; }

    public UnifiedSaleSource Source { get; init; }

    public string? Number { get; init; }

    public DateTimeOffset SaleDate { get; init; }

    public int? CustomerId { get; init; }

    public string? CustomerDisplayName { get; init; }

    public decimal TotalPrice { get; init; }

    public int ItemCount { get; init; }

    /// <summary>Source'a göre yorumlanan raw durum kodu.</summary>
    public int RawStatusCode { get; init; }

    public string? CargoTrackingNumber { get; init; }

    public int? MarketPlaceId { get; init; }
}
```

- [ ] **Step 3: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/Views/UnifiedSaleView.cs
git commit -m "feat(sales): UnifiedSaleView keyless entity ekle"
```

---

## Task 3: DTO'lar (Filter, ListItem, Summary, SourceCount)

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleFilterDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleListItemDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleSummaryDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSaleSourceCountDto.cs`

- [ ] **Step 1: `UnifiedSaleFilterDto.cs` yaz**

```csharp
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleFilterDto(
    UnifiedSaleSource? Source,
    UnifiedSaleStatus? Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? SearchText,
    int? CustomerId,
    int PageIndex = 0,
    int PageSize = 25
);
```

- [ ] **Step 2: `UnifiedSaleListItemDto.cs` yaz**

```csharp
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleListItemDto
{
    public Guid Id { get; init; }
    public int EntityType { get; init; }
    public UnifiedSaleSource Source { get; init; }
    public string? Number { get; init; }
    public DateTimeOffset SaleDate { get; init; }
    public string? CustomerDisplayName { get; init; }
    public string? CustomerSubLine { get; init; }
    public decimal TotalPrice { get; init; }
    public int ItemCount { get; init; }
    public UnifiedSaleStatus Status { get; init; }
    public string? StatusSubLine { get; init; }
    public string DetailUrl { get; init; } = "";
}
```

- [ ] **Step 3: `UnifiedSaleSummaryDto.cs` yaz**

```csharp
namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleSummaryDto(
    decimal TotalRevenue,
    int SaleCount,
    decimal AverageBasket,
    double ReturnRate
);
```

- [ ] **Step 4: `UnifiedSaleSourceCountDto.cs` yaz**

```csharp
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleSourceCountDto(
    UnifiedSaleSource? Source,    // null = Tümü
    string Label,
    int Count
);
```

- [ ] **Step 5: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Sale/UnifiedSale*.cs
git commit -m "feat(sales): birleşik liste DTO'ları (Filter/ListItem/Summary/SourceCount)"
```

---

## Task 4: Status Mapper — Normalize Durum Mantığı

Bu, ham kaynak durumunu 6'lı `UnifiedSaleStatus`'a çeviren saf fonksiyondur. Önce test.

**Files:**
- Create: `Test/Entegrasyon.Test/Business/UnifiedSaleStatusMapperTests.cs`
- Create: `Application/Entegrasyon.Business/Mappers/UnifiedSaleStatusMapper.cs`

- [ ] **Step 1: Failing test yaz**

```csharp
// Test/Entegrasyon.Test/Business/UnifiedSaleStatusMapperTests.cs
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class UnifiedSaleStatusMapperTests
{
    [Theory]
    [InlineData(1, 0, UnifiedSaleStatus.Completed)]    // SaleStatus.Completed
    [InlineData(2, 0, UnifiedSaleStatus.PartialReturn)] // SaleStatus.PartialReturn
    [InlineData(3, 0, UnifiedSaleStatus.FullReturn)]    // SaleStatus.FullReturn
    [InlineData(4, 0, UnifiedSaleStatus.Cancelled)]     // SaleStatus.Cancelled
    public void Map_ReturnsNormalizedStatus_ForSale(int rawCode, int entityType, UnifiedSaleStatus expected)
    {
        var result = UnifiedSaleStatusMapper.Map(rawCode, entityType);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1, UnifiedSaleStatus.Pending)]   // null/unknown order status
    [InlineData(1, 1, UnifiedSaleStatus.Pending)]   // Storefront Pending
    [InlineData(2, 1, UnifiedSaleStatus.Pending)]   // Storefront Confirmed
    [InlineData(3, 1, UnifiedSaleStatus.Shipping)]  // Storefront Shipped
    [InlineData(4, 1, UnifiedSaleStatus.Completed)] // Storefront Delivered
    [InlineData(5, 1, UnifiedSaleStatus.Cancelled)] // Storefront Cancelled
    public void Map_ReturnsNormalizedStatus_ForOrder(int rawCode, int entityType, UnifiedSaleStatus expected)
    {
        var result = UnifiedSaleStatusMapper.Map(rawCode, entityType);
        result.Should().Be(expected);
    }
}
```

- [ ] **Step 2: Testi çalıştır — FAIL beklenir**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~UnifiedSaleStatusMapperTests"`
Expected: FAIL, `UnifiedSaleStatusMapper` bulunamadı.

- [ ] **Step 3: Mapper implementasyonu yaz**

```csharp
// Application/Entegrasyon.Business/Mappers/UnifiedSaleStatusMapper.cs
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Mappers;

/// <summary>
/// Sale/Order ham durum kodlarını 6'lı <see cref="UnifiedSaleStatus"/>'a indirger.
/// </summary>
public static class UnifiedSaleStatusMapper
{
    public static UnifiedSaleStatus Map(int rawCode, int entityType)
    {
        // entityType: 0 = Sale (SaleStatus), 1 = Order (Storefront/Marketplace)
        if (entityType == 0)
        {
            return (SaleStatus)rawCode switch
            {
                SaleStatus.Completed      => UnifiedSaleStatus.Completed,
                SaleStatus.PartialReturn  => UnifiedSaleStatus.PartialReturn,
                SaleStatus.FullReturn     => UnifiedSaleStatus.FullReturn,
                SaleStatus.Cancelled      => UnifiedSaleStatus.Cancelled,
                _                         => UnifiedSaleStatus.Completed
            };
        }

        // Order — Storefront.OrderStatus (1=Pending, 2=Confirmed, 3=Shipped, 4=Delivered, 5=Cancelled)
        return rawCode switch
        {
            1 or 2 => UnifiedSaleStatus.Pending,
            3      => UnifiedSaleStatus.Shipping,
            4      => UnifiedSaleStatus.Completed,
            5      => UnifiedSaleStatus.Cancelled,
            _      => UnifiedSaleStatus.Pending
        };
    }
}
```

- [ ] **Step 4: Testi çalıştır — PASS beklenir**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~UnifiedSaleStatusMapperTests"`
Expected: PASS (10 test).

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Mappers/UnifiedSaleStatusMapper.cs Test/Entegrasyon.Test/Business/UnifiedSaleStatusMapperTests.cs
git commit -m "feat(sales): UnifiedSaleStatusMapper + testleri"
```

---

## Task 5: UnifiedSaleView EF Configuration + DbSet

**Files:**
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/UnifiedSaleViewConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`

- [ ] **Step 1: Configuration dosyasını yaz**

```csharp
// Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/UnifiedSaleViewConfiguration.cs
using Entegrasyon.Entity.Sales.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class UnifiedSaleViewConfiguration : IEntityTypeConfiguration<UnifiedSaleView>
{
    public void Configure(EntityTypeBuilder<UnifiedSaleView> builder)
    {
        builder.HasNoKey();
        builder.ToView("vw_unified_sales");
    }
}
```

- [ ] **Step 2: `IntegrationDbContext`'e `DbSet` ekle**

`Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs` içindeki diğer `DbSet` tanımlarının arasına ekle:

```csharp
public DbSet<UnifiedSaleView> UnifiedSales => Set<UnifiedSaleView>();
```

Gerekli using'i dosyanın başına ekle:
```csharp
using Entegrasyon.Entity.Sales.Views;
```

- [ ] **Step 3: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/UnifiedSaleViewConfiguration.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs
git commit -m "feat(sales): UnifiedSaleView DbSet + EF mapping"
```

---

## Task 6: Migration — `vw_unified_sales` View'ı Oluştur

Bu migration, Sale+SaleItem aggregate + Order'dan UNION ile okuma yapan view'ı oluşturur. Sale'de `TotalPrice`/`ItemCount` kolonu olmadığı için subquery ile `SUM`/`COUNT` hesaplanır.

**Files:**
- Migration dosyası EF CLI ile otomatik üretilecek, içine raw SQL yazılacak.

- [ ] **Step 1: Boş migration üret**

Run:
```bash
dotnet ef migrations add CreateUnifiedSalesView \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: `Migrations/<timestamp>_CreateUnifiedSalesView.cs` dosyası oluşur, içi boş olur (snapshot değişmediği için `Up`/`Down` boş).

- [ ] **Step 2: Migration `Up` metoduna view oluşturma SQL'i yaz**

Oluşan dosyanın `Up` metodunu şöyle güncelle:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW vw_unified_sales AS
SELECT
    s.""Id""                                 AS ""Id"",
    0                                         AS ""EntityType"",
    CASE s.""SaleSource""
        WHEN 1 THEN 1
        WHEN 2 THEN 2
        ELSE 2
    END                                       AS ""Source"",
    s.""SaleNumber""                          AS ""Number"",
    s.""SaleDate""                            AS ""SaleDate"",
    s.""CustomerId""                          AS ""CustomerId"",
    CASE
        WHEN s.""CustomerId"" IS NULL THEN NULL
        ELSE COALESCE(c.""Name"", 'Müşteri')
    END                                       AS ""CustomerDisplayName"",
    COALESCE((SELECT SUM(si.""UnitPrice"" * si.""Quantity"")
              FROM ""SaleItems"" si
              WHERE si.""SaleId"" = s.""Id"" AND si.""IsDeleted"" = false), 0)
                                              AS ""TotalPrice"",
    COALESCE((SELECT SUM(si.""Quantity"")
              FROM ""SaleItems"" si
              WHERE si.""SaleId"" = s.""Id"" AND si.""IsDeleted"" = false), 0)
                                              AS ""ItemCount"",
    CAST(s.""SaleStatus"" AS int)             AS ""RawStatusCode"",
    NULL                                      AS ""CargoTrackingNumber"",
    NULL::int                                 AS ""MarketPlaceId""
FROM ""Sales"" s
LEFT JOIN ""Customers"" c ON c.""Id"" = s.""CustomerId"" AND c.""IsDeleted"" = false
WHERE s.""IsDeleted"" = false

UNION ALL

SELECT
    o.""Id""                                 AS ""Id"",
    1                                         AS ""EntityType"",
    CASE
        WHEN o.""MarketPlaceId"" IS NULL THEN 3
        ELSE 10 + o.""MarketPlaceId""
    END                                       AS ""Source"",
    o.""OrderNumber""                         AS ""Number"",
    COALESCE(o.""OrderDate"", o.""CreatedAt"")AS ""SaleDate"",
    o.""CustomerId""                          AS ""CustomerId"",
    COALESCE(
        TRIM(CONCAT(o.""CustomerFirstName"", ' ', o.""CustomerLastName"")),
        c.""Name""
    )                                         AS ""CustomerDisplayName"",
    COALESCE(o.""TotalPrice"", 0)             AS ""TotalPrice"",
    COALESCE(o.""TotalQuantity"", 0)          AS ""ItemCount"",
    COALESCE(CAST(o.""StorefrontOrderStatus"" AS int), 0)
                                              AS ""RawStatusCode"",
    o.""CargoTrackingNumber""                 AS ""CargoTrackingNumber"",
    o.""MarketPlaceId""                       AS ""MarketPlaceId""
FROM ""Orders"" o
LEFT JOIN ""Customers"" c ON c.""Id"" = o.""CustomerId"" AND c.""IsDeleted"" = false
WHERE o.""IsDeleted"" = false;
");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP VIEW IF EXISTS vw_unified_sales;");
}
```

**Not:** Gerçek `Customer` tablosunun ad alanı `Name` değil olabilir (ör. `FirstName`+`LastName` veya `FullName`). Migration uygulanmadan önce `Customer` entity'sini kontrol et ve SQL'deki `c."Name"` ifadesini ona göre ayarla.

- [ ] **Step 3: `Customer` entity kolonlarını doğrula**

Run: `grep -E "public string.*(Name|FullName|FirstName)" Application/Entegrasyon.Entity/Customers/*.cs`
Expected: Customer'da kullanılan isim alanı görülür. SQL'deki `c."Name"` ifadesini bu alana göre düzenle (ör: `COALESCE(c."FirstName" || ' ' || c."LastName", 'Müşteri')`).

- [ ] **Step 4: Migration'ı DB'ye uygula**

Run:
```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.MVC \
  --context IntegrationDbContext
```

Expected: `Applying migration '<timestamp>_CreateUnifiedSalesView'. Done.`

- [ ] **Step 5: View varlığını doğrula**

Run:
```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d IntegrationDb -c "SELECT COUNT(*) FROM vw_unified_sales;"
```
Expected: Tek sayıcı dönmeli (hata olmamalı).

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(sales): vw_unified_sales view migration'ı"
```

---

## Task 7: IUnifiedSaleManager Interface

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IUnifiedSaleManager.cs`

- [ ] **Step 1: Interface'i yaz**

```csharp
// Application/Entegrasyon.Business/Abstract/IUnifiedSaleManager.cs
using Entegrasyon.Core.Utilities.Results;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Utilities;

namespace Entegrasyon.Business.Abstract;

public interface IUnifiedSaleManager
{
    Task<IDataResult<Paginate<UnifiedSaleListItemDto>>> GetPageableAsync(UnifiedSaleFilterDto filter);
    Task<IDataResult<UnifiedSaleSummaryDto>> GetSummaryAsync(UnifiedSaleFilterDto filter);
    Task<IDataResult<List<UnifiedSaleSourceCountDto>>> GetSourceCountsAsync(UnifiedSaleFilterDto filter);
}
```

**Not:** `Paginate<T>` ve `IDataResult<T>` tiplerinin mevcut namespace'ini bu projede kullanılan `SaleManager`'dan teyit et (`Application/Entegrasyon.Business/Concrete/SaleManager.cs` return tiplerine bak, gerekirse using'leri güncelle).

- [ ] **Step 2: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IUnifiedSaleManager.cs
git commit -m "feat(sales): IUnifiedSaleManager interface"
```

---

## Task 8: UnifiedSaleManager — Filtre & Sayfalama Testi

Önce TDD ile sorgu davranışını sabitle.

**Files:**
- Create: `Test/Entegrasyon.IntegrationTest/Sales/UnifiedSalesViewTests.cs`

- [ ] **Step 1: Integration test yaz**

```csharp
// Test/Entegrasyon.IntegrationTest/Sales/UnifiedSalesViewTests.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Entegrasyon.IntegrationTest.Sales;

public class UnifiedSalesViewTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public UnifiedSalesViewTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPageableAsync_ReturnsBothSaleAndOrder_InSameList()
    {
        await _factory.ResetAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<IUnifiedSaleManager>();

        // Seed: 1 Sale (POS) + 1 Order (Trendyol)
        var sale = new Sale {
            Id = Guid.NewGuid(),
            SaleNumber = "POS-TEST-1",
            SaleDate = DateTimeOffset.UtcNow.AddHours(-1),
            SaleSource = SaleSource.POS,
            SaleStatus = SaleStatus.Completed,
            SalePersonId = Guid.NewGuid(),
            BranchOfficeId = 1
        };
        var order = new Order {
            Id = Guid.NewGuid(),
            OrderNumber = "TY-TEST-1",
            OrderDate = DateTimeOffset.UtcNow.AddMinutes(-30),
            MarketPlaceId = 1,
            BillingAddress = TestData.Address(),
            ShippingAddress = TestData.Address()
        };
        db.Sales.Add(sale);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var filter = new UnifiedSaleFilterDto(
            Source: null, Status: null,
            StartDate: DateTimeOffset.UtcNow.AddDays(-1),
            EndDate: DateTimeOffset.UtcNow.AddDays(1),
            SearchText: null, CustomerId: null,
            PageIndex: 0, PageSize: 25);

        var result = await manager.GetPageableAsync(filter);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Items.Should().Contain(x => x.Number == "POS-TEST-1");
        result.Data.Items.Should().Contain(x => x.Number == "TY-TEST-1");
    }

    [Fact]
    public async Task GetPageableAsync_FiltersBySource_WhenSourceProvided()
    {
        await _factory.ResetAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<IUnifiedSaleManager>();

        db.Sales.Add(new Sale { Id = Guid.NewGuid(), SaleNumber = "POS-1", SaleDate = DateTimeOffset.UtcNow, SaleSource = SaleSource.POS, SalePersonId = Guid.NewGuid(), BranchOfficeId = 1 });
        db.Orders.Add(new Order { Id = Guid.NewGuid(), OrderNumber = "TY-1", OrderDate = DateTimeOffset.UtcNow, MarketPlaceId = 1, BillingAddress = TestData.Address(), ShippingAddress = TestData.Address() });
        await db.SaveChangesAsync();

        var filter = new UnifiedSaleFilterDto(
            Source: UnifiedSaleSource.POS, Status: null,
            StartDate: DateTimeOffset.UtcNow.AddDays(-1),
            EndDate: DateTimeOffset.UtcNow.AddDays(1),
            SearchText: null, CustomerId: null,
            PageIndex: 0, PageSize: 25);

        var result = await manager.GetPageableAsync(filter);

        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Source.Should().Be(UnifiedSaleSource.POS);
    }
}
```

**Not:** `TestData.Address()` helper'ı test projesinde yoksa oluştur; mevcut seed yardımcılarına uyarla (örnek: `new Address { Line1 = "X", City = "İstanbul", ZipCode = "34000" }`).

- [ ] **Step 2: Testi çalıştır — FAIL beklenir**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~UnifiedSalesViewTests"`
Expected: FAIL, `IUnifiedSaleManager` DI'da yok.

---

## Task 9: UnifiedSaleManager Implementasyonu

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/UnifiedSaleManager.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: `UnifiedSaleManager.cs` implementasyonu**

```csharp
// Application/Entegrasyon.Business/Concrete/UnifiedSaleManager.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Core.Utilities.Results;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Sales.Views;
using Entegrasyon.Entity.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class UnifiedSaleManager(IntegrationDbContext db) : IUnifiedSaleManager
{
    public async Task<IDataResult<Paginate<UnifiedSaleListItemDto>>> GetPageableAsync(UnifiedSaleFilterDto filter)
    {
        var query = BuildBaseQuery(filter);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.SaleDate)
            .Skip(filter.PageIndex * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtoList = items.Select(MapToDto).ToList();

        var page = new Paginate<UnifiedSaleListItemDto>
        {
            Items = dtoList,
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };

        return new SuccessDataResult<Paginate<UnifiedSaleListItemDto>>(page);
    }

    public async Task<IDataResult<UnifiedSaleSummaryDto>> GetSummaryAsync(UnifiedSaleFilterDto filter)
    {
        var query = BuildBaseQuery(filter);

        var totalRevenue = await query.SumAsync(x => (decimal?)x.TotalPrice) ?? 0m;
        var count = await query.CountAsync();
        var avg = count == 0 ? 0m : totalRevenue / count;

        var returnCount = await query
            .CountAsync(x => x.RawStatusCode == (int)SaleStatus.PartialReturn
                          || x.RawStatusCode == (int)SaleStatus.FullReturn);
        var returnRate = count == 0 ? 0d : (double)returnCount / count;

        return new SuccessDataResult<UnifiedSaleSummaryDto>(
            new UnifiedSaleSummaryDto(totalRevenue, count, avg, returnRate));
    }

    public async Task<IDataResult<List<UnifiedSaleSourceCountDto>>> GetSourceCountsAsync(UnifiedSaleFilterDto filter)
    {
        // Source filtresini sayacı için yok sayıyoruz (her kaynağın sayısını istiyoruz)
        var baseFilter = filter with { Source = null };
        var query = BuildBaseQuery(baseFilter);

        var grouped = await query
            .GroupBy(x => x.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = grouped.Sum(g => g.Count);
        var list = new List<UnifiedSaleSourceCountDto>
        {
            new(null, "Tümü", total)
        };

        foreach (var g in grouped.OrderBy(x => (int)x.Source))
        {
            list.Add(new UnifiedSaleSourceCountDto(g.Source, g.Source.ToString(), g.Count));
        }

        return new SuccessDataResult<List<UnifiedSaleSourceCountDto>>(list);
    }

    private IQueryable<UnifiedSaleView> BuildBaseQuery(UnifiedSaleFilterDto filter)
    {
        var q = db.Set<UnifiedSaleView>().AsQueryable();

        if (filter.StartDate.HasValue)
            q = q.Where(x => x.SaleDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            q = q.Where(x => x.SaleDate < filter.EndDate.Value);
        if (filter.Source.HasValue)
            q = q.Where(x => x.Source == filter.Source.Value);
        if (filter.CustomerId.HasValue)
            q = q.Where(x => x.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var s = filter.SearchText.Trim().ToLower();
            q = q.Where(x =>
                (x.Number ?? "").ToLower().Contains(s) ||
                (x.CustomerDisplayName ?? "").ToLower().Contains(s));
        }

        if (filter.Status.HasValue)
        {
            // Normalize durum filtresi: controller katmanında ham koda çevirerek filtrele
            var targetStatus = filter.Status.Value;
            q = q.AsEnumerable()
                 .Where(x => UnifiedSaleStatusMapper.Map(x.RawStatusCode, x.EntityType) == targetStatus)
                 .AsQueryable();
        }

        return q;
    }

    private static UnifiedSaleListItemDto MapToDto(UnifiedSaleView v)
    {
        var status = UnifiedSaleStatusMapper.Map(v.RawStatusCode, v.EntityType);
        var detailUrl = v.EntityType == 0
            ? $"/sales/sale/{v.Id}"
            : $"/sales/order/{v.Id}";

        return new UnifiedSaleListItemDto
        {
            Id = v.Id,
            EntityType = v.EntityType,
            Source = v.Source,
            Number = v.Number,
            SaleDate = v.SaleDate,
            CustomerDisplayName = v.CustomerDisplayName,
            CustomerSubLine = v.CustomerId.HasValue ? null : "Tekil",
            TotalPrice = v.TotalPrice,
            ItemCount = v.ItemCount,
            Status = status,
            StatusSubLine = v.CargoTrackingNumber,
            DetailUrl = detailUrl
        };
    }
}
```

**Not:** `Status` filtresi `AsEnumerable()` ile bellekte yapılıyor çünkü `UnifiedSaleStatusMapper.Map` SQL'e çevrilemez. Performans darboğazı olursa view'a `NormalizedStatus` kolonu SQL `CASE` ile eklenebilir; şimdilik basit tutuyoruz.

- [ ] **Step 2: DI kaydı ekle**

`Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` içindeki diğer manager kayıtlarının arasına:

```csharp
services.AddScoped<IUnifiedSaleManager, UnifiedSaleManager>();
```

Gerekli using'i dosya başına ekle:
```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
```

- [ ] **Step 3: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded.

- [ ] **Step 4: Integration testlerini çalıştır**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~UnifiedSalesViewTests"`
Expected: PASS (2 test).

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/UnifiedSaleManager.cs Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs Test/Entegrasyon.IntegrationTest/Sales/UnifiedSalesViewTests.cs
git commit -m "feat(sales): UnifiedSaleManager + DI + integration testleri"
```

---

## Task 10: SaleController — Yeniden Yazım (Index + Detail Route'ları)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`
- Create: `Application/Entegrasyon.MVC/Features/Sales/ViewModels/UnifiedSaleIndexViewModel.cs`

- [ ] **Step 1: ViewModel yaz**

```csharp
// Application/Entegrasyon.MVC/Features/Sales/ViewModels/UnifiedSaleIndexViewModel.cs
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Utilities;

namespace Entegrasyon.MVC.Features.Sales.ViewModels;

public sealed class UnifiedSaleIndexViewModel
{
    public Paginate<UnifiedSaleListItemDto> Page { get; init; } = new();
    public UnifiedSaleSummaryDto Summary { get; init; } = new(0, 0, 0, 0);
    public List<UnifiedSaleSourceCountDto> SourceCounts { get; init; } = [];
    public UnifiedSaleFilterDto Filter { get; init; } = new(null, null, null, null, null, null);
}
```

- [ ] **Step 2: `SaleController`'ı yeniden yaz**

`Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`'i tamamen şu içerikle değiştir:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Features.Sales.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Sales;

[Authorize]
public class SaleController(
    IUnifiedSaleManager unifiedSaleManager,
    ISaleManager saleManager,
    ISaleReturnManager saleReturnManager,
    IPaymentMethodManager paymentMethodManager,
    IOrderManager orderManager) : Controller
{
    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Birleşik Liste ──────────────────────────────────────────────────

    [HttpGet("/sales")]
    public async Task<IActionResult> Index(
        UnifiedSaleSource? source = null,
        UnifiedSaleStatus? status = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? search = null,
        int page = 0)
    {
        var startLocal = startDate.HasValue ? TurkeyTime.StartOfDay(startDate.Value) : TurkeyTime.StartOfToday.AddDays(-7);
        var endLocal = endDate.HasValue ? TurkeyTime.StartOfDay(endDate.Value).AddDays(1) : TurkeyTime.StartOfToday.AddDays(1);
        var start = startLocal.ToUniversalTime();
        var end = endLocal.ToUniversalTime();

        var filter = new UnifiedSaleFilterDto(
            Source: source, Status: status,
            StartDate: start, EndDate: end,
            SearchText: search, CustomerId: null,
            PageIndex: page, PageSize: 25);

        var pageResult = await unifiedSaleManager.GetPageableAsync(filter);
        var summaryResult = await unifiedSaleManager.GetSummaryAsync(filter);
        var countsResult = await unifiedSaleManager.GetSourceCountsAsync(filter);

        var vm = new UnifiedSaleIndexViewModel
        {
            Page = pageResult.Data ?? new(),
            Summary = summaryResult.Data ?? new(0, 0, 0, 0),
            SourceCounts = countsResult.Data ?? [],
            Filter = filter
        };

        ViewData.SetPageTitle("Satışlar");
        ViewData.SetActiveNav("sales");

        if (Request.IsHtmx())
            return PartialView("Partials/_UnifiedSaleTable", vm);

        return View(vm);
    }

    // ── Sale Detay (POS / Manuel) ───────────────────────────────────────

    [HttpGet("/sales/sale/{id:guid}")]
    public async Task<IActionResult> SaleDetail(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData.SetError(result.Message ?? "Satış bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(1);

        ViewData.SetPageTitle($"Satış Detay — {result.Data.SaleNumber}");
        ViewData.SetActiveNav("sales");
        ViewData.SetBreadcrumb(("Satışlar", "/sales"), ($"#{result.Data.SaleNumber}", null));
        ViewBag.PaymentMethods = paymentMethods.Data ?? [];

        return View("SaleDetail", result.Data);
    }

    // ── Order Detay (Marketplace / Storefront) ──────────────────────────

    [HttpGet("/sales/order/{id:guid}")]
    public async Task<IActionResult> OrderDetail(Guid id)
    {
        var result = await orderManager.GetOrderByIdAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData.SetError(result.Message ?? "Sipariş bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var order = result.Data;
        var title = order.OrderNumber ?? order.Id.ToString()[..8];

        ViewData.SetPageTitle($"Sipariş #{title}");
        ViewData.SetActiveNav("sales");
        ViewData.SetBreadcrumb(("Satışlar", "/sales"), ($"#{title}", null));

        return View("OrderDetail", order);
    }

    // ── Geriye Uyumluluk: Eski /sales/{id} ve /orders redirect'leri ─────

    [HttpGet("/sales/{id:guid}")]
    public IActionResult LegacySaleDetail(Guid id)
        => RedirectToActionPermanent(nameof(SaleDetail), new { id });
}
```

- [ ] **Step 3: Eski `Detail.cshtml`'i `SaleDetail.cshtml` olarak taşı**

Run:
```bash
git mv Application/Entegrasyon.MVC/Features/Sales/Views/Detail.cshtml Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml
```
Expected: dosya taşındı.

- [ ] **Step 4: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/
git commit -m "feat(sales): SaleController'ı birleşik liste + yeni detay route'larıyla yeniden yaz"
```

---

## Task 11: OrderController'ı Redirect'lere İndirge

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Orders/OrderController.cs`
- Move: `Application/Entegrasyon.MVC/Features/Orders/Views/OrderDetail.cshtml` → `Application/Entegrasyon.MVC/Features/Sales/Views/OrderDetail.cshtml`
- Move: `Application/Entegrasyon.MVC/Features/Orders/Views/Print.cshtml` → `Application/Entegrasyon.MVC/Features/Sales/Views/OrderPrint.cshtml`

- [ ] **Step 1: Detay view'ını taşı**

```bash
git mv Application/Entegrasyon.MVC/Features/Orders/Views/OrderDetail.cshtml Application/Entegrasyon.MVC/Features/Sales/Views/OrderDetail.cshtml
git mv Application/Entegrasyon.MVC/Features/Orders/Views/Print.cshtml Application/Entegrasyon.MVC/Features/Sales/Views/OrderPrint.cshtml
```

- [ ] **Step 2: `OrderController`'ı redirect'lere indirge**

```csharp
// Application/Entegrasyon.MVC/Features/Orders/OrderController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Orders;

[Authorize]
public class OrderController : Controller
{
    [HttpGet("/orders")]
    public IActionResult IndexRedirect()
        => RedirectPermanent("/sales");

    [HttpGet("/marketplace/orders")]
    public IActionResult MarketplaceRedirect(int mp = 1)
    {
        var source = mp switch
        {
            1 => "Trendyol",
            2 => "N11",
            3 => "Hepsiburada",
            4 => "Amazon",
            5 => "Pazarama",
            7 => "PttAvm",
            8 => "Ciceksepeti",
            _ => null
        };

        var url = source is null ? "/sales" : $"/sales?source={source}";
        return RedirectPermanent(url);
    }

    [HttpGet("/orders/{id:guid}")]
    public IActionResult DetailRedirect(Guid id)
        => RedirectPermanent($"/sales/order/{id}");

    [HttpGet("/orders/{id:guid}/print")]
    public IActionResult PrintRedirect(Guid id)
        => RedirectPermanent($"/sales/order/{id}/print");
}
```

- [ ] **Step 3: Eski liste view'larını sil**

Run:
```bash
git rm Application/Entegrasyon.MVC/Features/Orders/Views/Index.cshtml
git rm Application/Entegrasyon.MVC/Features/Orders/Views/MarketplaceOrders.cshtml
git rm Application/Entegrasyon.MVC/Features/Orders/Views/Partials/_OrderTable.cshtml
```

- [ ] **Step 4: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: Build succeeded. Eksik cshtml referansı varsa burada hata verir — `OrderDetail.cshtml` içinde tam yol referansları (örn. `~/Features/Orders/Views/Partials/...`) varsa `Features/Sales/Views/Partials/`'e güncellenmesi gerekir.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Orders Application/Entegrasyon.MVC/Features/Sales/Views/OrderDetail.cshtml Application/Entegrasyon.MVC/Features/Sales/Views/OrderPrint.cshtml
git commit -m "refactor(orders): OrderController'ı redirect'lere indirge, detay view'ları Features/Sales altına taşı"
```

---

## Task 12: Ortak Detail Header Partial'ı

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleDetailHeader.cshtml`

- [ ] **Step 1: Partial'ı yaz**

```razor
@* Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleDetailHeader.cshtml *@
@model Entegrasyon.MVC.Features.Sales.ViewModels.UnifiedSaleDetailHeaderViewModel

<div class="card mb-3">
  <div class="card-body d-flex align-items-center justify-content-between">
    <div>
      <div class="d-flex align-items-center gap-2 mb-1">
        <span class="badge @Model.SourceBadgeClass">@Model.SourceLabel</span>
        <span class="badge @Model.StatusBadgeClass">@Model.StatusLabel</span>
      </div>
      <h2 class="mb-0">#@Model.Number</h2>
      <div class="text-muted small mt-1">
        @Model.SaleDate.ToString("dd.MM.yyyy HH:mm")
        @if (!string.IsNullOrWhiteSpace(Model.Subtitle))
        {
            <text> · @Model.Subtitle</text>
        }
      </div>
    </div>
    <div class="dropdown">
      <button class="btn btn-primary dropdown-toggle" type="button" data-bs-toggle="dropdown">
        <i class="ti ti-dots-vertical me-1"></i> İşlemler
      </button>
      <ul class="dropdown-menu dropdown-menu-end">
        @foreach (var group in Model.ActionGroups)
        {
            @if (group.Header is not null)
            {
                <li><h6 class="dropdown-header">@group.Header</h6></li>
            }
            @foreach (var action in group.Actions)
            {
                <li>
                  <a class="dropdown-item"
                     href="@action.Href"
                     @(action.HxPost is not null ? $"hx-post={action.HxPost}" : "")>
                    <i class="ti @action.Icon me-2"></i>@action.Label
                  </a>
                </li>
            }
            <li><hr class="dropdown-divider" /></li>
        }
      </ul>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Header ViewModel'ini yaz**

`Application/Entegrasyon.MVC/Features/Sales/ViewModels/UnifiedSaleDetailHeaderViewModel.cs`:

```csharp
namespace Entegrasyon.MVC.Features.Sales.ViewModels;

public sealed class UnifiedSaleDetailHeaderViewModel
{
    public string Number { get; init; } = "";
    public string SourceLabel { get; init; } = "";
    public string SourceBadgeClass { get; init; } = "bg-blue-lt text-blue";
    public string StatusLabel { get; init; } = "";
    public string StatusBadgeClass { get; init; } = "bg-green-lt text-green";
    public DateTimeOffset SaleDate { get; init; }
    public string? Subtitle { get; init; }
    public List<ActionGroup> ActionGroups { get; init; } = [];

    public sealed class ActionGroup
    {
        public string? Header { get; init; }
        public List<ActionItem> Actions { get; init; } = [];
    }

    public sealed class ActionItem
    {
        public string Label { get; init; } = "";
        public string Icon { get; init; } = "ti-circle";
        public string? Href { get; init; }
        public string? HxPost { get; init; }
    }
}
```

- [ ] **Step 3: `SaleDetail.cshtml` içindeki eski başlığı partial çağrısıyla değiştir**

`Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml`'in en üstündeki `<h1>`/breadcrumb bloğunu şunla değiştir:

```razor
@{
    var headerVm = new UnifiedSaleDetailHeaderViewModel
    {
        Number = Model.SaleNumber,
        SourceLabel = Model.SaleSource == SaleSource.POS ? "POS" : "Manuel",
        SourceBadgeClass = Model.SaleSource == SaleSource.POS ? "bg-blue-lt text-blue" : "bg-yellow-lt text-yellow",
        StatusLabel = Model.SaleStatus switch
        {
            SaleStatus.Completed => "Tamamlandı",
            SaleStatus.PartialReturn => "Kısmi İade",
            SaleStatus.FullReturn => "Tam İade",
            SaleStatus.Cancelled => "İptal",
            _ => Model.SaleStatus.ToString()
        },
        StatusBadgeClass = Model.SaleStatus switch
        {
            SaleStatus.Completed => "bg-green-lt text-green",
            SaleStatus.PartialReturn => "bg-orange-lt text-orange",
            SaleStatus.FullReturn => "bg-red-lt text-red",
            SaleStatus.Cancelled => "bg-muted-lt text-muted",
            _ => "bg-blue-lt text-blue"
        },
        SaleDate = Model.SaleDate,
        Subtitle = Model.SalePerson?.UserName,
        ActionGroups =
        [
            new()
            {
                Actions =
                [
                    new() { Label = "İade Al",      Icon = "ti-arrow-back-up", Href = $"#return-modal-{Model.Id}" },
                    new() { Label = "Fatura Kes",    Icon = "ti-file-invoice", Href = $"/invoicing/for-sale/{Model.Id}" },
                    new() { Label = "Fiş Yazdır",    Icon = "ti-printer",      Href = $"/sales/sale/{Model.Id}/print" },
                ]
            },
            new()
            {
                Actions =
                [
                    new() { Label = "İptal Et", Icon = "ti-ban", HxPost = $"/sales/sale/{Model.Id}/cancel" },
                ]
            }
        ]
    };
}

<partial name="Partials/_UnifiedSaleDetailHeader" model="headerVm" />
```

**Not:** SaleDetail'daki mevcut breadcrumb/başlık bloğunu TAMAMEN silmek yerine önceki versiyonunu korumak isteyebilirsin — karar işleme sırasında verilir. Minimum müdahale için mevcut h1 bloğunu sil ve yukarıdakini koy.

- [ ] **Step 4: `OrderDetail.cshtml`'i aynı pattern'le güncelle**

Aynı blok, Order için:

```razor
@{
    var isStorefront = Model.MarketPlaceId is null;
    var sourceLabel = isStorefront ? "Storefront" : Model.MarketPlace?.Name ?? "Pazaryeri";
    var sourceClass = isStorefront ? "bg-cyan-lt text-cyan" : "bg-orange-lt text-orange";

    var rawStatus = (int)(Model.StorefrontOrderStatus ?? 0);
    var statusLabel = rawStatus switch
    {
        1 or 2 => "Beklemede",
        3 => "Kargoda",
        4 => "Tamamlandı",
        5 => "İptal",
        _ => Model.MarketplaceOrderStatus ?? "—"
    };
    var statusClass = rawStatus switch
    {
        1 or 2 => "bg-yellow-lt text-yellow",
        3 => "bg-blue-lt text-blue",
        4 => "bg-green-lt text-green",
        5 => "bg-muted-lt text-muted",
        _ => "bg-blue-lt text-blue"
    };

    var actions = isStorefront
        ? new List<UnifiedSaleDetailHeaderViewModel.ActionGroup>
        {
            new() { Actions = [
                new() { Label = "Onayla",       Icon = "ti-check",       HxPost = $"/sales/order/{Model.Id}/confirm" },
                new() { Label = "Kargoya Ver",  Icon = "ti-truck",       Href  = $"#ship-modal-{Model.Id}" },
                new() { Label = "Teslim Edildi",Icon = "ti-package-exclamation", HxPost = $"/sales/order/{Model.Id}/deliver" },
            ]},
            new() { Actions = [
                new() { Label = "Fatura Kes",   Icon = "ti-file-invoice", Href = $"/invoicing/for-order/{Model.Id}" },
                new() { Label = "Yazdır",       Icon = "ti-printer",      Href = $"/sales/order/{Model.Id}/print" },
            ]}
        }
        : new List<UnifiedSaleDetailHeaderViewModel.ActionGroup>
        {
            new() { Actions = [
                new() { Label = "Kargo Kodu Gir",       Icon = "ti-barcode",      Href = $"#cargo-modal-{Model.Id}" },
                new() { Label = "Pazaryerinden Yenile", Icon = "ti-refresh",      HxPost = $"/sales/order/{Model.Id}/refresh" },
                new() { Label = "Durumu Güncelle",      Icon = "ti-edit",         Href = $"#status-modal-{Model.Id}" },
            ]},
            new() { Actions = [
                new() { Label = "Fatura Kes",  Icon = "ti-file-invoice", Href = $"/invoicing/for-order/{Model.Id}" },
                new() { Label = "Yazdır",      Icon = "ti-printer",      Href = $"/sales/order/{Model.Id}/print" },
            ]}
        };

    var headerVm = new UnifiedSaleDetailHeaderViewModel
    {
        Number = Model.OrderNumber ?? Model.Id.ToString()[..8],
        SourceLabel = sourceLabel,
        SourceBadgeClass = sourceClass,
        StatusLabel = statusLabel,
        StatusBadgeClass = statusClass,
        SaleDate = Model.OrderDate ?? Model.CreatedAt,
        Subtitle = $"{Model.CustomerFirstName} {Model.CustomerLastName}".Trim(),
        ActionGroups = actions
    };
}

<partial name="Partials/_UnifiedSaleDetailHeader" model="headerVm" />
```

- [ ] **Step 5: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/ViewModels/UnifiedSaleDetailHeaderViewModel.cs Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleDetailHeader.cshtml Application/Entegrasyon.MVC/Features/Sales/Views/SaleDetail.cshtml Application/Entegrasyon.MVC/Features/Sales/Views/OrderDetail.cshtml
git commit -m "feat(sales): ortak detay header partial'ı ve işlemler dropdown'u"
```

---

## Task 13: Liste UI — KPI Kartları Partial

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedKpiCards.cshtml`

- [ ] **Step 1: Partial'ı yaz**

```razor
@* Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedKpiCards.cshtml *@
@model Entegrasyon.Entity.Dtos.Sale.UnifiedSaleSummaryDto

<div class="row row-cards mb-3">
  <div class="col-sm-6 col-lg-3">
    <div class="card">
      <div class="card-body">
        <div class="text-muted small">Toplam Ciro</div>
        <div class="h1 mt-2 mb-0">@Model.TotalRevenue.ToString("C2", new System.Globalization.CultureInfo("tr-TR"))</div>
      </div>
    </div>
  </div>
  <div class="col-sm-6 col-lg-3">
    <div class="card">
      <div class="card-body">
        <div class="text-muted small">Satış Sayısı</div>
        <div class="h1 mt-2 mb-0">@Model.SaleCount</div>
      </div>
    </div>
  </div>
  <div class="col-sm-6 col-lg-3">
    <div class="card">
      <div class="card-body">
        <div class="text-muted small">Ortalama Sepet</div>
        <div class="h1 mt-2 mb-0">@Model.AverageBasket.ToString("C2", new System.Globalization.CultureInfo("tr-TR"))</div>
      </div>
    </div>
  </div>
  <div class="col-sm-6 col-lg-3">
    <div class="card">
      <div class="card-body">
        <div class="text-muted small">İade Oranı</div>
        <div class="h1 mt-2 mb-0">@((Model.ReturnRate * 100).ToString("F1"))%</div>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Build kontrolü**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedKpiCards.cshtml
git commit -m "feat(sales): _UnifiedKpiCards partial'ı"
```

---

## Task 14: Liste UI — Kaynak Sekmeleri Partial

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSourceTabs.cshtml`

- [ ] **Step 1: Partial'ı yaz**

```razor
@* Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSourceTabs.cshtml *@
@model Entegrasyon.MVC.Features.Sales.ViewModels.UnifiedSaleIndexViewModel

<div class="card mb-3">
  <ul class="nav nav-bordered px-2" role="tablist">
    @foreach (var c in Model.SourceCounts)
    {
        var isActive = (Model.Filter.Source is null && c.Source is null)
                    || (Model.Filter.Source.HasValue && c.Source == Model.Filter.Source);

        var url = "/sales?" +
                  (c.Source.HasValue ? $"source={c.Source}&" : "") +
                  (Model.Filter.Status.HasValue ? $"status={Model.Filter.Status}&" : "") +
                  (Model.Filter.StartDate.HasValue ? $"startDate={Model.Filter.StartDate:yyyy-MM-dd}&" : "") +
                  (Model.Filter.EndDate.HasValue ? $"endDate={Model.Filter.EndDate:yyyy-MM-dd}&" : "") +
                  (!string.IsNullOrWhiteSpace(Model.Filter.SearchText) ? $"search={Model.Filter.SearchText}" : "");

        <li class="nav-item">
          <a class="nav-link @(isActive ? "active" : "")" href="@url">
            @c.Label
            <span class="badge bg-muted-lt ms-1">@c.Count</span>
          </a>
        </li>
    }
  </ul>
</div>
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSourceTabs.cshtml
git commit -m "feat(sales): kaynak sekmeleri partial'ı"
```

---

## Task 15: Liste UI — Filtre Barı Partial

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedFilters.cshtml`

- [ ] **Step 1: Partial'ı yaz**

```razor
@* Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedFilters.cshtml *@
@model Entegrasyon.MVC.Features.Sales.ViewModels.UnifiedSaleIndexViewModel

<form class="card card-body mb-3" method="get" action="/sales"
      hx-get="/sales" hx-target="#unified-sale-list" hx-trigger="change delay:300ms from:find input, change delay:300ms from:find select, submit">

  <input type="hidden" name="source" value="@(Model.Filter.Source?.ToString() ?? "")" />

  <div class="row g-2 align-items-end">
    <div class="col-md-3">
      <label class="form-label">Tarih aralığı</label>
      <input type="text" class="form-control flatpickr-range"
             name="dateRange"
             data-flatpickr-mode="range"
             data-flatpickr-locale="tr"
             value="@(Model.Filter.StartDate?.ToString("yyyy-MM-dd")) - @(Model.Filter.EndDate?.ToString("yyyy-MM-dd"))" />
      <input type="hidden" name="startDate" value="@Model.Filter.StartDate?.ToString("yyyy-MM-dd")" />
      <input type="hidden" name="endDate" value="@Model.Filter.EndDate?.ToString("yyyy-MM-dd")" />
    </div>

    <div class="col-md-3">
      <label class="form-label">Durum</label>
      <select class="form-select" name="status">
        <option value="">Tümü</option>
        @foreach (var s in Enum.GetValues<Entegrasyon.Entity.Sales.UnifiedSaleStatus>())
        {
            <option value="@s" selected="@(Model.Filter.Status == s)">
                @(s switch
                {
                    Entegrasyon.Entity.Sales.UnifiedSaleStatus.Completed     => "Tamamlandı",
                    Entegrasyon.Entity.Sales.UnifiedSaleStatus.Pending       => "Beklemede",
                    Entegrasyon.Entity.Sales.UnifiedSaleStatus.Shipping      => "Kargoda",
                    Entegrasyon.Entity.Sales.UnifiedSaleStatus.PartialReturn => "Kısmi İade",
                    Entegrasyon.Entity.Sales.UnifiedSaleStatus.FullReturn    => "Tam İade",
                    Entegrasyon.Entity.Sales.UnifiedSaleStatus.Cancelled     => "İptal",
                    _ => s.ToString()
                })
            </option>
        }
      </select>
    </div>

    <div class="col-md-4">
      <label class="form-label">Ara</label>
      <input type="text" class="form-control" name="search"
             placeholder="No veya müşteri adı"
             value="@Model.Filter.SearchText" />
    </div>

    <div class="col-md-2">
      <button type="submit" class="btn btn-primary w-100">Filtrele</button>
    </div>
  </div>
</form>
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedFilters.cshtml
git commit -m "feat(sales): birleşik filtre barı partial'ı"
```

---

## Task 16: Liste UI — Tablo Partial (İki-Katlı Satırlar)

**Files:**
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleTable.cshtml`

- [ ] **Step 1: Partial'ı yaz**

```razor
@* Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleTable.cshtml *@
@using Entegrasyon.Entity.Sales
@model Entegrasyon.MVC.Features.Sales.ViewModels.UnifiedSaleIndexViewModel
@{
    string SourceBadge(UnifiedSaleSource s) => s switch
    {
        UnifiedSaleSource.POS         => "bg-blue-lt text-blue",
        UnifiedSaleSource.Manual      => "bg-yellow-lt text-yellow",
        UnifiedSaleSource.Storefront  => "bg-cyan-lt text-cyan",
        UnifiedSaleSource.Trendyol    => "bg-orange-lt text-orange",
        UnifiedSaleSource.N11         => "bg-red-lt text-red",
        UnifiedSaleSource.Hepsiburada => "bg-purple-lt text-purple",
        UnifiedSaleSource.Amazon      => "bg-yellow-lt text-yellow",
        UnifiedSaleSource.Pazarama    => "bg-green-lt text-green",
        UnifiedSaleSource.PttAvm      => "bg-azure-lt text-azure",
        UnifiedSaleSource.Ciceksepeti => "bg-pink-lt text-pink",
        _                             => "bg-muted-lt text-muted"
    };

    string StatusBadge(UnifiedSaleStatus s) => s switch
    {
        UnifiedSaleStatus.Completed     => "bg-green-lt text-green",
        UnifiedSaleStatus.Pending       => "bg-yellow-lt text-yellow",
        UnifiedSaleStatus.Shipping      => "bg-blue-lt text-blue",
        UnifiedSaleStatus.PartialReturn => "bg-orange-lt text-orange",
        UnifiedSaleStatus.FullReturn    => "bg-red-lt text-red",
        UnifiedSaleStatus.Cancelled     => "bg-muted-lt text-muted",
        _                               => "bg-muted-lt text-muted"
    };

    string StatusLabel(UnifiedSaleStatus s) => s switch
    {
        UnifiedSaleStatus.Completed     => "Tamamlandı",
        UnifiedSaleStatus.Pending       => "Beklemede",
        UnifiedSaleStatus.Shipping      => "Kargoda",
        UnifiedSaleStatus.PartialReturn => "Kısmi İade",
        UnifiedSaleStatus.FullReturn    => "Tam İade",
        UnifiedSaleStatus.Cancelled     => "İptal",
        _                               => s.ToString()
    };
}

<div id="unified-sale-list">
  <div class="card">
    <div class="table-responsive">
      <table class="table table-vcenter card-table">
        <thead>
          <tr>
            <th>Tarih</th>
            <th>Sipariş / Satış</th>
            <th>Müşteri</th>
            <th class="text-end">Kalem</th>
            <th class="text-end">Tutar</th>
            <th>Durum</th>
          </tr>
        </thead>
        <tbody>
          @if (Model.Page.Items.Count == 0)
          {
              <tr><td colspan="6" class="text-center text-muted py-4">Kayıt bulunamadı.</td></tr>
          }
          @foreach (var item in Model.Page.Items)
          {
              <tr style="cursor:pointer;" onclick="location.href='@item.DetailUrl'">
                  <td>
                      <div>@item.SaleDate.ToLocalTime().ToString("dd.MM HH:mm")</div>
                      <div class="text-muted small">@GetRelative(item.SaleDate)</div>
                  </td>
                  <td>
                      <div class="fw-bold">@item.Number</div>
                      <span class="badge @SourceBadge(item.Source) mt-1">@item.Source</span>
                  </td>
                  <td>
                      <div>@(item.CustomerDisplayName ?? "—")</div>
                      @if (!string.IsNullOrWhiteSpace(item.CustomerSubLine))
                      {
                          <div class="text-muted small">@item.CustomerSubLine</div>
                      }
                  </td>
                  <td class="text-end">
                      <div>@item.ItemCount adet</div>
                  </td>
                  <td class="text-end">
                      <div class="fw-bold">@item.TotalPrice.ToString("C2", new System.Globalization.CultureInfo("tr-TR"))</div>
                  </td>
                  <td>
                      <span class="badge @StatusBadge(item.Status)">@StatusLabel(item.Status)</span>
                      @if (!string.IsNullOrWhiteSpace(item.StatusSubLine))
                      {
                          <div class="text-muted small mt-1">@item.StatusSubLine</div>
                      }
                  </td>
              </tr>
          }
        </tbody>
      </table>
    </div>

    @* Basic pagination *@
    @{
        var totalPages = (int)Math.Ceiling(Model.Page.TotalCount / (double)Model.Page.PageSize);
    }
    @if (totalPages > 1)
    {
        <div class="card-footer d-flex align-items-center">
          <p class="m-0 text-muted">Toplam @Model.Page.TotalCount kayıt</p>
          <ul class="pagination m-0 ms-auto">
            @for (int i = 0; i < totalPages; i++)
            {
                var isActive = i == Model.Page.PageIndex;
                var url = Url.Action("Index", "Sale", new {
                    source = Model.Filter.Source,
                    status = Model.Filter.Status,
                    startDate = Model.Filter.StartDate,
                    endDate = Model.Filter.EndDate,
                    search = Model.Filter.SearchText,
                    page = i
                });
                <li class="page-item @(isActive ? "active" : "")">
                  <a class="page-link" href="@url" hx-get="@url" hx-target="#unified-sale-list" hx-push-url="true">@(i+1)</a>
                </li>
            }
          </ul>
        </div>
    }
  </div>
</div>

@functions {
    static string GetRelative(DateTimeOffset dt)
    {
        var diff = DateTimeOffset.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "az önce";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk önce";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} sa önce";
        return $"{(int)diff.TotalDays} gün önce";
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_UnifiedSaleTable.cshtml
git commit -m "feat(sales): birleşik tablo partial'ı (iki katlı satırlar + row click)"
```

---

## Task 17: Liste UI — Index.cshtml Yeniden Yazım

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/Index.cshtml`

- [ ] **Step 1: Index.cshtml'i komple değiştir**

```razor
@* Application/Entegrasyon.MVC/Features/Sales/Views/Index.cshtml *@
@model Entegrasyon.MVC.Features.Sales.ViewModels.UnifiedSaleIndexViewModel

<div class="container-xl">
  <div class="page-header d-print-none mb-3">
    <div class="row align-items-center">
      <div class="col">
        <h2 class="page-title">Satışlar</h2>
        <div class="text-muted">Tüm kanallardan satış ve siparişler</div>
      </div>
    </div>
  </div>

  <partial name="Partials/_UnifiedKpiCards" model="Model.Summary" />
  <partial name="Partials/_UnifiedSourceTabs" model="Model" />
  <partial name="Partials/_UnifiedFilters" model="Model" />
  <partial name="Partials/_UnifiedSaleTable" model="Model" />
</div>
```

- [ ] **Step 2: Eski dosyaları temizle**

```bash
git rm Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleTable.cshtml
git rm Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleSummaryCards.cshtml
```

- [ ] **Step 3: Build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/Views/Index.cshtml Application/Entegrasyon.MVC/Features/Sales/Views/Partials/
git commit -m "feat(sales): Index.cshtml birleşik liste view'ı"
```

---

## Task 18: Sidebar Nav Güncellemesi

**Files:**
- Modify: `Application/Entegrasyon.MVC/Shared/Views/_Sidebar.cshtml`

- [ ] **Step 1: "Satış Gecmisi" ve "Satışlar ve Siparişler" satırlarını tek "Satışlar"la değiştir**

Mevcut satır 43-44:
```razor
<li class="nav-item"><a class="nav-link @(activeNav == "sales" ? "active" : "")" require-permission="Permissions.Sales.View" href="/sales"><span class="nav-link-icon"><i class="ti ti-receipt"></i></span><span class="nav-link-title">Satış Gecmisi</span></a></li>
<li class="nav-item"><a class="nav-link @(activeNav == "orders" ? "active" : "")" require-permission="Permissions.Orders.View" href="/orders"><span class="nav-link-icon"><i class="ti ti-shopping-cart"></i></span><span class="nav-link-title">Satışlar ve Siparişler</span></a></li>
```

Şununla değiştir:
```razor
<li class="nav-item"><a class="nav-link @(activeNav == "sales" ? "active" : "")" require-permission="Permissions.Sales.View" href="/sales"><span class="nav-link-icon"><i class="ti ti-receipt"></i></span><span class="nav-link-title">Satışlar</span></a></li>
```

- [ ] **Step 2: Satır 76 "Siparişler" (marketplace-orders) öğesini sil**

Tamamen kaldır:
```razor
<li class="nav-item"><a class="nav-link @(activeNav == "marketplace-orders" ? "active" : "")" require-permission="Permissions.Orders.View" href="/marketplace/orders"><span class="nav-link-icon"><i class="ti ti-clipboard-list"></i></span><span class="nav-link-title">Siparişler</span></a></li>
```

- [ ] **Step 3: Uygulamayı başlat, sidebar'ı kontrol et**

Run (ayrı terminalde): `cd Application/Entegrasyon.MVC && dotnet run`
Tarayıcıda `http://localhost:5100/sales`'e admin/123456789 ile gir, sidebar'da tek "Satışlar" görünmeli, /sales sayfası birleşik tabloyu göstermeli.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Shared/Views/_Sidebar.cshtml
git commit -m "chore(nav): sidebar'daki ikili Satış/Sipariş öğelerini tek 'Satışlar'a indirge"
```

---

## Task 19: E2E Test — Birleşik Liste Akışı

**Files:**
- Create: `Test/Entegrasyon.E2E/Tests/UnifiedSalesPageTests.cs`

- [ ] **Step 1: E2E testi yaz**

```csharp
// Test/Entegrasyon.E2E/Tests/UnifiedSalesPageTests.cs
using Microsoft.Playwright;
using NUnit.Framework;

namespace Entegrasyon.E2E.Tests;

[TestFixture]
public class UnifiedSalesPageTests : E2ETestBase
{
    [Test]
    public async Task SalesPage_LoadsAndShowsUnifiedTable()
    {
        var page = await LoginAsAdminAsync();
        await page.GotoAsync($"{BaseUrl}/sales");

        // Sayfa başlığı
        var title = page.Locator("h2.page-title");
        await Assertions.Expect(title).ToHaveTextAsync("Satışlar");

        // KPI kartları görünmeli
        await Assertions.Expect(page.Locator(".row.row-cards .card")).ToHaveCountAsync(4);

        // Kaynak sekmeleri görünmeli, "Tümü" aktif
        await Assertions.Expect(page.Locator(".nav-item .nav-link.active")).ToContainTextAsync("Tümü");

        // Tablo var
        await Assertions.Expect(page.Locator("table.card-table")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SalesPage_SourceTabChange_UpdatesUrl()
    {
        var page = await LoginAsAdminAsync();
        await page.GotoAsync($"{BaseUrl}/sales");

        await page.Locator(".nav-item .nav-link", new() { HasTextString = "POS" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("source=POS"));
    }

    [Test]
    public async Task SalesPage_RowClick_NavigatesToDetail()
    {
        var page = await LoginAsAdminAsync();
        await page.GotoAsync($"{BaseUrl}/sales");

        var firstRow = page.Locator("table.card-table tbody tr").First;
        if (await firstRow.CountAsync() == 0)
        {
            Assert.Ignore("Seed data olmadığı için atlandı");
            return;
        }

        await firstRow.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/sales/(sale|order)/"));
    }
}
```

**Not:** `E2ETestBase` ve `LoginAsAdminAsync()` mevcut E2E helper'larına göre isimlendirilmiş; gerçek sınıf/metot adlarını `Test/Entegrasyon.E2E/` altındaki mevcut testlere bakıp eşle.

- [ ] **Step 2: Uygulama debug'da ayakta mı kontrol et**

Run: `curl -s -o /dev/null -w "%{http_code}" http://localhost:5099/`
Expected: `200` veya `302` (çalışıyor demek).

- [ ] **Step 3: E2E çalıştır**

Run: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --filter "FullyQualifiedName~UnifiedSalesPageTests"`
Expected: 3 test yeşil.

- [ ] **Step 4: Commit**

```bash
git add Test/Entegrasyon.E2E/Tests/UnifiedSalesPageTests.cs
git commit -m "test(e2e): birleşik satış sayfası akış testleri"
```

---

## Task 20: Tam Test Koşusu ve Son Kontrol

- [ ] **Step 1: Unit testler**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Tümü yeşil.

- [ ] **Step 2: Integration testler**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: Tümü yeşil.

- [ ] **Step 3: E2E testler**

Run: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj`
Expected: Tümü yeşil.

- [ ] **Step 4: Elde deneme**

Tarayıcıda `http://localhost:5100/sales` aç:
- 4 KPI kartı, kaynak sekmeleri, filtre barı, iki-katlı satırlı tablo görünmeli.
- Bir kaynak sekmesine tıkla → URL güncellensin, tablo filtrelensin.
- Bir satıra tıkla → ilgili detay sayfasına (`/sales/sale/{id}` veya `/sales/order/{id}`) gitsin.
- Detay sayfasında sağ üstte "İşlemler" dropdown'u olmalı, içinde tipe göre aksiyonlar.
- `http://localhost:5100/orders` → 301 redirect `/sales`'e.
- `http://localhost:5100/marketplace/orders?mp=1` → `/sales?source=Trendyol`'a.

- [ ] **Step 5: Son commit — varsa temizlik**

```bash
git status
# hiçbir değişiklik yoksa OK, varsa ilgili mesajla commit
```

---

## Self-Review Notları

- **Spec coverage:** Spec'teki 14 bölüm (URL/routing, veri modeli, liste UI, detay sayfası, mimari, test planı) bu planda task'a bağlandı.
- **Placeholder scan:** "TBD"/"TODO"/"similar to" yok. Bilinmeyen kolon/ad gerektiren yerler `Not:` ile açıkça belirtildi (Customer.Name, TestData.Address, E2ETestBase).
- **Type consistency:** `UnifiedSaleSource`, `UnifiedSaleStatus`, `UnifiedSaleListItemDto`, `UnifiedSaleFilterDto`, `UnifiedSaleSummaryDto`, `UnifiedSaleSourceCountDto` tanımları Task 1-3'te bir kez tanımlandı; sonraki task'larda aynı isimler kullanıldı.
- **Skipped scope:** İade akışı, POS ekranı, fatura, print view'larının kendisi — dokunulmuyor, sadece route'lar/header güncelleniyor.
