using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

/// <summary>
/// PttavmStockPriceService unit testleri.
/// </summary>
public class PttavmStockPriceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPttavmCatalogApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<PttavmStockPriceService>> _mockLogger = new();

    private PttavmStockPriceService CreateSut() => new(
        _mockApiClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T payload, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request", System.Text.Encoding.UTF8, "application/json") };

    // ── UpdateStockPrices Tests ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateStockPricesAsync_EmptyList_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.UpdateStockPricesAsync(new List<PttavmStockPriceRequest>());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("boş");
    }

    [Fact]
    public async Task UpdateStockPricesAsync_ExceedsMaxBatch_ReturnsError()
    {
        var sut = CreateSut();
        var items = Enumerable.Range(0, 1001)
            .Select(i => new PttavmStockPriceRequest($"barcode-{i}", true, 10, 100, 120, 20, 0, false, null))
            .ToList();

        var result = await sut.UpdateStockPricesAsync(items);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("1000");
    }

    [Fact]
    public async Task UpdateStockPricesAsync_SuccessfulResponse_ReturnsTrackingId()
    {
        var upsertResult = new PttavmUpsertResult(1, "tracking-456", true, null);
        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("stock-prices")), It.IsAny<List<PttavmStockPriceRequest>>()))
            .ReturnsAsync(CreateJsonResponse(upsertResult));

        var items = new List<PttavmStockPriceRequest>
        {
            new("BC001", true, 50, 100, 120, 20, 0, false, null)
        };

        var sut = CreateSut();
        var result = await sut.UpdateStockPricesAsync(items);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TrackingId.Should().Be("tracking-456");
    }

    [Fact]
    public async Task UpdateStockPricesAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<PttavmStockPriceRequest>>()))
            .ReturnsAsync(ErrorResponse());

        var items = new List<PttavmStockPriceRequest>
        {
            new("BC001", true, 50, 100, 120, 20, 0, false, null)
        };

        var sut = CreateSut();
        var result = await sut.UpdateStockPricesAsync(items);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStockPricesAsync_InvalidStockRange_ReturnsError()
    {
        var sut = CreateSut();
        var items = new List<PttavmStockPriceRequest>
        {
            new("BC001", true, 10000, 100, 120, 20, 0, false, null) // exceeds 9999
        };

        var result = await sut.UpdateStockPricesAsync(items);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("9999");
    }

    [Fact]
    public async Task UpdateStockPricesAsync_InvalidDiscount_ReturnsError()
    {
        var sut = CreateSut();
        var items = new List<PttavmStockPriceRequest>
        {
            new("BC001", true, 10, 100, 120, 20, 71, false, null) // exceeds 70
        };

        var result = await sut.UpdateStockPricesAsync(items);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("70");
    }

    // ── SearchProducts Tests ────────────────────────────────────────────────

    [Fact]
    public async Task SearchProductsAsync_SuccessfulResponse_ReturnsList()
    {
        var products = new List<PttavmProductInfo>
        {
            new(1, "BC001", "Product1", 10, 100, 120, 20, true, true, 0, 1, 2, null, null)
        };

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("products/search"))))
            .ReturnsAsync(CreateJsonResponse(products));

        var sut = CreateSut();
        var result = await sut.SearchProductsAsync(new PttavmProductSearchFilter());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
    }

    [Fact]
    public async Task SearchProductsAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.SearchProductsAsync(new PttavmProductSearchFilter());

        result.Success.Should().BeFalse();
    }
}
