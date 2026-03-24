using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Concrete.Temu;
using Entegrasyon.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Temu;

/// <summary>
/// TemuApiClient unit tests — verifies MD5 sign calculation, credential resolution,
/// router pattern (single POST endpoint), and error handling.
/// </summary>
public class TemuApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<TemuApiClient>> _loggerMock = new();

    public TemuApiClientTests()
    {
        // Clear static credential cache between tests
        TemuApiClient.ClearCredentialCache();
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
        RefreshToken = accessToken, // Temu access_token is stored in RefreshToken field (3 month validity)
        BaseUrl = baseUrl
    };

    /// <summary>
    /// Captures HTTP requests for assertion.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":true,"error_code":0,"error_msg":"","result":{}}""",
                System.Text.Encoding.UTF8,
                "application/json")
        };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(ct);
            return ResponseToReturn;
        }
    }

    private void SetupFactoryWithHandler(HttpMessageHandler handler)
    {
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler));
    }

    // ── Test 1: MD5 Sign Calculation ─────────────────────────────────────────────

    [Fact]
    public void CalculateSign_ShouldReturnCorrectMd5Hash_WithSortedParams()
    {
        // Arrange — known input and expected output
        // Parameters sorted: access_token, app_key, data_type, timestamp, type
        // Concat: {secret}access_tokentest-tokenapp_keytest-keydata_typeJSONtimestamp1234567890typebg.goods.get{secret}
        var parameters = new Dictionary<string, string>
        {
            { "type", "bg.goods.get" },
            { "app_key", "test-key" },
            { "timestamp", "1234567890" },
            { "access_token", "test-token" },
            { "data_type", "JSON" }
        };

        var appSecret = "my-secret";

        // Act
        var sign = TemuApiClient.CalculateSign(parameters, appSecret);

        // Assert — sign should be uppercase hex MD5
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

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.get", new { page = 1 }, CancellationToken.None);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Contain("/openapi/router");
    }

    [Fact]
    public async Task CallAsync_ShouldIncludeTypeParameter()
    {
        // Arrange
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.cats.get", null, CancellationToken.None);

        // Assert
        handler.LastRequestBody.Should().NotBeNull();
        handler.LastRequestBody.Should().Contain("bg.goods.cats.get");
    }

    [Fact]
    public async Task CallAsync_ShouldIncludeSignInRequest()
    {
        // Arrange
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.get", null, CancellationToken.None);

        // Assert
        handler.LastRequestBody.Should().NotBeNull();
        handler.LastRequestBody.Should().Contain("sign");
    }

    // ── Test 3: Credential resolution ────────────────────────────────────────────

    [Fact]
    public async Task CallAsync_MissingMarketPlace_ThrowsInvalidOperationException()
    {
        // Arrange — empty marketplace table
        SetupMarketPlaces(Enumerable.Empty<MarketPlace>());

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Temu*");
    }

    [Fact]
    public async Task CallAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange — marketplace exists but ApiKey is null
        var mp = CreateTemuMarketPlace(apiKey: null);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*app_key*");
    }

    [Fact]
    public async Task CallAsync_MissingApiSecret_ThrowsInvalidOperationException()
    {
        // Arrange — marketplace exists but ApiSecret is null
        var mp = CreateTemuMarketPlace(apiSecret: null);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*app_secret*");
    }

    // ── Test 4: Base URL handling ────────────────────────────────────────────────

    [Fact]
    public async Task CallAsync_UsesDefaultBaseUrl_WhenMarketPlaceBaseUrlIsNull()
    {
        // Arrange
        var mp = CreateTemuMarketPlace(baseUrl: null);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.get", null, CancellationToken.None);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("openapi-b-eu.temu.com");
    }

    [Fact]
    public async Task CallAsync_UsesCustomBaseUrl_WhenProvided()
    {
        // Arrange
        var customUrl = "https://custom.temu.example.com";
        var mp = CreateTemuMarketPlace(baseUrl: customUrl);
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler();
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        await sut.CallAsync<object>("bg.goods.get", null, CancellationToken.None);

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().StartWith(customUrl);
    }

    // ── Test 5: Response deserialization ─────────────────────────────────────────

    [Fact]
    public async Task CallAsync_DeserializesSuccessResponse()
    {
        // Arrange
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"success":true,"error_code":0,"error_msg":"","result":{"cat_id":123,"cat_name":"Test"}}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act
        var result = await sut.CallAsync<JsonElement>("bg.goods.cats.get", null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CallAsync_ThrowsOnApiError()
    {
        // Arrange
        var mp = CreateTemuMarketPlace();
        SetupMarketPlaces([mp]);

        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"success":false,"error_code":1001,"error_msg":"Parameter error","result":null}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };
        SetupFactoryWithHandler(handler);

        var sut = CreateSut();

        // Act & Assert
        await sut.Invoking(s => s.CallAsync<object>("bg.goods.get", null, CancellationToken.None))
            .Should().ThrowAsync<TemuApiException>()
            .Where(ex => ex.ErrorCode == 1001);
    }
}
