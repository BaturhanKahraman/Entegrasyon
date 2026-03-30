# Brand Matching UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Mevcut brand matching sayfasini calistir hale getirmek: SearchBrandsAsync, auto-match (string + Ollama), duplicate prevention.
**Architecture:** TrendyolMarketplaceSearchService'e Trendyol brand API implementasyonu, DB fallback diger marketplace'ler icin, BrandAutoMatchService (string match + Ollama), mevcut UI'a buton + dialog + duplicate chip ekleme.
**Tech Stack:** .NET 8, MudBlazor, Ollama API, xUnit + FluentAssertions

---

## Task 1: SearchBrandsAsync — Trendyol API + DB Fallback (TDD)

### 1a. Failing Tests (write first)

File: `Test/Entegrasyon.Test/Trendyol/TrendyolMarketplaceSearchServiceTests.cs`

Replace the existing stub test `SearchBrandsAsync_ReturnsEmptyList_NotYetImplemented` and add new tests:

```csharp
// ═══════════════════════════════════════════════════════════════════════
// SearchBrandsAsync — Trendyol API (real)
// ═══════════════════════════════════════════════════════════════════════

[Fact]
public async Task SearchBrandsAsync_Trendyol_ReturnsMatchingBrands()
{
    // Arrange
    var trendyolResponse = new TrendyolBrandsResponse
    {
        Brands = [new TrendyolBrandItem { Id = 111, Name = "Nike" }]
    };
    _brandHttpHandler.SetResponse(HttpStatusCode.OK,
        JsonSerializer.Serialize(trendyolResponse));

    var sut = CreateSut();

    // Act
    var result = await sut.SearchBrandsAsync(1, "Nike");

    // Assert
    result.Success.Should().BeTrue();
    result.Data.Should().HaveCount(1);
    result.Data[0].Id.Should().Be(111);
    result.Data[0].Name.Should().Be("Nike");
}

[Fact]
public async Task SearchBrandsAsync_WhenApiThrows_ReturnsError()
{
    // Arrange
    _brandHttpHandler.SetResponse(HttpStatusCode.InternalServerError, "");

    var sut = CreateSut();

    // Act
    var result = await sut.SearchBrandsAsync(1, "Nike");

    // Assert
    result.Success.Should().BeFalse();
}

[Fact]
public async Task SearchBrandsAsync_NonTrendyol_FallsBackToDb()
{
    // Arrange — marketPlaceId=2 (N11), no HTTP mock needed
    var matches = new List<BrandMarketPlaceMatch>
    {
        new() { MarketPlaceId = 2, MarketPlaceBrandId = 50,
                MarketPlaceBrandName = "Nike TR" },
        new() { MarketPlaceId = 2, MarketPlaceBrandId = 51,
                MarketPlaceBrandName = "Adidas" },
        new() { MarketPlaceId = 3, MarketPlaceBrandId = 99,
                MarketPlaceBrandName = "Nike" }, // different marketplace, excluded
    };
    mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
        .ReturnsDbSet(matches);

    var sut = CreateSut();

    // Act
    var result = await sut.SearchBrandsAsync(2, "Nike");

    // Assert
    result.Success.Should().BeTrue();
    result.Data.Should().HaveCount(1);
    result.Data[0].Name.Should().Be("Nike TR");
}
```

To wire up the HTTP mock, update `CreateSut()` to inject `IHttpClientFactory`. The `TrendyolMarketplaceSearchService` constructor will gain a new `IHttpClientFactory httpClientFactory` parameter.

Add to `TrendyolMarketplaceSearchServiceTests`:
```csharp
private readonly MockHttpMessageHandler _brandHttpHandler = new();
private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();

// In constructor:
var httpClient = new HttpClient(_brandHttpHandler)
{
    BaseAddress = new Uri("https://apigw.trendyol.com/integration/")
};
_httpClientFactoryMock.Setup(f => f.CreateClient(StringConstants.TrendyolApi))
    .Returns(httpClient);
```

### 1b. Trendyol Brand API Response Model

Trendyol endpoint: `GET /integration/product/brands/by-name?name={query}`
Rate limit: 50 req/min (same bucket as other reference endpoints per BASE_KNOWLEDGE.md).

Add private models inside `TrendyolMarketplaceSearchService.cs`:

```csharp
private record TrendyolBrandsResponse
{
    [JsonPropertyName("brands")]
    public List<TrendyolBrandItem> Brands { get; init; } = [];
}

private record TrendyolBrandItem
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
```

### 1c. Implementation

File: `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolMarketplaceSearchService.cs`

Change constructor to accept `IHttpClientFactory`:
```csharp
public sealed class TrendyolMarketplaceSearchService(
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ITrendyolCategoryImportService categoryImportService,
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolMarketplaceSearchService> logger) : IMarketplaceSearchService
```

