# Attribute Eslestirme + Duplicate Prevention Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Attribute ve value eslestirme sayfasi olusturmak, Ollama destekli otomatik oneri sunmak, ve duplicate matching'i engellemek.

**Architecture:** `IMarketplaceCategoryAttributeProvider` interface ile her marketplace'in attribute API'si soyutlanir. `AttributeAutoMatchService` Ollama uzerinden isim benzerligi eslestirmesi yapar. Yeni `/marketplace/sync/attributes` sayfasi 3 panelli layout kullanir. Kategori sayfalarinda duplicate prevention UI filtreleri eklenir.

**Tech Stack:** Blazor Server (.NET 8), MudBlazor, EF Core 8, Ollama API (entegrasyon-coder model), xUnit + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-29-attribute-matching-and-duplicate-prevention-design.md`

---

## File Structure

| Dosya | Islem | Sorumluluk |
|-------|-------|-----------|
| `Business/Abstract/IMarketplaceCategoryAttributeProvider.cs` | Olustur | Marketplace attribute API abstraction |
| `Business/Concrete/Trendyol/TrendyolCategoryAttributeProvider.cs` | Olustur | Trendyol attribute cekme |
| `Business/Abstract/IAttributeAutoMatchService.cs` | Olustur | Ollama auto-match interface |
| `Business/Concrete/AttributeAutoMatchService.cs` | Olustur | Ollama ile attribute eslestirme onerisi |
| `Business/Abstract/IAttributeMatchManager.cs` | Olustur | Attribute/value match CRUD |
| `Business/Concrete/AttributeMatchManager.cs` | Olustur | Match kaydetme/kaldirma |
| `Entity/Dtos/Marketplace/MarketplaceAttributeDto.cs` | Olustur | Provider return DTO |
| `Entity/Dtos/Marketplace/AttributeMatchSuggestionDto.cs` | Olustur | Ollama oneri DTO |
| `Blazor/Features/MarketplaceSync/AttributeSync/AttributeSyncPage.razor` | Olustur | Ana sayfa (ust bar + layout) |
| `Blazor/Features/MarketplaceSync/AttributeSync/AttributeSyncPage.razor.cs` | Olustur | Sayfa logic |
| `Blazor/Features/MarketplaceSync/AttributeSync/AttributeListPanel.razor` | Olustur | Sol panel — attribute listesi |
| `Blazor/Features/MarketplaceSync/AttributeSync/AttributeListPanel.razor.cs` | Olustur | Liste logic |
| `Blazor/Features/MarketplaceSync/AttributeSync/AttributeMatchPanel.razor` | Olustur | Sag panel — eslestirme |
| `Blazor/Features/MarketplaceSync/AttributeSync/AttributeMatchPanel.razor.cs` | Olustur | Eslestirme logic |
| `Blazor/Features/MarketplaceSync/AttributeSync/ValueMatchSection.razor` | Olustur | Genisletilir value eslestirme |
| `Blazor/Features/MarketplaceSync/AttributeSync/ValueMatchSection.razor.cs` | Olustur | Value eslestirme logic |
| `Blazor/Features/MarketplaceSync/CategorySync.razor` | Değiştir | Duplicate prevention + "Attribute Eslestir" butonu |
| `Blazor/Features/MarketplaceSync/CategorySync.razor.cs` | Değiştir | Buton handler |
| `Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor` | Değiştir | Marketplace sayac |
| `Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor.cs` | Değiştir | Sayac hesaplama |
| `Test/Entegrasyon.Test/Business/AttributeMatchManagerTests.cs` | Olustur | Match CRUD testleri |
| `Test/Entegrasyon.Test/Business/AttributeAutoMatchServiceTests.cs` | Olustur | Ollama oneri testleri |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs` | Değiştir | Yeni servisleri DI'a kaydet |

Not: Tum Blazor dosya yollari `Application/Entegrasyon.Blazor/` prefix'i, Business dosyalari `Application/Entegrasyon.Business/` prefix'i iledir.

---

### Task 1: DTOlar + Interface'ler

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Marketplace/MarketplaceAttributeDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Marketplace/AttributeMatchSuggestionDto.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IMarketplaceCategoryAttributeProvider.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IAttributeAutoMatchService.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IAttributeMatchManager.cs`

- [ ] **Step 1: Create MarketplaceAttributeDto**

Create `Application/Entegrasyon.Entity/Dtos/Marketplace/MarketplaceAttributeDto.cs`:

```csharp
namespace Entegrasyon.Entity.Dtos.Marketplace;

