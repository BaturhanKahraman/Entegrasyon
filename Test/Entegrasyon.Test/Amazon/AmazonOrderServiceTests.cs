using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity.Dtos.Amazon;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonOrderService unit tests — GetOrders, GetOrder, GetOrderItems, ConfirmShipment.
/// </summary>
public class AmazonOrderServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IAmazonApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<AmazonOrderService>> _mockLogger = new();

    private AmazonOrderService CreateSut() => new(
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

    // ── GetOrdersAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_Success_ReturnsOrders()
    {
        // Arrange
        var orderListResponse = new AmazonOrderListResponse(
            new AmazonOrderListPayload(
                new List<AmazonOrderDto>
                {
                    new("111-222-333", "Unshipped", null, "A33AVAJ2PDY3EV", null, "MFN", null, 0, 1)
                },
                null));

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("orders")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(orderListResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrdersAsync(DateTimeOffset.UtcNow.AddHours(-1), new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].AmazonOrderId.Should().Be("111-222-333");
    }

    [Fact]
    public async Task GetOrdersAsync_WithOrderStatuses_AppendsToUrl()
    {
        // Arrange
        string? capturedUrl = null;
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(new AmazonOrderListResponse(
                new AmazonOrderListPayload(new List<AmazonOrderDto>(), null))));

        var sut = CreateSut();

        // Act
        await sut.GetOrdersAsync(DateTimeOffset.UtcNow, new[] { "A33AVAJ2PDY3EV" },
            orderStatuses: new[] { "Unshipped", "PartiallyShipped" });

        // Assert
        capturedUrl.Should().Contain("OrderStatuses=Unshipped,PartiallyShipped");
    }

    [Fact]
    public async Task GetOrdersAsync_EmptyPayload_ReturnsEmptyList()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(new AmazonOrderListResponse(null)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrdersAsync(DateTimeOffset.UtcNow, new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrdersAsync_ApiError_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("{}")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrdersAsync(DateTimeOffset.UtcNow, new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Orders API error");
    }

    [Fact]
    public async Task GetOrdersAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrdersAsync(DateTimeOffset.UtcNow, new[] { "A33AVAJ2PDY3EV" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("timeout");
    }

    // ── GetOrderAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderAsync_Success_ReturnsOrder()
    {
        // Arrange
        var order = new AmazonOrderDto("ORD-123", "Shipped", null, null, null, "MFN", null, 1, 0);
        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("ORD-123")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(order));

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrderAsync("ORD-123");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AmazonOrderId.Should().Be("ORD-123");
        result.Data.OrderStatus.Should().Be("Shipped");
    }

    [Fact]
    public async Task GetOrderAsync_NotFound_ReturnsError()
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
        var result = await sut.GetOrderAsync("INVALID");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Order not found");
    }

    // ── GetOrderItemsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderItemsAsync_Success_ReturnsItems()
    {
        // Arrange
        var itemsResponse = new AmazonOrderItemListResponse(
            new AmazonOrderItemPayload(
                new List<AmazonOrderItemDto>
                {
                    new("B00ASIN1", "SKU-1", "ITEM-1", "Test Product", 2, 0, null)
                }));

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("orderItems")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(itemsResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrderItemsAsync("ORD-123");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Asin.Should().Be("B00ASIN1");
        result.Data[0].QuantityOrdered.Should().Be(2);
    }

    [Fact]
    public async Task GetOrderItemsAsync_EmptyPayload_ReturnsEmptyList()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJsonResponse(new AmazonOrderItemListResponse(null)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrderItemsAsync("ORD-123");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // ── ConfirmShipmentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmShipmentAsync_Success_ReturnsSuccess()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("shipment/confirm")), It.IsAny<AmazonConfirmShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var request = new AmazonConfirmShipmentRequest(
            "A33AVAJ2PDY3EV",
            new AmazonPackageDetail("PKG-1", "YURTICI", "TR123456789", "2026-03-23",
                new List<AmazonShipmentItem> { new("ITEM-1", 1) }));

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmShipmentAsync("ORD-123", request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Kargo onaylandı");
    }

    [Fact]
    public async Task ConfirmShipmentAsync_ApiError_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<AmazonConfirmShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid shipment")
            });

        var request = new AmazonConfirmShipmentRequest(
            "A33AVAJ2PDY3EV",
            new AmazonPackageDetail("PKG-1", "YURTICI", "TR123", "2026-03-23",
                new List<AmazonShipmentItem>()));

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmShipmentAsync("ORD-123", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kargo onay hatası");
    }

    [Fact]
    public async Task ConfirmShipmentAsync_Exception_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<AmazonConfirmShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("network error"));

        var request = new AmazonConfirmShipmentRequest(
            "A33AVAJ2PDY3EV",
            new AmazonPackageDetail("PKG-1", "YURTICI", "TR123", "2026-03-23",
                new List<AmazonShipmentItem>()));

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmShipmentAsync("ORD-123", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("network error");
    }
}
