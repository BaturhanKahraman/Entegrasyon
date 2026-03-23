using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity.Dtos.Amazon;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonCatalogService unit tests.
/// </summary>
public class AmazonCatalogServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IAmazonApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<AmazonCatalogService>> _mockLogger = new();

    private AmazonCatalogService CreateSut() => new(
        _mockApiClient.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    // ── SearchCatalogItemsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SearchCatalogItemsAsync_Success_ReturnsData()
    {
        // Arrange
        var expectedResponse = new AmazonCatalogSearchResponse(
            NumberOfResults: 1,
            Pagination: null,
            Items: new List<AmazonCatalogItem>
            {
                new("B00TEST123", null, null, null, null, null)
            });

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("catalog")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(expectedResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCatalogItemsAsync("test keyword", new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.NumberOfResults.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items![0].Asin.Should().Be("B00TEST123");
    }

    [Fact]
    public async Task SearchCatalogItemsAsync_ApiReturnsError_ReturnsErrorResult()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{}")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCatalogItemsAsync("test", new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Catalog search failed");
    }

    [Fact]
    public async Task SearchCatalogItemsAsync_Exception_ReturnsErrorResult()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCatalogItemsAsync("test", new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    // ── GetCatalogItemAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCatalogItemAsync_Success_ReturnsItem()
    {
        // Arrange
        var item = new AmazonCatalogItem("B00ASIN001", null, null, null,
            new List<AmazonCatalogSummary>
            {
                new("A33AVAJ2PDY3EV", "TestBrand", "Test Product", null)
            }, null);

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("B00ASIN001")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(item));

        var sut = CreateSut();

        // Act
        var result = await sut.GetCatalogItemAsync("B00ASIN001", new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Asin.Should().Be("B00ASIN001");
    }

    [Fact]
    public async Task GetCatalogItemAsync_NotFound_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetCatalogItemAsync("INVALID", new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Catalog item not found");
    }

    [Fact]
    public async Task GetCatalogItemAsync_WithCustomIncludedData_PassesCorrectUrl()
    {
        // Arrange
        string? capturedUrl = null;
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(new AmazonCatalogItem("B00X", null, null, null, null, null)));

        var sut = CreateSut();

        // Act
        await sut.GetCatalogItemAsync("B00X", new[] { "A33AVAJ2PDY3EV" }, new[] { "summaries", "images" });

        // Assert
        capturedUrl.Should().Contain("includedData=summaries,images");
    }

    // ── SearchByIdentifierAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SearchByIdentifierAsync_Success_ReturnsData()
    {
        // Arrange
        var expectedResponse = new AmazonCatalogSearchResponse(
            NumberOfResults: 1,
            Pagination: null,
            Items: new List<AmazonCatalogItem>
            {
                new("B00FOUND", null, null, null, null, null)
            });

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("identifiers")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(expectedResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchByIdentifierAsync("8680123456789", "EAN", new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchByIdentifierAsync_ApiError_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{}")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SearchByIdentifierAsync("123", "EAN", new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Identifier search failed");
    }
}