public record MarketplaceAttributeDto(
    int Id,
    string Name,
    bool IsRequired,
    bool AllowCustom,
    List<MarketplaceAttributeValueDto> Values);

public record MarketplaceAttributeValueDto(
    int Id,
    string Name);
```

- [ ] **Step 2: Create AttributeMatchSuggestionDto**

Create `Application/Entegrasyon.Entity/Dtos/Marketplace/AttributeMatchSuggestionDto.cs`:

```csharp
namespace Entegrasyon.Entity.Dtos.Marketplace;

public record AttributeMatchSuggestionDto(
    int ApplicationAttributeId,
    string ApplicationAttributeName,
    int SuggestedMarketplaceAttributeId,
    string SuggestedMarketplaceAttributeName,
    double Confidence);

public record ValueMatchSuggestionDto(
    int ApplicationValueId,
    string ApplicationValueName,
    int SuggestedMarketplaceValueId,
    string SuggestedMarketplaceValueName,
    double Confidence);
```

- [ ] **Step 3: Create IMarketplaceCategoryAttributeProvider**

Create `Application/Entegrasyon.Business/Abstract/IMarketplaceCategoryAttributeProvider.cs`:

```csharp
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IMarketplaceCategoryAttributeProvider
{
    int MarketPlaceId { get; }
    Task<IDataResult<List<MarketplaceAttributeDto>>> GetAttributesForCategoryAsync(
        int marketplaceCategoryId, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create IAttributeAutoMatchService**

Create `Application/Entegrasyon.Business/Abstract/IAttributeAutoMatchService.cs`:

```csharp
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAttributeAutoMatchService
{
    Task<IDataResult<List<AttributeMatchSuggestionDto>>> SuggestAttributeMatchesAsync(
        List<AppAttributeForMatchDto> appAttributes,
        List<MarketplaceAttributeDto> marketplaceAttributes,
        CancellationToken ct = default);

    Task<IDataResult<List<ValueMatchSuggestionDto>>> SuggestValueMatchesAsync(
        List<AppValueForMatchDto> appValues,
        List<MarketplaceAttributeValueDto> marketplaceValues,
        CancellationToken ct = default);

    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

public record AppAttributeForMatchDto(int Id, string Name);
public record AppValueForMatchDto(int Id, string Name);
```

- [ ] **Step 5: Create IAttributeMatchManager**

Create `Application/Entegrasyon.Business/Abstract/IAttributeMatchManager.cs`:

```csharp
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAttributeMatchManager
{
    Task<List<CategoryAttributeMarketPlaceMatch>> GetAttributeMatchesAsync(int marketPlaceId, List<int> attributeIds);
    Task<List<CategoryAttributeValueMarketPlaceMatch>> GetValueMatchesAsync(int marketPlaceId, List<int> valueIds);
    Task<IResult> SaveAttributeMatchAsync(int applicationAttributeId, int marketPlaceId, int marketplaceAttributeId, string? externalId = null);
    Task<IResult> RemoveAttributeMatchAsync(int applicationAttributeId, int marketPlaceId);
    Task<IResult> SaveValueMatchAsync(int applicationValueId, int marketPlaceId, int marketplaceValueId, string? externalId = null);
    Task<IResult> RemoveValueMatchAsync(int applicationValueId, int marketPlaceId);
}
```

- [ ] **Step 6: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/Marketplace/MarketplaceAttributeDto.cs \
       Application/Entegrasyon.Entity/Dtos/Marketplace/AttributeMatchSuggestionDto.cs \
       Application/Entegrasyon.Business/Abstract/IMarketplaceCategoryAttributeProvider.cs \
       Application/Entegrasyon.Business/Abstract/IAttributeAutoMatchService.cs \
       Application/Entegrasyon.Business/Abstract/IAttributeMatchManager.cs
git commit -m "feat: add DTOs and interfaces for attribute matching"
```

---

### Task 2: AttributeMatchManager Implementation (TDD)

**Files:**
- Create: `Test/Entegrasyon.Test/Business/AttributeMatchManagerTests.cs`
- Create: `Application/Entegrasyon.Business/Concrete/AttributeMatchManager.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Write failing tests**

Create `Test/Entegrasyon.Test/Business/AttributeMatchManagerTests.cs`:

```csharp
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Matches;
using FluentAssertions;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.Test.Business;

public class AttributeMatchManagerTests : BaseTest
{
    private readonly AttributeMatchManager _manager;

    public AttributeMatchManagerTests()
    {
        _manager = new AttributeMatchManager(mockContextFactory.Object);
    }

    [Fact]
    public async Task SaveAttributeMatchAsync_Should_Add_New_Match()
    {
        // Arrange
        var existingMatches = new List<CategoryAttributeMarketPlaceMatch>();
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(existingMatches);

        // Act
        var result = await _manager.SaveAttributeMatchAsync(
            applicationAttributeId: 1, marketPlaceId: 1, marketplaceAttributeId: 100);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAttributeMatchAsync_Should_Return_Error_When_Already_Exists()
    {
        // Arrange
        var existingMatches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 }
        };
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(existingMatches);

        // Act
        var result = await _manager.SaveAttributeMatchAsync(
            applicationAttributeId: 1, marketPlaceId: 1, marketplaceAttributeId: 200);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten eşleştirilmiş");
    }

    [Fact]
    public async Task RemoveAttributeMatchAsync_Should_Remove_Existing_Match()
    {
        // Arrange
        var existingMatches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 }
        };
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(existingMatches);

        // Act
        var result = await _manager.RemoveAttributeMatchAsync(applicationAttributeId: 1, marketPlaceId: 1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetAttributeMatchesAsync_Should_Return_Matches_For_MarketPlace()
    {
        // Arrange
        var matches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 },
            new() { ApplicationCategoryAttributeId = 2, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 200 },
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 2, MarketPlaceCategoryAttributeId = 300 } // different marketplace
        };
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(matches);

        // Act
        var result = await _manager.GetAttributeMatchesAsync(marketPlaceId: 1, attributeIds: [1, 2]);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MarketPlaceId == 1);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AttributeMatchManagerTests"`
Expected: FAIL — class does not exist

- [ ] **Step 3: Implement AttributeMatchManager**

Create `Application/Entegrasyon.Business/Concrete/AttributeMatchManager.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class AttributeMatchManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IAttributeMatchManager
{
    public async Task<List<CategoryAttributeMarketPlaceMatch>> GetAttributeMatchesAsync(
        int marketPlaceId, List<int> attributeIds)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.CategoryAttributeMarketPlaceMatches
            .AsNoTracking()
            .Where(m => m.MarketPlaceId == marketPlaceId && attributeIds.Contains(m.ApplicationCategoryAttributeId))
            .ToListAsync();
    }

    public async Task<List<CategoryAttributeValueMarketPlaceMatch>> GetValueMatchesAsync(
        int marketPlaceId, List<int> valueIds)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.CategoryAttributeValueMarketPlaceMatches
            .AsNoTracking()
            .Where(m => m.MarketPlaceId == marketPlaceId && valueIds.Contains(m.ApplicationCategoryAttributeValueId))
            .ToListAsync();
    }

    public async Task<IResult> SaveAttributeMatchAsync(
        int applicationAttributeId, int marketPlaceId, int marketplaceAttributeId, string? externalId = null)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var existing = await dbContext.CategoryAttributeMarketPlaceMatches
            .AnyAsync(m => m.ApplicationCategoryAttributeId == applicationAttributeId && m.MarketPlaceId == marketPlaceId);

        if (existing)
            return new ErrorResult("Bu özellik bu pazar yeri için zaten eşleştirilmiş.");

        dbContext.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
        {
            ApplicationCategoryAttributeId = applicationAttributeId,
            MarketPlaceId = marketPlaceId,
            MarketPlaceCategoryAttributeId = marketplaceAttributeId,
            MarketPlaceCategoryAttributeExternalId = externalId
        });

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Özellik eşleştirmesi kaydedildi.");
    }

    public async Task<IResult> RemoveAttributeMatchAsync(int applicationAttributeId, int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var match = await dbContext.CategoryAttributeMarketPlaceMatches
            .FirstOrDefaultAsync(m => m.ApplicationCategoryAttributeId == applicationAttributeId && m.MarketPlaceId == marketPlaceId);

        if (match is null)
            return new ErrorResult("Eşleştirme bulunamadı.");

        dbContext.CategoryAttributeMarketPlaceMatches.Remove(match);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Eşleştirme kaldırıldı.");
    }

    public async Task<IResult> SaveValueMatchAsync(
        int applicationValueId, int marketPlaceId, int marketplaceValueId, string? externalId = null)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var existing = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .AnyAsync(m => m.ApplicationCategoryAttributeValueId == applicationValueId && m.MarketPlaceId == marketPlaceId);

        if (existing)
            return new ErrorResult("Bu değer bu pazar yeri için zaten eşleştirilmiş.");

        dbContext.CategoryAttributeValueMarketPlaceMatches.Add(new CategoryAttributeValueMarketPlaceMatch
        {
            ApplicationCategoryAttributeValueId = applicationValueId,
            MarketPlaceId = marketPlaceId,
            MarketPlaceCategoryAttributeValueId = marketplaceValueId,
            MarketPlaceCategoryAttributeValueExternalId = externalId
        });

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Değer eşleştirmesi kaydedildi.");
    }

    public async Task<IResult> RemoveValueMatchAsync(int applicationValueId, int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var match = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .FirstOrDefaultAsync(m => m.ApplicationCategoryAttributeValueId == applicationValueId && m.MarketPlaceId == marketPlaceId);

        if (match is null)
            return new ErrorResult("Değer eşleştirmesi bulunamadı.");

        dbContext.CategoryAttributeValueMarketPlaceMatches.Remove(match);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Değer eşleştirmesi kaldırıldı.");
    }
}
```

- [ ] **Step 4: Register in DI**

In `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`, add in the `AddApplicationDependencies` method:

```csharp
services.AddScoped<IAttributeMatchManager, AttributeMatchManager>();
```

- [ ] **Step 5: Run tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AttributeMatchManagerTests"`
Expected: All PASS

- [ ] **Step 6: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/AttributeMatchManager.cs \
       Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
       Test/Entegrasyon.Test/Business/AttributeMatchManagerTests.cs
git commit -m "feat: implement AttributeMatchManager with CRUD for attribute/value matches"
```

---

### Task 3: TrendyolCategoryAttributeProvider + DI

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolCategoryAttributeProvider.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Read Trendyol category attributes API pattern**

Read `Application/Entegrasyon.Business/Concrete/Trendyol/Import/TrendyolCategoryImporterService.cs` to understand how Trendyol category attributes are fetched. The importer already calls the Trendyol API for attributes — extract the HTTP call pattern.

Also read `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs` for the HttpClient usage pattern.

- [ ] **Step 2: Implement TrendyolCategoryAttributeProvider**

Create `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolCategoryAttributeProvider.cs`:

```csharp
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

public class TrendyolCategoryAttributeProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolCategoryAttributeProvider> logger) : IMarketplaceCategoryAttributeProvider
{
    public int MarketPlaceId => 1; // Trendyol

    public async Task<IDataResult<List<MarketplaceAttributeDto>>> GetAttributesForCategoryAsync(
        int marketplaceCategoryId, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("TrendyolPublic");
            var response = await client.GetAsync(
                $"product-categories/{marketplaceCategoryId}/attributes", ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Trendyol attribute API {Status} for category {Id}",
                    response.StatusCode, marketplaceCategoryId);
                return new ErrorDataResult<List<MarketplaceAttributeDto>>([], "Trendyol API hatası.");
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<TrendyolCategoryAttributesResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.CategoryAttributes is null)
                return new SuccessDataResult<List<MarketplaceAttributeDto>>([]);

            var attributes = result.CategoryAttributes.Select(a => new MarketplaceAttributeDto(
                Id: a.Attribute.Id,
                Name: a.Attribute.Name,
                IsRequired: a.Required,
                AllowCustom: a.AllowCustom,
                Values: a.AttributeValues?.Select(v => new MarketplaceAttributeValueDto(v.Id, v.Name)).ToList() ?? []
            )).ToList();

            return new SuccessDataResult<List<MarketplaceAttributeDto>>(attributes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol attribute fetch failed for category {Id}", marketplaceCategoryId);
            return new ErrorDataResult<List<MarketplaceAttributeDto>>([], ex.Message);
        }
    }

    // Trendyol API response models
    private record TrendyolCategoryAttributesResponse(List<TrendyolCategoryAttribute>? CategoryAttributes);
    private record TrendyolCategoryAttribute(TrendyolAttribute Attribute, bool Required, bool AllowCustom, List<TrendyolAttributeValue>? AttributeValues);
    private record TrendyolAttribute(int Id, string Name);
    private record TrendyolAttributeValue(int Id, string Name);
}
```

IMPORTANT: Read the actual Trendyol importer to verify the API response structure. The record names above are based on common Trendyol patterns — adjust field names to match the actual JSON response.

- [ ] **Step 3: Register in DI**

In `ApplicationDependencyExtension.cs`, add:

```csharp
services.AddScoped<IMarketplaceCategoryAttributeProvider, TrendyolCategoryAttributeProvider>();
```

Note: When multiple marketplaces are added, this will become `services.AddScoped<IEnumerable<IMarketplaceCategoryAttributeProvider>>(...)` or a factory pattern. For now, single registration.

- [ ] **Step 4: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolCategoryAttributeProvider.cs \
       Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat: add TrendyolCategoryAttributeProvider for marketplace attribute fetching"
```

