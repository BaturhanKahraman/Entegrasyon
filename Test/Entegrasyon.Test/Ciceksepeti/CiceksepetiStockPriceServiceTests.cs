using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiStockPriceService unit testleri.
/// </summary>
public class CiceksepetiStockPriceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICiceksepetiApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<CiceksepetiStockPriceService>> _mockLogger = new();

    private CiceksepetiStockPriceService CreateSut() => new(
        _mockApiClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateBatchResponse(string batchId, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(new CiceksepetiBatchResponse(BatchId: batchId));
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStockAndPriceAsync_ValidItems_ReturnsBatchId()
    {
        // Arrange
        var items = Enumerable.Range(1, 5)
            .Select(i => new CiceksepetiStockPriceItem(
                StockCode: $"SKU-{i}",
                StockQuantity: 10,
                SalesPrice: 99.90m,
                ListPrice: null))
            .ToList();

        _mockApiClient
            .Setup(x => x.PutAsync(
                It.Is<string>(u => u.Contains("Products/price-and-stock")),
                It.IsAny<CiceksepetiStockPriceUpdateRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBatchResponse("test-batch-123"));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStockAndPriceAsync(items);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Should().Be("test-batch-123");

        _mockApiClient.Verify(
            x => x.PutAsync(It.IsAny<string>(), It.IsAny<CiceksepetiStockPriceUpdateRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStockAndPriceAsync_Over200Items_SplitsIntoBatches()
    {
        // Arrange — 250 items → 2 batches: 200 + 50
        var items = Enumerable.Range(1, 250)
            .Select(i => new CiceksepetiStockPriceItem(
                StockCode: $"SKU-{i}",
                StockQuantity: 5,
                SalesPrice: 50m,
                ListPrice: null))
            .ToList();

        var callCount = 0;
        _mockApiClient
            .Setup(x => x.PutAsync(
                It.Is<string>(u => u.Contains("Products/price-and-stock")),
                It.IsAny<CiceksepetiStockPriceUpdateRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateBatchResponse($"batch-{++callCount}"));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStockAndPriceAsync(items);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Should().Be("batch-1");
        result.Data[1].Should().Be("batch-2");

        _mockApiClient.Verify(
            x => x.PutAsync(It.IsAny<string>(), It.IsAny<CiceksepetiStockPriceUpdateRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStockAndPriceAsync_ListPriceWithoutSalesPrice_ReturnsError()
    {
        // Arrange — item has ListPrice but no SalesPrice (business rule violation)
        var items = new List<CiceksepetiStockPriceItem>
        {
            new CiceksepetiStockPriceItem(
                StockCode: "SKU-BAD",
                StockQuantity: 10,
                SalesPrice: null,       // missing SalesPrice!
                ListPrice: 199.90m)     // has ListPrice — invalid
        };

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStockAndPriceAsync(items);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();

        // API should NOT be called
        _mockApiClient.Verify(
            x => x.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStockAndPriceAsync_EmptyItems_ReturnsError()
    {
        // Arrange
        var items = new List<CiceksepetiStockPriceItem>();

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStockAndPriceAsync(items);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();

        // API should NOT be called
        _mockApiClient.Verify(
            x => x.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
