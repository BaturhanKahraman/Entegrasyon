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
/// PazaramaFinanceService ve MockPazaramaFinanceService birim testleri.
/// Finans/muhasebe sorgusunun doğru endpoint'e gönderildiğini doğrular.
/// </summary>
public class PazaramaFinanceServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HttpResponseMessage BuildSuccessFinanceResponse(PazaramaFinanceData? data = null)
    {
        data ??= new PazaramaFinanceData(
            TransactionList: new List<PazaramaFinanceTransaction>(),
            TotalAmount: 0m,
            TotalCommission: 0m,
            TotalAllowance: 0m);

        var body = JsonSerializer.Serialize(new { data, success = true });
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
        var body = JsonSerializer.Serialize(new { data = (object?)null, success = false, message });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static PazaramaFinanceTransaction MakeSampleTransaction(long orderId = 459916556L) =>
        new(
            OrderId: orderId,
            TrxCode: "ORDER-23003TArH07110322",
            TrxId: "cab6971e-946b-40ec-97ab-08daed6ab000",
            Amount: 100m,
            InstallmentNumber: 0,
            CommissionAmount: 5m,
            CouponDiscount: 0m,
            AllowanceAmount: 95m,
            Status: "Satış",
            TransactionDate: "2023-01-03T19:00:00",
            TransferredDate: "2023-02-03T00:00:00");

    // -----------------------------------------------------------------------
    // Real service — GetPaymentAgreementAsync
    // -----------------------------------------------------------------------

    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaFinanceService>> _loggerMock = new();

    private PazaramaFinanceService CreateSut() => new(_apiClientMock.Object, _loggerMock.Object);

    [Fact]
    public async Task GetPaymentAgreementAsync_WhenApiSucceeds_ShouldReturnFinanceData()
    {
        var transactions = new List<PazaramaFinanceTransaction> { MakeSampleTransaction() };
        var data = new PazaramaFinanceData(transactions, 100m, 5m, 95m);

        _apiClientMock
            .Setup(a => a.PostAsync("order/paymentAgreement", It.IsAny<PazaramaFinanceRequest>()))
            .ReturnsAsync(BuildSuccessFinanceResponse(data));

        var sut = CreateSut();
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2023, 1, 2, 23, 59, 59, TimeSpan.Zero);

        var result = await sut.GetPaymentAgreementAsync(start, end);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TransactionList.Should().HaveCount(1);
        result.Data.TotalAmount.Should().Be(100m);
        result.Data.TotalCommission.Should().Be(5m);
        result.Data.TotalAllowance.Should().Be(95m);
    }

    [Fact]
    public async Task GetPaymentAgreementAsync_ShouldFormatDatesCorrectly()
    {
        PazaramaFinanceRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/paymentAgreement", It.IsAny<PazaramaFinanceRequest>()))
            .Callback<string, PazaramaFinanceRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessFinanceResponse());

        var sut = CreateSut();
        var start = new DateTimeOffset(2023, 3, 5, 0, 0, 1, TimeSpan.FromHours(3));
        var end = new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.FromHours(3));

        await sut.GetPaymentAgreementAsync(start, end);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.StartDate.Should().Be("2023-03-05T00:00:01");
        capturedRequest.EndDate.Should().Be("2023-12-31T23:59:59");
    }

    [Fact]
    public async Task GetPaymentAgreementAsync_WithOrderId_ShouldIncludeOrderIdInRequest()
    {
        PazaramaFinanceRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/paymentAgreement", It.IsAny<PazaramaFinanceRequest>()))
            .Callback<string, PazaramaFinanceRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessFinanceResponse());

        var sut = CreateSut();
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2023, 1, 31, 0, 0, 0, TimeSpan.Zero);

        await sut.GetPaymentAgreementAsync(start, end, orderId: 459916556L);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.OrderId.Should().Be(459916556L);
    }

    [Fact]
    public async Task GetPaymentAgreementAsync_WithoutOrderId_ShouldSendNullOrderId()
    {
        PazaramaFinanceRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("order/paymentAgreement", It.IsAny<PazaramaFinanceRequest>()))
            .Callback<string, PazaramaFinanceRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessFinanceResponse());

        var sut = CreateSut();
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2023, 1, 31, 0, 0, 0, TimeSpan.Zero);

        await sut.GetPaymentAgreementAsync(start, end);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.OrderId.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentAgreementAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/paymentAgreement", It.IsAny<PazaramaFinanceRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.InternalServerError));

        var sut = CreateSut();
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2023, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetPaymentAgreementAsync(start, end);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaymentAgreementAsync_WhenApiReturnsSuccessFalse_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/paymentAgreement", It.IsAny<PazaramaFinanceRequest>()))
            .ReturnsAsync(BuildApiFailureResponse("Finans verisi alınamadı"));

        var sut = CreateSut();
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2023, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetPaymentAgreementAsync(start, end);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Finans verisi alınamadı");
    }

    [Fact]
    public async Task GetPaymentAgreementAsync_WhenEmptyTransactionList_ShouldReturnSuccessWithEmptyList()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/paymentAgreement", It.IsAny<PazaramaFinanceRequest>()))
            .ReturnsAsync(BuildSuccessFinanceResponse());

        var sut = CreateSut();
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2023, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetPaymentAgreementAsync(start, end);

        result.Success.Should().BeTrue();
        result.Data!.TransactionList.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Mock service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<ILogger<MockPazaramaFinanceService>> _mockLoggerMock = new();

    private MockPazaramaFinanceService CreateMockSut() => new(_mockLoggerMock.Object);

    [Fact]
    public async Task MockService_GetPaymentAgreementAsync_ShouldReturnEmptySuccessResult()
    {
        var sut = CreateMockSut();
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2023, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await sut.GetPaymentAgreementAsync(start, end);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TransactionList.Should().BeEmpty();
    }
}
