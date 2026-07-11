using System.Net;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiApiClient unit tests — verifies x-api-key header injection and URL construction.
/// WireMock pattern: stub kur → SUT cagir → FindLogEntries ile header/path dogrula.
/// </summary>
[Collection(WireMockCollection.Name)]
public class CiceksepetiApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<CiceksepetiApiClient>> _loggerMock = new();

    public CiceksepetiApiClientTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        // Tenant-aware static credential cache her test oncesi temizlenmeli
        CiceksepetiApiClient.ClearCredentialCache();

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient());
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
        BaseUrl = baseUrl ?? _wm.BaseUrl
    };

    /// <summary>
    /// Catch-all stub: her path icin 200 "{}" doner.
    /// Client'in runtime'da kurdugu URL'i bilmiyoruz; geniş kapsamli stub
    /// testlerin path-independent calismasini saglar.
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

    // ── Test 1: GET injects x-api-key header ──

    [Fact]
    public async Task GetAsync_InjectsApiKeyHeader()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace(apiKey: "my-secret-key");
        SetupMarketPlaces([mp]);
        StubCatchAll();
        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Headers.Should().ContainKey("x-api-key");
        log.RequestMessage!.Headers!["x-api-key"].ToString().Should().Be("my-secret-key");
    }

    // ── Test 2: POST sends JSON body with api key ──

    [Fact]
    public async Task PostAsync_SendsJsonBody_WithApiKeyHeader()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace(apiKey: "post-api-key");
        SetupMarketPlaces([mp]);
        StubCatchAll();
        var sut = CreateSut();

        // Act
        var response = await sut.PostAsync("products", new { name = "Test Product", quantity = 5 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Method.Should().Be("POST");
        log.RequestMessage!.Headers.Should().ContainKey("x-api-key");
        log.RequestMessage!.Headers!["x-api-key"].ToString().Should().Be("post-api-key");
        log.RequestMessage!.Headers.Should().ContainKey("Content-Type");
        log.RequestMessage!.Headers["Content-Type"].ToString().Should().Contain("application/json");
    }

    // ── Test 3: SendRawAsync uses absolute path, no /api/v1/ prefix ──

    [Fact]
    public async Task SendRawAsync_UsesAbsolutePath_NoApiV1Prefix()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace(apiKey: "raw-api-key");
        SetupMarketPlaces([mp]);
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.SendRawAsync("/Branch/SendInvoiceMail", HttpMethod.Post, content: null);

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Path.Should().Be("/Branch/SendInvoiceMail");
        log.RequestMessage!.Path.Should().NotContain("/api/v1/");

        log.RequestMessage!.Headers.Should().ContainKey("x-api-key");
        log.RequestMessage!.Headers!["x-api-key"].ToString().Should().Be("raw-api-key");
    }

    // ── Test 4: Missing marketplace throws ──

    [Fact]
    public async Task GetAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange — hic stub kurma, exception erken atilmali
        SetupMarketPlaces(Enumerable.Empty<MarketPlace>());
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("products"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Çiçeksepeti*");
    }

    // ── Test 5: Uses BaseUrl from MarketPlace ──

    [Fact]
    public async Task GetAsync_UsesBaseUrlFromMarketPlace()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace(baseUrl: _wm.BaseUrl, apiKey: "key");
        SetupMarketPlaces([mp]);
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.GetAsync("products");

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.AbsoluteUrl.Should().StartWith(_wm.BaseUrl);
    }

    // ── Test 6: Missing ApiKey throws ──

    [Fact]
    public async Task GetAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange — marketplace exists but ApiKey null
        var mp = CreateCiceksepetiMarketPlace(apiKey: null);
        SetupMarketPlaces([mp]);
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.GetAsync("products"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    // ── Test 7: /api/v1/ prefix on standard GET ──

    [Fact]
    public async Task GetAsync_PrependsApiV1Prefix()
    {
        // Arrange
        var mp = CreateCiceksepetiMarketPlace();
        SetupMarketPlaces([mp]);
        StubCatchAll();
        var sut = CreateSut();

        // Act
        await sut.GetAsync("products");

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Path.Should().Contain("/api/v1/");
    }
}
