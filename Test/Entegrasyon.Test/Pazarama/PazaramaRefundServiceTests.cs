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
/// PazaramaRefundService ve MockPazaramaRefundService için birim testleri.
/// İade ve iptal işlemlerinin doğru endpoint'lere gönderildiğini doğrular.
/// </summary>
public class PazaramaRefundServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HttpResponseMessage BuildRefundListResponse(List<PazaramaRefundDto>? refunds = null)
    {
        refunds ??= new List<PazaramaRefundDto>();
        var body = JsonSerializer.Serialize(new
        {
            data = new
            {
                responsePage = new { pageSize = 100, pageIndex = 1, totalCount = refunds.Count, totalPages = 1 },
                pageReport = new { totalRefundCount = refunds.Count, totalWaitingRefundCount = 0, totalApprovedRefundCount = 0, totalRejectedRefundCount = 0 },
                refundList = refunds
            },
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

    private static PazaramaRefundDto MakeSampleRefund(string refundId = "REF-001", long refundNumber = 987654321L) =>
        new(
            Id: 1,
            RefundId: refundId,
            OrderNumber: 12345678L,
            OrderDate: "2024-01-15",
            RefundNumber: refundNumber,
            RefundType: "FullRefund",
            RefundStatus: 1,
            RefundStatusName: "Beklemede",
            PaymentType: "Kredi Kartı",
            RefundDate: "2024-01-20",
            TotalAmount: null,
            RefundAmount: null,
            CustomerId: "CUST-001",
            CustomerName: "Test Müşteri",
            CustomerEmail: "test@example.com",
            CustomerPhoneNumber: "5551234567",
            CustomerAddress: "Test Adres",
            ProductName: "Test Ürün",
            ProductCode: "PRD-001",
            ProductStockCode: "STK-001",
            ShipmentCompanyName: "Test Kargo",
            ShipmentCode: null,
            Description: "İade talebi",
            BoDescription: null,
            Quantity: 1);

    // -----------------------------------------------------------------------
    // Real service — GetRefundsAsync
    // -----------------------------------------------------------------------

    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaRefundService>> _loggerMock = new();

    private PazaramaRefundService CreateSut() => new(_apiClientMock.Object, _loggerMock.Object);

    [Fact]
    public async Task GetRefundsAsync_WhenApiSucceeds_ShouldReturnRefundList()
    {
        var refunds = new List<PazaramaRefundDto>
        {
            MakeSampleRefund("REF-001", 111111L),
            MakeSampleRefund("REF-002", 222222L)
        };

        _apiClientMock
            .Setup(a => a.PostAsync("order/getRefund", It.IsAny<PazaramaRefundFetchRequest>()))
            .ReturnsAsync(BuildRefundListResponse(refunds));

        var sut = CreateSut();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetRefundsAsync(startDate, endDate);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RefundList.Should().HaveCount(2);
        result.Data.RefundList![0].RefundId.Should().Be("REF-001");
        result.Data.RefundList![1].RefundId.Should().Be("REF-002");
    }

    [Fact]
    public async Task GetRefundsAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/getRefund", It.IsAny<PazaramaRefundFetchRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.InternalServerError));

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetRefundsAsync(start, end);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetRefundsAsync_WhenApiReturnsSuccessFalse_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/getRefund", It.IsAny<PazaramaRefundFetchRequest>()))
            .ReturnsAsync(BuildApiFailureResponse("İadeler alınamadı"));

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetRefundsAsync(start, end);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("İadeler alınamadı");
    }

    [Fact]
    public async Task GetRefundsAsync_ShouldSendCorrectRequestParameters()
    {
        PazaramaRefundFetchRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/getRefund", It.IsAny<PazaramaRefundFetchRequest>()))
            .Callback<string, PazaramaRefundFetchRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildRefundListResponse());

        var sut = CreateSut();
        var startDate = new DateTimeOffset(2024, 3, 5, 10, 30, 0, TimeSpan.FromHours(3));
        var endDate = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.FromHours(3));

        await sut.GetRefundsAsync(startDate, endDate, refundStatus: 1, pageSize: 50, pageNumber: 2);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestStartDate.Should().Be("2024-03-05");
        capturedRequest.RequestEndDate.Should().Be("2024-12-31");
        capturedRequest.RefundStatus.Should().Be(1);
        capturedRequest.PageSize.Should().Be(50);
        capturedRequest.PageNumber.Should().Be(2);
    }

    // -----------------------------------------------------------------------
    // Real service — UpdateRefundAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateRefundAsync_WhenApiSucceeds_ShouldReturnSuccess()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/updateRefund", It.IsAny<PazaramaRefundUpdateRequest>()))
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();

        var result = await sut.UpdateRefundAsync("REF-001", status: 2);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRefundAsync_WithRejectType_ShouldIncludeRefundRejectType()
    {
        PazaramaRefundUpdateRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/updateRefund", It.IsAny<PazaramaRefundUpdateRequest>()))
            .Callback<string, PazaramaRefundUpdateRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();

        await sut.UpdateRefundAsync("REF-XYZ", status: 3, refundRejectType: 2);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RefundId.Should().Be("REF-XYZ");
        capturedRequest.Status.Should().Be(3);
        capturedRequest.RefundRejectType.Should().Be(2);
    }

    [Fact]
    public async Task UpdateRefundAsync_ShouldCallCorrectEndpoint()
    {
        PazaramaRefundUpdateRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/updateRefund", It.IsAny<PazaramaRefundUpdateRequest>()))
            .Callback<string, PazaramaRefundUpdateRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();

        await sut.UpdateRefundAsync("REF-001", status: 2);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RefundId.Should().Be("REF-001");
        capturedRequest.Status.Should().Be(2);
        capturedRequest.RefundRejectType.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRefundAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/updateRefund", It.IsAny<PazaramaRefundUpdateRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.BadRequest));

        var sut = CreateSut();

        var result = await sut.UpdateRefundAsync("REF-001", status: 2);

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Real service — GetCancellationsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCancellationsAsync_ShouldCallCorrectEndpoint()
    {
        PazaramaRefundFetchRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/api/cancel/items", It.IsAny<PazaramaRefundFetchRequest>()))
            .Callback<string, PazaramaRefundFetchRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildRefundListResponse());

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetCancellationsAsync(start, end);

        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestStartDate.Should().Be("2024-01-01");
        capturedRequest.RequestEndDate.Should().Be("2024-01-31");
    }

    [Fact]
    public async Task GetCancellationsAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/api/cancel/items", It.IsAny<PazaramaRefundFetchRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.ServiceUnavailable));

        var sut = CreateSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetCancellationsAsync(start, end);

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Real service — UpdateCancellationAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateCancellationAsync_ShouldCallPutWithCorrectBody()
    {
        PazaramaCancelUpdateRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PutAsync("order/api/cancel", It.IsAny<PazaramaCancelUpdateRequest>()))
            .Callback<string, PazaramaCancelUpdateRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessUpdateResponse());

        var sut = CreateSut();

        var result = await sut.UpdateCancellationAsync("CAN-001", status: 2);

        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RefundId.Should().Be("CAN-001");
        capturedRequest.Status.Should().Be(2);
    }

    [Fact]
    public async Task UpdateCancellationAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("order/api/cancel", It.IsAny<PazaramaCancelUpdateRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.BadRequest));

        var sut = CreateSut();

        var result = await sut.UpdateCancellationAsync("CAN-001", status: 2);

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Mock service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<ILogger<MockPazaramaRefundService>> _mockLoggerMock = new();

    private MockPazaramaRefundService CreateMockSut() => new(_mockLoggerMock.Object);

    [Fact]
    public async Task MockService_GetRefundsAsync_ShouldReturnEmptySuccessResult()
    {
        var sut = CreateMockSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetRefundsAsync(start, end);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RefundList.Should().NotBeNull();
        result.Data.RefundList.Should().BeEmpty();
    }

    [Fact]
    public async Task MockService_UpdateRefundAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();

        var result = await sut.UpdateRefundAsync("REF-001", status: 2);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MockService_GetCancellationsAsync_ShouldReturnEmptySuccessResult()
    {
        var sut = CreateMockSut();
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetCancellationsAsync(start, end);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RefundList.Should().NotBeNull();
        result.Data.RefundList.Should().BeEmpty();
    }

    [Fact]
    public async Task MockService_UpdateCancellationAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();

        var result = await sut.UpdateCancellationAsync("CAN-001", status: 2);

        result.Success.Should().BeTrue();
    }
}