---

### Task 4: AttributeAutoMatchService (Ollama) + TDD

**Files:**
- Create: `Test/Entegrasyon.Test/Business/AttributeAutoMatchServiceTests.cs`
- Create: `Application/Entegrasyon.Business/Concrete/AttributeAutoMatchService.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Write failing test**

Create `Test/Entegrasyon.Test/Business/AttributeAutoMatchServiceTests.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Marketplace;
using FluentAssertions;
using Moq;

namespace Entegrasyon.Test.Business;

public class AttributeAutoMatchServiceTests
{
    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Return_Suggestions()
    {
        // Arrange
        var appAttrs = new List<AppAttributeForMatchDto>
        {
            new(1, "Renk"),
            new(2, "Beden")
        };
        var mpAttrs = new List<MarketplaceAttributeDto>
        {
            new(100, "Renk", true, false, []),
            new(200, "Beden", true, false, [])
        };

        var mockHttpFactory = new Mock<IHttpClientFactory>();
        // Setup mock HTTP handler that returns Ollama response
        var handler = new MockHttpMessageHandler();
        handler.SetResponse(System.Net.HttpStatusCode.OK, """
        {"response": "[{\"appId\":1,\"mpId\":100,\"confidence\":0.95},{\"appId\":2,\"mpId\":200,\"confidence\":0.90}]"}
        """);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/api/") };
        mockHttpFactory.Setup(f => f.CreateClient("Ollama")).Returns(client);

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<AttributeAutoMatchService>>();
        var service = new AttributeAutoMatchService(mockHttpFactory.Object, logger.Object);

        // Act
        var result = await service.SuggestAttributeMatchesAsync(appAttrs, mpAttrs);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data[0].ApplicationAttributeId.Should().Be(1);
        result.Data[0].SuggestedMarketplaceAttributeId.Should().Be(100);
    }
}
```

Note: Reuse the `MockHttpMessageHandler` pattern from `CategoryAutoMatchServiceTests.cs`. Read that file first to copy the mock handler.

- [ ] **Step 2: Implement AttributeAutoMatchService**

Create `Application/Entegrasyon.Business/Concrete/AttributeAutoMatchService.cs`:

Follow the EXACT same pattern as `CategoryAutoMatchService.cs`:
- `IHttpClientFactory` with "Ollama" named client
- POST to `api/generate`
- `model = "entegrasyon-coder"`, `temperature = 0.1`
- Turkish prompt for attribute matching
- JSON response parsing

The prompt should ask Ollama to match application attributes to marketplace attributes by name similarity, returning JSON with `appId`, `mpId`, `confidence`.

Register in DI:
```csharp
services.AddScoped<IAttributeAutoMatchService, AttributeAutoMatchService>();
```

- [ ] **Step 3: Run tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~AttributeAutoMatchServiceTests"`
Expected: PASS

