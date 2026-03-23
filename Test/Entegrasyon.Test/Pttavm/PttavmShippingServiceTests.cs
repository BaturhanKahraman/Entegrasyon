using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

/// <summary>
/// PttavmShippingService unit testleri.
/// </summary>
public class PttavmShippingServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPttavmShipmentApiClient> _mockShipmentClient = new();
    private readonly Mock<ILogger<PttavmShippingService>> _mockLogger = new();

    private PttavmShippingService CreateSut() => new(
        _mockShipmentClient.Object,
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

    // ── GetWarehouses Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetWarehousesAsync_SuccessfulResponse_ReturnsList()
    {
        var warehouses = new List<PttavmWarehouse>
        {
            new(100301619, "Ana Depo", false, "", true)
        };

        _mockShipmentClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("get-warehouse")), It.IsAny<object>()))
            .ReturnsAsync(CreateJsonResponse(warehouses));

        var sut = CreateSut();
        var result = await sut.GetWarehousesAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
        result.Data![0].Name.Should().Be("Ana Depo");
    }

    [Fact]
    public async Task GetWarehousesAsync_ApiError_ReturnsError()
    {
        _mockShipmentClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.GetWarehousesAsync();

        result.Success.Should().BeFalse();
    }

    // ── CreateBarcodes Tests ────────────────────────────────────────────────

    [Fact]
    public async Task CreateBarcodesAsync_EmptyList_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.CreateBarcodesAsync(new List<PttavmBarcodeRequest>());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("boş");
    }

    [Fact]
    public async Task CreateBarcodesAsync_SuccessfulResponse_ReturnsTrackingId()
    {
        var response = new PttavmBarcodeCreateResult("tracking-789", 1, 200, true, "", false);

        _mockShipmentClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("create-barcode")), It.IsAny<PttavmBarcodeCreateRequestBody>()))
            .ReturnsAsync(CreateJsonResponse(response));

        var orders = new List<PttavmBarcodeRequest>
        {
            new("ORD-001", 100301619)
        };

        var sut = CreateSut();
        var result = await sut.CreateBarcodesAsync(orders);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TrackingId.Should().Be("tracking-789");
    }

    // ── CheckBarcodeStatus Tests ────────────────────────────────────────────

    [Fact]
    public async Task CheckBarcodeStatusAsync_EmptyTrackingId_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.CheckBarcodeStatusAsync("");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CheckBarcodeStatusAsync_SuccessfulResponse_ReturnsResult()
    {
        var response = new PttavmBarcodeStatusResult("tracking-789", "completed",
            new List<PttavmBarcodeStatusData>
            {
                new("ORD-001", new List<string> { "67890" })
            }, "");

        _mockShipmentClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("barcode-status")), It.IsAny<PttavmBarcodeStatusRequestBody>()))
            .ReturnsAsync(CreateJsonResponse(response));

        var sut = CreateSut();
        var result = await sut.CheckBarcodeStatusAsync("tracking-789");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be("completed");
    }

    // ── GetBarcodeTag Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetBarcodeTagAsync_EmptyBarcode_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetBarcodeTagAsync("", "ORD-001");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetBarcodeTagAsync_SuccessfulResponse_ReturnsTagContent()
    {
        var htmlContent = "<html><body>Etiket bilgisi</body></html>";

        _mockShipmentClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("get-barcode-tag")), It.IsAny<PttavmBarcodeTagRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(htmlContent, System.Text.Encoding.UTF8, "text/html")
            });

        var sut = CreateSut();
        var result = await sut.GetBarcodeTagAsync("67890", "ORD-001");

        result.Success.Should().BeTrue();
        result.Data.Should().Contain("Etiket");
    }

    // ── UpdateNoShippingOrder Tests ─────────────────────────────────────────

    [Fact]
    public async Task UpdateNoShippingOrderAsync_EmptyOrderId_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.UpdateNoShippingOrderAsync("");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNoShippingOrderAsync_SuccessfulResponse_ReturnsSuccess()
    {
        var response = new PttavmNoShippingResult("Teslim edildi", true);

        _mockShipmentClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("update-no-shipping-order")), It.IsAny<PttavmNoShippingOrderRequest>()))
            .ReturnsAsync(CreateJsonResponse(response));

        var sut = CreateSut();
        var result = await sut.UpdateNoShippingOrderAsync("ORD-001");

        result.Success.Should().BeTrue();
    }
}
