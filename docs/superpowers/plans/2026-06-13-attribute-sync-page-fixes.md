# Özellik Eşleme Sayfası 3-Bug Düzeltme — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/marketplace/sync/attributes` sayfasındaki üç bug'ı düzelt: credential gating, pazaryeri özelliğinin stage API'den gelmesi (1:1 eşleme zorlamasıyla), eşleşme sayacının tab değişiminde güncellenmesi.

**Architecture:** Yeni `TrendyolAttributeCatalog` servisi Trendyol'a eşli kategorilerin attribute+değerlerini stage API'den (mevcut `IMarketplaceCategoryAttributeProvider`) toplayıp `TenantMemoryCache`'te tutar; `TrendyolMarketplaceSearchService` attribute/değer aramayı buraya yönlendirir. Credential mantığı mevcut `MarketPlace.IsCredentialComplete()` ile view+controller'a taşınır. Sayaç partial'a taşınarak HTMX swap'ine dahil olur. 1:1 ters yön DB unique index + manager guard ile zorlanır.

**Tech Stack:** ASP.NET Core MVC + HTMX + Tabler UI, EF Core (PostgreSQL, no-tracking), xUnit + Moq + FluentAssertions, Testcontainers integration, WireMock (dev stage mock).

**Referans spec:** `docs/superpowers/specs/2026-06-13-attribute-sync-page-fixes-design.md`

---

## File Structure

| Dosya | Sorumluluk |
|---|---|
| `Business/Abstract/ITrendyolAttributeCatalog.cs` | **Yeni** — Trendyol attribute/değer arama arayüzü |
| `Business/Concrete/Trendyol/TrendyolAttributeCatalog.cs` | **Yeni** — eşli kategorilerden topla+dedupe+cache |
| `Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs` | attribute/değer aramayı catalog'a yönlendir |
| `Business/Abstract/IMarketplaceSearchService.cs` | value arama param adı netleşir (`marketplaceAttributeId`) |
| `Business/Concrete/AttributeMatchManager.cs` | ters-yön 1:1 guard |
| `DataAccess/.../EntityConfigurations/CategoryAttributeMarketPlaceMatchEntityConfiguration.cs` | unique index |
| `DataAccess/.../Migrations/*` | yeni migration |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs` | `ITrendyolAttributeCatalog` DI |
| `MVC/Features/MarketplaceSync/ViewModels/AttributeSyncVm.cs` | `AnyCredentialed`, `SelectedHasCredentials` |
| `MVC/Features/MarketplaceSync/AttributeSyncController.cs` | credential hesabı + erken çıkış |
| `MVC/Features/MarketplaceSync/SuggestionsController.cs` | value endpoint imzası |
| `MVC/.../Views/AttributeSync/Index.cshtml` | empty-state + disabled tab + özet barı kaldır |
| `MVC/.../Views/AttributeSync/Partials/_AttributeList.cshtml` | özet bar (sayaç+progress) buraya |
| `MVC/.../Views/AttributeSync/Partials/_AttributeMatchPanel.cshtml` | değer modal Trendyol attr id + disabled |
| `Test/Entegrasyon.Test/Trendyol/TrendyolAttributeCatalogTests.cs` | **Yeni** unit |
| `Test/Entegrasyon.Test/Trendyol/TrendyolMarketplaceSearchServiceTests.cs` | attribute/değer testleri güncelle |
| `Test/Entegrasyon.Test/Business/AttributeMatchManagerTests.cs` | 1:1 guard testi |
| `Test/Entegrasyon.IntegrationTest/Business/AttributeMatchUniqueIndexTests.cs` | **Yeni** unique index integration |

---

## Task 1: 1:1 Ters Yön — Unique Index + Migration

**Files:**
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/CategoryAttributeMarketPlaceMatchEntityConfiguration.cs`
- Create: `Test/Entegrasyon.IntegrationTest/Business/AttributeMatchUniqueIndexTests.cs`
- Create: migration (EF üretir)

- [ ] **Step 1: Integration test yaz (RED)**

`Test/Entegrasyon.IntegrationTest/Business/AttributeMatchUniqueIndexTests.cs`:
```csharp
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

public class AttributeMatchUniqueIndexTests : IntegrationTestBase
{
    private int _attr1Id;
    private int _attr2Id;

    public AttributeMatchUniqueIndexTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        dbContext.MarketPlaces.Add(new MarketPlace { Id = 1, Name = "Trendyol", CreatedAt = DateTimeOffset.UtcNow });

        var attr1 = new CategoryAttribute { CategoryAttributeKey = "color", CategoryAttributeHumanized = "Renk", CreatedAt = DateTimeOffset.UtcNow };
        var attr2 = new CategoryAttribute { CategoryAttributeKey = "maincolor", CategoryAttributeHumanized = "Ana Renk", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.CategoryAttributes.AddRange(attr1, attr2);
        await dbContext.SaveChangesAsync();

        _attr1Id = attr1.Id;
        _attr2Id = attr2.Id;
    }

    [Fact]
    public async Task SameTrendyolAttribute_MappedToTwoOfOurAttributes_Throws()
    {
        // Arrange — aynı Trendyol attribute (348) bizim attr1'e eşli
        using (var ctx = CreateDbContext())
        {
            ctx.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
            {
                ApplicationCategoryAttributeId = _attr1Id, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 348
            });
            await ctx.SaveChangesAsync();
        }

        // Act + Assert — aynı (mp=1, 348) bizim attr2'ye → unique index ihlali
        using var ctx2 = CreateDbContext();
        ctx2.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
        {
            ApplicationCategoryAttributeId = _attr2Id, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 348
        });

        var act = async () => await ctx2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
```

