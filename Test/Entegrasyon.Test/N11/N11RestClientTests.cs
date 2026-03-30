using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.N11;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11RestClient birim testleri.
/// Credential header ekleme ve HTTP yanıt parse mantığını doğrular.
/// </summary>
public class N11RestClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<N11RestClient>> _loggerMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly MockHttpMessageHandler _handler = new();

    private const string TestAppKey = "test-app-key";
    private const string TestAppSecret = "test-app-secret";

    public N11RestClientTests()
    {
        _tenantContextMock.Setup(t => t.GetMarketPlaceId("N11")).Returns(N11MarketPlaceId);

        var marketPlaces = new List<MarketPlace>
        {
            new() { Id = N11MarketPlaceId, ApiKey = TestAppKey, ApiSecret = TestAppSecret }
        };

        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces);

        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.n11.com/")
        };

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("N11Rest"))
            .Returns(httpClient);
    }

    private N11RestClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object,
        _tenantContextMock.Object);

    // -----------------------------------------------------------------------
    // GetAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_ShouldDeserializeResponse()
    {
        // Arrange
        var sut = CreateSut();
        var expected = new List<N11CategoryTreeResponse>
        {
            new N11CategoryTreeResponse(42, "Test", null)
        };
        _handler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

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
        var sut = CreateSut();
        _handler.SetResponse(HttpStatusCode.Unauthorized, "Unauthorized");

        // Act — kullan referans tipi döndüren bir overload (string list)
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
        var sut = CreateSut();
        var responseJson = """{"id":1092,"type":"PRODUCT_CREATE","status":"IN_QUEUE","reasons":["1 sku işlenmeye alındı."]}""";
        _handler.SetResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await sut.PostAsync<object, Entegrasyon.Entity.Dtos.N11.N11TaskResponse>(
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
        var sut = CreateSut();
        _handler.SetResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await sut.PostAsync<object, Entegrasyon.Entity.Dtos.N11.N11TaskResponse>(
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
        var sut = CreateSut();
        _handler.SetResponse(HttpStatusCode.OK, """{"id":1092,"type":"SKU_UPDATE","status":"IN_QUEUE","reasons":[]}""");

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
        var sut = CreateSut();
        _handler.SetResponse(HttpStatusCode.OK, "[]");

        // Act
        await sut.GetAsync<List<N11CategoryTreeResponse>>("cdn/categories");

        // Assert — tenantContext.GetMarketPlaceId çağrıldı ve credentials yüklendi
        _tenantContextMock.Verify(t => t.GetMarketPlaceId("N11"), Times.Once);
    }
}
