using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity.Dtos.Amazon;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonProductTypeService unit tests — search and get product type definitions.
/// </summary>
public class AmazonProductTypeServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IAmazonApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<AmazonProductTypeService>> _mockLogger = new();

    private AmazonProductTypeService CreateSut() => new(
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

    // ── SearchProductTypesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchProductTypesAsync_Success_ReturnsProductTypes()
    {
        // Arrange
        var response = new AmazonProductTypeSearchResponse(
            new List<AmazonProductTypeSearchResult>
            {
                new("SHOES", "Shoes", new List<string> { "A33AVAJ2PDY3EV" }),
                new("SANDALS", "Sandals", new List<string> { "A33AVAJ2PDY3EV" })
            });

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("productTypes")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(response));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchProductTypesAsync("shoes", "A33AVAJ2PDY3EV");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Name.Should().Be("SHOES");
        result.Data[1].Name.Should().Be("SANDALS");
    }

    [Fact]
    public async Task SearchProductTypesAsync_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(new AmazonProductTypeSearchResponse(null)));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchProductTypesAsync("nonexistent", "A33AVAJ2PDY3EV");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProductTypesAsync_ApiError_ReturnsError()
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
        var result = await sut.SearchProductTypesAsync("test", "A33AVAJ2PDY3EV");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Product type search failed");
    }

    [Fact]
    public async Task SearchProductTypesAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchProductTypesAsync("test", "A33AVAJ2PDY3EV");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("timeout");
    }

    // ── GetProductTypeDefinitionAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetProductTypeDefinitionAsync_Success_ReturnsDefinition()
    {
        // Arrange
        var definition = new AmazonProductTypeDefinition(
            "SHOES",
            new AmazonProductTypeVersion("1.0", true),
            new AmazonJsonSchemaLink(new AmazonLink("https://schema.example.com", "GET"), "abc123"),
            "LISTING",
            "ENFORCED",
            new Dictionary<string, AmazonPropertyGroup>
            {
                ["basic"] = new("Basic Info", "Basic product info", new List<string> { "item_name", "brand" })
            });

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("SHOES")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(definition));

        var sut = CreateSut();

        // Act
        var result = await sut.GetProductTypeDefinitionAsync("SHOES", "A33AVAJ2PDY3EV");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.ProductType.Should().Be("SHOES");
        result.Data.PropertyGroups.Should().ContainKey("basic");
    }

    [Fact]
    public async Task GetProductTypeDefinitionAsync_NotFound_ReturnsError()
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
        var result = await sut.GetProductTypeDefinitionAsync("INVALID_TYPE", "A33AVAJ2PDY3EV");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Product type definition not found");
    }

    [Fact]
    public async Task GetProductTypeDefinitionAsync_PassesRequirementsParam()
    {
        // Arrange
        string? capturedUrl = null;
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(new AmazonProductTypeDefinition(
                "TEST", null, null, "LISTING_OFFER_ONLY", null, null)));

        var sut = CreateSut();

        // Act
        await sut.GetProductTypeDefinitionAsync("TEST", "A33AVAJ2PDY3EV", "LISTING_OFFER_ONLY");

        // Assert
        capturedUrl.Should().Contain("requirements=LISTING_OFFER_ONLY");
    }

    [Fact]
    public async Task GetProductTypeDefinitionAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("server error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetProductTypeDefinitionAsync("SHOES", "A33AVAJ2PDY3EV");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("server error");
    }
}
