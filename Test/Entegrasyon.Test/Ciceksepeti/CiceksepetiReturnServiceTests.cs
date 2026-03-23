using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiReturnService unit testleri.
/// </summary>
public class CiceksepetiReturnServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICiceksepetiApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<CiceksepetiReturnService>> _mockLogger = new();

    private CiceksepetiReturnService CreateSut() => new(
        _mockApiClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateReturnListResponse(CiceksepetiReturnListResponse payload, HttpStatusCode status = HttpStatusCode.OK)
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
    public async Task GetReturnOrdersAsync_ReturnsParsedResponse()
    {
        // Arrange
        var returnItem = new CiceksepetiReturnItemDto(
            OrderId: 1001L,
            OrderItemId: 2001L,
            OrderItemStatusId: 5,
            CustomerName: "Ayşe Kaya",
            SalesPrice: 79.90m,
            CancelReason: "Ürün beklentileri karşılamadı",
            CancelStatusId: 1,
            CargoCompany: "PTT",
            CargoTrackingNumber: "PTT-12345",
            ProductName: "Test Çiçek",
            StockCode: "FLOWER-001");

        var apiResponse = new CiceksepetiReturnListResponse(
            OrderItemList: new List<CiceksepetiReturnItemDto> { returnItem });

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/getcanceledorders")),
                It.IsAny<CiceksepetiGetReturnsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReturnListResponse(apiResponse));

        var request = new CiceksepetiGetReturnsRequest(
            StartDate: "2024-01-01",
            EndDate: "2024-01-31",
            PageSize: 20,
            Page: 0,
            StatusId: null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetReturnOrdersAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.OrderItemList.Should().HaveCount(1);
        result.Data.OrderItemList[0].OrderId.Should().Be(1001L);
        result.Data.OrderItemList[0].CustomerName.Should().Be("Ayşe Kaya");
        result.Data.OrderItemList[0].ProductName.Should().Be("Test Çiçek");
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmReturnReceivedAsync_CallsCorrectEndpoint()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/refundprocessstartreceivedprocess")),
                It.IsAny<CiceksepetiReturnReceivedRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiReturnReceivedRequest(
            OrderItemIds: new List<int> { 2001, 2002 });

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmReturnReceivedAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/refundprocessstartreceivedprocess")),
                It.IsAny<CiceksepetiReturnReceivedRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateReturnAsync_ApproveProcess1()
    {
        // Arrange — Process=1 means approval
        CiceksepetiReturnEvaluationRequest? capturedRequest = null;

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("Order/cancelevaluation")),
                It.IsAny<CiceksepetiReturnEvaluationRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, CiceksepetiReturnEvaluationRequest, CancellationToken>((_, req, _) => capturedRequest = req)
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiReturnEvaluationRequest(
            OrderItemId: 3001,
            Process: 1); // 1 = approve

        var sut = CreateSut();

        // Act
        var result = await sut.EvaluateReturnAsync(request);

        // Assert — process=1 (approve) must be forwarded as-is
        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Process.Should().Be(1);
        capturedRequest.OrderItemId.Should().Be(3001);
    }
}
