# SP-1: Storefront Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the ASP.NET Core MVC storefront application with tenant resolution, Tailwind theming, SEO infrastructure, and foundation entity/business layers.

**Architecture:** New MVC project (`Entegrasyon.Storefront`) added to existing solution, sharing Entity/DataAccess/Business layers. Tenant resolved per-request from domain name via middleware. CSS Custom Properties enable per-tenant theming with a single Tailwind build.

**Tech Stack:** ASP.NET Core 8 MVC, Tailwind CSS, EF Core (PostgreSQL), IMemoryCache, xUnit + FluentAssertions + Moq

**Security Note:** All user-generated HTML content (yasal metinler, page content) is stored by the tenant admin via dashboard and rendered with `@Html.Raw()`. In SP-6 (dashboard), an HTML sanitizer (HtmlSanitizer NuGet package) will be applied at write-time before persisting to DB. SP-1 renders admin-authored content only — no end-user submitted HTML.

---

### Task 1: Create Storefront MVC Project Scaffold

**Files:**
- Create: `Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj`
- Create: `Application/Entegrasyon.Storefront/Program.cs`
- Create: `Application/Entegrasyon.Storefront/appsettings.json`
- Create: `Application/Entegrasyon.Storefront/appsettings.Development.json`
- Modify: `Entegrasyon.sln`

- [ ] **Step 1: Create csproj**