Replace the stub `SearchBrandsAsync` with:
```csharp
public async Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(
    int marketPlaceId, string query, CancellationToken ct = default)
{
    // Trendyol (Id=1): real API call
    if (marketPlaceId == TrendyolMarketPlaceId)
        return await SearchBrandsTrendyolAsync(query, ct);

    // Other marketplaces: DB fallback via BrandMarketPlaceMatch
    return await SearchBrandsFromDbAsync(marketPlaceId, query, ct);
}

private async Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsTrendyolAsync(
    string query, CancellationToken ct)
{
    try
    {
        var client = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
        var url = $"product/brands/by-name?name={Uri.EscapeDataString(query)}&size=20";

        var response = await client.GetFromJsonAsync<TrendyolBrandsResponse>(url, ct);
        var brands = response?.Brands
            .Select(b => new MarketplaceBrandSearchResult(b.Id, b.Name))
            .ToList() ?? [];

        return new SuccessDataResult<List<MarketplaceBrandSearchResult>>(brands);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Trendyol marka arama hatasi");
        return new ErrorDataResult<List<MarketplaceBrandSearchResult>>([], "Marka arama sirasinda hata olustu.");
    }
}

private async Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsFromDbAsync(
    int marketPlaceId, string query, CancellationToken ct)
{
    try
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var q = dbContext.BrandMarketPlaceMatches
            .Where(m => m.MarketPlaceId == marketPlaceId);

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(m => m.MarketPlaceBrandName != null &&
                              m.MarketPlaceBrandName.Contains(query));

        var brands = await q
            .Select(m => new MarketplaceBrandSearchResult(m.MarketPlaceBrandId, m.MarketPlaceBrandName ?? $"Brand #{m.MarketPlaceBrandId}"))
            .Distinct()
            .Take(20)
            .ToListAsync(ct);

        return new SuccessDataResult<List<MarketplaceBrandSearchResult>>(brands);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DB marka arama hatasi, marketPlaceId={MarketPlaceId}", marketPlaceId);
        return new ErrorDataResult<List<MarketplaceBrandSearchResult>>([], "Marka arama sirasinda hata olustu.");
    }
}
```

**Note:** `BrandMarketPlaceMatch` entity currently has no `MarketPlaceBrandName` field. Check if it exists; if not, the DB fallback will only show `"Brand #{id}"`. The real fix (adding `MarketPlaceBrandName` to the entity) is out of scope for this task — use the existing string formatting as a graceful degradation.

**Check `BrandMarketPlaceMatch`:** The entity only has `MarketPlaceBrandId` (int) and `MarketPlaceBrandExternalId` (string). There is no name field. For the DB fallback query, select `MarketPlaceBrandExternalId` as name when available:

```csharp
.Select(m => new MarketplaceBrandSearchResult(
    m.MarketPlaceBrandId,
    m.MarketPlaceBrandExternalId ?? $"Brand #{m.MarketPlaceBrandId}"))
```

### 1d. MockMarketplaceSearchService

Already has `MockBrands` list with 30 brands. The existing `SearchBrandsAsync` implementation is complete — no changes needed.

### 1e. Run tests + commit

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TrendyolMarketplaceSearchServiceTests"
```

Commit message: `feat(search): implement SearchBrandsAsync — Trendyol API + DB fallback`

---

## Task 2: BrandAutoMatchService — String Matching (TDD)

### 2a. New DTO

File: `Application/Entegrasyon.Entity/Dtos/Brand/BrandAutoMatchResultDto.cs` (new file)

```csharp
namespace Entegrasyon.Entity.Dtos.Brand;

public record BrandAutoMatchResultDto
{
    public int AutoMatchedCount { get; init; }
    public int SuggestionCount { get; init; }
    public int FailedCount { get; init; }
    public List<BrandAutoMatchSuggestionDto> Suggestions { get; init; } = [];
}

