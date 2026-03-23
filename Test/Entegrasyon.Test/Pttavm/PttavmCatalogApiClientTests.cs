using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.Test.Pttavm;

public class TestDelegatingHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public TestDelegatingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = req => Task.FromResult(handler(req));

    public TestDelegatingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => _handler(request);
}

public class PttavmCatalogApiClientTests : IDisposable
{
    private const string TestApiKey = "test-api-key-123";
    private const string TestAccessToken = "test-access-token-456";
    private const string TestBaseUrl = "https://integration-api.pttavm.com";

    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<PttavmCatalogApiClient>> _loggerMock;

    public PttavmCatalogApiClientTests()
    {
        _contextFactoryMock = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<PttavmCatalogApiClient>>();

        SetupDbContext();
        PttavmCatalogApiClient.ClearCacheForTesting();
    }

    public void Dispose()
    {
        PttavmCatalogApiClient.ClearCacheForTesting();
    }

    private void SetupDbContext()
    {
        var marketPlace = new MarketPlace
        {
            Id = MarketPlaceConstants.PttavmMarketPlaceId,
            Name = "PttAVM",
            ApiKey = TestApiKey,
            ApiSecret = TestAccessToken,
            BaseUrl = TestBaseUrl
        };

        var mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        mockDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { marketPlace });

        _contextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new Mock<IntegrationDbContext>(
                    new DbContextOptionsBuilder<IntegrationDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .Options);

                ctx.Setup(x => x.MarketPlaces)
                    .ReturnsDbSet(new List<MarketPlace> { marketPlace });

                return ctx.Object;
            });
    }

    private PttavmCatalogApiClient CreateSut(HttpMessageHandler handler)
    {
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        return new PttavmCatalogApiClient(
            _contextFactoryMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldIncludeRequiredHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestDelegatingHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });
        var sut = CreateSut(handler);

        // Act
        await sut.GetAsync("/api/rest/category/main-category");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.GetValues("Api-Key").Should().ContainSingle().Which.Should().Be(TestApiKey);
        capturedRequest.Headers.GetValues("Access-Token").Should().ContainSingle().Which.Should().Be(TestAccessToken);
        capturedRequest.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        var correlationId = capturedRequest.Headers.GetValues("X-Correlation-Id").Single();
        Guid.TryParse(correlationId, out _).Should().BeTrue("X-Correlation-Id should be a valid GUID");
    }

    [Fact]
    public async Task PostAsync_ShouldSerializeBodyAsJson()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new TestDelegatingHandler(async req =>
        {
            capturedRequest = req;
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });
        var sut = CreateSut(handler);
        var payload = new { name = "test-product", price = 99.9m };

        // Act
        await sut.PostAsync("/api/rest/product", payload);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        capturedBody.Should().NotBeNullOrEmpty();

        var doc = JsonDocument.Parse(capturedBody!);
        doc.RootElement.GetProperty("name").GetString().Should().Be("test-product");
        doc.RootElement.GetProperty("price").GetDecimal().Should().Be(99.9m);
    }

    [Fact]
    public async Task GetAsync_ShouldUseCachedCredentials_OnSecondCall()
    {
        // Arrange
        var handler = new TestDelegatingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        var sut = CreateSut(handler);

        // Act
        await sut.GetAsync("/api/rest/category/main-category");
        await sut.GetAsync("/api/rest/category/tree");

        // Assert — DB should only be queried once (credentials cached after first call)
        _contextFactoryMock.Verify(
            f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