```xml
<!-- Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.15">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Entegrasyon.ApplicationBootstrap\Entegrasyon.ApplicationBootstrap.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create minimal Program.cs**

```csharp
// Application/Entegrasyon.Storefront/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/hata/500");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCaching();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
```

- [ ] **Step 3: Create appsettings.json**

```json
{
  "ConnectionStrings": {
    "IntegrationDb": "Host=localhost;Port=5432;Database=IntegrationDb;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 4: Add project to solution**

Run:
```bash
cd /home/baturhan/Projeler/Entegrasyon
dotnet sln Entegrasyon.sln add Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj --solution-folder Application
```

- [ ] **Step 5: Verify build**

Run: `dotnet build Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Storefront/ Entegrasyon.sln
git commit -m "feat(storefront): scaffold MVC project and add to solution"
```

---

### Task 2: Storefront Entity Definitions

**Files:**
- Create: `Application/Entegrasyon.Entity/Storefront/StorefrontSettings.cs`
- Create: `Application/Entegrasyon.Entity/Storefront/StorefrontDomainMapping.cs`
- Create: `Application/Entegrasyon.Entity/Storefront/StorefrontBanner.cs`
- Create: `Application/Entegrasyon.Entity/Storefront/StorefrontPage.cs`
- Create: `Application/Entegrasyon.Entity/Storefront/SslStatus.cs`
- Create: `Application/Entegrasyon.Entity/Storefront/BannerPosition.cs`
- Modify: `Application/Entegrasyon.Entity/Products/Product.cs`
- Modify: `Application/Entegrasyon.Entity/Categories/Category.cs`
- Modify: `Application/Entegrasyon.Entity/Brands/Brand.cs`

- [ ] **Step 1: Create SslStatus and BannerPosition enums**

```csharp
// Application/Entegrasyon.Entity/Storefront/SslStatus.cs
namespace Entegrasyon.Entity.Storefront;

public enum SslStatus { Pending, Active, Expired, Failed }
```

```csharp
// Application/Entegrasyon.Entity/Storefront/BannerPosition.cs
namespace Entegrasyon.Entity.Storefront;

public enum BannerPosition { Hero, Sidebar, Footer, Popup }
```

- [ ] **Step 2: Create StorefrontSettings entity**

```csharp
// Application/Entegrasyon.Entity/Storefront/StorefrontSettings.cs
namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontSettings : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Tema
    public int ThemeId { get; set; } = 1;
    public string StoreName { get; set; } = null!;
    public string? StoreSlogan { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string PrimaryColor { get; set; } = "#2563EB";
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? CustomCss { get; set; }

    // Firma
    public string CompanyName { get; set; } = null!;
    public string CompanyTaxOffice { get; set; } = null!;
    public string CompanyTaxNumber { get; set; } = null!;
    public string? MersisNumber { get; set; }
    public string? KepAddress { get; set; }

    // Iletisim
    public string ContactPhone { get; set; } = null!;
    public string? WhatsAppNumber { get; set; }
    public string ContactEmail { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? District { get; set; }

    // Sosyal Medya
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? TikTokUrl { get; set; }

    // Analytics
    public string? GoogleAnalyticsId { get; set; }
    public string? GoogleTagManagerId { get; set; }
    public string? FacebookPixelId { get; set; }

    // Duyuru
    public string? AnnouncementBarText { get; set; }
    public bool AnnouncementBarActive { get; set; }
    public string? AnnouncementBarColor { get; set; }

    // Yasal (admin-authored HTML, sanitized at write-time in SP-6)
    public string? AboutHtml { get; set; }
    public string? ReturnPolicyHtml { get; set; }
    public string? PrivacyPolicyHtml { get; set; }
    public string? TermsHtml { get; set; }
    public string? KvkkHtml { get; set; }
    public string? CookiePolicyHtml { get; set; }
    public string? DistanceSalesContractHtml { get; set; }
    public string? PreInfoFormHtml { get; set; }
    public string? DeliveryTermsHtml { get; set; }

    // SEO
    public string? DefaultSeoTitle { get; set; }
    public string? DefaultSeoDescription { get; set; }
    public string? DefaultSeoKeywords { get; set; }

    // Kargo
    public decimal FreeShippingThreshold { get; set; }
    public decimal FlatShippingRate { get; set; }
    public int EstimatedDeliveryDays { get; set; } = 3;

    // Genel
    public bool IsMaintenanceMode { get; set; }
    public string? MaintenanceMessage { get; set; }
    public bool CookieConsentActive { get; set; } = true;
    public bool IsWhatsAppWidgetActive { get; set; }
    public bool NewsletterEnabled { get; set; }
}
```

- [ ] **Step 3: Create StorefrontDomainMapping entity**

```csharp
// Application/Entegrasyon.Entity/Storefront/StorefrontDomainMapping.cs
namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontDomainMapping : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string DomainName { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public SslStatus SslStatus { get; set; }
    public DateTimeOffset? SslExpiresAt { get; set; }
}
```

- [ ] **Step 4: Create StorefrontBanner entity**

```csharp
// Application/Entegrasyon.Entity/Storefront/StorefrontBanner.cs
namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontBanner : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public BannerPosition Position { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 5: Create StorefrontPage entity**

```csharp
// Application/Entegrasyon.Entity/Storefront/StorefrontPage.cs
namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontPage : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    // Admin-authored HTML, sanitized at write-time in SP-6
    public string ContentHtml { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }
    public bool ShowInNavigation { get; set; }
    public bool ShowInFooter { get; set; }
}
```

- [ ] **Step 6: Add SEO fields to existing entities**

Add to `Application/Entegrasyon.Entity/Products/Product.cs` before closing brace:
```csharp
    // SEO
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoKeywords { get; set; }
```

Add to `Application/Entegrasyon.Entity/Categories/Category.cs` before closing brace:
```csharp
    // SEO
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoKeywords { get; set; }
```

Add to `Application/Entegrasyon.Entity/Brands/Brand.cs` before `Products`:
```csharp
    public string? SeoSlug { get; set; }
```

- [ ] **Step 7: Verify build**

Run: `dotnet build Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 8: Commit**

```bash
git add Application/Entegrasyon.Entity/
git commit -m "feat(storefront): add storefront entities and SEO fields"
```

---

### Task 3: Entity Configurations + DbContext + Migration

**Files:**
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/StorefrontSettingsEntityConfiguration.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/StorefrontDomainMappingEntityConfiguration.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/StorefrontBannerEntityConfiguration.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/StorefrontPageEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/ProductEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/CategoryEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/BrandEntityConfiguration.cs`

- [ ] **Step 1: Create all 4 entity configurations**

```csharp
// StorefrontSettingsEntityConfiguration.cs
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontSettingsEntityConfiguration : IEntityTypeConfiguration<StorefrontSettings>
{
    public void Configure(EntityTypeBuilder<StorefrontSettings> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.Property(x => x.StoreName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CompanyTaxOffice).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CompanyTaxNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ContactPhone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
        builder.Property(x => x.City).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PrimaryColor).IsRequired().HasMaxLength(10);
        builder.Property(x => x.FreeShippingThreshold).HasPrecision(18, 2);
        builder.Property(x => x.FlatShippingRate).HasPrecision(18, 2);
    }
}
```

```csharp
// StorefrontDomainMappingEntityConfiguration.cs
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontDomainMappingEntityConfiguration : IEntityTypeConfiguration<StorefrontDomainMapping>
{
    public void Configure(EntityTypeBuilder<StorefrontDomainMapping> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.DomainName).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.DomainName).IsRequired().HasMaxLength(253);
    }
}
```

```csharp
// StorefrontBannerEntityConfiguration.cs
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontBannerEntityConfiguration : IEntityTypeConfiguration<StorefrontBanner>
{
    public void Configure(EntityTypeBuilder<StorefrontBanner> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Position, x.IsActive });
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
    }
}
```

```csharp
// StorefrontPageEntityConfiguration.cs
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontPageEntityConfiguration : IEntityTypeConfiguration<StorefrontPage>
{
    public void Configure(EntityTypeBuilder<StorefrontPage> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContentHtml).IsRequired();
    }
}
```

- [ ] **Step 2: Add SEO field indexes to existing configurations**

Add to ProductEntityConfiguration.cs Configure method:
```csharp
builder.HasIndex(x => x.SeoSlug).IsUnique().HasFilter("\"SeoSlug\" IS NOT NULL");
```

Add to CategoryEntityConfiguration.cs Configure method:
```csharp
builder.HasIndex(x => x.SeoSlug).IsUnique().HasFilter("\"SeoSlug\" IS NOT NULL");
```

Add to BrandEntityConfiguration.cs Configure method:
```csharp
builder.HasIndex(x => x.SeoSlug).IsUnique().HasFilter("\"SeoSlug\" IS NOT NULL");
```

- [ ] **Step 3: Add DbSets to IntegrationDbContext**

Add using at top:
```csharp
using Entegrasyon.Entity.Storefront;
```

Add DbSets:
```csharp
    public virtual DbSet<StorefrontSettings> StorefrontSettings { get; set; }
    public virtual DbSet<StorefrontDomainMapping> StorefrontDomainMappings { get; set; }
    public virtual DbSet<StorefrontBanner> StorefrontBanners { get; set; }
    public virtual DbSet<StorefrontPage> StorefrontPages { get; set; }
