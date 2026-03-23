using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiOrderService unit testleri.
/// </summary>
public class CiceksepetiOrderServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICiceksepetiApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<CiceksepetiOrderService>> _mockLogger = new();

    private CiceksepetiOrderService CreateSut() => new(
        _mockApiClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateOrderListResponse(CiceksepetiOrderListResponse payload, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("", System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request", System.Text.Encoding.UTF8, "application/json") };

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_Uses0BasedPagination()
    {
        // Arrange
        var request = new CiceksepetiGetOrdersRequest(
            StartDate: "2024-01-01",
            EndDate: "2024-01-14",
            PageSize: 20,
            Page: 0,   // 0-based
            StatusId: null,
            OrderNo: null,
            OrderItemNo: null);

        CiceksepetiGetOrdersRequest? capturedRequest = null;

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/GetOrders")),
                It.IsAny<CiceksepetiGetOrdersRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, CiceksepetiGetOrdersRequest, CancellationToken>((_, req, _) => capturedRequest = req)
            .ReturnsAsync(CreateOrderListResponse(new CiceksepetiOrderListResponse(
                OrderListCount: 0,
                SupplierOrderListWithBranch: new List<CiceksepetiOrderItemDto>())));

        var sut = CreateSut();

        // Act
        await sut.GetOrdersAsync(request);

        // Assert — page=0 must be forwarded as-is (0-based)
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Page.Should().Be(0);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_SuccessfulResponse_ReturnsParsedOrders()
    {
        // Arrange
        var orderItem = new CiceksepetiOrderItemDto(
            BranchId: 1,
            OrderId: 1001L,
            OrderItemId: 2001L,
            OrderItemStatusId: 1,
            OrderDate: "2024-01-05",
            ProductName: "Test Ürün",
            ProductCode: "PRD-001",
            StockCode: "SKU-001",
            Quantity: 2,
            SalesPrice: 99.90m,
            ListPrice: 120.00m,
            InvoicePrice: 99.90m,
            AllowanceRate: 0m,
            ReceiverName: "Ali Veli",
            ReceiverAddress: "Test Sk. No:1",
            ReceiverCity: "İstanbul",
            ReceiverDistrict: "Kadıköy",
            ReceiverPhone: "05001234567",
            SenderName: "Mağaza A",
            CargoCompany: "Yurtiçi",
            CargoTrackingNumber: "TRACK-123",
            CargoTrackingUrl: null,
            DeliveryType: 1,
            DeliveryMessageType: 1,
            Barcode: null,
            CancellationResult: null,
            Note: null);

        var apiResponse = new CiceksepetiOrderListResponse(
            OrderListCount: 1,
            SupplierOrderListWithBranch: new List<CiceksepetiOrderItemDto> { orderItem });

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/GetOrders")),
                It.IsAny<CiceksepetiGetOrdersRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrderListResponse(apiResponse));

        var request = new CiceksepetiGetOrdersRequest(
            StartDate: "2024-01-01",
            EndDate: "2024-01-14",
            PageSize: 50,
            Page: 0,
            StatusId: null,
            OrderNo: null,
            OrderItemNo: null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrdersAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.OrderListCount.Should().Be(1);
        result.Data.SupplierOrderListWithBranch.Should().HaveCount(1);
        result.Data.SupplierOrderListWithBranch[0].OrderId.Should().Be(1001L);
        result.Data.SupplierOrderListWithBranch[0].ProductName.Should().Be("Test Ürün");
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadyForCargoWithCsAsync_CallsPutWithCorrectPath()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/readyforcargowithcsintegration")),
                It.IsAny<CiceksepetiCsCargoRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiCsCargoRequest(
            CargoGroups: new List<CiceksepetiCargoGroup>
            {
                new(OrderItemIds: new List<long> { 1001L, 1002L })
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ReadyForCargoWithCsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/readyforcargowithcsintegration")),
                It.IsAny<CiceksepetiCsCargoRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusWithOwnCargoAsync_CallsPutWithCorrectPath()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/statusupdatewithsupplierintegration")),
                It.IsAny<CiceksepetiOwnCargoRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiOwnCargoRequest(
            Items: new List<CiceksepetiOwnCargoItem>
            {
                new(OrderItemId: 2001L, CargoCompany: "Yurtiçi", TrackingNumber: "TRK-001")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStatusWithOwnCargoAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/statusupdatewithsupplierintegration")),
                It.IsAny<CiceksepetiOwnCargoRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 5 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeCargoCompanyAsync_CallsPutWithCorrectPath()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/CargoCompany")),
                It.IsAny<CiceksepetiChangeCargoRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiChangeCargoRequest(
            Items: new List<CiceksepetiChangeCargoItem>
            {
                new(OrderItemId: 3001L, CargoCompany: "MNG", TrackingNumber: "MNG-999")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ChangeCargoCompanyAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/CargoCompany")),
                It.IsAny<CiceksepetiChangeCargoRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 6 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendCargoMeasurementAsync_CallsPostWithCorrectPath()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/CargoMeasurement")),
                It.IsAny<CiceksepetiCargoMeasurementRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiCargoMeasurementRequest(
            Items: new List<CiceksepetiCargoMeasurementItem>
            {
                new(OrderItemId: 4001L, Desi: 2.5m, Weight: 1.2m)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SendCargoMeasurementAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/CargoMeasurement")),
                It.IsAny<CiceksepetiCargoMeasurementRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 7 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLaborCostAsync_CallsPutWithCorrectPath()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/UpdateLaborCost")),
                It.IsAny<CiceksepetiLaborCostRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiLaborCostRequest(
            Items: new List<CiceksepetiLaborCostItem>
            {
                new(OrderItemId: 5001L, LaborCost: 15.00m)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateLaborCostAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.PutAsync(
                It.Is<string>(u => u.Contains("Order/UpdateLaborCost")),
                It.IsAny<CiceksepetiLaborCostRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 8 — Error handling ───────────────────────────────────────────────

    [Fact]
    public async Task ReadyForCargoWithCsAsync_ApiError_ReturnsErrorResult()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PutAsync(
                It.IsAny<string>(),
                It.IsAny<CiceksepetiCsCargoRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorResponse());

        var request = new CiceksepetiCsCargoRequest(
            CargoGroups: new List<CiceksepetiCargoGroup>
            {
                new(OrderItemIds: new List<long> { 9999L })
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ReadyForCargoWithCsAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }
}
