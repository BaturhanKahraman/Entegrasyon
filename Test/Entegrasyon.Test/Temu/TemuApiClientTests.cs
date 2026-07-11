using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Concrete.Temu;
using Entegrasyon.Entity;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Temu;

/// <summary>
/// TemuApiClient unit tests — verifies MD5 sign calculation, credential resolution,
/// router pattern (single POST endpoint), and error handling.
/// WireMock pattern: Router endpoint stub + request body inspection.
/// </summary>
[Collection(WireMockCollection.Name)]
public class TemuApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<TemuApiClient>> _loggerMock = new();

    public TemuApiClientTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        // Tenant-aware static credential cache her test oncesi temizlenmeli
        TemuApiClient.ClearCredentialCache();

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient());
    }

    private TemuApiClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlaces(IEnumerable<MarketPlace> marketPlaces)
    {
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces.ToList());
    }

    private MarketPlace CreateTemuMarketPlace(
        string? baseUrl = null,
        string? apiKey = "test-app-key",
        string? apiSecret = "test-app-secret",
        string? accessToken = "test-access-token") => new()
    {
        Id = TemuMarketPlaceId,
        Name = "Temu",
        ApiKey = apiKey,
        ApiSecret = apiSecret,
        RefreshToken = accessToken, // Temu access_token is stored in RefreshToken field
        BaseUrl = baseUrl ?? _wm.BaseUrl
    };

    /// <summary>
    /// Temu router endpoint'i stub'u — her test default olarak success JSON doner.
    /// Test kendisi farkli bir body isterse parametre olarak JSON gecebilir.
    /// </summary>
    private void StubRouter(string body = """{"success":true,"error_code":0,"error_msg":"","result":{}}""")
    {
        _wm.Server
            .Given(Request.Create().WithPath("/openapi/router").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    // ── Test 1: MD5 Sign Calculation (no HTTP — pure static method) ──────────────

    [Fact]
    public void CalculateSign_ShouldReturnCorrectMd5Hash_WithSortedParams()
    {
        var parameters = new Dictionary<string, string>
        {
            { "type", "bg.goods.get" },
            { "app_key", "test-key" },
            { "timestamp", "1234567890" },
            { "access_token", "test-token" },
            { "data_type", "JSON" }
        };

        var sign = TemuApiClient.CalculateSign(parameters, "my-secret");

        sign.Should().NotBeNullOrEmpty();
        sign.Should().MatchRegex("^[A-F0-9]{32}$", "MD5 hash should be 32 chars uppercase hex");
    }

    [Fact]
    public void CalculateSign_WithSameParams_ShouldReturnSameSign()
    {
        var parameters = new Dictionary<string, string>
        {
            { "app_key", "key1" },
            { "type", "bg.goods.get" }
        };

        var sign1 = TemuApiClient.CalculateSign(parameters, "secret");
        var sign2 = TemuApiClient.CalculateSign(parameters, "secret");

        sign1.Should().Be(sign2);
    }

    [Fact]
    public void CalculateSign_WithDifferentSecrets_ShouldReturnDifferentSigns()
    {
        var parameters = new Dictionary<string, string>
        {
            { "app_key", "key1" },
            { "type", "bg.goods.get" }
        };

        var sign1 = TemuApiClient.CalculateSign(parameters, "secret1");
        var sign2 = TemuApiClient.CalculateSign(parameters, "secret2");

        sign1.Should().NotBe(sign2);
    }

    [Fact]
    public void CalculateSign_ParameterOrder_ShouldNotAffectResult()
    {
        var params1 = new Dictionary<string, string>
        {
            { "b_param", "2" },
            { "a_param", "1" }
        };
        var params2 = new Dictionary<string, string>
        {
            { "a_param", "1" },
            { "b_param", "2" }
        };

        var sign1 = TemuApiClient.CalculateSign(params1, "secret");
        var sign2 = TemuApiClient.CalculateSign(params2, "secret");

        sign1.Should().Be(sign2, "alphabetic sort should normalize parameter order");
    }

    // ── Test 2: Router endpoint (POST /openapi/router) ───────────────────────────

    [Fact]
    public async Task CallAsync_ShouldPostToRouterEndpoint()
    {
        // Arrange
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);
        StubRouter();
        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.get", new { page = 1 }, CancellationToken.None);

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Method.Should().Be("POST");
        log.RequestMessage!.Path.Should().Be("/openapi/router");
    }

    [Fact]
    public async Task CallAsync_ShouldIncludeTypeParameter()
    {
        // Arrange
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);
        StubRouter();
        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.cats.get", null, CancellationToken.None);

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Body.Should().NotBeNull();
        log.RequestMessage!.Body.Should().Contain("bg.goods.cats.get");
    }

    [Fact]
    public async Task CallAsync_ShouldIncludeSignInRequest()
    {
        // Arrange
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);
        StubRouter();
        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.get", null, CancellationToken.None);

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Body.Should().NotBeNull();
        log.RequestMessage!.Body.Should().Contain("sign");
    }

    // ── Test 3: Credential resolution ────────────────────────────────────────────

    [Fact]
    public async Task CallAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange — empty marketplace table, no stub needed
        SetupMarketPlaces(Enumerable.Empty<MarketPlace>());
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Temu*");
    }

    [Fact]
    public async Task CallAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var mp = CreateTemuMarketPlace(apiKey: null);
        SetupMarketPlaces([mp]);
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*app_key*");
    }

    [Fact]
    public async Task CallAsync_MissingApiSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var mp = CreateTemuMarketPlace(apiSecret: null);
        SetupMarketPlaces([mp]);
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*app_secret*");
    }

    // ── Test 4: Base URL handling ────────────────────────────────────────────────

    [Fact]
    public async Task CallAsync_UsesCustomBaseUrl_WhenProvided()
    {
        // Arrange — custom base URL olarak WireMock URL'i kullaniyoruz
        var mp = CreateTemuMarketPlace(baseUrl: _wm.BaseUrl);
        SetupMarketPlaces([mp]);
        StubRouter();
        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.get", null, CancellationToken.None);

        // Assert
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.AbsoluteUrl.Should().StartWith(_wm.BaseUrl);
    }

    // ── Test 5: Response deserialization ─────────────────────────────────────────

    [Fact]
    public async Task CallAsync_DeserializesSuccessResponse()
    {
        // Arrange — custom success body with actual data
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);
        StubRouter(
            """{"success":true,"error_code":0,"error_msg":"","result":{"cat_id":123,"cat_name":"Test"}}""");
        var sut = CreateSut();

        // Act
        var result = await sut.CallAsync<JsonElement>("bg.goods.cats.get", null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CallAsync_ThrowsOnApiError()
    {
        // Arrange — error response body
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);
        StubRouter(
            """{"success":false,"error_code":1001,"error_msg":"Parameter error","result":null}""");
        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<TemuApiException>()
            .Where(ex => ex.ErrorCode == 1001);
    }
}
