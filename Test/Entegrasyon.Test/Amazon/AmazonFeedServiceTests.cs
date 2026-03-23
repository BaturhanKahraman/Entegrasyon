using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity.Dtos.Amazon;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonFeedService unit tests — verifies submit feed workflow and status polling.
/// </summary>
public class AmazonFeedServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IAmazonApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<AmazonFeedService>> _mockLogger = new();

    private AmazonFeedService CreateSut() => new(
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

    // ── SubmitFeedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitFeedAsync_FullWorkflow_ReturnsFeedId()
    {
        // Arrange
        var docResponse = new AmazonFeedDocumentResponse("doc-123", "https://s3.amazonaws.com/presigned");
        var feedResponse = new AmazonCreateFeedResponse("feed-abc-456");

        // Step 1: Create feed document
        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("documents")), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(docResponse));

        // Step 2: Upload content
        _mockApiClient
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Step 3: Create feed
        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("feeds") && !u.Contains("documents")), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(feedResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.SubmitFeedAsync(
            "POST_PRODUCT_DATA", "application/json",
            new byte[] { 1, 2, 3 },
            new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("feed-abc-456");
    }

    [Fact]
    public async Task SubmitFeedAsync_DocumentCreationFails_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("documents")), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad request")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SubmitFeedAsync("POST_PRODUCT_DATA", "application/json", new byte[] { 1 }, new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Feed document creation failed");
    }

    [Fact]
    public async Task SubmitFeedAsync_UploadFails_ReturnsError()
    {
        // Arrange
        var docResponse = new AmazonFeedDocumentResponse("doc-123", "https://s3.amazonaws.com/presigned");

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("documents")), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(docResponse));

        _mockApiClient
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden));

        var sut = CreateSut();

        // Act
        var result = await sut.SubmitFeedAsync("POST_PRODUCT_DATA", "application/json", new byte[] { 1 }, new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Feed content upload failed");
    }

    [Fact]
    public async Task SubmitFeedAsync_FeedCreationFails_ReturnsError()
    {
        // Arrange
        var docResponse = new AmazonFeedDocumentResponse("doc-123", "https://s3.amazonaws.com/presigned");

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("documents")), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(docResponse));

        _mockApiClient
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("feeds") && !u.Contains("documents")), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server error")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SubmitFeedAsync("POST_PRODUCT_DATA", "application/json", new byte[] { 1 }, new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Feed creation failed");
    }

    [Fact]
    public async Task SubmitFeedAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();

        // Act
        var result = await sut.SubmitFeedAsync("POST_PRODUCT_DATA", "application/json", new byte[] { 1 }, new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    // ── GetFeedStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetFeedStatusAsync_Success_ReturnsStatus()
    {
        // Arrange
        var statusResponse = new AmazonFeedStatusResponse("feed-123", "POST_PRODUCT_DATA", "DONE", "result-doc-456");

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("feed-123")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(statusResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetFeedStatusAsync("feed-123");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.FeedId.Should().Be("feed-123");
        result.Data.ProcessingStatus.Should().Be("DONE");
        result.Data.ResultFeedDocumentId.Should().Be("result-doc-456");
    }

    [Fact]
    public async Task GetFeedStatusAsync_ApiError_ReturnsError()
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
        var result = await sut.GetFeedStatusAsync("invalid-feed");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Feed status error");
    }

    [Fact]
    public async Task GetFeedStatusAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("timeout"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetFeedStatusAsync("feed-x");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("timeout");
    }
}
