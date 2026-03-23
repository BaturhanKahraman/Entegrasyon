using System.Net;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Trendyol;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolStockPriceService unit tests — verifies stock/price update endpoint behavior.
/// </summary>
public class TrendyolStockPriceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<TrendyolStockPriceService>> _loggerMock = new();

    private TrendyolStockPriceService CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlace(string? sellerId = "12345")
    {
        var mp = new MarketPlace { Id = TrendyolMarketPlaceId, Name = "Trendyol", SellerId = sellerId };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { mp });
    }

    private static List<TrendyolPriceInventoryItem> CreateDummyItems(int count = 2) =>
        Enumerable.Range(1, count)
            .Select(i => new TrendyolPriceInventoryItem($"BARCODE{i}", 10 * i, 100m + i, 120m + i))
            .ToList();

    // ── Test 1: Happy path ──

    [Fact]
    public async Task UpdatePriceAndInventoryAsync_HappyPath_ReturnsBatchId()
    {
        // Arrange
        SetupMarketPlace("12345");

        var batchResponse = new TrendyolBatchResponse("batch-stock-123");
        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(batchResponse)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAndInventoryAsync(CreateDummyItems());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("batch-stock-123");
        result.Message.Should().Contain("2");
    }

    // ── Test 2: Empty items list ──

    [Fact]
    public async Task UpdatePriceAndInventoryAsync_EmptyItems_ReturnsError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAndInventoryAsync(new List<TrendyolPriceInventoryItem>());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("yok");
    }

    // ── Test 3: SellerId missing ──

    [Fact]
    public async Task UpdatePriceAndInventoryAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAndInventoryAsync(CreateDummyItems());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SellerId");
    }

    // ── Test 4: API error ──

    [Fact]
    public async Task UpdatePriceAndInventoryAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAndInventoryAsync(CreateDummyItems());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("InternalServerError");
    }

    // ── Test 5: Uses correct URL with SellerId ──

    [Fact]
    public async Task UpdatePriceAndInventoryAsync_UsesCorrectUrlWithSellerId()
    {
        // Arrange
        SetupMarketPlace("99999");

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((url, _) => capturedUrl = url)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TrendyolBatchResponse("ok"))
            });

        var sut = CreateSut();

        // Act
        await sut.UpdatePriceAndInventoryAsync(CreateDummyItems(1));

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("99999");
        capturedUrl.Should().Contain("price-and-inventory");
    }

    // ── Test 6: Null batch response returns "ok" ──

    [Fact]
    public async Task UpdatePriceAndInventoryAsync_WhenNullBatchId_ReturnsFallbackOk()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAndInventoryAsync(CreateDummyItems(1));

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("ok");
    }
}
