# SP-1: Storefront Foundation — Tasarim Dokumani

## Ozet

Storefront platformunun temel altyapisini olusturur: ASP.NET Core MVC projesi, tenant resolution middleware, Default tema (Tailwind CSS), SEO altyapisi, ve temel entity'ler. Bu foundation uzerine SP-2 ile SP-8 arasi tum alt projeler insa edilecek.

## Kapsam

**Dahil:**
- MVC proje scaffold (Entegrasyon.Storefront)
- Tenant resolution middleware (domain -> tenant)
- 4 yeni entity (StorefrontSettings, StorefrontDomainMapping, StorefrontBanner, StorefrontPage)
- Mevcut entity'lere SEO alanlari (Product, Category, Brand)
- Default tema (Tailwind CSS, CSS Custom Properties ile dinamik renk)
- Master layout (_Layout.cshtml) + ViewComponent'lar (Header, Footer, MegaMenu, AnnouncementBar, Breadcrumb)
- SEO altyapisi (SeoMetaTagHelper, JsonLdBuilder, SeoController — robots.txt, sitemap.xml)
- Hata sayfalari (404, 500) + bakim modu
- 3 yeni business manager (Settings, Page, Banner)
- Response cache + IMemoryCache katmanli cache
- Unit + integration testler

**Haric (sonraki SP'ler):**
- Urun listeleme/detay UI (SP-2)
- Uyelik/auth sistemi (SP-3)
- Sepet/odeme (SP-4)
- Email bildirimleri (SP-5)
- Dashboard wizard (SP-6)
- Kargo/fatura (SP-7)
- Kupon/pazarlama (SP-8)

## Mimari Karar: Yaklasim A

Mevcut solution'a yeni MVC projesi olarak eklenir. Entity, DataAccess, Business katmanlari paylasilan. AdminPanel ile ayni pattern.

```
Entegrasyon.sln
├── Application/Entegrasyon.Entity        (paylasilan)
├── Application/Entegrasyon.DataAccess    (paylasilan)
├── Application/Entegrasyon.Business      (paylasilan + yeni Storefront manager'lar)
├── Application/Entegrasyon.ApplicationBootstrap (paylasilan + AddStorefrontServices)
├── Application/Entegrasyon.Blazor        (mevcut dashboard)
├── Application/Entegrasyon.Storefront    (YENI: ASP.NET Core MVC)
└── Application/Entegrasyon.AdminPanel    (mevcut admin panel)
```

---

## 1. Proje Yapisi

```
Application/Entegrasyon.Storefront/
├── Program.cs
├── appsettings.json
├── Entegrasyon.Storefront.csproj
├── tailwind.config.js
├── package.json
├── Styles/
│   └── input.css
├── Middleware/
│   └── TenantResolutionMiddleware.cs
├── Controllers/
│   ├── HomeController.cs
│   ├── CatalogController.cs        (SP-2'de icerik dolar, SP-1'de iskelet)
│   ├── PageController.cs
│   ├── SeoController.cs
│   └── ErrorController.cs
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _Header.cshtml
│   │   ├── _Footer.cshtml
│   │   ├── _AnnouncementBar.cshtml
│   │   ├── _CookieConsent.cshtml
│   │   ├── _JsonLd.cshtml
│   │   └── _Error.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Page/
│   │   └── Legal.cshtml
│   └── Error/
│       ├── NotFound.cshtml
│       └── ServerError.cshtml
├── TagHelpers/
│   └── SeoMetaTagHelper.cs
├── ViewComponents/
│   ├── HeaderViewComponent.cs
│   ├── FooterViewComponent.cs
│   ├── MegaMenuViewComponent.cs
│   ├── AnnouncementBarViewComponent.cs
│   └── BreadcrumbViewComponent.cs
├── Infrastructure/
│   └── JsonLdBuilder.cs
└── wwwroot/
    ├── css/site.css
    ├── js/
    │   ├── search.js
    │   ├── cookie-consent.js
    │   ├── ui.js
    │   └── slider.js
    └── favicon.ico
```

### csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Entegrasyon.ApplicationBootstrap\Entegrasyon.ApplicationBootstrap.csproj" />
  </ItemGroup>
  <Target Name="TailwindBuild" BeforeTargets="Build">
    <Exec Command="npx tailwindcss -i ./Styles/input.css -o ./wwwroot/css/site.css --minify" />
  </Target>
</Project>
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddCustomDbContext(builder.Configuration);
builder.Services.AddStorefrontServices();
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
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000"
});
app.UseResponseCaching();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseRouting();
app.UseStatusCodePagesWithReExecute("/hata/{0}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
```

---

## 2. Tenant Resolution Middleware

### TenantResolutionMiddleware

```csharp
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IStorefrontTenantResolver tenantResolver,
        IStorefrontTenantContext tenantContext)
    {
        // Statik dosyalar icin skip
        if (context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/images"))
        {
            await next(context);
            return;
        }

        var host = context.Request.Host.Host;
        var tenantInfo = await tenantResolver.ResolveAsync(host);

        if (tenantInfo is null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (tenantInfo.Settings.IsMaintenanceMode)
        {
            context.Response.StatusCode = 503;
            context.Response.Headers.RetryAfter = "3600";
            return;
        }

        tenantContext.Initialize(tenantInfo);
        await next(context);
    }
}
```

### IStorefrontTenantContext (Scoped)

```csharp
public interface IStorefrontTenantContext
{
    int TenantId { get; }
    StorefrontSettings Settings { get; }
    StorefrontDomainMapping Domain { get; }
    void Initialize(StorefrontTenantInfo info);
}

public record StorefrontTenantInfo(
    int TenantId,
    StorefrontSettings Settings,
    StorefrontDomainMapping Domain);
```

### IStorefrontTenantResolver (Singleton + IMemoryCache)

```csharp
public interface IStorefrontTenantResolver
{
    Task<StorefrontTenantInfo?> ResolveAsync(string hostname);
    void InvalidateCache(int tenantId);
}
```

Cache: IMemoryCache, 10 dakika TTL. Dashboard'dan settings degistiginde EventChannel ile invalidate.

---

## 3. Entity'ler

### Yeni Entity'ler

**StorefrontSettings : BaseEntity** — Tenant bazli magaza ayarlari
- TenantId (unique), ThemeId, StoreName, StoreSlogan
- LogoUrl, FaviconUrl, PrimaryColor, SecondaryColor, AccentColor, CustomCss
- CompanyName, CompanyTaxOffice, CompanyTaxNumber, MersisNumber, KepAddress
- ContactPhone, WhatsAppNumber, ContactEmail, Address, City, District
- Instagram/Facebook/Twitter/YouTube/TikTokUrl
- GoogleAnalyticsId, GoogleTagManagerId, FacebookPixelId
- AnnouncementBarText, AnnouncementBarActive, AnnouncementBarColor
- Yasal HTML metinleri (About, Return, Privacy, Terms, Kvkk, Cookie, DistanceSales, PreInfo, Delivery)
- DefaultSeoTitle, DefaultSeoDescription, DefaultSeoKeywords
- FreeShippingThreshold, FlatShippingRate, EstimatedDeliveryDays
- IsMaintenanceMode, MaintenanceMessage
- CookieConsentActive, IsWhatsAppWidgetActive, NewsletterEnabled

**StorefrontDomainMapping : BaseEntity** — Domain -> Tenant eslestirme
- TenantId, DomainName (unique), IsPrimary, IsActive
- SslStatus (enum: Pending/Active/Expired/Failed), SslExpiresAt

**StorefrontBanner : BaseEntity** — Hero slider banner'lari
- TenantId, Title, ImageUrl, MobileImageUrl, LinkUrl
- Position (enum: Hero/Sidebar/Footer/Popup), DisplayOrder
- StartDate, EndDate, IsActive

**StorefrontPage : BaseEntity** — Ozel sayfalar
- TenantId, Title, Slug (unique per tenant), ContentHtml
- SeoTitle, SeoDescription
- IsPublished, DisplayOrder, ShowInNavigation, ShowInFooter

### Mevcut Entity'lere SEO Alanlari (Migration)

```
Product  -> + SeoTitle, SeoDescription, SeoSlug (unique), SeoKeywords
Category -> + SeoTitle, SeoDescription, SeoSlug (unique), SeoKeywords
Brand    -> + SeoSlug (unique)
```

### Entity Configurations

Her entity icin IEntityTypeConfiguration<T>:
- StorefrontSettings: HasQueryFilter(!IsDeleted), unique index TenantId
- StorefrontDomainMapping: unique index DomainName, index TenantId
- StorefrontBanner: index (TenantId, Position, IsActive)
- StorefrontPage: composite unique index (TenantId, Slug)
- Product: unique index SeoSlug (where not null)
- Category: unique index SeoSlug (where not null)
- Brand: unique index SeoSlug (where not null)

### DbContext Ekleme

IntegrationDbContext'e 4 yeni DbSet eklenir.

---

## 4. Tailwind & Tema Sistemi

### CSS Custom Properties ile Dinamik Renk

_Layout.cshtml'de tenant settings'den inline style:
```css
:root {
  --color-primary: {PrimaryColor};
  --color-secondary: {SecondaryColor};
  --color-accent: {AccentColor};
}
```

tailwind.config.js'de `var()` referanslari:
```js
colors: {
  primary: 'var(--color-primary)',
  secondary: 'var(--color-secondary)',
  accent: 'var(--color-accent)',
}
```

Tek CSS dosyasi tum tenant'lara hizmet eder. CDN cache dostu.

### Layout Yapisi

```
AnnouncementBar -> Header -> MegaMenu -> Breadcrumb -> @RenderBody() -> Footer
+ WhatsApp FAB (sag alt, floating)
+ CookieConsent (alt banner, ilk ziyaret)
+ ScrollToTop (scroll sonrasi gorunur)
+ Mobile Bottom Nav (<768px)
```

### ViewComponent'lar

| Component | Veri Kaynagi | Cache |
|-----------|-------------|-------|
| HeaderViewComponent | StorefrontSettings (logo, ad) | 10dk |
| FooterViewComponent | StorefrontSettings + StorefrontPage (footer linkleri) | 10dk |
| MegaMenuViewComponent | Category tree (ilk 2 seviye) | 30dk |
| AnnouncementBarViewComponent | StorefrontSettings | 10dk |
| BreadcrumbViewComponent | ViewData'dan | Yok |

### JS Butcesi

| Dosya | Amac | Boyut |
|-------|------|-------|
| search.js | Arama autocomplete | ~3KB |
| cookie-consent.js | Cerez onay | ~1KB |
| ui.js | Hamburger, scroll-to-top, dismiss | ~2KB |
| slider.js | Hero banner | ~3KB |
| **Toplam** | | **< 10KB** |

### Responsive

- Desktop (>=1024px): 4 sutun grid, mega menu hover
- Tablet (768-1023): 2 sutun, hamburger
- Mobile (<768px): 1-2 sutun, hamburger, bottom nav bar

---

## 5. SEO Altyapisi

### SeoController

- GET /robots.txt -> tenant-aware robots, 1 saat cache
- GET /sitemap.xml -> dinamik sitemap (urunler, kategoriler, markalar, sayfalar), 1 saat cache

### SeoMetaTagHelper

Custom TagHelper: `<seo-meta title="..." description="..." image="..." canonical="..." />`
Render: title, meta description, canonical, OG tags, Twitter card

### JsonLdBuilder (Static Helper)

- BuildStore(settings) -> Store schema
- BuildBreadcrumb(items) -> BreadcrumbList schema
- BuildProduct(product, price, reviews) -> Product schema (SP-2'de kullanilir)
- BuildCollectionPage(category) -> CollectionPage schema (SP-2'de kullanilir)

### URL Route'lari

```
/                           -> HomeController.Index
/kategori/{slug}            -> CatalogController.Category
/kategori/{parent}/{slug}   -> CatalogController.Category (alt kategori)
/urun/{slug}                -> CatalogController.Product
/marka/{slug}               -> CatalogController.Brand
/arama?q=...                -> CatalogController.Search
/urunler                    -> CatalogController.AllProducts
/{slug}                     -> PageController.Legal (yasal + ozel sayfalar)
/robots.txt                 -> SeoController.Robots
/sitemap.xml                -> SeoController.Sitemap
/hata/{statusCode}          -> ErrorController.Index
```

---

## 6. Business Layer

### Yeni Manager'lar

**IStorefrontSettingsManager / StorefrontSettingsManager**
- GetByTenantIdAsync(tenantId)
- CreateOrUpdateAsync(dto)
- UpdateLegalTextAsync(tenantId, field, html)
- ToggleMaintenanceModeAsync(tenantId, enabled, message)

**IStorefrontPageManager / StorefrontPageManager**
- GetPublishedPagesAsync(tenantId)
- GetBySlugAsync(tenantId, slug)
- CreateAsync(dto), UpdateAsync(id, dto), DeleteAsync(id)

**IStorefrontBannerManager / StorefrontBannerManager**
- GetActiveBannersAsync(tenantId, position)
- CreateAsync(dto), UpdateAsync(id, dto), DeleteAsync(id)
- ReorderAsync(tenantId, orderedIds)

### DI Kaydi

ApplicationDependencyExtension.cs'e AddStorefrontServices() extension method eklenir.

### Mevcut Manager Kullanimi

ICategoryService, IProductService, IBrandService, IOfficeStockManager dogrudan kullanilir.

---

## 7. Cache Stratejisi

### Katmanli Cache

**Response Cache (HTTP):** Ana sayfa 60sn, kategori/urun 5dk, yasal 1 saat, statik 1 yil
**IMemoryCache (Application):** Tenant settings 10dk, kategori agaci 30dk, banner 10dk
**EF Core:** No-tracking (mevcut)

### Cache Invalidation

Dashboard'dan degisiklik -> EventChannel<StorefrontSettingsUpdatedEvent> -> StorefrontTenantResolver.InvalidateCache(tenantId)

---

## 8. Test Stratejisi

### Unit Tests (Test/Entegrasyon.Test/Storefront/)

- StorefrontSettingsManagerTests
- StorefrontPageManagerTests
- StorefrontBannerManagerTests
- StorefrontTenantResolverTests
- JsonLdBuilderTests
- SeoMetaTagHelperTests

### Integration Tests (Test/Entegrasyon.IntegrationTest/Storefront/)

- TenantResolutionIntegrationTests
- StorefrontSettingsIntegrationTests
- StorefrontPageIntegrationTests

### Kritik Senaryolar

| Test | Assert |
|------|--------|
| Bilinmeyen domain | 404 |
| Bakim modunda tenant | 503 + Retry-After header |
| Gecerli domain | 200 + title tenant adini icerir |
| /robots.txt | tenant domain'ini icerir |
| /sitemap.xml | aktif urun slug'lari mevcut |
| SEO meta tag helper | title, og:title, canonical dogru |
| JSON-LD Store | gecerli JSON, @type: Store |
| Cache invalidation | update sonrasi yeni deger |
| Page slug uniqueness | ayni slug + ayni tenant = hata |

---

## 9. Sonraki Adimlar

SP-1 tamamlandiktan sonra siradaki alt projeler:
- **SP-2:** Urun Vitrin & Katalog (ana sayfa icerik, kategori listeleme, urun detay, arama, filtreler)
- **SP-3:** Uyelik & Auth (kayit, giris, email dogrulama, Google OAuth, hesap paneli)
- **SP-4:** Sepet & Odeme (sepet, checkout, iyzico 3D Secure, siparis)
