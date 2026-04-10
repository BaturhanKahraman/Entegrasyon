using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// AttributeAutoMatchService testleri — Ollama HTTP cagrilarini WireMock ile simule eder.
/// CategoryAutoMatchService ile ayni pattern: StubOllama + SimulateOllamaOffline helper'lari.
/// </summary>
[Collection(WireMockCollection.Name)]
public class AttributeAutoMatchServiceTests : BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<AttributeAutoMatchService>> _mockLogger;
    private readonly AttributeAutoMatchService _sut;

    public AttributeAutoMatchServiceTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<AttributeAutoMatchService>>();

        _mockHttpClientFactory
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(() => new HttpClient { BaseAddress = new Uri(_wm.BaseUrl) });

        _sut = new AttributeAutoMatchService(
            _mockHttpClientFactory.Object,
            _mockLogger.Object);
    }

    /// <summary>
    /// Ollama /api/generate endpoint'ini stub'lar.
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

    private void StubOllamaAvailable()
    {
        _wm.Server
            .Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Ollama is running"));
    }

    /// <summary>
    /// Factory'ye kasitli connection-refused URL doner → offline Ollama senaryosu.
    /// </summary>
    private void SimulateOllamaOffline()
    {
        _mockHttpClientFactory
            .Setup(f => f.CreateClient("Ollama"))
            .Returns(() => new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1/") });
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
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

        var appAttributes = new List<AppAttributeForMatchDto> { new(1, "Renk") };
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
        SimulateOllamaOffline();

        // Act
        var available = await _sut.IsAvailableAsync();

        // Assert
        available.Should().BeFalse();
    }

    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Return_Empty_When_Ollama_Offline()
    {
        // Arrange
        SimulateOllamaOffline();

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
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

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
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

        var appValues = new List<AppValueForMatchDto> { new(10, "Kırmızı") };
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
        SimulateOllamaOffline();

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
        StubOllamaAvailable();

        // Act
        var available = await _sut.IsAvailableAsync();

        // Assert
        available.Should().BeTrue();
    }

    [Fact]
    public async Task SuggestAttributeMatchesAsync_Should_Return_Empty_When_AppAttributes_Empty()
    {
        // Arrange — no HTTP call expected
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
        StubOllama(JsonSerializer.Serialize(ollamaResponse));

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
}