- [ ] **Step 2: Testi koş, FAIL gör (index yok → ikinci insert başarılı oluyor)**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~AttributeMatchUniqueIndexTests"`
Expected: FAIL (DbUpdateException atılmıyor — index yok). SSH Docker tüneli gerektiğinden bu adım kullanıcıya bırakılabilir; en azından kod review ile RED mantığı doğrulanır.

- [ ] **Step 3: Unique index ekle**

`CategoryAttributeMarketPlaceMatchEntityConfiguration.cs`:
```csharp
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryAttributeMarketPlaceMatchEntityConfiguration : IEntityTypeConfiguration<CategoryAttributeMarketPlaceMatch>
{
    public void Configure(EntityTypeBuilder<CategoryAttributeMarketPlaceMatch> builder)
    {
        builder.HasKey(x => new { x.MarketPlaceId, x.ApplicationCategoryAttributeId });

        // 1:1 ters yön — bir pazaryeri özelliği yalnız bir bizim özelliğimize eşlenebilir.
        builder.HasIndex(x => new { x.MarketPlaceId, x.MarketPlaceCategoryAttributeId })
            .IsUnique();
    }
}
```

- [ ] **Step 4: Migration üret + gözden geçir**

Run:
```bash
dotnet ef migrations add AddUniqueTrendyolAttributeMatchIndex -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```
Üretilen migration'ı aç: yalnız `CreateIndex(..., unique: true)` (ve Down'da `DropIndex`) içermeli. Beklenmeyen başka değişiklik varsa DUR, model snapshot drift'i araştır.

- [ ] **Step 5: Migration uygula + drift doğrula**

Run:
```bash
dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
dotnet ef migrations has-pending-model-changes -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```
Expected: update başarılı; has-pending-model-changes → "No changes". (DB bağlantısı yerelde yoksa bu adım kullanıcıya bırakılır; migration dosyası + build yeterli kanıt.)

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.DataAccess Test/Entegrasyon.IntegrationTest/Business/AttributeMatchUniqueIndexTests.cs
git commit -m "feat(attributes): Trendyol özellik eşlemesine 1:1 ters-yön unique index"
```

---

## Task 2: AttributeMatchManager — Ters Yön 1:1 Guard

**Files:**
- Modify: `Application/Entegrasyon.Business/Concrete/AttributeMatchManager.cs:28-50` (`SaveAttributeMatchAsync`)
- Modify: `Test/Entegrasyon.Test/Business/AttributeMatchManagerTests.cs`

- [ ] **Step 1: Failing unit test yaz (RED)**

`AttributeMatchManagerTests.cs` içine ekle:
```csharp
[Fact]
public async Task SaveAttributeMatchAsync_ShouldReturnError_WhenSameMarketplaceAttributeAlreadyMappedToAnother()
{
    // Arrange — Trendyol attr 100 zaten bizim attr 1'e eşli
    var existing = new List<CategoryAttributeMarketPlaceMatch>
    {
        new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 }
    };
    mockIntegrationDbContext
        .Setup(x => x.CategoryAttributeMarketPlaceMatches)
        .ReturnsDbSet(existing);

    // Act — şimdi bizim attr 2'yi de aynı Trendyol attr 100'e eşlemeye çalış
    var result = await _sut.SaveAttributeMatchAsync(2, 1, 100);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Contain("başka bir özelliğinize");
}
```

- [ ] **Step 2: Testi koş, FAIL gör**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaveAttributeMatchAsync_ShouldReturnError_WhenSameMarketplaceAttributeAlreadyMappedToAnother"`
Expected: FAIL (şu an guard yok, kayıt ekleniyor → Success=true).

- [ ] **Step 3: Guard ekle**

`AttributeMatchManager.SaveAttributeMatchAsync` — mevcut "zaten eşli" kontrolünden SONRA, `Add`'den ÖNCE:
```csharp
        if (existing != null)
            return new ErrorResult($"Bu özellik bu marketplace için zaten eşleştirilmiş.");

        var reverseConflict = await dbContext.CategoryAttributeMarketPlaceMatches
            .AnyAsync(x =>
                x.MarketPlaceId == marketPlaceId &&
                x.MarketPlaceCategoryAttributeId == marketplaceAttributeId &&
                x.ApplicationCategoryAttributeId != applicationAttributeId);

        if (reverseConflict)
            return new ErrorResult("Bu pazaryeri özelliği başka bir özelliğinize zaten eşlenmiş.");

        dbContext.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
        // ...
```

- [ ] **Step 4: Testi koş, PASS gör + tüm AttributeMatchManager testleri**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AttributeMatchManagerTests"`
Expected: PASS (tüm testler).

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/AttributeMatchManager.cs Test/Entegrasyon.Test/Business/AttributeMatchManagerTests.cs
git commit -m "feat(attributes): SaveAttributeMatch ters-yön 1:1 guard"
```

---

## Task 3: TrendyolAttributeCatalog — Stage API'den Topla + Cache

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ITrendyolAttributeCatalog.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolAttributeCatalog.cs`
- Create: `Test/Entegrasyon.Test/Trendyol/TrendyolAttributeCatalogTests.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs:162` (DI)

- [ ] **Step 1: Arayüz + cache veri tipini yaz**