```

- [ ] **Step 4: Create migration**

Run:
```bash
dotnet ef migrations add AddStorefrontFoundation -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor
```
Expected: Migration file created

- [ ] **Step 5: Verify build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.DataAccess/
git commit -m "feat(storefront): add entity configurations, DbSets, and migration"
```

---

### Task 4: Tenant Context & Resolver with Unit Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IStorefrontTenantContext.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IStorefrontTenantResolver.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Storefront/StorefrontTenantContext.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Storefront/StorefrontTenantResolver.cs`
- Create: `Test/Entegrasyon.Test/Storefront/StorefrontTenantResolverTests.cs`

- [ ] **Step 1: Write failing test for tenant resolver**

See spec doc section 2 for full test code. Create `Test/Entegrasyon.Test/Storefront/StorefrontTenantResolverTests.cs` with these tests:
- `ResolveAsync_KnownDomain_ReturnsTenantInfo`
- `ResolveAsync_UnknownDomain_ReturnsNull`
- `ResolveAsync_InactiveDomain_ReturnsNull`
- `ResolveAsync_CachesResult_OnSecondCall`
- `InvalidateCache_ClearsEntry`

Full test code is in the design document section 2. Each test uses Mock<IDbContextFactory<IntegrationDbContext>>, Mock<IntegrationDbContext>, and real MemoryCache.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~StorefrontTenantResolverTests" --no-build 2>&1 || true`
Expected: FAIL — classes don't exist yet

- [ ] **Step 3: Create interfaces and implementations**

