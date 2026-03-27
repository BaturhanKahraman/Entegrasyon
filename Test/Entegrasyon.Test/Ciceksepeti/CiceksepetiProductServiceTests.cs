using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiProductService unit testleri.
/// </summary>
public class CiceksepetiProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICiceksepetiApiClient> _mockApiClient = new();
    private readonly Mock<ICiceksepetiProductMapper> _mockMapper = new();
    private readonly Mock<CiceksepetiMappingValidator> _mockValidator;
    private readonly Mock<IProductActivityLogger> _mockActivityLogger = new();
    private readonly Mock<ILogger<CiceksepetiProductService>> _mockLogger = new();

    public CiceksepetiProductServiceTests()
    {
        _mockValidator = new Mock<CiceksepetiMappingValidator>(mockContextFactory.Object);

        // Default: activity logger does nothing
        _mockActivityLogger
            .Setup(x => x.LogAsync(
                It.IsAny<Guid>(), It.IsAny<ProductActivityType>(), It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private CiceksepetiProductService CreateSut() => new(
        _mockApiClient.Object,
        _mockMapper.Object,
        _mockValidator.Object,
        _mockActivityLogger.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishProductAsync_ValidProduct_ReturnsBatchId()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedBatchId = "batch-abc-123";

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mockMapper
            .Setup(x => x.MapToCreateRequestAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<CiceksepetiCreateProductsRequest>(
                new CiceksepetiCreateProductsRequest(new List<CiceksepetiProductRequest>())));

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("Products")), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(new CiceksepetiBatchResponse(BatchId: expectedBatchId)));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(expectedBatchId);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishProductAsync_ValidationFails_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        const string errorMsg = "Ürünün kategorisi eşleştirilmemiş.";

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new ErrorResult(errorMsg));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMsg);

        // Mapper and API client should NOT be called
        _mockMapper.Verify(x => x.MapToCreateRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApiClient.Verify(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckBatchStatusAsync_ReturnsParsedStatuses()
    {
        // Arrange
        var batchId = "batch-xyz";
        var expectedResponse = new CiceksepetiBatchStatusResponse(
            BatchId: batchId,
            ItemCount: 2,
            Items: new List<CiceksepetiBatchItemDto>
            {
                new(Data: null, ItemId: "item1", Status: "Completed",
                    FailureReasons: null, LastModificationDate: null),
                new(Data: null, ItemId: "item2", Status: "Failed",
                    FailureReasons: new List<CiceksepetiFailureReason>
                    {
                        new(Message: "Geçersiz barkod", Code: "ERR_001")
                    },
                    LastModificationDate: null)
            });

        _mockApiClient
            .Setup(x => x.GetAsync(
                It.Is<string>(u => u.Contains($"Products/batch-status/{batchId}")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(expectedResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.CheckBatchStatusAsync(batchId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.BatchId.Should().Be(batchId);
        result.Data.ItemCount.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items[0].Status.Should().Be("Completed");
        result.Data.Items[1].Status.Should().Be("Failed");
        result.Data.Items[1].FailureReasons.Should().HaveCount(1);
        result.Data.Items[1].FailureReasons![0].Message.Should().Be("Geçersiz barkod");
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductsAsync_Uses1BasedPagination()
    {
        // Arrange
        var listResponse = new CiceksepetiProductListResponse(
            TotalCount: 3,
            Products: new List<CiceksepetiProductDto>
            {
                new("Ürün A", "P001", 100, "Kategori", "SKU-A", "MAIN-A", 1,
                    null, null, 99.9m, 10, "BAR-A", true, null, null),
                new("Ürün B", "P002", 100, "Kategori", "SKU-B", "MAIN-A", 1,
                    null, null, 49.9m, 5, "BAR-B", true, null, null),
                new("Ürün C", "P003", 100, "Kategori", "SKU-C", "MAIN-A", 2,
                    null, null, 199.9m, 2, "BAR-C", false, null, null),
            });

        string? capturedUrl = null;
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(listResponse));

        var sut = CreateSut();

        // Act — page=1 (1-based)
        var result = await sut.GetProductsAsync(page: 1, pageSize: 60);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(3);
        result.Data.Products.Should().HaveCount(3);

        // URL must use 1-based page parameter
        capturedUrl.Should().Contain("Page=1");
        capturedUrl.Should().Contain("PageSize=60");
    }
}