- [ ] **Step 4: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/AttributeAutoMatchService.cs \
       Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
       Test/Entegrasyon.Test/Business/AttributeAutoMatchServiceTests.cs
git commit -m "feat: add AttributeAutoMatchService with Ollama integration"
```

---

### Task 5: AttributeSyncPage — Main Layout + Marketplace/Category Selection

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeSyncPage.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeSyncPage.razor.cs`

- [ ] **Step 1: Read existing CategorySync page for patterns**

Read `CategorySync.razor` and `CategorySync.razor.cs` to understand the page structure, injected services, and marketplace selection pattern.

- [ ] **Step 2: Create AttributeSyncPage.razor.cs**

Key logic:
- Route parameters: `[SupplyParameterFromQuery] MarketplaceId`, `[SupplyParameterFromQuery] CategoryId`
- Inject: `IDbContextFactory`, `ICategoryMatchService`, `IMarketplaceCategoryAttributeProvider`, `IAttributeMatchManager`, `IAttributeAutoMatchService`, `NavigationManager`, `ISnackbar`
- Load marketplaces from DB
- On marketplace selected: load matched categories for that marketplace (from `CategoryMarketplaces` where `IsActive && MarketPlaceId == selected`)
- On category selected: load category's attributes (from `CategoryAttributeCategories`), load existing attribute matches, load marketplace attributes via provider
- State: `_selectedMarketplaceId`, `_selectedCategoryId`, `_appAttributes`, `_marketplaceAttributes`, `_existingMatches`, `_selectedAttribute`

