using System.Net;
using System.Text;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolApiClient unit tests — verifies Basic Auth header injection,
/// User-Agent header, URL construction and error handling.
/// </summary>
public class TrendyolApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<TrendyolApiClient>> _loggerMock = new();

    private TrendyolApiClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlaces(IEnumerable<MarketPlace> marketPlaces)
    {
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces.ToList());
    }

    private static MarketPlace CreateTrendyolMarketPlace(
        string? baseUrl = null,
        string? apiKey = "test-api-key",
        string? apiSecret = "test-api-secret",
        string? sellerId = "12345",
        string? userAgentPrefix = null) => new()
    {
        Id = TrendyolMarketPlaceId,
        Name = "Trendyol",
        ApiKey = apiKey,
        ApiSecret = apiSecret,
        SellerId = sellerId,
        BaseUrl = baseUrl,
        UserAgentPrefix = userAgentPrefix
    };

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

    // ── Test 1: GET injects Basic Auth header ──

    [Fact]
    public async Task GetAsync_InjectsBasicAuthHeader()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace(apiKey: "myKey", apiSecret: "mySecret");
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("test/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Basic");

        var expectedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("myKey:mySecret"));
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be(expectedCredentials);
    }

    // ── Test 2: POST sends JSON body with auth ──

    [Fact]
    public async Task PostAsync_SendsJsonBody_WithBasicAuthHeader()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.PostAsync("test/products", new { name = "Test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.Content.Should().NotBeNull();
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        handler.LastRequest.Headers.Authorization.Should().NotBeNull();
    }

    // ── Test 3: PUT sends JSON body ──

    [Fact]
    public async Task PutAsync_SendsJsonBody()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.PutAsync("test/update", new { data = 1 });

        // Assert
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    // ── Test 4: DELETE sends request ──

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.DeleteAsync("test/delete/123");

        // Assert
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
    }

    // ── Test 5: Missing marketplace throws ──

    [Fact]
    public async Task GetAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupMarketPlaces(Enumerable.Empty<MarketPlace>());

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("test"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Trendyol*");
    }

    // ── Test 6: Uses BaseUrl from MarketPlace ──

    [Fact]
    public async Task GetAsync_UsesBaseUrlFromMarketPlace()
    {
        // Arrange
        var customBaseUrl = "https://custom.trendyol.example.com";
        var mp = CreateTrendyolMarketPlace(baseUrl: customBaseUrl);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        await sut.GetAsync("test/endpoint");

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().StartWith(customBaseUrl);
    }

    // ── Test 7: Uses default BaseUrl when null ──

    [Fact]
    public async Task GetAsync_UsesDefaultBaseUrl_WhenMarketPlaceBaseUrlIsNull()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace(baseUrl: null);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        await sut.GetAsync("test");

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("apigw.trendyol.com");
    }

    // ── Test 8: User-Agent from UserAgentPrefix ──

    [Fact]
    public async Task GetAsync_UsesUserAgentPrefixFromMarketPlace()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace(userAgentPrefix: "MySeller - SelfIntegration");
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        await sut.GetAsync("test");

        // Assert
        handler.LastRequest!.Headers.UserAgent.ToString().Should().Contain("MySeller - SelfIntegration");
    }

    // ── Test 9: User-Agent fallback from SellerId ──

    [Fact]
    public async Task GetAsync_GeneratesUserAgent_FromSellerId_WhenPrefixIsNull()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace(userAgentPrefix: null, sellerId: "99999");
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        await sut.GetAsync("test");

        // Assert
        handler.LastRequest!.Headers.UserAgent.ToString().Should().Contain("99999");
        handler.LastRequest.Headers.UserAgent.ToString().Should().Contain("SelfIntegration");
    }
}