`ITrendyolAttributeCatalog.cs`:
```csharp
using Entegrasyon.Entity.Dtos.Marketplace;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Trendyol'a eşli kategorilerin attribute + değerlerini stage API'den toplayıp
/// (id+ad ile dedupe) tenant-cache'te tutar. Özellik eşleme dropdown'larını besler.
/// </summary>
public interface ITrendyolAttributeCatalog
{
    Task<IReadOnlyList<MarketplaceAttributeSearchResult>> SearchAttributesAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<MarketplaceOption>> SearchValuesAsync(int marketplaceAttributeId, string query, CancellationToken ct = default);
}
```

- [ ] **Step 2: Failing unit test yaz (RED)**

`TrendyolAttributeCatalogTests.cs`:
```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Trendyol;

public class TrendyolAttributeCatalogTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICategoryMatchService> _categoryMatch = new();
    private readonly Mock<IMarketplaceCategoryAttributeProvider> _provider = new();
    private readonly TenantMemoryCache _cache;

    public TrendyolAttributeCatalogTests()
    {
        _cache = new TenantMemoryCache(new MemoryCache(new MemoryCacheOptions()), mockTenantContext.Object);
        _provider.Setup(p => p.MarketPlaceId).Returns(1);
    }

    private TrendyolAttributeCatalog CreateSut() => new(
        _categoryMatch.Object,
        new[] { _provider.Object },
        _cache,
        Mock.Of<ILogger<TrendyolAttributeCatalog>>());

    private void SetupMappedCategories(params int[] trendyolCategoryIds)
    {
        _categoryMatch
            .Setup(s => s.GetAllCategoryMappingsAsync(1))
            .ReturnsAsync(trendyolCategoryIds.Select(id => new CategoryMarketplaceMappingDto
            {
                ApplicationCategoryId = id,
                MarketPlaceId = 1,
                MarketPlaceCategoryId = id
            }).ToList());
    }

    private void SetupCategoryAttributes(int categoryId, params MarketplaceAttributeDto[] attrs)
    {
        _provider
            .Setup(p => p.GetAttributesForCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceAttributeDto>>(attrs.ToList()));
    }

    [Fact]
    public async Task SearchAttributesAsync_AggregatesAndDedupesAcrossMappedCategories()
    {
        // Arrange — iki kategori, ikisinde de "Renk" (348) tekrar ediyor
        SetupMappedCategories(388, 400);
        SetupCategoryAttributes(388,
            new MarketplaceAttributeDto(348, "Renk", true, false, []),
            new MarketplaceAttributeDto(338, "Beden", true, false, []));
        SetupCategoryAttributes(400,
            new MarketplaceAttributeDto(348, "Renk", true, false, []));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchAttributesAsync("");

        // Assert — Renk + Beden, dedupe sonrası 2
        result.Should().HaveCount(2);
        result.Select(a => a.Id).Should().BeEquivalentTo(new[] { 348, 338 });
    }

    [Fact]
    public async Task SearchAttributesAsync_FiltersByNameCaseInsensitive()
    {
        SetupMappedCategories(388);
        SetupCategoryAttributes(388,
            new MarketplaceAttributeDto(348, "Renk", true, false, []),
            new MarketplaceAttributeDto(338, "Beden", true, false, []));

        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync("renk");

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Renk");
    }

    [Fact]
    public async Task SearchAttributesAsync_NoMappedCategories_ReturnsEmpty()
    {
        SetupMappedCategories(); // hiç eşli kategori yok

        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync("renk");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchValuesAsync_ReturnsValuesForGivenMarketplaceAttribute()
    {
        SetupMappedCategories(388);
        SetupCategoryAttributes(388,
            new MarketplaceAttributeDto(348, "Renk", true, false, new()
            {
                new MarketplaceAttributeValueDto(1001, "Mavi"),
                new MarketplaceAttributeValueDto(1002, "Kırmızı")
            }));

        var sut = CreateSut();

        var result = await sut.SearchValuesAsync(348, "kır");

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1002);
        result[0].Name.Should().Be("Kırmızı");
    }

    [Fact]
    public async Task SearchAttributesAsync_UsesCache_ProviderCalledOncePerCategory()
    {
        SetupMappedCategories(388);
        SetupCategoryAttributes(388, new MarketplaceAttributeDto(348, "Renk", true, false, []));

        var sut = CreateSut();

        await sut.SearchAttributesAsync("a");
        await sut.SearchAttributesAsync("b");

        _provider.Verify(p => p.GetAttributesForCategoryAsync(388, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 3: Testi koş, FAIL gör (sınıf yok → derlenmiyor)**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolAttributeCatalogTests"`
Expected: FAIL (compile error / sınıf yok).

- [ ] **Step 4: TrendyolAttributeCatalog implement et**