public record BrandAutoMatchSuggestionDto
{
    public int ApplicationBrandId { get; init; }
    public string ApplicationBrandName { get; init; } = string.Empty;
    public int MarketPlaceBrandId { get; init; }
    public string MarketPlaceBrandName { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Reason { get; init; } = string.Empty;
}
```

### 2b. Interface

File: `Application/Entegrasyon.Business/Abstract/IBrandAutoMatchService.cs` (new file)

```csharp
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBrandAutoMatchService
{
    /// <summary>
    /// Secilen marketplace icin eslenmemis markalari string matching + Ollama ile otomatik eslestirir.
    /// confidence >= 0.8 olan eslesmeleri otomatik kaydeder, dusuk guvenli olanlar onay icin doner.
    /// </summary>
    Task<IDataResult<BrandAutoMatchResultDto>> AutoMatchAsync(int marketPlaceId, CancellationToken ct = default);

    /// <summary>
    /// Ollama servisinin ayakta olup olmadığını kontrol eder.
    /// </summary>
    Task<bool> IsOllamaAvailableAsync(CancellationToken ct = default);
}
```

### 2c. Failing Tests (write first)

File: `Test/Entegrasyon.Test/Business/BrandAutoMatchServiceTests.cs` (new file)

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;

namespace Entegrasyon.UnitTest.Business;

public class BrandAutoMatchServiceTests : BaseTest
{
    private readonly Mock<IBrandMatchService> _brandMatchServiceMock = new();
    private readonly Mock<IMarketplaceSearchService> _searchServiceMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<BrandAutoMatchService>> _loggerMock = new();
    private readonly MockHttpMessageHandler _ollamaHandler = new();

    private BrandAutoMatchService CreateSut()
    {
        var httpClient = new HttpClient(_ollamaHandler)
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };
        _httpClientFactoryMock.Setup(f => f.CreateClient("Ollama")).Returns(httpClient);

        return new BrandAutoMatchService(
            _brandMatchServiceMock.Object,
            _searchServiceMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // String Matching
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AutoMatch_ExactMatch_CaseInsensitive_AutoSaves()
    {
        // Arrange
        SetupUnmappedBrands([new BrandDto { Id = 1, Name = "Nike" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(100, "NIKE")]);
        SetupCreateMapping(true);
        DisableOllama();

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.AutoMatchedCount.Should().Be(1);
        result.Data.SuggestionCount.Should().Be(0);
        result.Data.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task AutoMatch_NormalizedMatch_TurkishChars_AutoSaves()
    {
        // Arrange — "Çiçeksepeti" matches "Ciceksepeti" after normalization
        SetupUnmappedBrands([new BrandDto { Id = 2, Name = "Çiçeksepeti" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(200, "Ciceksepeti")]);
        SetupCreateMapping(true);
        DisableOllama();

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.AutoMatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task AutoMatch_ContainsMatch_AutoSaves()
    {
        // Arrange — "Nike TR" contains "Nike"
        SetupUnmappedBrands([new BrandDto { Id = 3, Name = "Nike" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(300, "Nike TR")]);
        SetupCreateMapping(true);
        DisableOllama();

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert
        result.Data.AutoMatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task AutoMatch_NoStringMatch_NoOllama_ReturnsFailedCount()
    {
        // Arrange
        SetupUnmappedBrands([new BrandDto { Id = 4, Name = "Totally Unknown Brand" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(400, "Completely Different")]);
        DisableOllama();

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert
        result.Data.AutoMatchedCount.Should().Be(0);
        result.Data.SuggestionCount.Should().Be(0);
        result.Data.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task AutoMatch_EmptyUnmappedList_ReturnsZeroCounts()
    {
        SetupUnmappedBrands([]);
        var sut = CreateSut();

        var result = await sut.AutoMatchAsync(1);

        result.Success.Should().BeTrue();
        result.Data.AutoMatchedCount.Should().Be(0);
        result.Data.FailedCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Ollama Fallback
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AutoMatch_OllamaHighConfidence_AutoSaves()
    {
        // Arrange — no string match, Ollama returns confidence 0.9
        SetupUnmappedBrands([new BrandDto { Id = 5, Name = "Mavi Giyim" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(500, "Mavi")]);
        SetupCreateMapping(true);
        SetupOllamaResponse([new { appId = 5, mpId = 500, confidence = 0.9 }]);

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert
        result.Data.AutoMatchedCount.Should().Be(1);
        result.Data.SuggestionCount.Should().Be(0);
    }

    [Fact]
    public async Task AutoMatch_OllamaLowConfidence_ReturnsSuggestion()
    {
        // Arrange — Ollama returns confidence 0.6 (below 0.8 threshold)
        SetupUnmappedBrands([new BrandDto { Id = 6, Name = "ABC Tekstil" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(600, "ABC")]);
        SetupOllamaResponse([new { appId = 6, mpId = 600, confidence = 0.6 }]);

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert
        result.Data.AutoMatchedCount.Should().Be(0);
        result.Data.SuggestionCount.Should().Be(1);
        result.Data.Suggestions[0].ApplicationBrandId.Should().Be(6);
        result.Data.Suggestions[0].MarketPlaceBrandId.Should().Be(600);
        result.Data.Suggestions[0].Confidence.Should().Be(0.6);
    }

    [Fact]
    public async Task AutoMatch_OllamaUnavailable_GracefulDegradation()
    {
        // Arrange — Ollama throws HttpRequestException
        SetupUnmappedBrands([new BrandDto { Id = 7, Name = "Some Brand" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(700, "Different Brand")]);
        _ollamaHandler.SetException(new HttpRequestException("Connection refused"));

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert — graceful, no exception bubbled
        result.Success.Should().BeTrue();
        result.Data.FailedCount.Should().Be(1);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    private void SetupUnmappedBrands(List<BrandDto> brands)
    {
        _brandMatchServiceMock
            .Setup(s => s.GetUnmappedBrandsAsync(It.IsAny<int>()))
            .ReturnsAsync(brands);
    }

    private void SetupMarketplaceBrands(List<MarketplaceBrandSearchResult> brands)
    {
        // Return the full list for empty query (used for loading all MP brands)
        _searchServiceMock
            .Setup(s => s.SearchBrandsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceBrandSearchResult>>(brands));
    }

    private void SetupCreateMapping(bool success)
    {
        _brandMatchServiceMock
            .Setup(s => s.CreateBrandMappingAsync(It.IsAny<CreateBrandMarketPlaceMatchDto>()))
            .ReturnsAsync(success ? new SuccessResult() : new ErrorResult("Hata"));
    }

    private void DisableOllama()
    {
        _ollamaHandler.SetResponse(HttpStatusCode.ServiceUnavailable, "");
    }

    private void SetupOllamaResponse(IEnumerable<object> results)
    {
        var inner = JsonSerializer.Serialize(results);
        var outer = JsonSerializer.Serialize(new { response = inner });
        _ollamaHandler.SetResponse(HttpStatusCode.OK, outer);
    }
}
```

### 2d. String Normalization Utility

File: `Application/Entegrasyon.Business/Utilities/TurkishStringNormalizer.cs` (new file)

```csharp
namespace Entegrasyon.Business.Utilities;

/// <summary>
/// Turkce karakter normalizasyonu — brand matching icin.
/// </summary>
public static class TurkishStringNormalizer
{
    private static readonly (char From, char To)[] TurkishMap =
    [
        ('ş', 's'), ('Ş', 'S'),
        ('ç', 'c'), ('Ç', 'C'),
        ('ğ', 'g'), ('Ğ', 'G'),
        ('ü', 'u'), ('Ü', 'U'),
        ('ö', 'o'), ('Ö', 'O'),
        ('ı', 'i'), ('İ', 'I'),
    ];

    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = input.ToLowerInvariant();
        foreach (var (from, to) in TurkishMap)
            result = result.Replace(from, to);
        return result;
    }
}
```

### 2e. Implementation

File: `Application/Entegrasyon.Business/Concrete/BrandAutoMatchService.cs` (new file)

```csharp
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class BrandAutoMatchService(
    IBrandMatchService brandMatchService,
    IMarketplaceSearchService searchService,
    IHttpClientFactory httpClientFactory,
    ILogger<BrandAutoMatchService> logger) : IBrandAutoMatchService
{
    private const double AutoSaveThreshold = 0.8;
    private const string OllamaModel = "entegrasyon-coder";
    private const double Temperature = 0.1;

    public async Task<IDataResult<BrandAutoMatchResultDto>> AutoMatchAsync(
        int marketPlaceId, CancellationToken ct = default)
    {
        // 1. Load unmapped brands
        var unmapped = await brandMatchService.GetUnmappedBrandsAsync(marketPlaceId);
        if (unmapped.Count == 0)
            return new SuccessDataResult<BrandAutoMatchResultDto>(new BrandAutoMatchResultDto());

        // 2. Load all marketplace brands (empty query = all, capped at 20 in service)
        //    For small datasets this is fine. For large ones a paginated approach is needed.
        var mpBrandsResult = await searchService.SearchBrandsAsync(marketPlaceId, "", ct);
        var mpBrands = mpBrandsResult.Success ? mpBrandsResult.Data ?? [] : [];

        // 3. Round 1: String matching
        var autoMatched = 0;
        var remaining = new List<BrandDto>();

        foreach (var brand in unmapped)
        {
            var match = FindStringMatch(brand.Name, mpBrands);
            if (match is not null)
            {
                var saved = await TrySaveMatchAsync(brand.Id, marketPlaceId, match.Id);
                if (saved) autoMatched++;
                else remaining.Add(brand); // save failed, try Ollama
            }
            else
            {
                remaining.Add(brand);
            }
        }

        // 4. Round 2: Ollama fallback for remaining
        var suggestions = new List<BrandAutoMatchSuggestionDto>();
        var failed = 0;

        if (remaining.Count > 0 && mpBrands.Count > 0)
        {
            var ollamaSuggestions = await CallOllamaAsync(remaining, mpBrands, ct);

            foreach (var suggestion in ollamaSuggestions)
            {
                if (suggestion.Confidence >= AutoSaveThreshold)
                {
                    var saved = await TrySaveMatchAsync(
                        suggestion.ApplicationBrandId, marketPlaceId, suggestion.MarketPlaceBrandId);
                    if (saved)
                    {
                        autoMatched++;
                        remaining.RemoveAll(b => b.Id == suggestion.ApplicationBrandId);
                    }
                    else
                    {
                        suggestions.Add(suggestion);
                    }
                }
                else
                {
                    suggestions.Add(suggestion);
                    remaining.RemoveAll(b => b.Id == suggestion.ApplicationBrandId);
                }
            }

            // Brands still in remaining after Ollama = truly unmatched
            failed = remaining.Count(b =>
                !suggestions.Any(s => s.ApplicationBrandId == b.Id));
        }
        else if (remaining.Count > 0)
        {
            failed = remaining.Count;
        }

        var resultDto = new BrandAutoMatchResultDto
        {
            AutoMatchedCount = autoMatched,
            SuggestionCount = suggestions.Count,
            FailedCount = failed,
            Suggestions = suggestions
        };

        return new SuccessDataResult<BrandAutoMatchResultDto>(resultDto);
    }

    public async Task<bool> IsOllamaAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ollama");
            var response = await client.GetAsync("", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ─── String Matching ───────────────────────────────────────────────────

    private static MarketplaceBrandSearchResult? FindStringMatch(
        string appBrandName, List<MarketplaceBrandSearchResult> mpBrands)
    {
        // Priority 1: Exact match (case-insensitive, trimmed)
        var exact = mpBrands.FirstOrDefault(m =>
            string.Equals(m.Name.Trim(), appBrandName.Trim(),
                StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // Priority 2: Normalized match (Turkish chars stripped)
        var normalizedApp = TurkishStringNormalizer.Normalize(appBrandName.Trim());
        var normalized = mpBrands.FirstOrDefault(m =>
            TurkishStringNormalizer.Normalize(m.Name.Trim()) == normalizedApp);
        if (normalized is not null) return normalized;

        // Priority 3: Contains match (either direction)
        var contains = mpBrands.FirstOrDefault(m =>
            m.Name.Contains(appBrandName, StringComparison.OrdinalIgnoreCase) ||
            appBrandName.Contains(m.Name, StringComparison.OrdinalIgnoreCase));
        return contains;
    }

    private async Task<bool> TrySaveMatchAsync(int appBrandId, int marketPlaceId, int mpBrandId)
    {
        try
        {
            var result = await brandMatchService.CreateBrandMappingAsync(new CreateBrandMarketPlaceMatchDto
            {
                ApplicationBrandId = appBrandId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceBrandId = mpBrandId
            });
            return result.Success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Brand mapping kaydedilemedi: AppBrandId={AppBrandId}", appBrandId);
            return false;
        }
    }

    // ─── Ollama Fallback ───────────────────────────────────────────────────

    private async Task<List<BrandAutoMatchSuggestionDto>> CallOllamaAsync(
        List<BrandDto> appBrands,
        List<MarketplaceBrandSearchResult> mpBrands,
        CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ollama");

            var appJson = JsonSerializer.Serialize(appBrands.Select(b => new { id = b.Id, name = b.Name }));
            var mpJson = JsonSerializer.Serialize(mpBrands.Select(b => new { id = b.Id, name = b.Name }));
            var prompt = BuildPrompt(appJson, mpJson);

            var requestBody = new
            {
                model = OllamaModel,
                prompt,
                stream = false,
                options = new { temperature = Temperature }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("api/generate", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

            if (string.IsNullOrWhiteSpace(ollamaResponse?.Response)) return [];

            var ollamaResults = JsonSerializer.Deserialize<List<OllamaMatchResult>>(
                ollamaResponse.Response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            return ollamaResults
                .Select(r =>
                {
                    var appBrand = appBrands.FirstOrDefault(b => b.Id == r.AppId);
                    var mpBrand = mpBrands.FirstOrDefault(b => b.Id == r.MpId);
                    if (appBrand is null || mpBrand is null) return null;

                    return new BrandAutoMatchSuggestionDto
                    {
                        ApplicationBrandId = appBrand.Id,
                        ApplicationBrandName = appBrand.Name,
                        MarketPlaceBrandId = mpBrand.Id,
                        MarketPlaceBrandName = mpBrand.Name,
                        Confidence = r.Confidence,
                        Reason = r.Reason
                    };
                })
                .Where(s => s is not null)
                .Select(s => s!)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Ollama servisine baglanamadi, brand auto-match Ollama adimi atlanıyor.");
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ollama yaniti parse edilemedi.");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ollama brand auto-match beklenmedik hata.");
            return [];
        }
    }

    private static string BuildPrompt(string appBrandsJson, string mpBrandsJson) => $$"""
        Sen bir e-ticaret marka eslestirme uzmanisis. Uygulama markalarini marketplace markalariyla isim benzerligine gore eslestir.

        Uygulama markalari:
        {{appBrandsJson}}

        Marketplace markalari:
        {{mpBrandsJson}}

        SADECE JSON array dondur, baska bir sey yazma. Format:
        [
          {"appId": 1, "mpId": 100, "confidence": 0.9, "reason": "Ayni marka farki isim varyanti"}
        ]

        confidence degeri 0-1 arasi olmali:
        - 0.8-1.0: Cok yuksek guven (ayni marka, sadece kucuk fark)
        - 0.5-0.8: Orta guven (muhtemelen ayni marka ama emin degilim)
        - 0.0-0.5: Dusuk guven (tahmini eslestirme)

        Eslestirme bulunamazsa: []
        Her uygulama markasi icin en fazla 1 eslestirme yap.
        """;

    // ─── Private record types ──────────────────────────────────────────────

    private record OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; init; } = string.Empty;
    }

    private record OllamaMatchResult
    {
        [JsonPropertyName("appId")]
        public int AppId { get; init; }

        [JsonPropertyName("mpId")]
        public int MpId { get; init; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonPropertyName("reason")]
        public string Reason { get; init; } = string.Empty;
    }
}
```

### 2f. DI Registration

File: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

`BrandAutoMatchService` follows the `IXxxService → XxxService` naming convention but Scrutor only scans specific namespaces. Add explicit registration after the Scrutor scan block:

```csharp
// Brand auto-match (explicit — IBrandAutoMatchService naming exception vs Scrutor)
services.AddScoped<IBrandAutoMatchService, BrandAutoMatchService>();
```

### 2g. Run tests + commit

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~BrandAutoMatchServiceTests"
dotnet build Entegrasyon.sln
```

Commit message: `feat(auto-match): add BrandAutoMatchService — string matching + Ollama fallback`

---

## Task 3: UI — "Tümünü Otomatik Eşleştir" Button + Result Dialog

### 3a. AutoMatchResultDialog Component (new)

File: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AutoMatchResultDialog.razor` (new)

```razor
@using Entegrasyon.Entity.Dtos.Brand

<MudDialog>
    <DialogContent>
        <MudStack Spacing="3">
            <MudGrid Spacing="2">
                <MudItem xs="4">
                    <MudPaper Elevation="0" Class="pa-3" Style="text-align: center; background: var(--mud-palette-success-lighten);">
                        <MudText Typo="Typo.h5" Color="Color.Success">@Result.AutoMatchedCount</MudText>
                        <MudText Typo="Typo.caption">Otomatik Eşleşti</MudText>
                    </MudPaper>
                </MudItem>
                <MudItem xs="4">
                    <MudPaper Elevation="0" Class="pa-3" Style="text-align: center; background: var(--mud-palette-warning-lighten);">
                        <MudText Typo="Typo.h5" Color="Color.Warning">@Result.SuggestionCount</MudText>
                        <MudText Typo="Typo.caption">Öneri Var</MudText>
                    </MudPaper>
                </MudItem>
                <MudItem xs="4">
                    <MudPaper Elevation="0" Class="pa-3" Style="text-align: center; background: var(--mud-palette-error-lighten);">
                        <MudText Typo="Typo.h5" Color="Color.Error">@Result.FailedCount</MudText>
                        <MudText Typo="Typo.caption">Eşleşmedi</MudText>
                    </MudPaper>
                </MudItem>
            </MudGrid>

            @if (Result.Suggestions.Count > 0)
            {
                <MudText Typo="Typo.subtitle1" Class="mt-2">Onay Bekleyen Öneriler</MudText>
                <MudStack Spacing="1">
                    @foreach (var suggestion in Result.Suggestions)
                    {
                        <MudPaper Elevation="0" Outlined="true" Class="pa-2">
                            <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
                                <MudText Typo="Typo.body2" Style="flex: 1; min-width: 100px;">
                                    @suggestion.ApplicationBrandName
                                </MudText>
                                <MudIcon Icon="@Icons.Material.Filled.ArrowForward" Size="Size.Small" />
                                <MudText Typo="Typo.body2" Style="flex: 1; min-width: 100px;">
                                    @suggestion.MarketPlaceBrandName
                                </MudText>
                                <MudTooltip Text="@($"Güven: {suggestion.Confidence:P0} — {suggestion.Reason}")">
                                    <MudProgressLinear Value="@(suggestion.Confidence * 100)"
                                                      Color="@GetConfidenceColor(suggestion.Confidence)"
                                                      Style="width: 80px;" />
                                </MudTooltip>
                                <MudIconButton Icon="@Icons.Material.Filled.Check"
                                               Color="Color.Success" Size="Size.Small"
                                               OnClick="@(() => ApproveSuggestion(suggestion))"
                                               Disabled="_approving.Contains(suggestion.ApplicationBrandId)" />
                                <MudIconButton Icon="@Icons.Material.Filled.Close"
                                               Color="Color.Error" Size="Size.Small"
                                               OnClick="@(() => RejectSuggestion(suggestion))" />
                            </div>
                        </MudPaper>
                    }
                </MudStack>
            }
        </MudStack>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Close" Variant="Variant.Filled" Color="Color.Primary">Kapat</MudButton>
    </DialogActions>
</MudDialog>
```

File: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/AutoMatchResultDialog.razor.cs` (new)

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class AutoMatchResultDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public BrandAutoMatchResultDto Result { get; set; } = new();
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public EventCallback OnMappingsChanged { get; set; }

    [Inject] private IBrandMatchService BrandMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private readonly HashSet<int> _approving = [];
    private readonly HashSet<int> _rejected = [];

    private async Task ApproveSuggestion(BrandAutoMatchSuggestionDto suggestion)
    {
        _approving.Add(suggestion.ApplicationBrandId);
        try
        {
            var result = await BrandMatchService.CreateBrandMappingAsync(new CreateBrandMarketPlaceMatchDto
            {
                ApplicationBrandId = suggestion.ApplicationBrandId,
                MarketPlaceId = MarketPlaceId,
                MarketPlaceBrandId = suggestion.MarketPlaceBrandId
            });

            if (result.Success)
            {
                Snackbar.Add($"{suggestion.ApplicationBrandName} eşleştirildi.", Severity.Success);
                Result.Suggestions.Remove(suggestion);
                await OnMappingsChanged.InvokeAsync();
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
        finally
        {
            _approving.Remove(suggestion.ApplicationBrandId);
        }
    }

    private void RejectSuggestion(BrandAutoMatchSuggestionDto suggestion)
    {
        Result.Suggestions.Remove(suggestion);
        _rejected.Add(suggestion.ApplicationBrandId);
    }

    private void Close() => MudDialog.Close();

    private static Color GetConfidenceColor(double confidence) => confidence switch
    {
        >= 0.8 => Color.Success,
        >= 0.5 => Color.Warning,
        _ => Color.Error
    };
}
```

### 3b. BrandMappingPage changes

File: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor`

Add marketplace selector + auto-match button to the summary section. Insert after the `</MudGrid>` for summary stats:

```razor
<!-- Marketplace Selector + Auto-Match Button -->
<MudPaper Elevation="1" Class="pa-3 mb-4">
    <div style="display: flex; align-items: center; gap: 16px; flex-wrap: wrap;">
        <MudSelect T="int?" Label="Marketplace" Variant="Variant.Outlined" Dense="true"
                   Value="_selectedMarketplaceId" ValueChanged="OnMarketplaceSelected"
                   Style="min-width: 200px;">
            @foreach (var mp in _marketplaces)
            {
                <MudSelectItem T="int?" Value="@((int?)mp.Id)">@mp.Name</MudSelectItem>
            }
        </MudSelect>

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.AutoAwesome"
                   OnClick="RunAutoMatch"
                   Disabled="@(_selectedMarketplaceId is null || _isAutoMatching)"
                   Class="ml-auto">
            @if (_isAutoMatching)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                <span>Eşleştiriliyor...</span>
            }
            else
            {
                <span>Tümünü Otomatik Eşleştir</span>
            }
        </MudButton>
    </div>
