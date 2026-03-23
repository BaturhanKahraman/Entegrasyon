using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

/// <summary>
/// PttavmProductService unit testleri.
/// </summary>
public class PttavmProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPttavmCatalogApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<PttavmProductService>> _mockLogger = new();

    private PttavmProductService CreateSut() => new(
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

    private static HttpResponseMessage ErrorResponse(string body = "Bad Request") =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    // ── Upsert Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertProductsAsync_EmptyList_ReturnsError()
    {
        var sut = CreateSut();

        var result = await sut.UpsertProductsAsync(new List<PttavmProductRequest>());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("boş");
    }

    [Fact]
    public async Task UpsertProductsAsync_ExceedsMaxBatch_ReturnsError()
    {
        var sut = CreateSut();
        var products = Enumerable.Range(0, 1001)
            .Select(i => new PttavmProductRequest(1, $"barcode-{i}", $"Product-{i}", 100, 20, 120, 10, null, null, null, null, null, null))
            .ToList();

        var result = await sut.UpsertProductsAsync(products);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("1000");
    }

    [Fact]
    public async Task UpsertProductsAsync_SuccessfulResponse_ReturnsTrackingId()
    {
        // Arrange
        var upsertResult = new PttavmUpsertResult(1, "tracking-123", true, null);
        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("products/upsert")), It.IsAny<List<PttavmProductRequest>>()))
            .ReturnsAsync(CreateJsonResponse(upsertResult));

        var products = new List<PttavmProductRequest>
        {
            new(1, "BARCODE1", "Test Product", 100, 20, 120, 10, null, null, null, null, null, null)
        };

        var sut = CreateSut();

        // Act
        var result = await sut.UpsertProductsAsync(products);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TrackingId.Should().Be("tracking-123");
    }

    [Fact]
    public async Task UpsertProductsAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<PttavmProductRequest>>()))
            .ReturnsAsync(ErrorResponse());

        var products = new List<PttavmProductRequest>
        {
            new(1, "BARCODE1", "Test", 100, 20, 120, 10, null, null, null, null, null, null)
        };

        var sut = CreateSut();
        var result = await sut.UpsertProductsAsync(products);

        result.Success.Should().BeFalse();
    }

    // ── Tracking Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTrackingResultAsync_SuccessfulResponse_ReturnsResult()
    {
        var trackingResult = new PttavmTrackingResult("tracking-123", "Completed", 1, DateTime.UtcNow, DateTime.UtcNow,
            new PttavmSubTrackingResult(1, 0, 0, 1, 0, new List<PttavmProductTrackingInfo>()));

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("tracking-result")), It.IsAny<object>()))
            .ReturnsAsync(CreateJsonResponse(trackingResult));

        var sut = CreateSut();
        var result = await sut.GetTrackingResultAsync("tracking-123");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetTrackingResultAsync_EmptyTrackingId_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetTrackingResultAsync("");

        result.Success.Should().BeFalse();
    }

    // ── Barcode Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductByBarcodeAsync_SuccessfulResponse_ReturnsProduct()
    {
        var productInfo = new PttavmProductInfo(1, "BC001", "Test", 10, 100, 120, 20, true, true, 0, 1, 2, null, null);

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("products") && u.Contains("BC001"))))
            .ReturnsAsync(CreateJsonResponse(productInfo));

        var sut = CreateSut();
        var result = await sut.GetProductByBarcodeAsync("BC001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Barkod.Should().Be("BC001");
    }

    [Fact]
    public async Task GetProductByBarcodeAsync_EmptyBarcode_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetProductByBarcodeAsync("");

        result.Success.Should().BeFalse();
    }

    // ── Status Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetProductStatusAsync_SuccessfulResponse_ReturnsSuccess()
    {
        var baseResult = new PttavmBaseResult(true, null, null);
        _mockApiClient
            .Setup(x => x.PutAsync(It.Is<string>(u => u.Contains("status")), It.IsAny<PttavmProductStatusRequest>()))
            .ReturnsAsync(CreateJsonResponse(baseResult));

        var sut = CreateSut();
        var result = await sut.SetProductStatusAsync(123, true);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SetProductStatusAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<PttavmProductStatusRequest>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.SetProductStatusAsync(123, false);

        result.Success.Should().BeFalse();
    }

    // ── GetProductsByBarcodes Tests ─────────────────────────────────────────

    [Fact]
    public async Task GetProductsByBarcodesAsync_EmptyList_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetProductsByBarcodesAsync(new List<string>());

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductsByBarcodesAsync_SuccessfulResponse_ReturnsList()
    {
        var products = new List<PttavmProductInfo>
        {
            new(1, "BC001", "Product1", 10, 100, 120, 20, true, true, 0, 1, 2, null, null)
        };

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("get-by-barcodes")), It.IsAny<PttavmGetByBarcodesRequest>()))
            .ReturnsAsync(CreateJsonResponse(products));

        var sut = CreateSut();
        var result = await sut.GetProductsByBarcodesAsync(new List<string> { "BC001" });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
    }
}
