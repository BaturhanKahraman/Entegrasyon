using System.Net;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Hepsiburada;

public class HepsiburadaApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<HepsiburadaApiClient>> _loggerMock = new();

    private HepsiburadaApiClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlaces(IEnumerable<MarketPlace> marketPlaces)
    {
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces.ToList());
    }

    private static (HttpClient client, Mock<HttpMessageHandler> handler) CreateHttpClientWithHandler(
        string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });

        return (new HttpClient(handlerMock.Object), handlerMock);
    }

    private MarketPlace CreateHepsiburadaMarketPlace(
        string? baseUrl = null,
        string? username = "testuser",
        string? password = "testpass",
        string? userAgentPrefix = "TestEntegrator") => new()
    {
        Id = HepsiburadaMarketPlaceId,
        Name = "Hepsiburada",
        IsBasicAuth = true,
        BasicAuthUserName = username,
        BasicAuthPassword = password,
        BaseUrl = baseUrl,
        UserAgentPrefix = userAgentPrefix
    };

    [Fact]
    public async Task GetAsync_Should_Set_BasicAuth_Header()
    {
        // Arrange
        var mp = CreateHepsiburadaMarketPlace();
        SetupMarketPlaces([mp]);

        var (client, handler) = CreateHttpClientWithHandler("{\"success\":true}");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = CreateSut();

        // Act
        var response = await sut.GetAsync("/api/categories/get-all-categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.Headers.Authorization != null &&
                req.Headers.Authorization.Scheme == "Basic"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_Should_Use_Correct_BasicAuth_Credentials()
    {
        // Arrange
        var mp = CreateHepsiburadaMarketPlace(username: "myuser", password: "mypass");
        SetupMarketPlaces([mp]);

        var expectedCredentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("myuser:mypass"));

        var (client, handler) = CreateHttpClientWithHandler("{}");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.Authorization!.Parameter == expectedCredentials),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_Should_Set_UserAgent_Header()
    {
        // Arrange
        var mp = CreateHepsiburadaMarketPlace(userAgentPrefix: "MyEntegrator");
        SetupMarketPlaces([mp]);

        var (client, handler) = CreateHttpClientWithHandler("{}");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("/test");

        // Assert
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.UserAgent.ToString().Contains("MyEntegrator")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_Should_Use_DefaultBaseUrl_When_None_Set()
    {
        // Arrange
        var mp = CreateHepsiburadaMarketPlace(baseUrl: null);
        SetupMarketPlaces([mp]);

        var (client, handler) = CreateHttpClientWithHandler("{}");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = CreateSut();

        // Act
        await sut.GetAsync("/api/test");

        // Assert
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("mpop.hepsiburada.com")),
            ItExpr.IsAny<CancellationToken>());
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
            .WithMessage("*Hepsiburada*bulunamadı*");
    }

    [Fact]
    public async Task PostMultipartJsonFileAsync_Should_Send_Multipart_Content()
    {
        // Arrange
        var mp = CreateHepsiburadaMarketPlace();
        SetupMarketPlaces([mp]);

        var (client, handler) = CreateHttpClientWithHandler("{\"trackingId\":\"abc-123\"}");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = CreateSut();

        // Act
        var response = await sut.PostMultipartJsonFileAsync(
            "/api/products/import",
            "[{\"categoryId\":123}]",
            "products.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("trackingId");

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.Content is MultipartFormDataContent),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PostAsync_Should_Send_Json_Body()
    {
        // Arrange
        var mp = CreateHepsiburadaMarketPlace();
        SetupMarketPlaces([mp]);

        var (client, handler) = CreateHttpClientWithHandler("{\"success\":true}");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = CreateSut();

        // Act
        var response = await sut.PostAsync("/api/test", new { key = "value" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }
}
