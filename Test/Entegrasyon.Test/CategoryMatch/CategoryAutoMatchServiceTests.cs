using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Entegrasyon.UnitTest.CategoryMatch;

/// <summary>
/// CategoryAutoMatchService testleri — Ollama HTTP cagrilarini WireMock ile simule eder.
/// Offline scenarios icin factory'ye kasitli connection-refused URL verilir (port 1).
/// </summary>
[Collection(WireMockCollection.Name)]
public class CategoryAutoMatchServiceTests : BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IMarketplaceSearchService> _mockSearchService;
    private readonly Mock<ILogger<CategoryAutoMatchService>> _mockLogger;
    private readonly CategoryAutoMatchService _sut;

    public CategoryAutoMatchServiceTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockSearchService = new Mock<IMarketplaceSearchService>();
        _mockLogger = new Mock<ILogger<CategoryAutoMatchService>>();

        // Default: factory Ollama client'i WireMock URL'ine yonlendirir
        _mockHttpClientFactory
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(() => new HttpClient { BaseAddress = new Uri(_wm.BaseUrl) });

        // Default: no categories in DB → leaf guard passes everything through
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(new List<Category>());

        _sut = new CategoryAutoMatchService(
            _mockHttpClientFactory.Object,
            _mockSearchService.Object,
            mockContextFactory.Object,
            _mockLogger.Object);
    }

    /// <summary>
    /// Ollama /api/generate endpoint'ini stub'lar. SUT bu path'e POST ediyor.
    /// </summary>
    private void StubOllama(string responseBody)
    {
        _wm.Server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseBody));
    }

    /// <summary>
    /// Ollama root / endpoint (IsAvailableAsync icin) 200 doner.
    /// </summary>
    private void StubOllamaAvailable()
    {
        _wm.Server
            .Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Ollama is running"));
    }

    /// <summary>
    /// SUT'un Ollama offline oldugu senaryosu icin: factory'ye kasitli olarak
    /// connection-refused URL verir (port 1 genelde reserved, connect immediately refused).
    /// </summary>
    private void SimulateOllamaOffline()
    {
        _mockHttpClientFactory
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(() => new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1/") });
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_EmptyCategories_ReturnsEmptyList()
    {
        // Arrange — no HTTP call expected
        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories = []
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_OllamaReturnsValidJson_ReturnsSuggestions()
    {
        // Arrange
        var ollamaResponse = new
        {
            response = JsonSerializer.Serialize(new[]
            {
                new { categoryId = 1, suggestedName = "Elektronik > Telefon", confidence = 0.85, reason = "Isim benzerli\u011fi y\u00fcksek" }
            })
        };
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

        _mockSearchService
            .Setup(s => s.SearchCategoriesAsync(1, "Elektronik > Telefon", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceCategorySearchResult>>(
                [new MarketplaceCategorySearchResult(500, "Telefon", "Elektronik > Telefon")]));

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories =
            [
                new CategoryAutoMatchItemDto { CategoryId = 1, CategoryName = "Telefonlar", ParentCategoryName = "Elektronik" }
            ]
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].ApplicationCategoryId.Should().Be(1);
        result.Data[0].SuggestedMarketPlaceCategoryId.Should().Be(500);
        result.Data[0].Confidence.Should().BeApproximately(0.85, 0.01);
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_OllamaOffline_ReturnsEmptyGracefully()
    {
        // Arrange — factory connection-refused URL'e yonlendirir
        SimulateOllamaOffline();

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories =
            [
                new CategoryAutoMatchItemDto { CategoryId = 1, CategoryName = "Telefonlar" }
            ]
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert — service graceful degradation: bos liste doner
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_OllamaReturnsInvalidJson_ReturnsEmptyGracefully()
    {
        // Arrange
        var ollamaResponse = new { response = "This is not valid JSON" };
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories =
            [
                new CategoryAutoMatchItemDto { CategoryId = 1, CategoryName = "Telefonlar" }
            ]
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_BatchesRequestsOver50()
    {
        // Arrange — 60 kategori → 2 batch bekleniyor (50 + 10)
        var categories = Enumerable.Range(1, 60)
            .Select(i => new CategoryAutoMatchItemDto { CategoryId = i, CategoryName = $"Kategori {i}" })
            .ToList();

        var ollamaResponse = new { response = "[]" };
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories = categories
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert — WireMock log entries sayisi batch sayisina esit olmali
        result.Success.Should().BeTrue();
        var logs = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/api/generate").UsingPost());
        logs.Should().HaveCount(2); // 50 + 10
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_SearchServiceFindsNoMatch_SkipsSuggestion()
    {
        // Arrange
        var ollamaResponse = new
        {
            response = JsonSerializer.Serialize(new[]
            {
                new { categoryId = 1, suggestedName = "NonExistent Category", confidence = 0.9, reason = "Test" }
            })
        };
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

        _mockSearchService
            .Setup(s => s.SearchCategoriesAsync(1, "NonExistent Category", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceCategorySearchResult>>([]));

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories =
            [
                new CategoryAutoMatchItemDto { CategoryId = 1, CategoryName = "Test" }
            ]
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task IsAvailableAsync_OllamaOnline_ReturnsTrue()
    {
        // Arrange
        StubOllamaAvailable();

        // Act
        var available = await _sut.IsAvailableAsync();

        // Assert
        available.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_OllamaOffline_ReturnsFalse()
    {
        // Arrange
        SimulateOllamaOffline();

        // Act
        var available = await _sut.IsAvailableAsync();

        // Assert
        available.Should().BeFalse();
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_HighConfidence_SetsCorrectValue()
    {
        // Arrange
        var ollamaResponse = new
        {
            response = JsonSerializer.Serialize(new[]
            {
                new { categoryId = 1, suggestedName = "Elektronik", confidence = 0.95, reason = "Tam e\u015fle\u015fme" }
            })
        };
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

        _mockSearchService
            .Setup(s => s.SearchCategoriesAsync(1, "Elektronik", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceCategorySearchResult>>(
                [new MarketplaceCategorySearchResult(100, "Elektronik", "Elektronik")]));

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories =
            [
                new CategoryAutoMatchItemDto { CategoryId = 1, CategoryName = "Elektronik" }
            ]
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert
        result.Data[0].Confidence.Should().BeGreaterThanOrEqualTo(0.8);
        result.Data[0].Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_Should_Filter_NonLeaf_Categories()
    {
        // Arrange — parent category (has child) should be filtered out
        var parent = new Category { Id = 1, Name = "Parent", IsDeleted = false };
        var child = new Category { Id = 2, Name = "Child", SuperCategoryId = 1, IsDeleted = false };
        var categories = new List<Category> { parent, child };
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories = new List<CategoryAutoMatchItemDto>
            {
                new() { CategoryId = 1, CategoryName = "Parent", ParentCategoryName = null }
            }
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert — empty because the only category was non-leaf (no HTTP call needed)
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