</MudPaper>
```

File: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor.cs`

Add to class:
```csharp
[Inject] private IBrandAutoMatchService BrandAutoMatchService { get; set; } = null!;
[Inject] private IDialogService DialogService { get; set; } = null!;

private int? _selectedMarketplaceId;
private bool _isAutoMatching = false;

private void OnMarketplaceSelected(int? marketplaceId)
{
    _selectedMarketplaceId = marketplaceId;
}

private async Task RunAutoMatch()
{
    if (_selectedMarketplaceId is null) return;

    _isAutoMatching = true;
    try
    {
        var result = await BrandAutoMatchService.AutoMatchAsync(_selectedMarketplaceId.Value);
        if (!result.Success)
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            return;
        }

        // Refresh data
        await OnMappingChanged();

        // Show result dialog
        var parameters = new DialogParameters
        {
            [nameof(AutoMatchResultDialog.Result)] = result.Data,
            [nameof(AutoMatchResultDialog.MarketPlaceId)] = _selectedMarketplaceId.Value,
            [nameof(AutoMatchResultDialog.OnMappingsChanged)] = EventCallback.Factory.Create(this, OnMappingChanged)
        };

        await DialogService.ShowAsync<AutoMatchResultDialog>(
            "Otomatik Eşleştirme Sonucu",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
    }
    finally
    {
        _isAutoMatching = false;
    }
}
```