`TrendyolAttributeCatalog.cs`:
```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class TrendyolAttributeCatalog(
    ICategoryMatchService categoryMatchService,
    IEnumerable<IMarketplaceCategoryAttributeProvider> providers,
    TenantMemoryCache cache,
    ILogger<TrendyolAttributeCatalog> logger) : ITrendyolAttributeCatalog
{
    private const string CacheKey = "TrendyolAttributeCatalog";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<MarketplaceAttributeSearchResult>> SearchAttributesAsync(
        string query, CancellationToken ct = default)
    {
        var data = await GetCatalogAsync(ct);
        IEnumerable<MarketplaceAttributeSearchResult> attrs = data.Attributes;

        if (!string.IsNullOrWhiteSpace(query))
            attrs = attrs.Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        return attrs.Take(20).ToList();
    }

    public async Task<IReadOnlyList<MarketplaceOption>> SearchValuesAsync(
        int marketplaceAttributeId, string query, CancellationToken ct = default)
    {
        var data = await GetCatalogAsync(ct);
        if (!data.ValuesByAttributeId.TryGetValue(marketplaceAttributeId, out var values))
            return [];

        IEnumerable<MarketplaceOption> result = values;
        if (!string.IsNullOrWhiteSpace(query))
            result = result.Where(v => v.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        return result.Take(20).ToList();
    }

    private async Task<CatalogData> GetCatalogAsync(CancellationToken ct)
    {
        if (cache.TryGetValue<CatalogData>(CacheKey, out var cached) && cached is not null)
            return cached;

        var built = await BuildCatalogAsync(ct);
        cache.Set(CacheKey, built, Ttl);
        return built;
    }

    private async Task<CatalogData> BuildCatalogAsync(CancellationToken ct)
    {
        var provider = providers.FirstOrDefault(p => p.MarketPlaceId == TrendyolMarketPlaceId);
        if (provider is null)
        {
            logger.LogWarning("Trendyol attribute provider bulunamadı.");
            return CatalogData.Empty;
        }

        var mappings = await categoryMatchService.GetAllCategoryMappingsAsync(TrendyolMarketPlaceId);
        var categoryIds = mappings
            .Select(m => m.MarketPlaceCategoryId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0)
            return CatalogData.Empty;

        var attrById = new Dictionary<int, string>();
        var valuesByAttr = new Dictionary<int, Dictionary<int, string>>();

        // Sınırlı paralellik — stage API'yi boğmadan eşli kategorileri tara.
        using var gate = new SemaphoreSlim(4);
        var tasks = categoryIds.Select(async catId =>
        {
            await gate.WaitAsync(ct);
            try
            {
                return await provider.GetAttributesForCategoryAsync(catId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Trendyol kategori {CategoryId} attribute çekilemedi.", catId);
                return null;
            }
            finally { gate.Release(); }
        });

        var results = await Task.WhenAll(tasks);

        foreach (var res in results)
        {
            if (res is not { Success: true, Data: { } attrs }) continue;
            foreach (var a in attrs)
            {
                attrById[a.Id] = a.Name;
                if (!valuesByAttr.TryGetValue(a.Id, out var vmap))
                    valuesByAttr[a.Id] = vmap = new Dictionary<int, string>();
                foreach (var v in a.Values)
                    vmap[v.Id] = v.Name;
            }
        }

        var attributes = attrById
            .Select(kv => new MarketplaceAttributeSearchResult(kv.Key, kv.Value))
            .ToList();

        var values = valuesByAttr.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<MarketplaceOption>)kv.Value
                .Select(v => new MarketplaceOption(v.Key, v.Value))
                .ToList());

        return new CatalogData(attributes, values);
    }

    private sealed record CatalogData(
        IReadOnlyList<MarketplaceAttributeSearchResult> Attributes,
        IReadOnlyDictionary<int, IReadOnlyList<MarketplaceOption>> ValuesByAttributeId)
    {
        public static readonly CatalogData Empty = new([], new Dictionary<int, IReadOnlyList<MarketplaceOption>>());
    }
}
```

> **NOT (cache tipi):** `TenantMemoryCache.Set/TryGetValue` generic; private nested record cache'lenebilir. `MarketPlaceConstants.TrendyolMarketPlaceId` sabitinin var olduğunu doğrula (TrendyolApiClient `using static ...MarketPlaceConstants` + `TrendyolMarketPlaceId` kullanıyor → mevcut).

- [ ] **Step 5: DI kaydı ekle**

`ApplicationDependencyExtension.cs:162` civarı, provider kaydının altına:
```csharp
            services.AddScoped<IMarketplaceCategoryAttributeProvider, TrendyolCategoryAttributeProvider>();
            services.AddScoped<ITrendyolAttributeCatalog, TrendyolAttributeCatalog>();
```

- [ ] **Step 6: Testi koş, PASS gör**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolAttributeCatalogTests"`
Expected: PASS (5 test).

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ITrendyolAttributeCatalog.cs Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolAttributeCatalog.cs Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs Test/Entegrasyon.Test/Trendyol/TrendyolAttributeCatalogTests.cs
git commit -m "feat(attributes): TrendyolAttributeCatalog — eşli kategorilerden stage API attribute toplama + cache"
```

---

## Task 4: SearchService'i Catalog'a Yönlendir + Mevcut Testleri Güncelle

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IMarketplaceSearchService.cs:11`
- Modify: `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs`
- Modify: `Test/Entegrasyon.Test/Trendyol/TrendyolMarketplaceSearchServiceTests.cs`

- [ ] **Step 1: Interface value param adını netleştir**

`IMarketplaceSearchService.cs` satır 11:
```csharp
    Task<IDataResult<List<MarketplaceOption>>> SearchAttributeValuesAsync(int marketPlaceId, int marketplaceAttributeId, string query, CancellationToken ct = default);
```
(eski `attributeId` → `marketplaceAttributeId`; anlam artık Trendyol attribute id.)

- [ ] **Step 2: Mevcut attribute/değer testlerini yeni davranışa göre yeniden yaz (RED)**

