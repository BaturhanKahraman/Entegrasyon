---
name: aspnet-mvc-htmx
description: Entegrasyon projesi MVC ve Storefront katmanı için ASP.NET Core 10 + Razor + Tabler UI + HTMX desenleri. Controller/feature folder yapısı, PRG akışı, AutoValidationFilter, IExceptionHandler zinciri, custom Tag Helpers (form-group, require-role, nav-active), TempData/ViewData extensions, Mapperly DTO mapping, Manager/Interface (IXxxManager/XxxManager) pattern, FluentValidation + LogicRunner pipeline (Validation -> BusinessRules -> Execution), Primary Constructor DI, EF Core no-tracking + BaseEntity + multi-tenant, IApplicationLogManager + ILogger cift loglama, EventChannel publisher/subscriber, antiforgery, ConcurrentDictionary tenant cache. Bu projeye MVC view, controller, partial, filter, exception handler, tag helper, business manager veya Tabler bileseni eklerken/duzenlerken kullan.
---

# ASP.NET Core MVC + HTMX (Entegrasyon)

Entegrasyon projesinin MVC ve Storefront katmanlarinda kullanilan tum yerlesik desenler. Yeni controller, view, partial, filter, exception handler, tag helper, business manager veya migration eklerken bu skill'i takip et. Genel ASP.NET MVC dokumantasyonu icin Microsoft Learn lookup'larina basvur (asagidaki tabloya bak).

## Tech Stack