### 3c. Build + commit

```bash
dotnet build Entegrasyon.sln
```

Commit message: `feat(ui): add "Tumunu Otomatik Eslestir" button and result dialog to BrandMappingPage`

---

## Task 4: UI — Duplicate Prevention

### 4a. Add `AllMappings` parameter to `BrandMappingDetailPanel`

The goal: when a user searches for a marketplace brand, already-matched brands are shown as disabled with a "Kullanılıyor" chip.

File: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor.cs`

Add new parameter:
```csharp
/// <summary>
/// Tum markalar icin mevcut mappingler — duplicate prevention icin.
/// </summary>
[Parameter] public List<BrandMarketPlaceMatchDto> AllMappings { get; set; } = [];
```

Add helper:
```csharp
/// <summary>
/// Bu marketplace brand'i baska bir uygulama markasina eslestirildiyse o markayi dondurur.
/// </summary>
private BrandMarketPlaceMatchDto? GetExistingUsageForMpBrand(int marketPlaceId, int mpBrandId) =>
    AllMappings.FirstOrDefault(m =>
        m.MarketPlaceId == marketPlaceId &&
        m.MarketPlaceBrandId == mpBrandId &&
        m.ApplicationBrandId != Brand?.Id);
```

File: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor`

In the `MudAutocomplete` block, wrap each result with a disabled state. Replace the `<MudAutocomplete>` with:

```razor
<MudAutocomplete T="MarketplaceBrandSearchResult"
                 Value="@(_selectedResults.GetValueOrDefault(mp.Id))"
                 ValueChanged="@(v => OnResultSelected(mp.Id, v))"
                 SearchFunc="@((value, ct) => SearchBrands(mp.Id, value, ct))"
                 Placeholder="Marketplace markası ara..."
                 Variant="Variant.Outlined"
                 Dense="true"
                 ToStringFunc="@(x => x?.Name ?? "")"
                 DebounceInterval="300"
                 MinCharacters="0"
                 ItemTemplate="@BrandSearchItemTemplate"
                 Class="flex-grow-1" />
```

Add `ItemTemplate` as a `RenderFragment<MarketplaceBrandSearchResult>`:

```razor
@* Add at bottom of .razor file, inside else block *@
```

In the code-behind, add:

```csharp
private RenderFragment<MarketplaceBrandSearchResult> BrandSearchItemTemplate(int marketPlaceId) =>
    brand =>
    {
        var usage = GetExistingUsageForMpBrand(marketPlaceId, brand.Id);
        return @<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1">
            <MudText Typo="Typo.body2"
                     Style="@(usage != null ? "color: var(--mud-palette-text-disabled);" : "")">
                @brand.Name
            </MudText>
            @if (usage != null)
            {
                <MudChip T="string" Size="Size.Small" Color="Color.Default" Disabled="true">
                    Kullanılıyor: @(usage.ApplicationBrandName ?? $"Marka #{usage.ApplicationBrandId}")
                </MudChip>
            }
        </MudStack>;
    };
```

