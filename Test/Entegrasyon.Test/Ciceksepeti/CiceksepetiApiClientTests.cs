using System.Net;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiApiClient unit tests — verifies x-api-key header injection and URL construction.
/// </summary>
public class CiceksepetiApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<CiceksepetiApiClient>> _loggerMock = new();

    public CiceksepetiApiClientTests()
    {
        // Clear static credential cache between tests to avoid test pollution
        CiceksepetiApiClient.ClearCredentialCache();
    }

    private CiceksepetiApiClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlaces(IEnumerable<MarketPlace> marketPlaces)
    {
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces.ToList());
    }

    private MarketPlace CreateCiceksepetiMarketPlace(
        string? baseUrl = null,
        string? apiKey = "test-api-key") => new()
    {
        Id = CiceksepetiMarketPlaceId,
        Name = "Çiçeksepeti",
        ApiKey = apiKey,
        BaseUrl = baseUrl
    };

    /// <summary>
    /// MockHttpMessageHandler — captures the last request for assertion.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(ResponseToReturn);
        }
    }

    private void SetupFactoryWithHandler(HttpMessageHandler handler)
    {
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler));
    }

    // ── Test 1 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_InjectsApiKeyHeader()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace(apiKey: "my-secret-key");
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Should().ContainKey("x-api-key");
        handler.LastRequest.Headers.GetValues("x-api-key").Should().Contain("my-secret-key");
    }

    // ── Test 2 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostAsync_SendsJsonBody_WithApiKeyHeader()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace(apiKey: "post-api-key");
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();
        var body = new { name = "Test Product", quantity = 5 };

        // Act
        var response = await sut.PostAsync("products", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.Headers.Should().ContainKey("x-api-key");
        handler.LastRequest.Headers.GetValues("x-api-key").Should().Contain("post-api-key");

        // Verify JSON content type
        handler.LastRequest.Content.Should().NotBeNull();
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    // ── Test 3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendRawAsync_UsesAbsolutePath_NoApiV1Prefix()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace(apiKey: "raw-api-key");
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.SendRawAsync("/Branch/SendInvoiceMail", HttpMethod.Post, content: null);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        var url = handler.LastRequest!.RequestUri!.ToString();

        // Must contain the path without /api/v1/ prefix
        url.Should().Contain("/Branch/SendInvoiceMail");
        url.Should().NotContain("/api/v1/");

        // Must have x-api-key
        handler.LastRequest.Headers.Should().ContainKey("x-api-key");
        handler.LastRequest.Headers.GetValues("x-api-key").Should().Contain("raw-api-key");
    }

    // ── Test 4 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange — empty marketplace table
        SetupMarketPlaces(Enumerable.Empty<MarketPlace>());

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("products"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Çiçeksepeti*");
    }

    // ── Test 5 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_UsesBaseUrlFromMarketPlace()
    {
        // Arrange — custom BaseUrl in DB
        var customBaseUrl = "https://custom.ciceksepeti.example.com";
        var mp = CreateCiceksepetiMarketPlace(baseUrl: customBaseUrl, apiKey: "key");
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("products");

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().StartWith(customBaseUrl);
    }

    // ── Extra test: Missing ApiKey throws ────────────────────────────────────────

    [Fact]
    public async Task GetAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange — marketplace exists but ApiKey is null
        var mp = CreateCiceksepetiMarketPlace(apiKey: null);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("products"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    // ── Extra test: Default base URL fallback ────────────────────────────────────

    [Fact]
    public async Task GetAsync_UsesDefaultBaseUrl_WhenMarketPlaceBaseUrlIsNull()
    {
        // Arrange — BaseUrl is null, should fall back to https://apis.ciceksepeti.com
        var mp = CreateCiceksepetiMarketPlace(baseUrl: null);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("products");

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("apis.ciceksepeti.com");
    }

    // ── Extra test: /api/v1/ prefix on standard GET ──────────────────────────────

    [Fact]
    public async Task GetAsync_PrependsApiV1Prefix()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("products");

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("/api/v1/");
    }
}
