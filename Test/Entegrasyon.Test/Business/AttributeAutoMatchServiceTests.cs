using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Entegrasyon.UnitTest.Business;

public class AttributeAutoMatchServiceTests : BaseTest
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<AttributeAutoMatchService>> _mockLogger;
    private readonly AttributeAutoMatchService _sut;
    private readonly MockHttpMessageHandler _mockHandler;

    public AttributeAutoMatchServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<AttributeAutoMatchService>>();
        _mockHandler = new MockHttpMessageHandler();

        var httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(httpClient);

        _sut = new AttributeAutoMatchService(
            _mockHttpClientFactory.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Return_Suggestions()
    {
        // Arrange
        var ollamaInnerResponse = JsonSerializer.Serialize(new[]
        {
            new { appId = 1, mpId = 101, confidence = 0.9 }
        });
        var ollamaResponse = new { response = ollamaInnerResponse };
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

        var appAttributes = new List<AppAttributeForMatchDto>
        {
            new(1, "Renk")
        };

        var marketplaceAttributes = new List<MarketplaceAttributeDto>
        {
            new(101, "Renk", true, false, []),
            new(102, "Beden", false, true, [])
        };

        // Act
        var result = await _sut.SuggestAttributeMatchesAsync(appAttributes, marketplaceAttributes);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].ApplicationAttributeId.Should().Be(1);
        result.Data[0].ApplicationAttributeName.Should().Be("Renk");
        result.Data[0].SuggestedMarketplaceAttributeId.Should().Be(101);
        result.Data[0].SuggestedMarketplaceAttributeName.Should().Be("Renk");
        result.Data[0].Confidence.Should().BeApproximately(0.9, 0.01);
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_False_When_Ollama_Unavailable()
    {
        // Arrange
        _mockHandler.SetException(new HttpRequestException("Connection refused"));

        // Act
        var available = await _sut.IsAvailableAsync();

        // Assert
        available.Should().BeFalse();
    }

    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Return_Empty_When_Ollama_Offline()
    {
        // Arrange
        _mockHandler.SetException(new HttpRequestException("Connection refused"));

        var appAttributes = new List<AppAttributeForMatchDto> { new(1, "Renk") };
        var marketplaceAttributes = new List<MarketplaceAttributeDto>
        {
            new(101, "Renk", true, false, [])
        };

        // Act
        var result = await _sut.SuggestAttributeMatchesAsync(appAttributes, marketplaceAttributes);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Return_Empty_When_InvalidJson()
    {
        // Arrange
        var ollamaResponse = new { response = "not valid json" };
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

        var appAttributes = new List<AppAttributeForMatchDto> { new(1, "Renk") };
        var marketplaceAttributes = new List<MarketplaceAttributeDto>
        {
            new(101, "Renk", true, false, [])
        };

        // Act
        var result = await _sut.SuggestAttributeMatchesAsync(appAttributes, marketplaceAttributes);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestValueMatchesAsync_Should_Return_Suggestions()
    {
        // Arrange
        var ollamaInnerResponse = JsonSerializer.Serialize(new[]
        {
            new { appId = 10, mpId = 200, confidence = 0.85 }
        });
        var ollamaResponse = new { response = ollamaInnerResponse };
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

        var appValues = new List<AppValueForMatchDto>
        {
            new(10, "Kırmızı")
        };

        var marketplaceValues = new List<MarketplaceAttributeValueDto>
        {
            new(200, "Kırmızı"),
            new(201, "Mavi")
        };

        // Act
        var result = await _sut.SuggestValueMatchesAsync(appValues, marketplaceValues);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].ApplicationValueId.Should().Be(10);
        result.Data[0].ApplicationValueName.Should().Be("Kırmızı");
        result.Data[0].SuggestedMarketplaceValueId.Should().Be(200);
        result.Data[0].SuggestedMarketplaceValueName.Should().Be("Kırmızı");
        result.Data[0].Confidence.Should().BeApproximately(0.85, 0.01);
    }

    [Fact]
    public async Task SuggestValueMatchesAsync_Should_Return_Empty_When_Ollama_Offline()
    {
        // Arrange
        _mockHandler.SetException(new HttpRequestException("Connection refused"));

        var appValues = new List<AppValueForMatchDto> { new(1, "Kırmızı") };
        var marketplaceValues = new List<MarketplaceAttributeValueDto> { new(200, "Kırmızı") };

        // Act
        var result = await _sut.SuggestValueMatchesAsync(appValues, marketplaceValues);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_True_When_Ollama_Online()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.OK, "Ollama is running");

        // Act
        var available = await _sut.IsAvailableAsync();

        // Assert
        available.Should().BeTrue();
    }

    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Return_Empty_When_AppAttributes_Empty()
    {
        // Arrange
        var appAttributes = new List<AppAttributeForMatchDto>();
        var marketplaceAttributes = new List<MarketplaceAttributeDto>
        {
            new(101, "Renk", true, false, [])
        };

        // Act
        var result = await _sut.SuggestAttributeMatchesAsync(appAttributes, marketplaceAttributes);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Skip_Unknown_AppIds_In_Response()
    {
        // Arrange — Ollama returns an appId that doesn't exist in appAttributes
        var ollamaInnerResponse = JsonSerializer.Serialize(new[]
        {
            new { appId = 999, mpId = 101, confidence = 0.9 }
        });
        var ollamaResponse = new { response = ollamaInnerResponse };
        _mockHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

        var appAttributes = new List<AppAttributeForMatchDto> { new(1, "Renk") };
        var marketplaceAttributes = new List<MarketplaceAttributeDto>
        {
            new(101, "Renk", true, false, [])
        };

        // Act
        var result = await _sut.SuggestAttributeMatchesAsync(appAttributes, marketplaceAttributes);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty(); // appId 999 not found → skipped
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
