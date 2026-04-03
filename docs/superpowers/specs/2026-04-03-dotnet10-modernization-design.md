# .NET 10 Modernizasyon Tasarımı

## Bağlam

Proje `b79c0fe` commit'inde .NET 8'den .NET 10'a mekanik olarak geçirildi (TFM bump + paket güncelleme). Ancak .NET 10'un sunduğu yeni dil özellikleri, framework API'leri ve mimari iyileştirmeler henüz benimsenmedi. Bu tasarım, projeyi gerçek anlamda .NET 10-native hale getirmeyi hedefler.

**Strateji:** Kategori bazlı ayrı commit'ler — review kolay, rollback güvenli, git history temiz.

---

## Commit 1: Kod Kalitesi & Bug Fix

### 1.1 Test Projeleri net10.0'a Geçiş
4 test projesi hâlâ `net8.0`:
- `Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj`
- `Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj`
- `Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
- `Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj`

Değişiklik: `<TargetFramework>net8.0</TargetFramework>` → `net10.0` + ilgili paket versiyonları 10.0.0'a

### 1.2 Async Disposal Fix
`IDbContextFactory<T>.CreateDbContextAsync()` → `IAsyncDisposable` döner. Mevcut `using var` → `await using var` olmalı.

**Etkilenen dosyalar:** Tüm `Application/Entegrasyon.Business/Concrete/` altındaki manager'lar (`CategoryManager.cs`, `CustomerManager.cs`, `BrandMatchService.cs` vb.)

### 1.3 CustomerManager Primary Constructor
`CustomerManager.cs` (satır 18-31): Eski stil `private readonly` field + constructor body → primary constructor.

### 1.4 Navigation Collection Düzeltmesi
- `Order.cs:14` — `IEnumerable<OrderItem>` → `ICollection<OrderItem>` (EF Core change tracking + Add() desteği)
- Diğer `IEnumerable<>` navigation property'ler varsa aynı şekilde düzelt

### 1.5 String Karşılaştırma
- `CategoryManager.cs:28` — `.Name.ToLower() == dto.Name.ToLower()` → `string.Equals(x.Name, dto.Name, StringComparison.OrdinalIgnoreCase)` veya EF `EF.Functions.ILike()`

---

## Commit 2: C# 14 Dil Özellikleri

### 2.1 `field` Keyword (Sadece DTO/ViewModel'lerde)
⚠️ Entity sınıflarında KULLANILMAZ — EF Core lazy-loading proxy uyumu belirsiz.

Uygun hedefler:
- DTO property'lerinde setter validation (string trim, null guard)
- ViewModel'lerde computed/validated property'ler

Örnek:
```csharp
// Önce
private string _name = null!;
public string Name { get => _name; set => _name = value?.Trim() ?? ""; }

// Sonra (C# 14)
public string Name { get; set => field = value?.Trim() ?? ""; }
```

### 2.2 Extension Types
⚠️ C# 14 preview — `<LangVersion>preview</LangVersion>` gerekebilir. Stabil değilse ATLANIR.

Hedefler:
- `TempDataExtensions` (SetSuccess, SetError, GetToast)
- `ViewDataExtensions` (SetPageTitle, SetActiveNav, SetBreadcrumb)
- `HttpRequestExtensions` (IsHtmx)

---

## Commit 3: Framework API'leri

### 3.1 HybridCache Entegrasyonu

**Registration** (`Program.cs`):
```csharp
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});
```
Mevcut Redis (`AddStackExchangeRedisCache`) otomatik L2 backend olarak kullanılır.

**Öncelikli hedefler:**

| Manager | Metod | Mevcut Cache | Hedef |
|---|---|---|---|
| CategoryManager | `GetCategoryTreeAsync()` | YOK | HybridCache 30dk |
| MarketPlaceManager | `GetAllAsync()` | YOK | HybridCache 60dk |
| MarketPlaceManager | `GetByIdAsync()` | YOK | HybridCache 60dk |
| CategoryManager | `GetAllCategoriesWithHierarchyAsync()` | YOK | HybridCache 30dk |
| BrandService | `GetBrandListDetails()` | TenantMemoryCache | HybridCache'e migrate |
| CategoryManager | `GetAllCategoriesWithoutAttributesAsync()` | TenantMemoryCache | HybridCache'e migrate |

**Cache key formatı:** `t:{tenantId}:{entity}:{method}` (multi-tenant uyumlu)
**Invalidation:** Tag-based — mutating method'lar `RemoveByTagAsync("categories")` çağırır

### 3.2 LINQ İyileştirmeleri
- `CountBy()` / `AggregateBy()` uygun yerlerde (.NET 9+ BCL)
- `.ToListAsync()` + `.ToHashSet()` iki-adımlı pattern'leri tekli hale getirme

---

## Commit 4: Mimari İyileştirmeler

### 4a. Record-based Result<T> (Kademeli Migrasyon)

~1083 kullanım noktası → tek seferde değiştirilemez.

**Faz A (bu PR):**
1. `Result<T>` readonly record struct oluştur — `Ok()`, `Fail()`, `ValidationFail()` factory method'ları
2. `Result` (non-generic) record struct — `Ok()`, `Fail()`
3. Eski tiplere `[Obsolete("Use Result<T>.Ok() / Result<T>.Fail()")]` ekle
4. Implicit conversion operator'lar ekle: `SuccessDataResult<T>` → `Result<T>`
5. `IResult` / `IDataResult<T>` interface'leri korunur (controller uyumluluğu)

**Faz B (sonraki PR'lar):** Manager bazlı kademeli migrasyon — her manager ayrı PR.

### 4b. Central Package Management

1. `Directory.Packages.props` oluştur (repo root)
2. Tüm `.csproj` dosyalarından version'ları merkeze taşı
3. Tutarsızlıkları çöz:
   - FluentAssertions: 6.12 → 8.9 (tüm projeler)
   - xUnit: 2.4 → 2.7 (tüm projeler)
4. `.csproj`'lardan `Version=` attribute'ları kaldır

### 4c. HTMX Base Controller

```csharp
public abstract class HtmxController(/* DI params */) : Controller
{
    protected IActionResult HtmxView(string viewName, object? model = null)
        => Request.IsHtmx() ? PartialView(viewName, model) : View(viewName, model);

    protected IActionResult HtmxValidationError(string message)
    {
        if (Request.IsHtmx())
        {
            Response.HtmxTriggerWithData("showToast", new { type = "error", message });
            return StatusCode(422);
        }
        TempData.SetError(message);
        return RedirectToAction("Index");
    }
}
```

Mevcut controller'lar `HtmxController`'dan türeyecek, tekrarlayan if/else blokları kaldırılacak.

---

## Doğrulama Planı

1. `dotnet build Entegrasyon.sln` — her commit sonrası
2. `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj` — unit testler
3. `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj` — integration testler
4. `dotnet test Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj` — admin panel testleri
5. `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj` — MVC testleri
6. Runtime test: `cd Application/Entegrasyon.MVC && dotnet run` → tarayıcıda temel akışları doğrula

## Kapsam Dışı (Bu PR'da Yapılmayacak)
- Entity sınıflarında `field` keyword kullanımı (EF Core uyumu belirsiz)
- Collection expressions (`[]`) entity navigation property'lerinde
- `extension type` — C# 14 preview stabilitesi doğrulanana kadar
- Result<T> Faz B (manager bazlı kademeli migrasyon) — ayrı PR'larda
- OpenAPI built-in — proje Swagger/OpenAPI kullanmıyor, ihtiyaç yok
