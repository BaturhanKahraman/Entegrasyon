using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// BrandAutoMatchService testleri — string matching + Ollama fallback.
/// WireMock pattern: StubOllamaResponse/StubOllamaUnavailable helper'lari ile
/// servis davranisini simule eder. Offline senaryosu factory'nin connection-refused
/// URL'ine yonlendirilmesi ile.
/// </summary>
[Collection(WireMockCollection.Name)]
public class BrandAutoMatchServiceTests : BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IBrandMatchService> _brandMatchServiceMock = new();
    private readonly Mock<IMarketplaceSearchService> _searchServiceMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<BrandAutoMatchService>> _loggerMock = new();

    public BrandAutoMatchServiceTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(() => new HttpClient { BaseAddress = new Uri(_wm.BaseUrl) });
    }

    private BrandAutoMatchService CreateSut()
    {
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
        StubOllamaUnavailable();

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
        StubOllamaUnavailable();

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
        StubOllamaUnavailable();

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
        StubOllamaUnavailable();

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
        StubOllamaResponse([new { appId = 5, mpId = 500, confidence = 0.9, reason = "Benzer marka" }]);

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
        StubOllamaResponse([new { appId = 6, mpId = 600, confidence = 0.6, reason = "Kismi esleme" }]);

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
        // Arrange — Ollama offline (connection refused)
        SetupUnmappedBrands([new BrandDto { Id = 7, Name = "Some Brand" }]);
        SetupMarketplaceBrands([new MarketplaceBrandSearchResult(700, "Different Brand")]);
        SimulateOllamaOffline();

        var sut = CreateSut();

        // Act
        var result = await sut.AutoMatchAsync(1);

        // Assert — graceful, no exception bubbled
        result.Success.Should().BeTrue();
        result.Data.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task IsOllamaAvailableAsync_WhenOllamaOnline_ReturnsTrue()
    {
        // Arrange
        _wm.Server
            .Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Ollama is running"));
        var sut = CreateSut();

        // Act
        var available = await sut.IsOllamaAvailableAsync();

        // Assert
        available.Should().BeTrue();
    }

    [Fact]
    public async Task IsOllamaAvailableAsync_WhenOllamaOffline_ReturnsFalse()
    {
        // Arrange
        SimulateOllamaOffline();
        var sut = CreateSut();

        // Act
        var available = await sut.IsOllamaAvailableAsync();

        // Assert
        available.Should().BeFalse();
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

    /// <summary>
    /// Ollama 503 doner — SUT onu unavailable sayar ve Ollama fallback'ine gitmez
    /// (yalnizca string matching kullanir).
    /// </summary>
    private void StubOllamaUnavailable()
    {
        _wm.Server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody(""));
    }

    /// <summary>
    /// Ollama /api/generate endpoint'i saglanan result listesini wrap'li JSON olarak doner.
    /// </summary>
    private void StubOllamaResponse(IEnumerable<object> results)
    {
        var inner = JsonSerializer.Serialize(results);
        var outer = JsonSerializer.Serialize(new { response = inner });

        _wm.Server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(outer));
    }

    /// <summary>
    /// Factory'ye kasitli connection-refused URL doner → offline Ollama senaryosu.
    /// </summary>
    private void SimulateOllamaOffline()
    {
        _httpClientFactoryMock
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(() => new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1/") });
    }
}