`TrendyolMarketplaceSearchServiceTests.cs`:
- Sınıfa `ITrendyolAttributeCatalog` mock'u ekle, `CreateSut()`'a beşinci arg olarak ver:
```csharp
    private readonly Mock<ITrendyolAttributeCatalog> _attributeCatalogMock = new();

    private TrendyolMarketplaceSearchService CreateSut() => new(
        mockContextFactory.Object,
        _categoryImportMock.Object,
        _httpClientFactoryMock.Object,
        _attributeCatalogMock.Object,
        _loggerMock.Object);
```
- `SearchAttributesAsync` bölümündeki 2 testi (satır ~278-320) ŞUNLARLA değiştir:
```csharp
    [Fact]
    public async Task SearchAttributesAsync_Trendyol_DelegatesToCatalog()
    {
        _attributeCatalogMock
            .Setup(c => c.SearchAttributesAsync("Renk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MarketplaceAttributeSearchResult> { new(348, "Renk") });

        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync(1, "Renk");

        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(348);
    }

    [Fact]
    public async Task SearchAttributesAsync_NonTrendyol_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync(2, "Renk");

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
```
- `SearchAttributeValuesAsync` bölümündeki 2 testi (satır ~326-371) ŞUNLARLA değiştir:
```csharp
    [Fact]
    public async Task SearchAttributeValuesAsync_Trendyol_DelegatesToCatalog()
    {
        _attributeCatalogMock
            .Setup(c => c.SearchValuesAsync(348, "Kır", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MarketplaceOption> { new(1002, "Kırmızı") });

        var sut = CreateSut();

        var result = await sut.SearchAttributeValuesAsync(1, marketplaceAttributeId: 348, query: "Kır");

        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(1002);
    }

    [Fact]
    public async Task SearchAttributeValuesAsync_NonTrendyol_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.SearchAttributeValuesAsync(2, marketplaceAttributeId: 348, query: "Kır");

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
```
- Kullanılmayan `using Entegrasyon.Entity.Categories;` ve `using Entegrasyon.Entity.Dtos.Marketplace;` durumunu derleyiciye göre düzelt (MarketplaceAttributeSearchResult/Option için `Entegrasyon.Entity.Dtos.Marketplace` gerekir).

- [ ] **Step 3: Testi koş, FAIL gör (ctor 5 arg değil / catalog yok)**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolMarketplaceSearchServiceTests"`
Expected: FAIL (compile — ctor imzası uyuşmuyor).

- [ ] **Step 4: SearchService'i güncelle**

`TrendyolMarketplaceSearchService.cs` ctor'a `ITrendyolAttributeCatalog attributeCatalog` ekle:
```csharp
public sealed class TrendyolMarketplaceSearchService(
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ITrendyolCategoryImportService categoryImportService,
    IHttpClientFactory httpClientFactory,
    ITrendyolAttributeCatalog attributeCatalog,
    ILogger<TrendyolMarketplaceSearchService> logger) : IMarketplaceSearchService
```

`SearchAttributesAsync` gövdesini değiştir (satır 64-86):
```csharp
    public async Task<IDataResult<List<MarketplaceAttributeSearchResult>>> SearchAttributesAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        if (marketPlaceId != TrendyolMarketPlaceId)
            return new SuccessDataResult<List<MarketplaceAttributeSearchResult>>([]);

        try
        {
            var attrs = await attributeCatalog.SearchAttributesAsync(query, ct);
            return new SuccessDataResult<List<MarketplaceAttributeSearchResult>>(attrs.ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol özellik arama hatası");
            return new ErrorDataResult<List<MarketplaceAttributeSearchResult>>([], "Özellik arama sırasında hata oluştu.");
        }
    }
```

`SearchAttributeValuesAsync` gövdesini değiştir (satır 88-110), param adını da güncelle:
```csharp
    public async Task<IDataResult<List<MarketplaceOption>>> SearchAttributeValuesAsync(
        int marketPlaceId, int marketplaceAttributeId, string query, CancellationToken ct = default)
    {
        if (marketPlaceId != TrendyolMarketPlaceId)
            return new SuccessDataResult<List<MarketplaceOption>>([]);

        try
        {
            var values = await attributeCatalog.SearchValuesAsync(marketplaceAttributeId, query, ct);
            return new SuccessDataResult<List<MarketplaceOption>>(values.ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol özellik değeri arama hatası. AttributeId={AttributeId}", marketplaceAttributeId);
            return new ErrorDataResult<List<MarketplaceOption>>([], "Özellik değeri arama sırasında hata oluştu.");
        }
    }
```

> `TrendyolMarketPlaceId` sabiti sınıfta zaten tanımlı (satır 24). Artık kullanılmayan `Microsoft.EntityFrameworkCore` using'i SearchService'te başka yerde (SearchBrandsFromDbAsync) hâlâ gerektiği için KALIR.

- [ ] **Step 5: Testi koş, PASS gör**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolMarketplaceSearchServiceTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IMarketplaceSearchService.cs Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs Test/Entegrasyon.Test/Trendyol/TrendyolMarketplaceSearchServiceTests.cs
git commit -m "feat(attributes): attribute/değer aramasını TrendyolAttributeCatalog'a yönlendir"
```

---

## Task 5: SuggestionsController — Value Endpoint İmzası

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/MarketplaceSync/SuggestionsController.cs:62-76`

- [ ] **Step 1: Value endpoint'i mpAttributeId query param'a çevir**

