using System.Net;
using System.Net.Http.Headers;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonApiClient unit tests — verifies token injection, retry on 401, and upload behavior.
/// </summary>
public class AmazonApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IAmazonTokenManager> _tokenManagerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<AmazonApiClient>> _loggerMock = new();

    private AmazonApiClient CreateSut() => new(
        _tokenManagerMock.Object,
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupAmazonMarketPlace(string? baseUrl = null, string? userAgent = null)
    {
        var mp = new MarketPlace
        {
            Id = AmazonMarketPlaceId,
            Name = "Amazon",
            BaseUrl = baseUrl,
            UserAgentPrefix = userAgent
        };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { mp });
    }

    /// <summary>
    /// MockHttpMessageHandler — captures the last request for assertion.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public List<HttpRequestMessage> AllRequests { get; } = new();
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        private readonly Queue<HttpResponseMessage> _responseQueue = new();

        public void EnqueueResponse(HttpResponseMessage response) => _responseQueue.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            AllRequests.Add(request);
            var resp = _responseQueue.Count > 0 ? _responseQueue.Dequeue() : ResponseToReturn;
            return Task.FromResult(resp);
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
    public async Task GetAsync_InjectsAccessTokenHeader()
    {
        // Arrange
        SetupAmazonMarketPlace();
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-access-token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("/test/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Should().ContainKey("x-amz-access-token");
        handler.LastRequest.Headers.GetValues("x-amz-access-token").Should().Contain("test-access-token");
    }

    // ── Test 2 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostAsync_SendsJsonBody()
    {
        // Arrange
        SetupAmazonMarketPlace();
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.PostAsync("/test", new { name = "test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.Content.Should().NotBeNull();
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    // ── Test 3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PutAsync_SendsJsonBody()
    {
        // Arrange
        SetupAmazonMarketPlace();
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.PutAsync("/test", new { data = 1 });

        // Assert
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    // ── Test 4 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PatchAsync_SendsJsonBody()
    {
        // Arrange
        SetupAmazonMarketPlace();
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.PatchAsync("/test", new { data = 1 });

        // Assert
        handler.LastRequest!.Method.Should().Be(HttpMethod.Patch);
    }

    // ── Test 5 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        // Arrange
        SetupAmazonMarketPlace();
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        var response = await sut.DeleteAsync("/test/123");

        // Assert
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
    }

    // ── Test 6 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_On401_InvalidatesTokenAndRetries()
    {
        // Arrange
        SetupAmazonMarketPlace();
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{}") });
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _tokenManagerMock.Verify(t => t.InvalidateToken(), Times.Once);
        handler.AllRequests.Should().HaveCount(2);
    }

    // ── Test 7 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>());
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("/test"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Amazon*bulunamadı*");
    }

    // ── Test 8 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_UsesBaseUrlFromMarketPlace()
    {
        // Arrange
        var customBaseUrl = "https://custom-amazon.example.com";
        SetupAmazonMarketPlace(baseUrl: customBaseUrl);
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().StartWith(customBaseUrl);
    }

    // ── Test 9 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_UsesDefaultBaseUrl_WhenMarketPlaceBaseUrlIsNull()
    {
        // Arrange
        SetupAmazonMarketPlace(baseUrl: null);
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("sellingpartnerapi-eu.amazon.com");
    }

    // ── Test 10 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_DoesNotAddAccessTokenHeader()
    {
        // Arrange — Upload uses presigned URL, no auth header needed
        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        var content = new byte[] { 1, 2, 3 };

        // Act
        var response = await sut.UploadAsync("https://presigned.s3.amazonaws.com/doc", content, "application/json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest!.Headers.Contains("x-amz-access-token").Should().BeFalse();
        handler.LastRequest.Method.Should().Be(HttpMethod.Put);
    }

    // ── Test 11 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_SetsUserAgentFromMarketPlace()
    {
        // Arrange
        SetupAmazonMarketPlace(userAgent: "MyApp/2.0");
        _tokenManagerMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);
        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        handler.LastRequest!.Headers.UserAgent.ToString().Should().Contain("MyApp/2.0");
    }
}
