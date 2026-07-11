using System.Net;
using System.Text;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolApiClient unit tests — verifies Basic Auth header injection,
/// User-Agent header, URL construction and error handling.
///
/// WireMock pattern: Her test WireMock fixture'a stub kurar,
/// MarketPlace.BaseUrl'i WireMock URL'ine set eder, gercek HttpClient ile istek
/// atar ve FindLogEntries ile gelen request'i dogrular. Artik private
/// HttpMessageHandler yok.
/// </summary>
[Collection(WireMockCollection.Name)]
public class TrendyolApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<TrendyolApiClient>> _loggerMock = new();

    public TrendyolApiClientTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        // Factory mock'u: named client ismini umursamaz, her zaman taze bir HttpClient
        // dondurur. SUT runtime'da client.BaseAddress = marketplace.BaseUrl ile override
        // eder — bu yuzden burada BaseAddress set etmiyoruz.
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient());
    }

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

    private MarketPlace CreateTrendyolMarketPlace(
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
        BaseUrl = baseUrl ?? _wm.BaseUrl,
        UserAgentPrefix = userAgentPrefix
    };

    /// <summary>
    /// WireMock'a bir GET endpoint stub'i kurar — her test kendi stub'ini tanimlar.
    /// </summary>
    private void StubGet(string path, int statusCode = 200, string body = "{}")
    {
        _wm.Server
            .Given(Request.Create().WithPath(path).UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    // ── Test 1: GET injects Basic Auth header ──

    [Fact]
    public async Task GetAsync_InjectsBasicAuthHeader()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace(apiKey: "myKey", apiSecret: "mySecret");
        SetupMarketPlaces([mp]);
        StubGet("/test/endpoint");
        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("test/endpoint");

        // Assert — response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — header injection via WireMock log
        var log = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test/endpoint").UsingGet()).Single();

        log.RequestMessage!.Headers.Should().ContainKey("Authorization");
        var authHeader = log.RequestMessage!.Headers!["Authorization"].ToString();
        authHeader.Should().StartWith("Basic ");

        var expectedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("myKey:mySecret"));
        authHeader.Should().Contain(expectedCredentials);
    }

    // ── Test 2: POST sends JSON body with auth ──

    [Fact]
    public async Task PostAsync_SendsJsonBody_WithBasicAuthHeader()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace();
        SetupMarketPlaces([mp]);
        StubGet("/test/products");
        var sut = CreateSut();

        // Act
        var response = await sut.PostAsync("test/products", new { name = "Test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var log = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test/products").UsingPost()).Single();

        log.RequestMessage!.Method.Should().Be("POST");
        log.RequestMessage!.Headers.Should().ContainKey("Content-Type");
        log.RequestMessage!.Headers!["Content-Type"].ToString().Should().Contain("application/json");
        log.RequestMessage!.Headers.Should().ContainKey("Authorization");
    }

    // ── Test 3: PUT sends JSON body ──

    [Fact]
    public async Task PutAsync_SendsJsonBody()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace();
        SetupMarketPlaces([mp]);
        StubGet("/test/update");
        var sut = CreateSut();

        // Act
        await sut.PutAsync("test/update", new { data = 1 });

        // Assert
        var log = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test/update").UsingPut()).Single();

        log.RequestMessage!.Method.Should().Be("PUT");
        log.RequestMessage!.Headers!["Content-Type"].ToString().Should().Contain("application/json");
    }

    // ── Test 4: DELETE sends request ──

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace();
        SetupMarketPlaces([mp]);
        StubGet("/test/delete/123");
        var sut = CreateSut();

        // Act
        await sut.DeleteAsync("test/delete/123");

        // Assert
        var log = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test/delete/123").UsingDelete()).Single();

        log.RequestMessage!.Method.Should().Be("DELETE");
    }

    // ── Test 5: Missing marketplace throws ──

    [Fact]
    public async Task GetAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange — hic stub kurma, zaten SUT DB'de MarketPlace bulamayinca throw edecek
        SetupMarketPlaces(Enumerable.Empty<MarketPlace>());
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
        // Arrange — MarketPlace.BaseUrl olarak WireMock URL'ini veriyoruz,
        // dolayisiyla SUT bu URL'e gidecek. Gidiste path'i dogrulayalim.
        var mp = CreateTrendyolMarketPlace(baseUrl: _wm.BaseUrl);
        SetupMarketPlaces([mp]);
        StubGet("/test/endpoint");
        var sut = CreateSut();

        // Act
        await sut.GetAsync("test/endpoint");

        // Assert — request WireMock sunucusuna dustu (kanit: log entry mevcut)
        var log = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test/endpoint").UsingGet()).Single();

        log.RequestMessage!.AbsoluteUrl.Should().StartWith(_wm.BaseUrl);
    }

    // ── Test 7: User-Agent from UserAgentPrefix ──

    [Fact]
    public async Task GetAsync_UsesUserAgentPrefixFromMarketPlace()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace(userAgentPrefix: "MySeller - SelfIntegration");
        SetupMarketPlaces([mp]);
        StubGet("/test");
        var sut = CreateSut();

        // Act
        await sut.GetAsync("test");

        // Assert
        var log = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test").UsingGet()).Single();

        log.RequestMessage!.Headers.Should().ContainKey("User-Agent");
        log.RequestMessage!.Headers!["User-Agent"].ToString()
            .Should().Contain("MySeller - SelfIntegration");
    }

    // ── Test 8: User-Agent fallback from SellerId ──

    [Fact]
    public async Task GetAsync_GeneratesUserAgent_FromSellerId_WhenPrefixIsNull()
    {
        // Arrange
        var mp = CreateTrendyolMarketPlace(userAgentPrefix: null, sellerId: "99999");
        SetupMarketPlaces([mp]);
        StubGet("/test");
        var sut = CreateSut();

        // Act
        await sut.GetAsync("test");

        // Assert
        var log = _wm.Server.FindLogEntries(
            Request.Create().WithPath("/test").UsingGet()).Single();

        var userAgent = log.RequestMessage!.Headers!["User-Agent"].ToString();
        userAgent.Should().Contain("99999");
        userAgent.Should().Contain("SelfIntegration");
    }
}