- **ASP.NET Core 10 MVC** (port 5100) + Cookie Auth
- **Razor + Tabler UI** (https://tabler.io/docs/) + HTMX (Vanilla JS, npm yok)
- **EF Core** + PostgreSQL, default no-tracking, `BaseEntity` (`IsDeleted`, `CreatedAt`, `UpdatedAt`, `DeletedAt`)
- **Mapperly** (source-generator) DTO mapping
- **FluentValidation** + projenin kendi `LogicRunner`'i
- **Feature Folders:** `Features/<FeatureName>/{Controller.cs, ViewModels/, Views/}` (FeatureViewLocationExpander var)
- **TR-locale frontend lib'leri:** Tom Select, Flatpickr, IMask, Notyf, SortableJS, GLightbox

## Mutlaka Uyulacak Strict Rule'lar

1. **Business Pipeline (3 adim, sira sabit):**
   1. **Validation:** `await validator.ValidateAndThrowAsync(dto)` (FluentValidation)
   2. **Business Rules:** `LogicRunner.Run(...)` ile is kurallari
   3. **Execution:** Sadece iki adim gectiyse DB / dis sistem etkilesimi
   Bkz: [sample_codes/business/business-manager.cs](sample_codes/business/business-manager.cs)

2. **Cift loglama (her business islemde):**
   - `IApplicationLogManager.AddLog(...)` -> kullanici-facing, TR, admin dashboard'da gorunur
   - `ILogger<T>` -> developer-facing, exception/stack trace, kullanici gormez
   - Asla karistirma. Bkz: [sample_codes/business/business-manager.cs](sample_codes/business/business-manager.cs)

3. **Tabler bilesen kullanim oncesi:** Class isimlerini tahmin etme. https://tabler.io/docs/ui/<component> sayfasini ac, dogru class kombinasyonunu kullan. Ornek: `badge bg-green` yetmez -> solid icin `badge bg-green text-green-fg`, light icin `badge bg-green-lt`. Global CSS override yerine Tabler kombinasyonunu kullan.

4. **TDD-first:** Yeni feature/bug fix -> once test (RED) -> implementation (GREEN) -> refactor -> Unit + Integration + E2E hepsi gecmeli. Test olmadan tamamlanmis sayilmaz.

5. **EF migration zorunlu:** Entity / DbContext degisirse `dotnet ef migrations add ...` + `database update` + `has-pending-model-changes` ile dogrula. Migration yoksa tamamlanmis sayilmaz.

6. **Multi-tenant:** Singleton state -> `ConcurrentDictionary<int, T>`. SemaphoreSlim per-tenant. DB query'lerinde tenant filtresi. Hardcoded config yok. "Bu N tenant ile calisir mi?" sor.

7. **Antiforgery:** Tum POST/PUT/DELETE form'larinda `@Html.AntiForgeryToken()` veya Form Tag Helper (`<form method="post">` otomatik token ekler).

## Project Skeleton

```
Application/Entegrasyon.MVC/
  Program.cs
  Features/<Name>/
    <Name>Controller.cs          # Primary constructor DI
    ViewModels/                  # <Name>Vm.cs (Vm suffix zorunlu — AutoValidationFilter "Vm" ile bitleri bulur)
    Views/
      Index.cshtml, Create.cshtml, Edit.cshtml, Detail.cshtml
      Partials/_Form.cshtml, _List.cshtml, _<Name>Row.cshtml
  Infrastructure/
    Extensions/   (HtmxExtensions, TempDataExtensions, ViewDataExtensions, FeatureViewLocationExpander)
    Filters/      (AutoValidationFilter, TenantActionFilter, SkipAutoValidationAttribute)
    ExceptionHandlers/ (BusinessRuleExceptionHandler, HtmxExceptionHandler, ValidationExceptionHandler)
    Middleware/   (TenantResolutionMiddleware, ApiKeyAuthenticationMiddleware)
    Validation/   (TurkishValidationMetadataProvider)
  Shared/
    TagHelpers/   (FormGroupTagHelper, ActiveNavTagHelper, PermissionGuardTagHelper)
    Views/_Layout.cshtml, _ViewImports.cshtml, _ValidationScriptsPartial.cshtml
  wwwroot/lib/    (tabler, htmx, tom-select, flatpickr, imask, notyf, sortablejs, glightbox)
```

## Yerlesik Pattern'ler (kisa referans)

### 1. Controller (Primary Constructor + PRG + HTMX)

```csharp
[Authorize]
public class ProductController(
    IProductService productService,
    IBrandService brandService) : Controller
{
    [HttpGet("/products")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Urunler");
        ViewData.SetActiveNav("products");

        var result = await productService.GetPageable(new SearchablePageDto(search ?? "", page - 1, 20));

        // Ayni endpoint, iki davranis
        if (Request.IsHtmx())
            return PartialView("Partials/_ProductTable", result.Data);

        ViewBag.Search = search;
        return View(result.Data);
    }

    [HttpPost("/products/add")]
    public async Task<IActionResult> Add(CreateProductVm vm)
    {
        // AutoValidationFilter ModelState'i otomatik handle eder; buraya gelen veri valid demektir.
        var dto = mapper.MapToDto(vm);
        var result = await productService.Add(dto);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Islem basarisiz.");
            return RedirectToAction(nameof(Index));     // PRG
        }

        TempData.SetSuccess("Urun olusturuldu.");
        return RedirectToAction(nameof(Index));         // PRG — geri tusu guvenli
    }
}
```

Detayli: [sample_codes/controllers/feature-controller.cs](sample_codes/controllers/feature-controller.cs)

### 2. AutoValidationFilter (PRG redirect)

POST/PUT ister, ModelState invalid'se:
- **HTMX** -> `Partials/_Form` partial'i ModelState ile render
- **Normal** -> Hata mesajini `TempData.SetError(...)`'a yazip `Referer`'a `Redirect` (View(model) yapip null model'le NRE almamak icin)
- `[SkipAutoValidation]` attribute'u koyarsan filter skip eder (wizard step'lerinde kullan)

Tam kaynak: [sample_codes/filters/AutoValidationFilter.cs](sample_codes/filters/AutoValidationFilter.cs)

### 3. IExceptionHandler Zinciri (sira onemli)

`Program.cs`'te kayit sirasi: `BusinessRuleExceptionHandler` -> `HtmxExceptionHandler` -> default `ProblemDetails`.

- **BusinessRuleException** (`InvalidOperationException` + `RULE:` prefix) -> 422, HTMX'te inline `alert-warning`, normal'de `application/problem+json`
- **HtmxExceptionHandler** -> sadece `HX-Request` header'i varsa, 500, inline `alert-danger`
- **Fallback** -> `AddProblemDetails()` + `UseExceptionHandler()` -> default RFC 7807 JSON

.NET 10 davranisi: `TryHandleAsync` `true` donerse diagnostics suppress edilir (degistirmek istersen `ExceptionHandlerOptions.SuppressDiagnosticsCallback = ctx => false`).

Tam kaynak: [sample_codes/exception-handlers/](sample_codes/exception-handlers/)

### 4. Tag Helpers

- `<form-group asp-for="Title" type="text" placeholder="Urun adi" />` -> Tabler `.mb-3` div + label + input + validation span
- `<a nav-active="products">...</a>` -> `ViewData.GetActiveNav()` ile match olursa class'a `active` ekler
- `<button require-role="Admin">...</button>` -> `User.IsInRole` false ise `output.SuppressOutput()`
- `<a require-permission="Permissions.Products.View">` -> Admin bypass + `User.HasClaim("Permission", ...)` kontrol

Yeni Tag Helper yazarken:
1. `Microsoft.AspNetCore.Razor.TagHelpers.TagHelper`'den turet
2. `[HtmlTargetElement(...)]` ile element/attribute hedefle (kebab-case otomatik)
3. `Process` veya `ProcessAsync` override et
4. `Views/_ViewImports.cshtml`'ye `@addTagHelper *, Entegrasyon.MVC` ekli oldugundan emin ol
5. `ViewContext` icin: `[HtmlAttributeNotBound][ViewContext]` property

Tam kaynak: [sample_codes/tag-helpers/](sample_codes/tag-helpers/)

### 5. ViewData / TempData Extensions

```csharp
// Controller'da
ViewData.SetPageTitle("Urunler");
ViewData.SetActiveNav("products");
ViewData.SetActiveNavGroup("catalog");
ViewData.SetBreadcrumb(("Urunler", "/products"), ("Detay", null));
ViewData.AddBanner("warning", "Stok azaliyor", id: "low-stock");

TempData.SetSuccess("Olusturuldu.");
TempData.SetError("Bulunamadi.");
TempData.SetWarning("Dikkat.");
TempData.AddAlert("info", "Bilgi");

// Layout'ta okuma
var title = ViewData.GetPageTitle();
var toast = TempData.GetToast();   // ToastMessage? (Type, Text)
```

Toast/banner JSON serialize edilir; layout sayfasi okur Notyf'a basar.

### 6. HTMX Extensions

```csharp
if (Request.IsHtmx()) return PartialView(...);
if (Request.IsHtmxBoosted()) { ... }
var target = Request.HtmxTarget();           // HX-Target
var trigger = Request.HtmxTriggerName();     // HX-Trigger-Name

Response.HtmxRedirect("/products");          // HX-Redirect (tum sayfa nav)
Response.HtmxRefresh();                       // HX-Refresh
Response.HtmxTrigger("productSaved");         // HX-Trigger event
Response.HtmxTriggerWithData("productSaved", new { id = 5 });
Response.HtmxReswap("outerHTML");             // HX-Reswap
Response.HtmxRetarget("#main");               // HX-Retarget
Response.HtmxPushUrl("/products/5");          // HX-Push-Url
```

### 6b. Loading State / Cift-Submit Onleme (GLOBAL — alaskanlik)

Async submit'lerde kullanicinin "Kaydet"e iki kez basip cift-submit yapmasini
ONLE. Bu projede iki kat var:

**1. HTMX formlari (cogunluk) — OTOMATIK, ekstra is GEREKMEZ.**
`wwwroot/js/site.js`'te global bir hook var: `htmx:beforeRequest`'te istegi
tetikleyen form/element icindeki submit buton(lar)i otomatik `disabled` olur;
istegi tetikleyen butona Tabler `.btn-loading` (spinner; label gizli ama buton
genisligi sabit) eklenir. `htmx:afterRequest` / `sendError` / `timeout` /
`responseError` / `abort`'ta hepsi eski haline doner. Yani **herhangi bir
`hx-post`/`hx-get` formuna yeni buton koyarken hicbir sey yapmana gerek yok** —
disable+spinner kendiliginden gelir. (Top progress bar `#htmx-progress` ayri
calisir; ona dokunma.)

**2. Raw `fetch()` / JS-submit (htmx DISI) — ELLE yap.**
Global hook yalnizca htmx olaylarini dinler; `fetch()` ile gonderdigin yerde
butonu KENDIN kilitle. Ayni gorsel desen:

```js
btn.disabled = true;
btn.classList.add('btn-loading');   // Tabler: spinner + label gizli, genislik sabit
fetch(url, { method: 'POST', body: formData })
    .then(...)
    .catch(function () { showNotify('Islem basarisiz. Tekrar deneyin.', 'error'); })
    .finally(function () { btn.disabled = false; btn.classList.remove('btn-loading'); });
```

`.catch` + `.finally` SART — yoksa hata aninda buton kilitli kalir.
Ornek: `Features/Products/Views/Partials/_CreateStep3Variants.cshtml`
("Varyantlari Olustur") ve `_CreateStep1.cshtml` (marka hizli-ekle).

**Class dogrulama:** Buton loading icin Tabler `.btn-loading` (pure CSS).
Inline spinner gerekiyorsa `<span class="spinner-border spinner-border-sm me-2"
role="status"></span>`. Tahmin etme — `tabler-ui` skill'inden dogrula.

### 7. Business Manager (Validation -> Rules -> Execution + Cift Log)

```csharp
public class ProductManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IFluentValidator validator,
    ILogger<ProductManager> logger) : IProductService
{
    public async Task<IDataResult<Product>> Add(AddProductDto dto)
    {
        // 0. Cift log: kullanici-facing + dev-facing
        await applicationLogManager.AddLog("Urun ekleniyor.", LogType.Product, LogAction.Add, dto);
        logger.LogInformation("Adding product {StockCode}", dto.StockCode);

        // 1. Validation (FluentValidation)
        await validator.ValidateAndThrowAsync(dto);

        // 2. Business Rules (LogicRunner)
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var stockConflict = await dbContext.MainProducts
            .AnyAsync(p => p.StockCode == dto.StockCode && !p.IsDeleted);
        if (stockConflict)
            return new ErrorDataResult<Product>(null!, "Bu stok kodu zaten kullaniliyor.");

        var check = LogicRunner.Run(
            stockManager.CheckIfProductCountZero(dto.Variants)
        );
        if (check != null) return new ErrorDataResult<Product>(null!, check.Message!);

        // 3. Execution
        var product = mapper.MapToEntity(dto);
        dbContext.MainProducts.Add(product);
        await dbContext.SaveChangesAsync();   // BaseEntity timestamps + UTC otomatik

        return new SuccessDataResult<Product>(product);
    }
}
```

Manager/Interface: `Abstract/IProductService.cs` + `Concrete/ProductManager.cs`. DI kaydi `ApplicationDependencyExtension.AddApplicationDependencies()`'da.

### 8. Multi-Tenant ConcurrentDictionary Cache

```csharp
public class TrendyolTokenCache
{
    private readonly ConcurrentDictionary<int, (string Token, DateTime ExpiresAt)> _cache = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

    public async Task<string> GetTokenAsync(int tenantId, Func<Task<string>> factory)
    {
        if (_cache.TryGetValue(tenantId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Token;

        var sem = _locks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            if (_cache.TryGetValue(tenantId, out cached) && cached.ExpiresAt > DateTime.UtcNow)
                return cached.Token;
            var token = await factory();
            _cache[tenantId] = (token, DateTime.UtcNow.AddMinutes(50));
            return token;
        }
        finally { sem.Release(); }
    }
}
```

Global tek field veya tek lock -> kabul edilmez.

### 9. EventChannel Publisher/Subscriber

```csharp
// publisher
await eventChannel.Writer.WriteAsync(new CategoryUpdatedEvent(categoryId), ct);

// subscriber (BackgroundService)
await foreach (var evt in eventChannel.Reader.ReadAllAsync(stoppingToken))
{
    await handler.HandleAsync(evt, stoppingToken);
}
```

Channel'lar `ApplicationDependencyExtension.AddApplicationDependencies()`'da `EventChannel<T>` singleton olarak kayitli.

### 10. View Discovery (FeatureViewLocationExpander)

Standart MVC view discovery'ye ek olarak `Features/<Controller>/Views/<Action>.cshtml` ve `Features/<Controller>/Views/Partials/<Name>.cshtml` aranir. `_ViewImports.cshtml` zaten tum tag helper'lari register eder. Yeni Tag Helper assembly varsa `@addTagHelper *, Entegrasyon.MVC` ile expose et.

## Razor View Cheatsheet

```cshtml
@model CreateProductVm
@{
    ViewData.SetPageTitle("Yeni Urun");
    ViewData.SetActiveNav("products");
    ViewData.SetBreadcrumb(("Urunler", "/products"), ("Yeni", null));
}

<form asp-action="Add" method="post" hx-boost="true">
    @* method="post" -> Form Tag Helper antiforgery token'i otomatik ekler *@
    <form-group asp-for="Title" placeholder="Urun adi" />
    <form-group asp-for="StockCode" />
    <button class="btn btn-primary" type="submit">Kaydet</button>
</form>

<partial name="Partials/_VariantTable" model="@Model.Variants" />

<button require-permission="Permissions.Products.Delete"
        class="btn btn-danger"
        hx-delete="/products/@Model.Id"
        hx-confirm="Silinsin mi?">
    Sil
</button>
```

## Yeni Feature Eklerken Checklist

1. `Features/<Name>/` klasoru olustur
2. `<Name>Controller.cs` (Primary constructor, `[Authorize]`, route prefix)
3. `ViewModels/<Action><Name>Vm.cs` (Vm suffix!) + opsiyonel FluentValidator
4. `Views/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Partials/_Form.cshtml`
5. Business: `Abstract/I<Name>Service.cs` + `Concrete/<Name>Manager.cs` (Validation -> Rules -> Execution + cift log)
6. Entity + DataAccess: `Entity/<Name>.cs : BaseEntity`, config -> migration -> update
7. DI kaydi `ApplicationDependencyExtension`
8. **Testler:** Unit + Integration + E2E (TDD-first)
9. Tabler bilesenleri kullanmadan once docs ac

## Common Mistakes (yapma)

- `if (!ModelState.IsValid) return View(vm)` — AutoValidationFilter zaten halleder; tek View(vm) cagrisi null model'le 500 verir. PRG'ye guven.
- `View(model)` cagrisinda model null gecirme — `@Model.Id` NRE atar. `RedirectToAction` veya zengin model dondur.
- `await dbContext.SaveChangesAsync()` icinde domain event publish ederken transaction acmamak — `dbContext.AddDomainEvent(...)` deseni kullan.
- Singleton manager'da `private string _token` — multi-tenant'ta tum tenant'lar ayni token gorur. `ConcurrentDictionary<int,_>` zorunlu.
- Tabler `badge bg-red` (light expectation) — `badge bg-red text-red-fg` (solid) veya `badge bg-red-lt` (light) tam kombinasyon.
- POST form'unda antiforgery token unutmak — `<form method="post">` Tag Helper otomatik ekler; manuel form'da `@Html.AntiForgeryToken()` zorunlu.
- `[ApiController]` MVC view controller'a koymak — otomatik 400'e dusurur, view donmez.
- Migration olmadan entity degistirip "duzelttim" demek — strict rule ihlali.
- Raw `fetch()` ile submit yapip butonu kilitlememek — cift-submit riski. Elle `disabled` + `.btn-loading` + `.catch`/`.finally` (htmx formlarda global hook otomatik halleder; bkz. 6b).

## Tabler UI Quick Picks

| Ihtiyac | Tabler class |
|---|---|
| Sayfa ust banner | `alert alert-important alert-info` |
| Solid badge | `badge bg-green text-green-fg` |
| Light badge | `badge bg-green-lt` |
| Form input wrapper | `mb-3` div + `form-label` + `form-control` |
| Card with header | `card` > `card-header` + `card-body` |
| Empty state | `empty` div |
| Status dot | `status-dot status-green` |
| Steps (wizard) | `steps steps-counter` + `step-item` |
| Modal | `modal modal-blur fade` (Bootstrap 5) |

Daima docs ac: https://tabler.io/docs/ui/<component>

## Learn More (Microsoft Learn Lookup)

| Konu | Nasil bul |
|---|---|
| Tag Helper authoring | `microsoft_docs_search(query="ASP.NET Core author tag helpers ProcessAsync")` |
| Filter pipeline detay | `microsoft_docs_search(query="ASP.NET Core filters order execution")` |
| Model binding | `microsoft_docs_search(query="ASP.NET Core model binding complex types")` |
| ViewComponent | `microsoft_docs_search(query="ASP.NET Core view components Invoke")` |
| Custom validation | `microsoft_docs_search(query="ASP.NET Core IValidatableObject custom attribute")` |
| Antiforgery + AJAX | `microsoft_docs_fetch(url="https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0")` |
| IExceptionHandler | `microsoft_docs_fetch(url="https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0")` |
| ProblemDetails | `microsoft_docs_search(query="ASP.NET Core AddProblemDetails CustomizeProblemDetails")` |
| Razor syntax | `microsoft_docs_search(query="ASP.NET Core Razor syntax C#")` |
| EF Core no-tracking | `microsoft_docs_search(query="EF Core AsNoTracking QueryTrackingBehavior")` |

## CLI Alternative

Learn MCP server yoksa `mslearn` CLI:

| MCP Tool | CLI |
|---|---|
| `microsoft_docs_search(query: "...")` | `mslearn search "..."` |
| `microsoft_code_sample_search(query: "...", language: "csharp")` | `mslearn code-search "..." --language csharp` |
| `microsoft_docs_fetch(url: "...")` | `mslearn fetch "..."` |

`npx @microsoft/learn-cli <command>` veya `npm install -g @microsoft/learn-cli`.

## References (deep dive)

- [references/htmx-patterns.md](references/htmx-patterns.md) — HTMX request/response header, partial swap, hx-trigger event flow
- [references/business-pipeline.md](references/business-pipeline.md) — LogicRunner, Result/DataResult tipleri, FluentValidation entegrasyonu, cift log akisi
- [references/multi-tenant.md](references/multi-tenant.md) — ITenantContext, ConcurrentDictionary cache, TenantActionFilter, claims akisi
- [references/tabler-cheatsheet.md](references/tabler-cheatsheet.md) — Yaygin Tabler bilesen class kombinasyonlari (DAIMA docs ac)
