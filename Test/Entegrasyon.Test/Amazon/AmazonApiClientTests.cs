using System.Net;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonApiClient unit tests — verifies token injection, retry on 401, and upload behavior.
/// WireMock pattern: stub kur → SUT cagir → FindLogEntries ile dogrula.
/// Retry on 401 test'i icin Scenarios (state machine) kullanilir.
/// </summary>
[Collection(WireMockCollection.Name)]
public class AmazonApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IAmazonTokenManager> _tokenManagerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<AmazonApiClient>> _loggerMock = new();

    public AmazonApiClientTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient());

        // Default: token manager her zaman valid token doner — testler ihtiyaca gore override edebilir
        _tokenManagerMock
            .Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-access-token");
    }

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
            BaseUrl = baseUrl ?? _wm.BaseUrl,
            UserAgentPrefix = userAgent
        };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { mp });
    }

    /// <summary>
    /// Generic catch-all stub: her path ve method icin 200 "{}" doner.
    /// </summary>
    private void StubCatchAll()
    {
        _wm.Server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));
    }

    // ── Test 1: GET injects x-amz-access-token header ──

    [Fact]
    public async Task GetAsync_InjectsAccessTokenHeader()
    {
        // Arrange
        SetupAmazonMarketPlace();
        StubCatchAll();
        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("/test/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Headers.Should().ContainKey("x-amz-access-token");
        log.RequestMessage!.Headers!["x-amz-access-token"].ToString().Should().Be("test-access-token");
    }

    // ── Test 2: POST sends JSON body ──

    [Fact]
    public async Task PostAsync_SendsJsonBody()
    {
        // Arrange
        SetupAmazonMarketPlace();
        StubCatchAll();
        var sut = CreateSut();

        // Act
        var response = await sut.PostAsync("/test", new { name = "test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Method.Should().Be("POST");
        log.RequestMessage!.Headers!["Content-Type"].ToString().Should().Contain("application/json");
    }

    // ── Test 3: PUT sends JSON body ──

    [Fact]
    public async Task PutAsync_SendsJsonBody()
    {
        // Arrange
        SetupAmazonMarketPlace();
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.PutAsync("/test", new { data = 1 });

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Method.Should().Be("PUT");
        log.RequestMessage!.Headers!["Content-Type"].ToString().Should().Contain("application/json");
    }

    // ── Test 4: PATCH sends JSON body ──

    [Fact]
    public async Task PatchAsync_SendsJsonBody()
    {
        // Arrange
        SetupAmazonMarketPlace();
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.PatchAsync("/test", new { data = 1 });

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Method.Should().Be("PATCH");
    }

    // ── Test 5: DELETE sends request ──

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        // Arrange
        SetupAmazonMarketPlace();
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.DeleteAsync("/test/123");

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Method.Should().Be("DELETE");
    }

    // ── Test 6: Retry on 401 — Scenarios state machine ──

    [Fact]
    public async Task GetAsync_On401_InvalidatesTokenAndRetries()
    {
        // Arrange — scenario: ilk istekte 401, ikincide 200
        // Bu, eski _responseQueue.EnqueueResponse() pattern'inin WireMock karsiligidir.
        const string scenario = "TokenRefresh";
        SetupAmazonMarketPlace();

        _wm.Server
            .Given(Request.Create().WithPath("/test").UsingGet())
            .InScenario(scenario)
            .WillSetStateTo("AfterFirstCall")
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("{}"));

        _wm.Server
            .Given(Request.Create().WithPath("/test").UsingGet())
            .InScenario(scenario)
            .WhenStateIs("AfterFirstCall")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{}"));

        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _tokenManagerMock.Verify(t => t.InvalidateToken(), Times.Once);

        // Exactly 2 requests hit WireMock (first 401, then 200 after token refresh)
        var logs = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test").UsingGet());
        logs.Should().HaveCount(2);
    }

    // ── Test 7: Missing marketplace throws ──

    [Fact]
    public async Task GetAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>());
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("/test"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Amazon*bulunamadı*");
    }

    // ── Test 8: Uses BaseUrl from MarketPlace ──

    [Fact]
    public async Task GetAsync_UsesBaseUrlFromMarketPlace()
    {
        // Arrange — WireMock URL'i MarketPlace.BaseUrl'e yaz
        SetupAmazonMarketPlace(baseUrl: _wm.BaseUrl);
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.AbsoluteUrl.Should().StartWith(_wm.BaseUrl);
    }

    // ── Test 9: Upload to presigned URL, no token header ──

    [Fact]
    public async Task UploadAsync_DoesNotAddAccessTokenHeader()
    {
        // Arrange — presigned URL olarak WireMock URL'i kullaniyoruz
        StubCatchAll();
        var sut = CreateSut();

        var content = new byte[] { 1, 2, 3 };
        var presignedUrl = $"{_wm.BaseUrl}/presigned/upload/doc";

        // Act
        var response = await sut.UploadAsync(presignedUrl, content, "application/json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Method.Should().Be("PUT");
        log.RequestMessage!.Headers.Should().NotContainKey("x-amz-access-token");
    }

    // ── Test 10: User-Agent from MarketPlace ──

    [Fact]
    public async Task GetAsync_SetsUserAgentFromMarketPlace()
    {
        // Arrange
        SetupAmazonMarketPlace(userAgent: "MyApp/2.0");
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Headers.Should().ContainKey("User-Agent");
        log.RequestMessage!.Headers!["User-Agent"].ToString().Should().Contain("MyApp/2.0");
    }
}
