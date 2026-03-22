using System.Net;
using System.Xml.Linq;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.N11;

public class N11SoapClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<N11SoapClient>> _loggerMock = new();

    private N11SoapClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlaces(IEnumerable<MarketPlace> marketPlaces)
    {
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(marketPlaces.ToList());
    }

    private static HttpClient CreateHttpClientWithResponse(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
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

        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task SendAsync_Should_Include_Auth_In_Soap_Body()
    {
        // Arrange
        var marketplace = new MarketPlace
        {
            Id = N11MarketPlaceId,
            Name = "N11",
            ApiKey = "test-app-key",
            ApiSecret = "test-app-secret",
            BaseUrl = "https://api.n11.com/ws/"
        };

        SetupMarketPlaces([marketplace]);

        string? capturedRequestBody = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedRequestBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
                        <env:Body>
                            <ns3:GetTopLevelCategoriesResponse xmlns:ns3="http://www.n11.com/ws/schemas">
                                <result><status>success</status></result>
                            </ns3:GetTopLevelCategoriesResponse>
                        </env:Body>
                    </env:Envelope>
                    """)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var sut = CreateSut();

        var bodyContent = new XElement(
            XName.Get("GetTopLevelCategoriesRequest", "http://www.n11.com/ws/schemas"));

        // Act
        var result = await sut.SendAsync("CategoryService", "", bodyContent);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain("<appKey>test-app-key</appKey>");
        capturedRequestBody.Should().Contain("<appSecret>test-app-secret</appSecret>");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_Should_Throw_When_MarketPlace_Not_Found()
    {
        // Arrange
        SetupMarketPlaces([]);

        var sut = CreateSut();
        var bodyContent = new XElement("Dummy");

        // Act and Assert
        await sut.Invoking(s => s.SendAsync("CategoryService", "", bodyContent))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*N11*");
    }

    [Fact]
    public async Task SendAsync_Should_Return_Response_Body_Content()
    {
        // Arrange
        var marketplace = new MarketPlace
        {
            Id = N11MarketPlaceId,
            Name = "N11",
            ApiKey = "key",
            ApiSecret = "secret",
            BaseUrl = "https://api.n11.com/ws/"
        };

        SetupMarketPlaces([marketplace]);

        var httpClient = CreateHttpClientWithResponse(
            """
            <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
                <env:Body>
                    <ns3:TestResponse xmlns:ns3="http://www.n11.com/ws/schemas">
                        <result><status>success</status></result>
                        <data>test-value</data>
                    </ns3:TestResponse>
                </env:Body>
            </env:Envelope>
            """);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var sut = CreateSut();
        var bodyContent = new XElement(
            XName.Get("TestRequest", "http://www.n11.com/ws/schemas"));

        // Act
        var result = await sut.SendAsync("TestService", "", bodyContent);

        // Assert
        result.Should().NotBeNull();
        result.Name.LocalName.Should().Be("TestResponse");
    }
}
