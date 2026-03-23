using System.Net;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

/// <summary>
/// PazaramaOrderService ve MockPazaramaOrderService için birim testleri.
/// Sipariş çekme ve durum güncelleme işlemlerinin doğru endpoint'lere gönderildiğini doğrular.
/// </summary>
public class PazaramaOrderServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HttpResponseMessage BuildSuccessOrdersResponse(List<PazaramaOrderDto>? orders = null)
    {
        orders ??= new List<PazaramaOrderDto>();
        var body = JsonSerializer.Serialize(new
        {
            data = orders,
            success = true
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildSuccessUpdateResponse()
    {
        var body = JsonSerializer.Serialize(new
        {
            data = (object?)null,
            success = true,
            message = "İşlem başarılı"
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildErrorHttpResponse(HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent("{\"success\":false,\"message\":\"hata\"}", Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildApiFailureResponse(string message = "İşlem başarısız")
    {
        var body = JsonSerializer.Serialize(new
        {
            data = (object?)null,
            success = false,
            message
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static PazaramaOrderDto MakeSampleOrder(long orderNumber = 12345678L) =>
        new(
            OrderId: "ORD-" + orderNumber,
            OrderNumber: orderNumber,
            OrderDate: "2024-01-15",
            OrderAmount: 299.99m,
            ShipmentAmount: 0m,
            DiscountAmount: 0m,
            DiscountDescription: null,
            Currency: "TRY",
            PaymentType: 1,
            OrderStatus: 1,
            CustomerId: "CUST-001",
            CustomerName: "Test Müşteri",
            CustomerEmail: "test@example.com",
            ShipmentAddress: null,
            BillingAddress: null,
            Items: null);

    // -----------------------------------------------------------------------
    // Real service — FetchOrdersAsync
    // -----------------------------------------------------------------------

    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaOrderService>> _loggerMock = new();

    private PazaramaOrderService CreateSut() => new(_apiClientMock.Object, _loggerMock.Object);

    [Fact]
    public async Task FetchOrdersAsync_WhenApiSucceeds_ShouldReturnOrderList()
    {
        var orders = new List<PazaramaOrderDto>
        {
            MakeSampleOrder(11111111L),
            MakeSampleOrder(22222222L)
        };

        _apiClientMock
            .Setup(a => a.PostAsync("order/getOrdersForApi", It.IsAny<PazaramaOrderFetchRequest>()))
            .ReturnsAsync(BuildSuccessOrdersResponse(orders));

        var sut = CreateSut();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.FetchOrdersAsync(startDate, endDate);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].OrderNumber.Should().Be(11111111L);
        result.Data![1].OrderNumber.Should().Be(22222222L);
    }

    [Fact]
    public async Task FetchOrdersAsync_ShouldFormatDatesAsYyyyMmDd()
    {
        PazaramaOrderFetchRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/getOrdersForApi", It.IsAny<PazaramaOrderFetchRequest>()))
            .Callback<string, PazaramaOrderFetchRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessOrdersResponse());

        var sut = CreateSut();
        var startDate = new DateTimeOffset(2024, 3, 5, 10, 30, 0, TimeSpan.FromHours(3));
        var endDate = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.FromHours(3));

        await sut.FetchOrdersAsync(startDate, endDate);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.StartDate.Should().Be("2024-03-05");
        capturedRequest.EndDate.Should().Be("2024-12-31");
    }

    [Fact]
    public async Task FetchOrdersAsync_ShouldUseCorrectPageSizeAndPageNumber()
    {
        PazaramaOrderFetchRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/getOrdersForApi", It.IsAny<PazaramaOrderFetchRequest>()))
            .Callback<string, PazaramaOrderFetchRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessOrdersResponse());

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        await sut.FetchOrdersAsync(start, end, pageSize: 100, pageNumber: 3);

        capturedRequest!.PageSize.Should().Be(100);
        capturedRequest.PageNumber.Should().Be(3);
    }

    [Fact]
    public async Task FetchOrdersAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/getOrdersForApi", It.IsAny<PazaramaOrderFetchRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.InternalServerError));

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.FetchOrdersAsync(start, end);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task FetchOrdersAsync_WhenApiReturnsSuccessFalse_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/getOrdersForApi", It.IsAny<PazaramaOrderFetchRequest>()))
            .ReturnsAsync(BuildApiFailureResponse("Siparişler alınamadı"));

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.FetchOrdersAsync(start, end);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Siparişler alınamadı");
    }

    [Fact]
    public async Task FetchOrdersAsync_WhenApiReturnsEmptyList_ShouldReturnEmptySuccess()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/getOrdersForApi", It.IsAny<PazaramaOrderFetchRequest>()))
            .ReturnsAsync(BuildSuccessOrdersResponse(new List<PazaramaOrderDto>()));

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.FetchOrdersAsync(start, end);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Real service — UpdateOrderItemStatusAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateOrderItemStatusAsync_WhenApiSucceeds_ShouldReturnSuccess()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("order/updateOrderStatus", It.IsAny<PazaramaOrderStatusUpdateRequest>()))
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();
        var item = new PazaramaOrderItemUpdate("ITEM-001", 3);

        var result = await sut.UpdateOrderItemStatusAsync(12345678L, item);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOrderItemStatusAsync_ShouldCallPutWithCorrectBody()
    {
        PazaramaOrderStatusUpdateRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PutAsync("order/updateOrderStatus", It.IsAny<PazaramaOrderStatusUpdateRequest>()))
            .Callback<string, PazaramaOrderStatusUpdateRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();
        var item = new PazaramaOrderItemUpdate("ITEM-XYZ", 5, ShippingTrackingNumber: "TRK-999");

        await sut.UpdateOrderItemStatusAsync(99887766L, item);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.OrderNumber.Should().Be(99887766L);
        capturedRequest.Item.OrderItemId.Should().Be("ITEM-XYZ");
        capturedRequest.Item.Status.Should().Be(5);
        capturedRequest.Item.ShippingTrackingNumber.Should().Be("TRK-999");
    }

    [Fact]
    public async Task UpdateOrderItemStatusAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("order/updateOrderStatus", It.IsAny<PazaramaOrderStatusUpdateRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.BadRequest));

        var sut = CreateSut();
        var item = new PazaramaOrderItemUpdate("ITEM-001", 3);

        var result = await sut.UpdateOrderItemStatusAsync(12345678L, item);

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Real service — BulkUpdateOrderStatusAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BulkUpdateOrderStatusAsync_WhenApiSucceeds_ShouldReturnSuccess()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("order/updateOrderStatusList", It.IsAny<PazaramaBulkOrderStatusRequest>()))
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();

        var result = await sut.BulkUpdateOrderStatusAsync(12345678L, 4);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpdateOrderStatusAsync_ShouldCallPutWithCorrectBody()
    {
        PazaramaBulkOrderStatusRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PutAsync("order/updateOrderStatusList", It.IsAny<PazaramaBulkOrderStatusRequest>()))
            .Callback<string, PazaramaBulkOrderStatusRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();

        await sut.BulkUpdateOrderStatusAsync(55443322L, 7);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.OrderNumber.Should().Be(55443322L);
        capturedRequest.Status.Should().Be(7);
    }

    [Fact]
    public async Task BulkUpdateOrderStatusAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("order/updateOrderStatusList", It.IsAny<PazaramaBulkOrderStatusRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.ServiceUnavailable));

        var sut = CreateSut();

        var result = await sut.BulkUpdateOrderStatusAsync(12345678L, 4);

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Mock service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<ILogger<MockPazaramaOrderService>> _mockLoggerMock = new();

    private MockPazaramaOrderService CreateMockSut() => new(_mockLoggerMock.Object);

    [Fact]
    public async Task MockService_FetchOrdersAsync_ShouldReturnEmptySuccessResult()
    {
        var sut = CreateMockSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.FetchOrdersAsync(start, end);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task MockService_UpdateOrderItemStatusAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var item = new PazaramaOrderItemUpdate("ITEM-001", 3);

        var result = await sut.UpdateOrderItemStatusAsync(12345678L, item);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MockService_BulkUpdateOrderStatusAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();

        var result = await sut.BulkUpdateOrderStatusAsync(12345678L, 4);

        result.Success.Should().BeTrue();
    }
}
