using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity.Dtos.Amazon;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonListingService unit tests — PutListingItem, PatchListingItem, GetListingItem, DeleteListingItem.
/// </summary>
public class AmazonListingServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IAmazonApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<AmazonListingService>> _mockLogger = new();

    private AmazonListingService CreateSut() => new(
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

    private static readonly string[] MarketplaceIds = { "A33AVAJ2PDY3EV" };

    // ── PutListingItemAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task PutListingItemAsync_Success_ReturnsAccepted()
    {
        // Arrange
        var submissionResponse = new AmazonListingSubmissionResponse("TEST-SKU", "ACCEPTED", "sub-123", null);
        _mockApiClient
            .Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<AmazonListingItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(submissionResponse));

        var listingItem = new AmazonListingItem("PRODUCT", "LISTING", new Dictionary<string, object>());
        var sut = CreateSut();

        // Act
        var result = await sut.PutListingItemAsync("SELLER1", "TEST-SKU", listingItem, MarketplaceIds);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("ACCEPTED");
        result.Data.SubmissionId.Should().Be("sub-123");
    }

    [Fact]
    public async Task PutListingItemAsync_ApiError_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<AmazonListingItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("validation error")
            });

        var listingItem = new AmazonListingItem("PRODUCT", "LISTING", new Dictionary<string, object>());
        var sut = CreateSut();

        // Act
        var result = await sut.PutListingItemAsync("SELLER1", "TEST-SKU", listingItem, MarketplaceIds);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Listing oluşturma hatası");
    }

    [Fact]
    public async Task PutListingItemAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<AmazonListingItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var listingItem = new AmazonListingItem("PRODUCT", "LISTING", new Dictionary<string, object>());
        var sut = CreateSut();

        // Act
        var result = await sut.PutListingItemAsync("SELLER1", "SKU", listingItem, MarketplaceIds);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("timeout");
    }

    // ── PatchListingItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task PatchListingItemAsync_Success_ReturnsAccepted()
    {
        // Arrange
        var submissionResponse = new AmazonListingSubmissionResponse("SKU-1", "ACCEPTED", "sub-patch", null);
        _mockApiClient
            .Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<AmazonListingPatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(submissionResponse));

        var patchRequest = new AmazonListingPatchRequest("PRODUCT", new List<AmazonListingPatch>
        {
            new("replace", "/attributes/purchasable_offer", null)
        });
        var sut = CreateSut();

        // Act
        var result = await sut.PatchListingItemAsync("SELLER1", "SKU-1", patchRequest, MarketplaceIds);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("ACCEPTED");
    }

    [Fact]
    public async Task PatchListingItemAsync_ApiError_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<AmazonListingPatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("forbidden")
            });

        var patchRequest = new AmazonListingPatchRequest("PRODUCT", new List<AmazonListingPatch>());
        var sut = CreateSut();

        // Act
        var result = await sut.PatchListingItemAsync("SELLER1", "SKU-1", patchRequest, MarketplaceIds);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Listing güncelleme hatası");
    }

    // ── GetListingItemAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetListingItemAsync_Success_ReturnsItem()
    {
        // Arrange
        var listingResponse = new AmazonListingItemResponse(
            "TEST-SKU",
            new List<AmazonListingSummary>
            {
                new("A33AVAJ2PDY3EV", "B00ASIN1", "PRODUCT", new List<string> { "BUYABLE" }, "Test Product")
            },
            null, null, null);

        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(listingResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetListingItemAsync("SELLER1", "TEST-SKU", MarketplaceIds);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Sku.Should().Be("TEST-SKU");
        result.Data.Summaries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetListingItemAsync_NotFound_ReturnsError()
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
        var result = await sut.GetListingItemAsync("SELLER1", "UNKNOWN", MarketplaceIds);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Listing bulunamadı");
    }

    // ── DeleteListingItemAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteListingItemAsync_Success_ReturnsSuccess()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteListingItemAsync("SELLER1", "TEST-SKU", MarketplaceIds);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Listing silindi");
    }

    [Fact]
    public async Task DeleteListingItemAsync_ApiError_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteListingItemAsync("SELLER1", "TEST-SKU", MarketplaceIds);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Listing silme hatası");
    }

    [Fact]
    public async Task DeleteListingItemAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("network error"));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteListingItemAsync("SELLER1", "TEST-SKU", MarketplaceIds);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("network error");
    }
}
