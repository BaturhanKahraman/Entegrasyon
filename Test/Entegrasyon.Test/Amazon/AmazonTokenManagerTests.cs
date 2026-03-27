using System.Net;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Amazon;

public class AmazonTokenManagerTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<AmazonTokenManager>> _loggerMock = new();

    private AmazonTokenManager CreateSut() => new(
        mockScopeFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    private void SetupAmazonMarketPlace(string? refreshToken = "test-refresh-token",
        string? apiKey = "test-client-id", string? apiSecret = "test-client-secret")
    {
        var mp = new MarketPlace
        {
            Id = AmazonMarketPlaceId,
            Name = "Amazon",
            RefreshToken = refreshToken,
            ApiKey = apiKey,
            ApiSecret = apiSecret,
            TokenUrl = "https://api.amazon.com/auth/o2/token"
        };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { mp });
    }

    private static HttpClient CreateHttpClientWithTokenResponse(string accessToken = "test-access-token")
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"access_token\":\"{accessToken}\",\"token_type\":\"bearer\",\"expires_in\":3600}}")
            });
        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Should_Return_Token()
    {
        SetupAmazonMarketPlace();
        var client = CreateHttpClientWithTokenResponse("my-access-token");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        using var sut = CreateSut();
        var token = await sut.GetAccessTokenAsync();

        token.Should().Be("my-access-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_Should_Cache_Token()
    {
        SetupAmazonMarketPlace();
        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"access_token\":\"cached-token\",\"token_type\":\"bearer\",\"expires_in\":3600}")
                };
            });
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        using var sut = CreateSut();
        var token1 = await sut.GetAccessTokenAsync();
        var token2 = await sut.GetAccessTokenAsync();

        token1.Should().Be("cached-token");
        token2.Should().Be("cached-token");
        callCount.Should().Be(1); // Tek HTTP çağrısı — cache çalışıyor
    }

    [Fact]
    public async Task InvalidateToken_Should_Force_Refresh()
    {
        SetupAmazonMarketPlace();
        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $"{{\"access_token\":\"token-{callCount}\",\"token_type\":\"bearer\",\"expires_in\":3600}}")
                };
            });
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        using var sut = CreateSut();
        var token1 = await sut.GetAccessTokenAsync();
        sut.InvalidateToken();
        var token2 = await sut.GetAccessTokenAsync();

        callCount.Should().Be(2); // Invalidate sonrası yeni çağrı
        token1.Should().NotBe(token2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Should_Throw_When_No_RefreshToken()
    {
        SetupAmazonMarketPlace(refreshToken: null);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        using var sut = CreateSut();

        await sut.Invoking(s => s.GetAccessTokenAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*refresh token*");
    }

    [Fact]
    public async Task GetAccessTokenAsync_Should_Throw_When_MarketPlace_Not_Found()
    {
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>());

        using var sut = CreateSut();

        await sut.Invoking(s => s.GetAccessTokenAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Amazon*bulunamadı*");
    }
}
