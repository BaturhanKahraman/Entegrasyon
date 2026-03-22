using System.Net;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<PazaramaApiClient>> _loggerMock = new();

    private PazaramaApiClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlaces(IEnumerable<MarketPlace> marketPlaces)
    {
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces.ToList());
    }

    /// <param name="tokenUrl">
    /// Pass a value to set explicitly. Use <c>null</c> to leave TokenUrl unset (tests default URL fallback).
    /// Use <c>"default"</c> (or omit) to use the real default token URL.
    /// </param>
    private MarketPlace CreatePazaramaMarketPlace(
        string? baseUrl = null,
        string? apiKey = "test-client-id",
        string? apiSecret = "test-client-secret",
        string tokenUrl = "https://isortagimgiris.pazarama.com/connect/token") => new()
    {
        Id = PazaramaMarketPlaceId,
        Name = "Pazarama",
        ApiKey = apiKey,
        ApiSecret = apiSecret,
        BaseUrl = baseUrl,
        TokenUrl = tokenUrl
    };

    /// <summary>
    /// Creates a mock HttpMessageHandler that matches based on URL content:
    /// - URLs containing "token" or the tokenUrl host → return token JSON
    /// - All other URLs → return the provided responseBody
    /// Returns new HttpClient instances so BaseAddress can be set freely.
    /// </summary>
    private static Mock<HttpMessageHandler> CreateHandlerMock(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string tokenJson = """{"access_token":"test-bearer-token","expires_in":3600,"token_type":"Bearer"}""")
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("token", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenJson)
            });

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    !req.RequestUri.ToString().Contains("token", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });

        return handlerMock;
    }

    /// <summary>
    /// Sets up factory to return a fresh HttpClient per call — avoids BaseAddress-after-first-request issues.
    /// </summary>
    private void SetupFactoryWithHandler(Mock<HttpMessageHandler> handlerMock)
    {
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object));
    }

    [Fact]
    public async Task GetAsync_Should_Set_BearerAuth_Header()
    {
        // Arrange
        var mp = CreatePazaramaMarketPlace();
        SetupMarketPlaces([mp]);

        HttpRequestMessage? capturedApiRequest = null;
        var handlerMock = CreateHandlerMock("{\"success\":true}");
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    !req.RequestUri.ToString().Contains("token", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedApiRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}")
            });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("/merchantgateway/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedApiRequest.Should().NotBeNull();
        capturedApiRequest!.Headers.Authorization.Should().NotBeNull();
        capturedApiRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedApiRequest.Headers.Authorization.Parameter.Should().Be("test-bearer-token");
    }

    [Fact]
    public async Task GetAsync_Should_Use_DefaultBaseUrl_When_None_Set()
    {
        // Arrange
        var mp = CreatePazaramaMarketPlace(baseUrl: null);
        SetupMarketPlaces([mp]);

        HttpRequestMessage? capturedApiRequest = null;
        var handlerMock = CreateHandlerMock("{}");
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    !req.RequestUri.ToString().Contains("token", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedApiRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("/merchantgateway/api/categories");

        // Assert
        capturedApiRequest.Should().NotBeNull();
        capturedApiRequest!.RequestUri!.ToString().Should().Contain("isortagimapi.pazarama.com");
    }

    [Fact]
    public async Task GetAsync_Should_Throw_When_MarketPlace_Not_Found()
    {
        // Arrange
        SetupMarketPlaces(Enumerable.Empty<MarketPlace>());

        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("/test"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pazarama*bulunamadı*");
    }

    [Fact]
    public async Task TokenCaching_SecondCall_ShouldNotRefetchToken()
    {
        // Arrange
        var mp = CreatePazaramaMarketPlace();
        SetupMarketPlaces([mp]);

        var tokenCallCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.RequestUri != null && req.RequestUri.ToString().Contains("token"))
                {
                    tokenCallCount++;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"access_token":"cached-token","expires_in":3600,"token_type":"Bearer"}""")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act — two consecutive calls
        await sut.GetAsync("/merchantgateway/api/categories");
        await sut.GetAsync("/merchantgateway/api/brands");

        // Assert — token endpoint called only once
        tokenCallCount.Should().Be(1);
    }

    [Fact]
    public async Task TokenRequest_Should_Use_BasicAuth_With_ClientCredentials()
    {
        // Arrange
        var mp = CreatePazaramaMarketPlace(apiKey: "my-client-id", apiSecret: "my-client-secret");
        SetupMarketPlaces([mp]);

        var expectedCredentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("my-client-id:my-client-secret"));

        HttpRequestMessage? capturedTokenRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.RequestUri != null && req.RequestUri.ToString().Contains("token"))
                {
                    capturedTokenRequest = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"access_token":"tok","expires_in":3600,"token_type":"Bearer"}""")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        capturedTokenRequest.Should().NotBeNull();
        capturedTokenRequest!.Headers.Authorization.Should().NotBeNull();
        capturedTokenRequest.Headers.Authorization!.Scheme.Should().Be("Basic");
        capturedTokenRequest.Headers.Authorization.Parameter.Should().Be(expectedCredentials);
    }

    [Fact]
    public async Task PostAsync_Should_Send_Json_Body_With_PostMethod()
    {
        // Arrange
        var mp = CreatePazaramaMarketPlace();
        SetupMarketPlaces([mp]);

        HttpRequestMessage? capturedRequest = null;
        var handlerMock = CreateHandlerMock("{\"success\":true}");
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    !req.RequestUri.ToString().Contains("token", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true}") });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act
        var response = await sut.PostAsync("/api/product", new { name = "Test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task PutAsync_Should_Send_PutMethod()
    {
        // Arrange
        var mp = CreatePazaramaMarketPlace();
        SetupMarketPlaces([mp]);

        HttpRequestMessage? capturedRequest = null;
        var handlerMock = CreateHandlerMock("{\"success\":true}");
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    !req.RequestUri.ToString().Contains("token", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true}") });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act
        var response = await sut.PutAsync("/api/product/123", new { name = "Updated" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task DeleteAsync_Should_Send_DeleteMethod()
    {
        // Arrange
        var mp = CreatePazaramaMarketPlace();
        SetupMarketPlaces([mp]);

        HttpRequestMessage? capturedRequest = null;
        var handlerMock = CreateHandlerMock("{\"success\":true}");
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    !req.RequestUri.ToString().Contains("token", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true}") });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act
        var response = await sut.DeleteAsync("/api/product/123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task GetAsync_Should_Use_DefaultTokenUrl_When_None_Set()
    {
        // Arrange — tokenUrl is null in DB, should fall back to default
        var mp = new MarketPlace
        {
            Id = PazaramaMarketPlaceId,
            Name = "Pazarama",
            ApiKey = "test-client-id",
            ApiSecret = "test-client-secret",
            BaseUrl = null,
            TokenUrl = null  // explicitly null → implementation should use DefaultTokenUrl
        };
        SetupMarketPlaces([mp]);

        HttpRequestMessage? capturedTokenRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.RequestUri != null && req.RequestUri.ToString().Contains("isortagimgiris"))
                {
                    capturedTokenRequest = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"access_token":"tok","expires_in":3600,"token_type":"Bearer"}""")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            });

        SetupFactoryWithHandler(handlerMock);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert — default token URL (isortagimgiris.pazarama.com) was used
        capturedTokenRequest.Should().NotBeNull();
        capturedTokenRequest!.RequestUri!.ToString().Should().Contain("isortagimgiris.pazarama.com");
    }
}
