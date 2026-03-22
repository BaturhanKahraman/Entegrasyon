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
    private readonly Mock<Entegrasyon.Business.Abstract.ITenantContext> _tenantContextMock = new();

    public N11SoapClientTests()
    {
        _tenantContextMock.Setup(t => t.GetMarketPlaceId("N11")).Returns(N11MarketPlaceId);
    }

    private N11SoapClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object,
        _tenantContextMock.Object);

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

    private static HttpClient CreateHttpClientWithResponse(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var (client, _) = CreateHttpClientWithHandler(responseBody, statusCode);
        return client;
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

        string? capturedBody = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
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
                };
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
        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("<appKey>test-app-key</appKey>");
        capturedBody.Should().Contain("<appSecret>test-app-secret</appSecret>");
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

    [Fact]
    public async Task SendAsync_WhenHttpError_ShouldThrowHttpRequestException()
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
            "<soapFault>Error details</soapFault>",
            HttpStatusCode.InternalServerError);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var sut = CreateSut();
        var bodyContent = new XElement("TestRequest");

        // Act & Assert
        await sut.Invoking(s => s.SendAsync("TestService", "", bodyContent))
            .Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task SendAsync_ShouldNotMutateOriginalBodyContent()
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
                    </ns3:TestResponse>
                </env:Body>
            </env:Envelope>
            """);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var sut = CreateSut();
        var bodyContent = new XElement(
            XName.Get("TestRequest", "http://www.n11.com/ws/schemas"),
            new XElement("someField", "value"));

        var childCountBefore = bodyContent.Elements().Count();

        // Act
        await sut.SendAsync("TestService", "", bodyContent);

        // Assert — bodyContent should NOT have <auth> injected
        bodyContent.Elements().Count().Should().Be(childCountBefore);
        bodyContent.Element("auth").Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenSoapActionProvided_ShouldIncludeInRequestHeaders()
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

        IEnumerable<string>? soapActionValues = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Headers.TryGetValues("SOAPAction", out var values))
                    soapActionValues = values.ToList();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
                            <env:Body>
                                <ns3:TestResponse xmlns:ns3="http://www.n11.com/ws/schemas">
                                    <result><status>success</status></result>
                                </ns3:TestResponse>
                            </env:Body>
                        </env:Envelope>
                        """)
                });
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var sut = CreateSut();
        var bodyContent = new XElement(
            XName.Get("TestRequest", "http://www.n11.com/ws/schemas"));

        // Act
        await sut.SendAsync("TestService", "testAction", bodyContent);

        // Assert
        soapActionValues.Should().NotBeNull();
        soapActionValues!.First().Should().Contain("testAction");
    }
}