- [ ] **Step 3: Create AttributeSyncPage.razor**

```razor
@page "/marketplace/sync/attributes"
@rendermode InteractiveServer
@attribute [Authorize(Policy = AppPermissions.Marketplace.View)]
```

Layout:
- Top bar: Marketplace dropdown + Category dropdown (side by side)
- Below: Two-panel split (MudGrid xs=5 left, xs=7 right)
- Left: `<AttributeListPanel>` — shows app attributes with match status
- Right: `<AttributeMatchPanel>` — shows matching UI for selected attribute

- [ ] **Step 4: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded (child components don't exist yet — reference them conditionally or create stubs)

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/
git commit -m "feat: add AttributeSyncPage with marketplace/category selection"
```

---

### Task 6: AttributeListPanel (Sol Panel)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeListPanel.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeListPanel.razor.cs`

- [ ] **Step 1: Create AttributeListPanel.razor.cs**

Parameters:
- `AppAttributes` (list of category attributes with match info)
- `ExistingMatches` (dictionary: attributeId → match info)
- `SelectedAttributeId` (int?) + `SelectedAttributeIdChanged` (EventCallback)
- `OnRemoveMatch` (EventCallback<int>) — remove match for attribute

- [ ] **Step 2: Create AttributeListPanel.razor**

Shows a `MudList` of attributes. Each item:
- Attribute name (bold)
- Required/Varianter/Slicer badges
- Match status chip: green "Eşleştirildi: {mpAttrName}" or red "Eşleştirilmedi"
- If matched: small "Kaldır" icon button
- Selected item highlighted
- Include `ValueMatchSection` component (expandable) for matched attributes

- [ ] **Step 3: Build**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeListPanel.*
git commit -m "feat: add AttributeListPanel with match status display"
```

---

### Task 7: AttributeMatchPanel (Sag Panel) + Ollama Auto-Suggest

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeMatchPanel.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeMatchPanel.razor.cs`

- [ ] **Step 1: Create AttributeMatchPanel.razor.cs**

Parameters:
- `SelectedAttribute` (app attribute info)
- `MarketplaceAttributes` (all marketplace attributes for this category)
- `ExistingMatches` (all matches — to know which mp attrs are already used)
- `Suggestions` (auto-match suggestions from Ollama)
- `OnMatchConfirmed` (EventCallback<(int appAttrId, int mpAttrId)>)

Key logic:
- Show auto-suggestions at top (sorted by confidence, highest first)
- Show all marketplace attributes below (filterable by search)
- Already-used marketplace attributes shown as disabled with "Kullanılıyor" chip
- Click to select → confirm button → fires OnMatchConfirmed

- [ ] **Step 2: Create AttributeMatchPanel.razor**

Layout:
- If no attribute selected: info message "Sol panelden bir özellik seçin"
- Auto-suggest section (if Ollama available): confidence bars + suggested match
- Manual search: `MudTextField` to filter marketplace attributes
- Marketplace attributes list: name + required badge + used/available status
- Confirm button

- [ ] **Step 3: Build**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/AttributeMatchPanel.*
git commit -m "feat: add AttributeMatchPanel with auto-suggest and manual search"
```

---

### Task 8: ValueMatchSection (Genisletilir Panel)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/ValueMatchSection.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/ValueMatchSection.razor.cs`

- [ ] **Step 1: Create ValueMatchSection**

This is an expandable section within `AttributeListPanel`, shown for matched attributes.

Parameters:
- `ApplicationValues` (list of app attribute values)
- `MarketplaceValues` (list of marketplace attribute values — fetched when expanded)
- `ExistingValueMatches` (current value matches)
- `MarketPlaceId` (int)
- `OnValueMatchSaved` / `OnValueMatchRemoved` EventCallbacks

When expanded:
- Two-column table: App value | Marketplace value (dropdown or auto-matched)
- Already matched values shown with green check + value name
- Unmatched values shown with red warning + dropdown to select marketplace value
- "Otomatik Eşleştir" button — calls Ollama for value matching
- Already-used marketplace values disabled in dropdowns (duplicate prevention)

- [ ] **Step 2: Build**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/AttributeSync/ValueMatchSection.*
git commit -m "feat: add ValueMatchSection with expandable value matching"
```

---

### Task 9: Duplicate Prevention — CategorySync + BulkCategoryMatch

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor`
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor.cs`
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor`
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor.cs`

- [ ] **Step 1: Update CategorySync action buttons**

In `CategorySync.razor` lines 187-206 (action column), change "Eşleştir" button logic:
- If category has active marketplace links: show "Düzenle" + "Kaldır" buttons instead of "Eşleştir"
- Add "Attribute Eşleştir" button that navigates to `/marketplace/sync/attributes?marketplaceId=X&categoryId=Y`

- [ ] **Step 2: Update CategorySync.razor.cs**

Add navigation method:
```csharp
private void NavigateToAttributeMatch(Category category, int marketPlaceId)
{
    NavigationManager.NavigateTo($"/marketplace/sync/attributes?marketplaceId={marketPlaceId}&categoryId={category.Id}");
}
```

- [ ] **Step 3: Update BulkCategoryMatchPage**

In marketplace select, show match count next to each marketplace name:
```razor
<MudSelectItem Value="@((int?)mp.Id)">
    @mp.Name (@matchedCount/@totalCount)
</MudSelectItem>
```

Load match counts from `CategoryMatchService.GetCategoryMatchSummaryAsync()`.

- [ ] **Step 4: Build**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.* \
       Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/*
git commit -m "feat: add duplicate prevention + attribute match navigation to CategorySync"
```

---

### Task 10: Permission + NavMenu + Final Verification

**Files:**
- Modify: `Test/Entegrasyon.Test/Security/PagePermissionAttributeTests.cs`

- [ ] **Step 1: Add permission mapping for AttributeSyncPage**

In `PagePermissionAttributeTests.cs`, add:
```csharp
{ "Entegrasyon.Blazor.Features.MarketplaceSync.AttributeSync.AttributeSyncPage", AppPermissions.Marketplace.View },
```

NavMenu already has the link at `/marketplace/sync/attributes` — verify it points to the correct route.

- [ ] **Step 2: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 3: Build full solution**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 errors

- [ ] **Step 4: Manual smoke test checklist**

1. Navigate to `/marketplace/sync/attributes` — page loads, marketplace dropdown visible
2. Select Trendyol — category dropdown shows only Trendyol-matched categories
3. Select a category — left panel shows attributes with match status
4. Click unmatched attribute — right panel shows marketplace attributes + auto-suggest
5. Already-matched marketplace attributes show "Kullanılıyor" and are not selectable
6. Confirm a match — attribute moves to "Eşleştirildi" status
7. Expand matched attribute → value matching section shows
8. CategorySync page → matched category shows "Düzenle/Kaldır" instead of "Eşleştir"
9. CategorySync → "Attribute Eşleştir" button navigates correctly with query params
10. BulkCategoryMatch → marketplace dropdown shows match counts