Create these files with the code from the design document:
- `Application/Entegrasyon.Business/Abstract/IStorefrontTenantContext.cs` — includes `StorefrontTenantInfo` record + `IStorefrontTenantContext` interface
- `Application/Entegrasyon.Business/Abstract/IStorefrontTenantResolver.cs` — `ResolveAsync` + `InvalidateCache`
- `Application/Entegrasyon.Business/Concrete/Storefront/StorefrontTenantContext.cs` — scoped, throws if not initialized
- `Application/Entegrasyon.Business/Concrete/Storefront/StorefrontTenantResolver.cs` — IMemoryCache + IDbContextFactory, 10 min TTL

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~StorefrontTenantResolverTests"`
Expected: All 5 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/ Test/Entegrasyon.Test/Storefront/
git commit -m "feat(storefront): add tenant context and resolver with unit tests"
```

---

### Task 5: Tenant Resolution Middleware + Program.cs

**Files:**
- Create: `Application/Entegrasyon.Storefront/Middleware/TenantResolutionMiddleware.cs`
- Modify: `Application/Entegrasyon.Storefront/Program.cs`

- [ ] **Step 1: Create middleware and update Program.cs**

Full code in design document sections 5 (middleware) and 1 (Program.cs). Key behaviors:
- Skip static file paths (`/css`, `/js`, `/images`, `/favicon.ico`)
- Resolve hostname via `IStorefrontTenantResolver`
- Return 404 for unknown domains, 503 with `Retry-After` for maintenance mode
- Initialize `IStorefrontTenantContext` on success

- [ ] **Step 2: Verify build**

Run: `dotnet build Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Storefront/
git commit -m "feat(storefront): add tenant resolution middleware and route config"
```

---

### Task 6: Storefront Business Managers with Unit Tests

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IStorefrontSettingsManager.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IStorefrontPageManager.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IStorefrontBannerManager.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Storefront/StorefrontSettingsManager.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Storefront/StorefrontPageManager.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Storefront/StorefrontBannerManager.cs`
- Create: `Test/Entegrasyon.Test/Storefront/StorefrontSettingsManagerTests.cs`
- Create: `Test/Entegrasyon.Test/Storefront/StorefrontPageManagerTests.cs`

- [ ] **Step 1: Write failing tests**

Full test code in design document. Key tests:
- `StorefrontSettingsManagerTests`: GetByTenantIdAsync existing/nonexisting
- `StorefrontPageManagerTests`: GetBySlugAsync existing/nonexisting, GetPublishedPagesAsync filters correctly

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "Storefront" --no-build 2>&1 || true`
Expected: FAIL

- [ ] **Step 3: Create interfaces and implementations**

Full code in design document. All managers use `IDbContextFactory<IntegrationDbContext>` primary constructor DI. Follow existing `BrandService` pattern.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "Storefront"`
Expected: All tests PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/ Test/Entegrasyon.Test/Storefront/
git commit -m "feat(storefront): add settings, page, and banner managers with tests"
```

---

### Task 7: DI Registration

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Add AddStorefrontServices extension method**

Add to end of `ApplicationBootstrapExtensions` class:

```csharp
        public static IServiceCollection AddStorefrontServices(this IServiceCollection services)
        {
            services.AddSingleton<IStorefrontTenantResolver, StorefrontTenantResolver>();
            services.AddScoped<IStorefrontTenantContext, StorefrontTenantContext>();
            services.AddScoped<IStorefrontSettingsManager, StorefrontSettingsManager>();
            services.AddScoped<IStorefrontPageManager, StorefrontPageManager>();
            services.AddScoped<IStorefrontBannerManager, StorefrontBannerManager>();
            return services;
        }
```

Add using: `using Entegrasyon.Business.Concrete.Storefront;`

- [ ] **Step 2: Verify build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/
git commit -m "feat(storefront): register storefront services in DI container"
```

---

### Task 8: JsonLdBuilder + SeoMetaTagHelper with Tests

**Files:**
- Create: `Application/Entegrasyon.Storefront/Infrastructure/JsonLdBuilder.cs`
- Create: `Application/Entegrasyon.Storefront/TagHelpers/SeoMetaTagHelper.cs`
- Create: `Test/Entegrasyon.Test/Storefront/JsonLdBuilderTests.cs`
- Modify: `Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`

- [ ] **Step 1: Write failing tests for JsonLdBuilder**

Tests: `BuildStore_ValidSettings_ReturnsValidJsonLd`, `BuildBreadcrumb_MultipleItems_ReturnsValidBreadcrumbList`
Full code in design document.

