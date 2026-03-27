using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Entegrasyon.UnitTest.CategoryMatch;

public class CategoryAutoMatchServiceTests : BaseTest
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IMarketplaceSearchService> _mockSearchService;
    private readonly Mock<ILogger<CategoryAutoMatchService>> _mockLogger;
    private readonly CategoryAutoMatchService _sut;
    private readonly MockHttpMessageHandler _mockHandler;

    public CategoryAutoMatchServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockSearchService = new Mock<IMarketplaceSearchService>();
        _mockLogger = new Mock<ILogger<CategoryAutoMatchService>>();
        _mockHandler = new MockHttpMessageHandler();

        var httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(httpClient);

        _sut = new CategoryAutoMatchService(
            _mockHttpClientFactory.Object,
            _mockSearchService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAutoMatchSuggestionsAsync_EmptyCategories_ReturnsEmptyList()
    {
        // Arrange
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
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

        // Mock search service to resolve marketplace category
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
        // Arrange - simulate connection refused
        _mockHandler.SetException(new HttpRequestException("Connection refused"));

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
    public async Task GetAutoMatchSuggestionsAsync_OllamaReturnsInvalidJson_ReturnsEmptyGracefully()
    {
        // Arrange
        var ollamaResponse = new { response = "This is not valid JSON" };
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

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
        // Arrange
        var categories = Enumerable.Range(1, 60)
            .Select(i => new CategoryAutoMatchItemDto { CategoryId = i, CategoryName = $"Kategori {i}" })
            .ToList();

        var ollamaResponse = new { response = "[]" };
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = 1,
            Categories = categories
        };

        // Act
        var result = await _sut.GetAutoMatchSuggestionsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _mockHandler.CallCount.Should().Be(2); // 50 + 10
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
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

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
        _mockHandler.SetResponse(HttpStatusCode.OK, "Ollama is running");

        // Act
        var available = await _sut.IsAvailableAsync();

        // Assert
        available.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_OllamaOffline_ReturnsFalse()
    {
        // Arrange
        _mockHandler.SetException(new HttpRequestException("Connection refused"));

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
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

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
        result.Data[0].Confidence.Should().BeGreaterOrEqualTo(0.8);
        result.Data[0].Reason.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test helper: Mock HTTP message handler for simulating Ollama API responses.
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _responseContent = "";
        private Exception? _exception;
        public int CallCount { get; private set; }

        public void SetResponse(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _responseContent = content;
            _exception = null;
        }

        public void SetException(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            if (_exception is not null)
                throw _exception;

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            });
        }
    }
}
