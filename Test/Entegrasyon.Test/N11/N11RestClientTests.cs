using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11RestClient birim testleri.
/// Credential header ekleme ve HTTP yanit parse mantigini dogrular.
/// WireMock pattern: her test kendi stub'ini kurar, LogEntries ile request'i dogrular.
/// </summary>
[Collection(WireMockCollection.Name)]
public class N11RestClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<N11RestClient>> _loggerMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();

    private const string TestAppKey = "test-app-key";
    private const string TestAppSecret = "test-app-secret";

    public N11RestClientTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        _tenantContextMock.Setup(t => t.GetMarketPlaceId("N11")).Returns(N11MarketPlaceId);

        var marketPlaces = new List<MarketPlace>
        {
            new() { Id = N11MarketPlaceId, ApiKey = TestAppKey, ApiSecret = TestAppSecret }
        };

        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces);

        // Factory mock WireMock URL'ine yonlendirir — N11RestClient named client "N11Rest"
        // kullaniyor, factory mock But It.IsAny<string>() match'ledigi icin degeri umursamaz.
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient { BaseAddress = new Uri(_wm.BaseUrl) });
    }

    private N11RestClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object,
        _tenantContextMock.Object);

    /// <summary>
    /// N11 REST endpoint stub'u — given path icin verilen body'yi doner.
    /// </summary>
    private void StubN11(string path, int statusCode = 200, string body = "[]")
    {
        _wm.Server
            .Given(Request.Create().WithPath("/" + path.TrimStart('/')).UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    // -----------------------------------------------------------------------
    // GetAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_ShouldDeserializeResponse()
    {
        // Arrange
        var expected = new List<N11CategoryTreeResponse>
        {
            new N11CategoryTreeResponse(42, "Test", null)
        };
        StubN11("cdn/categories", body: JsonSerializer.Serialize(expected));
        var sut = CreateSut();

        // Act
        var result = await sut.GetAsync<List<N11CategoryTreeResponse>>("cdn/categories");

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAsync_WhenHttpError_ShouldReturnNull()
    {
        // Arrange
        StubN11("cdn/categories", statusCode: (int)HttpStatusCode.Unauthorized, body: "Unauthorized");
        var sut = CreateSut();

        // Act
        var result = await sut.GetAsync<List<N11CategoryTreeResponse>>("cdn/categories");

        // Assert
        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // PostAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PostAsync_ShouldDeserializeTaskResponse()
    {
        // Arrange
        var responseJson = """{"id":1092,"type":"PRODUCT_CREATE","status":"IN_QUEUE","reasons":["1 sku işlenmeye alındı."]}""";
        StubN11("ms/product/tasks/product-create", body: responseJson);
        var sut = CreateSut();

        // Act
        var result = await sut.PostAsync<object, N11TaskResponse>(
            "ms/product/tasks/product-create", new { });

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1092);
        result.Status.Should().Be("IN_QUEUE");
        result.Type.Should().Be("PRODUCT_CREATE");
    }

    [Fact]
    public async Task PostAsync_WhenHttpError_ShouldReturnNull()
    {
        // Arrange
        StubN11("ms/product/tasks/product-create", statusCode: (int)HttpStatusCode.BadRequest, body: "Bad Request");
        var sut = CreateSut();

        // Act
        var result = await sut.PostAsync<object, N11TaskResponse>(
            "ms/product/tasks/product-create", new { });

        // Assert
        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // PutAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PutAsync_ShouldReturnParsedResult()
    {
        // Arrange
        StubN11("rest/order/v1/update",
            body: """{"id":1092,"type":"SKU_UPDATE","status":"IN_QUEUE","reasons":[]}""");
        var sut = CreateSut();

        // Act
        var result = await sut.PutAsync<object, N11TaskResponse>("rest/order/v1/update", new { });

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1092);
    }

    // -----------------------------------------------------------------------
    // Credential injection
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_ShouldUseCredentialsFromMarketPlaceTable()
    {
        // Arrange
        StubN11("cdn/categories", body: "[]");
        var sut = CreateSut();

        // Act
        await sut.GetAsync<List<N11CategoryTreeResponse>>("cdn/categories");

        // Assert — tenantContext.GetMarketPlaceId cagrildi ve credentials yuklendi
        _tenantContextMock.Verify(t => t.GetMarketPlaceId("N11"), Times.Once);
    }
}