- [ ] **Step 2: Add Storefront project reference to test project**

Add to `Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`:
```xml
    <ProjectReference Include="..\..\Application\Entegrasyon.Storefront\Entegrasyon.Storefront.csproj" />
```

- [ ] **Step 3: Create JsonLdBuilder and SeoMetaTagHelper**

Full code in design document. JsonLdBuilder is a static helper. SeoMetaTagHelper is a Razor TagHelper that generates title + meta + OG + Twitter card tags with proper HTML encoding.

- [ ] **Step 4: Run tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "JsonLdBuilder"`
Expected: All 2 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Storefront/ Test/Entegrasyon.Test/
git commit -m "feat(storefront): add JsonLdBuilder and SeoMetaTagHelper with tests"
```

---

### Task 9: Tailwind Setup + Layout

**Files:**
- Create: `Application/Entegrasyon.Storefront/package.json`
- Create: `Application/Entegrasyon.Storefront/tailwind.config.js`
- Create: `Application/Entegrasyon.Storefront/Styles/input.css`
- Create: `Application/Entegrasyon.Storefront/wwwroot/css/site.css`
- Create: `Application/Entegrasyon.Storefront/wwwroot/js/site.js`
- Create: `Application/Entegrasyon.Storefront/Views/_ViewImports.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/_ViewStart.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Create Tailwind config files**

Full code in design document. Key: CSS custom properties `--color-primary/secondary/accent` referenced via Tailwind's `var()` support.

- [ ] **Step 2: Install dependencies and build CSS**

Run:
```bash
cd /home/baturhan/Projeler/Entegrasyon/Application/Entegrasyon.Storefront && npm install && npx tailwindcss -i ./Styles/input.css -o ./wwwroot/css/site.css --minify
```

- [ ] **Step 3: Create View infrastructure and _Layout.cshtml**

Full code in design document. Layout includes: announcement bar, header, footer (4-column), WhatsApp FAB, cookie consent banner, scroll-to-top button. All data from `IStorefrontTenantContext`.

- [ ] **Step 4: Create minimal site.js**

Scroll-to-top button only. Full code in design document.

- [ ] **Step 5: Verify build**

Run: `dotnet build Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Storefront/
git commit -m "feat(storefront): add Tailwind setup, master layout, and CSS custom properties"
```

---

### Task 10: Controllers + Views

**Files:**
- Create: `Application/Entegrasyon.Storefront/Controllers/HomeController.cs`
- Create: `Application/Entegrasyon.Storefront/Controllers/PageController.cs`
- Create: `Application/Entegrasyon.Storefront/Controllers/SeoController.cs`
- Create: `Application/Entegrasyon.Storefront/Controllers/ErrorController.cs`
- Create: `Application/Entegrasyon.Storefront/Views/Home/Index.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Page/Show.cshtml`
- Create: `Application/Entegrasyon.Storefront/Views/Error/Index.cshtml`

- [ ] **Step 1: Create all controllers**

Full code in design document:
- `HomeController`: renders index with Store JSON-LD, hero banners
- `PageController`: resolves legal pages from settings fields or custom pages from DB
- `SeoController`: tenant-aware robots.txt and sitemap.xml
- `ErrorController`: 404/403/500/503 status pages

- [ ] **Step 2: Create all views**

Full code in design document:
- `Home/Index.cshtml`: hero banner, trust badges, welcome section, newsletter
- `Page/Show.cshtml`: title + sanitized HTML content
- `Error/Index.cshtml`: status code + message + back link

- [ ] **Step 3: Verify build**

Run: `dotnet build Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Storefront/
git commit -m "feat(storefront): add controllers, views, and page templates"
```

---

### Task 11: Full Build + Test Verification

**Files:** None (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded. 0 Error(s)

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All tests pass (existing + new storefront tests)

- [ ] **Step 3: Run storefront-specific tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Storefront"`
Expected: All pass:
- StorefrontTenantResolverTests (5)
- StorefrontSettingsManagerTests (2)
- StorefrontPageManagerTests (3)
- JsonLdBuilderTests (2)

- [ ] **Step 4: Run integration tests (if Docker available)**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: All existing tests still pass (no regression)

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "feat(storefront): SP-1 foundation complete"
```