`AttributeValues` action'ını değiştir:
```csharp
    [HttpGet("/marketplace/sync/suggestions/attributes/values")]
    public async Task<IActionResult> AttributeValues(int mp = 1, int mpAttributeId = 0, string q = "", CancellationToken ct = default)
    {
        if (mpAttributeId <= 0)
            return Ok(Array.Empty<object>());

        var result = await searchService.SearchAttributeValuesAsync(mp, mpAttributeId, q, ct);
        if (!result.Success)
            return Ok(Array.Empty<object>());

        var items = result.Data.Select(v => new
        {
            id = v.Id,
            name = v.Name,
            label = v.Name
        });
        return Ok(items);
    }
```
(Route `{attributeId:int}/values` → query-param tabanlı `values`; `attributeId` artık Trendyol attr id ve `mpAttributeId` olarak gelir.)

- [ ] **Step 2: Build (derleme doğrulama)**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 error. (View JS Task 9'da güncellenecek; bu adımda controller derleniyor.)

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/MarketplaceSync/SuggestionsController.cs
git commit -m "feat(attributes): değer suggestions endpoint'i mpAttributeId query param"
```

---

## Task 6: Bug #1 — VM + Controller Credential Mantığı

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/MarketplaceSync/ViewModels/AttributeSyncVm.cs:7-17`
- Modify: `Application/Entegrasyon.MVC/Features/MarketplaceSync/AttributeSyncController.cs:17-52`

- [ ] **Step 1: VM alanları ekle**

`AttributeSyncVm.cs` — class `AttributeSyncVm` içine:
```csharp
public class AttributeSyncVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public List<MarketPlace> MarketPlaces { get; set; } = [];
    public List<AttributeListItemVm> Attributes { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? ReturnUrl { get; set; }

    /// <summary>En az bir pazaryeri credential'ı tam mı (sayfa aktif mi).</summary>
    public bool AnyCredentialed { get; set; }

    /// <summary>Seçili pazaryeri credential'ı tam mı.</summary>
    public bool SelectedHasCredentials { get; set; }

    /// <summary>Sayfa açılışında detayı otomatik yüklenecek özellik.</summary>
    public int? PreselectAttributeId { get; set; }
}
```

- [ ] **Step 2: Controller'da credential hesapla + erken çıkış**

`AttributeSyncController.Index` (using zaten `Entegrasyon.Entity` içeriyor mu doğrula — `IsCredentialComplete` extension `Entegrasyon.Entity` namespace'inde; gerekirse `using Entegrasyon.Entity;` ekle). Gövdeyi şu sıraya getir:
```csharp
    [HttpGet("/marketplace/sync/attributes")]
    public async Task<IActionResult> Index(int mp = 1, string? search = null, string? returnUrl = null, int? attributeId = null)
    {
        ViewData.SetPageTitle("Özellik Eşlemesi");
        ViewData.SetActiveNav("marketplace-sync");

        var marketPlacesResult = await marketPlaceManager.GetAllAsync();
        var marketPlaces = (marketPlacesResult.Data ?? []).Where(m => !m.IsDeleted).ToList();

        var credentialed = marketPlaces.Where(m => m.IsCredentialComplete()).ToList();
        var anyCredentialed = credentialed.Count > 0;

        // Hiç credential yoksa: boş VM, view full-page empty-state gösterir.
        if (!anyCredentialed)
        {
            return View($"{ViewBase}/Index.cshtml", new AttributeSyncVm
            {
                SelectedMarketPlaceId = mp,
                MarketPlaces = marketPlaces,
                AnyCredentialed = false,
                SelectedHasCredentials = false,
                SearchTerm = search,
                ReturnUrl = returnUrl,
                PreselectAttributeId = attributeId
            });
        }

        // Seçili mp credential'sızsa ilk credential'lı pazaryerine düş (HTMX değilse redirect).
        var selected = marketPlaces.FirstOrDefault(m => m.Id == mp);
        if (selected is null || !selected.IsCredentialComplete())
        {
            var fallbackId = credentialed[0].Id;
            if (!Request.IsHtmx())
                return RedirectToAction(nameof(Index), new { mp = fallbackId, search, returnUrl, attributeId });
            mp = fallbackId;
        }

        var attributesResult = await categoryAttributeManager.GetCategoryAttributes();
        var attributes = attributesResult.Data ?? [];

        var attributeIds = attributes.Select(a => a.Id).ToList();
        var matches = await attributeMatchManager.GetAttributeMatchesAsync(mp, attributeIds);
        var matchedIds = matches.ToDictionary(m => m.ApplicationCategoryAttributeId);

        var vm = new AttributeSyncVm
        {
            SelectedMarketPlaceId = mp,
            MarketPlaces = marketPlaces,
            AnyCredentialed = true,
            SelectedHasCredentials = true,
            SearchTerm = search,
            ReturnUrl = returnUrl,
            PreselectAttributeId = attributeId,
            Attributes = attributes.Select(a => new AttributeListItemVm
            {
                Id = a.Id,
                Key = a.CategoryAttributeKey ?? string.Empty,
                Humanized = a.CategoryAttributeHumanized,
                IsMapped = matchedIds.ContainsKey(a.Id),
                MarketPlaceAttributeId = matchedIds.TryGetValue(a.Id, out var m) ? m.MarketPlaceCategoryAttributeId : null
            }).ToList()
        };

        if (Request.IsHtmx() && Request.HtmxTarget() == "attribute-list-container")
            return PartialView($"{ViewBase}/Partials/_AttributeList.cshtml", vm);

        return View($"{ViewBase}/Index.cshtml", vm);
    }
```

> **NOT:** `marketPlaceManager.GetAllAsync()` dönüş tipini doğrula (`SyncOverviewController` `marketPlacesResult.Success ? marketPlacesResult.Data : []` kullanıyor → `IDataResult<List<MarketPlace>>`). `MarketPlaces` artık yalnız silinmemişleri taşır.

- [ ] **Step 3: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/MarketplaceSync/ViewModels/AttributeSyncVm.cs Application/Entegrasyon.MVC/Features/MarketplaceSync/AttributeSyncController.cs
git commit -m "feat(attributes): sync sayfası credential gating mantığı (VM + controller)"
```

---

## Task 7: Bug #1 — View Empty-State + Disabled Tab

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Index.cshtml`

Tabler `empty` bileşeni doğrulandı: https://tabler.io/docs/ui/empty (`empty` > `empty-icon` > `empty-title` > `empty-subtitle` > `empty-action`). Disabled nav-link: `nav-link disabled` (Tabler/Bootstrap nav).

- [ ] **Step 1: Empty-state guard'ı en üste ekle**

`Index.cshtml`'de `@{ ... }` bloğundan SONRA, `ReturnUrl` alert'inden ÖNCE:
```cshtml
@if (!Model.AnyCredentialed)
{
    <div class="card">
        <div class="card-body">
            <div class="empty">
                <div class="empty-icon">
                    <i class="ti ti-plug-connected-x" style="font-size:2.5rem;"></i>
                </div>
                <p class="empty-title">Aktif pazaryeri yok</p>
                <p class="empty-subtitle text-secondary">
                    Özellik eşlemesi yapabilmek için en az bir pazaryerinin API bilgilerini girmelisiniz.
                </p>
                <div class="empty-action">
                    <a href="/settings/integrations" class="btn btn-primary">
                        <i class="ti ti-settings me-1"></i>Pazaryeri Ayarları
                    </a>
                </div>
            </div>
        </div>
    </div>
    return;
}
```

(URL doğrulandı: `SettingsController` `[HttpGet("/settings/integrations")]` — doğru.)

- [ ] **Step 2: Tab barını credential-aware yap**

`Index.cshtml` tab `@foreach` bloğunu değiştir:
```cshtml
                <ul class="nav nav-pills">
                    @foreach (var mp in Model.MarketPlaces)
                    {
                        var isActive = mp.Id == Model.SelectedMarketPlaceId;
                        var hasCreds = mp.IsCredentialComplete();
                        <li class="nav-item">
                            @if (hasCreds)
                            {
                                <a class="nav-link @(isActive ? "active" : "")"
                                   href="/marketplace/sync/attributes?mp=@mp.Id"
                                   hx-get="/marketplace/sync/attributes?mp=@mp.Id"
                                   hx-target="#attribute-list-container"
                                   hx-push-url="true">
                                    @mp.Name
                                </a>
                            }
                            else
                            {
                                <span class="nav-link disabled" title="API bilgisi eksik">
                                    @mp.Name
                                    <i class="ti ti-lock ms-1"></i>
                                </span>
                            }
                        </li>
                    }
                </ul>
```
> `Index.cshtml` başına `@using Entegrasyon.Entity` ekli mi doğrula — `IsCredentialComplete()` extension için gerekli (namespace `Entegrasyon.Entity`). Yoksa ekle.

- [ ] **Step 3: Build + manuel göz**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Index.cshtml
git commit -m "feat(attributes): credential'sız pazaryeri tab disabled + sayfa empty-state"
```

---

## Task 8: Bug #3 — Sayaç + Progress'i Partial'a Taşı

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Index.cshtml` (özet barı sil)
- Modify: `Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Partials/_AttributeList.cshtml` (özet barı ekle)

- [ ] **Step 1: Index.cshtml'den Summary Bar kartını kaldır**

`<!-- Summary Bar -->` yorumuyla başlayan `<div class="card mb-3">...</div>` bloğunu (özet barı) tamamen sil. `@{ }` bloğundaki `mappedCount`/`totalCount`/`pct` hesapları da artık Index'te kullanılmıyorsa sil (partial'a taşınacak).

- [ ] **Step 2: _AttributeList.cshtml başına özet bar ekle**

`_AttributeList.cshtml` en üstüne (model satırından sonra, `<div class="card">`'dan önce):
```cshtml
@model Entegrasyon.MVC.Features.MarketplaceSync.ViewModels.AttributeSyncVm
@{
    var mappedCount = Model.Attributes.Count(a => a.IsMapped);
    var totalCount = Model.Attributes.Count;
    var pct = totalCount > 0 ? (int)(mappedCount * 100.0 / totalCount) : 0;
}

<div class="card mb-3">
    <div class="card-body">
        <div class="row align-items-center">
            <div class="col-auto">
                <span class="text-secondary">Eşleme Durumu:</span>
            </div>
            <div class="col">
                <div class="progress" style="height: 8px;">
                    <div class="progress-bar bg-success" style="width: @(pct)%"></div>
                </div>
            </div>
            <div class="col-auto">
                <strong>@mappedCount / @totalCount</strong> özellik eşlendi
            </div>
        </div>
    </div>
</div>

<div class="card">
```
(mevcut `<div class="card">` satırını yukarıdaki blokla değiştirerek tek `<div class="card">` kalsın — özet bar kartı + özellik listesi kartı ardışık.)

- [ ] **Step 3: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Index.cshtml Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Partials/_AttributeList.cshtml
git commit -m "fix(attributes): eşleşme sayacını partial'a taşı (tab değişiminde güncellensin)"
```

---

## Task 9: _AttributeMatchPanel — Değer Modal Trendyol Attr Id + Disabled State

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Partials/_AttributeMatchPanel.cshtml`

- [ ] **Step 1: Değer "Eşle" butonlarını eşleşme durumuna bağla**

Değer tablosundaki "Eşle" butonu (`openValueMapModal` çağıran) yalnız `Model.AttributeMatch is not null` iken aktif olsun. `else` (eşlenmemiş değer) dalındaki butonu değiştir:
```cshtml
                                    @if (Model.AttributeMatch is not null)
                                    {
                                        <button class="btn btn-outline-primary btn-sm"
                                                data-bs-toggle="modal"
                                                data-bs-target="#mapValueModal"
                                                onclick="openValueMapModal(@val.ValueId, '@val.ValueName.Replace("'", "\\'")' )">
                                            Eşle
                                        </button>
                                    }
                                    else
                                    {
                                        <button class="btn btn-outline-primary btn-sm" disabled
                                                title="Önce bu özelliği bir pazaryeri özelliğine eşleyin">
                                            Eşle
                                        </button>
                                    }
```

- [ ] **Step 2: Değer arama fetch URL'ini Trendyol attr id ile çağır**

`_AttributeMatchPanel.cshtml` script'inde `ensureValueSearchTomSelect` içindeki `load` fonksiyonunun fetch URL'ini değiştir. `@Model.AttributeMatch?.MarketPlaceCategoryAttributeId` Razor'da hesaplanıp JS sabitine yazılır:
```cshtml
<script>
    var valueSearchTs = null;
    var trendyolAttrId = @(Model.AttributeMatch?.MarketPlaceCategoryAttributeId ?? 0);

    function ensureValueSearchTomSelect() {
        var el = document.getElementById('valueSearchSelect');
        if (!el) return;
        if (el.tomselect) { valueSearchTs = el.tomselect; return; }
        valueSearchTs = new TomSelect(el, {
            valueField: 'id',
            labelField: 'label',
            searchField: ['name', 'label'],
            maxOptions: 20,
            placeholder: 'Değer adı yazın...',
            loadThrottle: 500,
            preload: false,
            load: function (query, callback) {
                if (query.length < 2 || !trendyolAttrId) { callback(); return; }
                fetch('/marketplace/sync/suggestions/attributes/values?mp=@Model.MarketPlaceId&mpAttributeId=' + trendyolAttrId + '&q=' + encodeURIComponent(query))
                    .then(function (r) { return r.json(); })
                    .then(function (data) { callback(data); })
                    .catch(function () { callback(); });
            }
        });
    }
```
(eski `var attributeId = @Model.AttributeId;` ve `/attributes/' + attributeId + '/values?...` URL'i kaldırılır.)

- [ ] **Step 3: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/MarketplaceSync/Views/AttributeSync/Partials/_AttributeMatchPanel.cshtml
git commit -m "feat(attributes): değer eşleme modal'ı Trendyol attr id ile + eşli değilken disabled"
```

---

## Task 10: Tam Doğrulama + Deploy

- [ ] **Step 1: Tüm unit testleri koş**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: tümü PASS (önceki ~1750 + yeni testler).

- [ ] **Step 2: Build (solution)**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 error, 0 warning.

- [ ] **Step 3: Integration testleri (Docker tüneli gerekir — kullanıcıya bırakılabilir)**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~AttributeMatchUniqueIndexTests"`
Expected: PASS. (Harness SSH tünelini öldürebilir → exit 144; o durumda kullanıcı koşar — bkz spec notu.)

- [ ] **Step 4: graphify güncelle**

Run: `graphify update .`

- [ ] **Step 5: Push (HER İKİ remote)**

```bash
git push origin develop && git push gitea develop
```
Gitea runner `develop` push'unu dev'e (192.168.1.78:8085) deploy eder.

- [ ] **Step 6: Manuel dev doğrulama (deploy bitince)**

`http://192.168.1.78:8085/marketplace/sync/attributes` (admin/123456789):
1. **#1:** Credential'sız pazaryeri tab'ı kilitli/disabled. Hiç credential yoksa empty-state.
2. **#2:** Trendyol tab → özellik seç → "Pazaryeri Özelliği" dropdown'a "renk" yaz → Trendyol attribute'ları (Renk/Beden/Cinsiyet) gelir. (En az bir kategori Trendyol'a eşli olmalı; değilse boş — önce kategori eşle.)
3. **#3:** Tab değiştir → `X / 44` sayacı + progress güncellenir.

---

## Self-Review Notları

- **Spec kapsamı:** #1 (Task 6-7), #2 (Task 3-5, 9), #3 (Task 8), 1:1 (Task 1-2). Tümü karşılandı.
- **Tip tutarlılığı:** `marketplaceAttributeId` adı interface (Task 4) + controller (Task 5) + JS (Task 9) boyunca tutarlı. `ITrendyolAttributeCatalog.SearchAttributesAsync/SearchValuesAsync` imzaları Task 3'te tanımlandığı gibi Task 4'te kullanılıyor.
- **Mevcut test kırılması:** `TrendyolMarketplaceSearchServiceTests`'in eski DB-tabanlı attribute/değer testleri Task 4'te yeni davranışla değiştirildi (silinmedi, repurpose edildi).
- **Riskler çözüldü:** Integration base sınıfı `IntegrationTestBase` (ctor `PostgreSqlFixture, WireMockFixture`, override `OnInitializeAsync`, `CreateDbContext()`) — Task 1 buna uygun. `/settings/integrations` URL'i doğrulandı (Task 7).