**Note on MudAutocomplete ItemTemplate:** MudBlazor's `MudAutocomplete<T>` supports `ItemTemplate` as a `RenderFragment<T>`. The item itself is not disabled at the autocomplete level (no built-in disabled-item support in MudBlazor v7) — it just shows a visual indicator. Actual save prevention is handled by the service layer (composite PK check in `CreateBrandMappingAsync`).

Pass `AllMappings` down from `BrandMappingPage.razor`:

```razor
<BrandMappingDetailPanel Brand="_selectedBrand"
                          BrandMappings="_selectedBrandMappings"
                          AllMappings="_allMappings"
                          Marketplaces="_marketplaces"
                          OnMappingChanged="OnMappingChanged" />
```

### 4b. Build + commit

```bash
dotnet build Entegrasyon.sln
```

Commit message: `feat(ui): add duplicate prevention chips to BrandMappingDetailPanel`

---

## Task 5: Final Verification

### 5a. Full test suite

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

Expected: all existing tests pass + new tests for `TrendyolMarketplaceSearchServiceTests` and `BrandAutoMatchServiceTests`.

### 5b. Build

```bash
dotnet build Entegrasyon.sln
```

### 5c. Manual smoke test checklist

1. Navigate to `/marketplace/sync/brands`
2. Verify summary stats load (Toplam / Eşleştirilen / Eksik)
3. Select a brand from the list
4. In detail panel, select Trendyol marketplace — verify autocomplete returns real brands (not empty list)
5. Select N11/other marketplace — verify autocomplete returns DB-based brand names (or "Brand #N" if no names stored)
6. Map a brand manually, verify "Kullanılıyor" chip appears for that brand in other brands' search results
7. Click "Tümünü Otomatik Eşleştir" with no marketplace selected — verify button is disabled
8. Select marketplace, click button — verify loading spinner, result dialog appears
9. In result dialog: verify counts, approve/reject suggestions work
10. After dialog closes, verify list panel updates (mapped brand turns green)

---

## Architecture Notes

- `TrendyolMarketplaceSearchService` gets `IHttpClientFactory` injected — uses `StringConstants.TrendyolApi` named client (already registered in `AddClients()` with base `https://apigw.trendyol.com/integration/`).
- `BrandAutoMatchService` is registered explicitly (not via Scrutor) because `IBrandAutoMatchService` naming does not match Scrutor's `IXxxManager → XxxManager` convention.
- `BrandAutoMatchResultDto.Suggestions` is `List<T>` (mutable) to allow approve/reject in dialog.
- DB fallback uses `MarketPlaceBrandExternalId` as brand name for non-integer-ID marketplaces (e.g., Pazarama uses GUIDs).
- Ollama timeout is 120s (configured in `AddClients()`), sufficient for batch brand matching.
- No new migrations needed — no entity changes.
