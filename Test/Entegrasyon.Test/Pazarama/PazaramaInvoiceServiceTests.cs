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
/// PazaramaInvoiceService ve MockPazaramaInvoiceService birim testleri.
/// Fatura linki yükleme işlemlerinin doğru endpoint'lere gönderildiğini doğrular.
/// </summary>
public class PazaramaInvoiceServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HttpResponseMessage BuildSuccessResponse()
    {
        var body = JsonSerializer.Serialize(new { data = (object?)null, success = true });
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

    // -----------------------------------------------------------------------
    // Real service — UploadInvoiceLinkAsync
    // -----------------------------------------------------------------------

    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaInvoiceService>> _loggerMock = new();

    private PazaramaInvoiceService CreateSut() => new(_apiClientMock.Object, _loggerMock.Object);

    [Fact]
    public async Task UploadInvoiceLinkAsync_WhenApiSucceeds_ShouldReturnSuccess()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/invoice-link", It.IsAny<PazaramaInvoiceLinkRequest>()))
            .ReturnsAsync(BuildSuccessResponse());

        var sut = CreateSut();
        var request = new PazaramaInvoiceLinkRequest("https://fatura.pdf", "ORD-123");

        var result = await sut.UploadInvoiceLinkAsync(request);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UploadInvoiceLinkAsync_ShouldCallPostWithCorrectEndpoint()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/invoice-link", It.IsAny<PazaramaInvoiceLinkRequest>()))
            .ReturnsAsync(BuildSuccessResponse());

        var sut = CreateSut();
        var request = new PazaramaInvoiceLinkRequest("https://fatura.pdf", "ORD-123", DeliveryCompanyId: "DC-1", TrackingNumber: "TRK-1");

        await sut.UploadInvoiceLinkAsync(request);

        _apiClientMock.Verify(a => a.PostAsync("order/invoice-link", It.Is<PazaramaInvoiceLinkRequest>(
            r => r.InvoiceLink == "https://fatura.pdf"
              && r.OrderId == "ORD-123"
              && r.DeliveryCompanyId == "DC-1"
              && r.TrackingNumber == "TRK-1")), Times.Once);
    }

    [Fact]
    public async Task UploadInvoiceLinkAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/invoice-link", It.IsAny<PazaramaInvoiceLinkRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.InternalServerError));

        var sut = CreateSut();
        var request = new PazaramaInvoiceLinkRequest("https://fatura.pdf", "ORD-123");

        var result = await sut.UploadInvoiceLinkAsync(request);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UploadInvoiceLinkAsync_WhenApiReturnsSuccessFalse_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/invoice-link", It.IsAny<PazaramaInvoiceLinkRequest>()))
            .ReturnsAsync(BuildApiFailureResponse("Fatura yüklenemedi"));

        var sut = CreateSut();
        var request = new PazaramaInvoiceLinkRequest("https://fatura.pdf", "ORD-123");

        var result = await sut.UploadInvoiceLinkAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Fatura yüklenemedi");
    }

    // -----------------------------------------------------------------------
    // Real service — UploadMultipleInvoiceLinkAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UploadMultipleInvoiceLinkAsync_WhenApiSucceeds_ShouldReturnSuccess()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/multiple-invoice-link", It.IsAny<PazaramaMultipleInvoiceLinkRequest>()))
            .ReturnsAsync(BuildSuccessResponse());

        var sut = CreateSut();
        var request = new PazaramaMultipleInvoiceLinkRequest(
            OrderId: "ORD-456",
            InvoiceLink: "https://fatura2.pdf",
            OrderItemIds: new List<string> { "ITEM-1", "ITEM-2" });

        var result = await sut.UploadMultipleInvoiceLinkAsync(request);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UploadMultipleInvoiceLinkAsync_ShouldCallCorrectEndpoint()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/multiple-invoice-link", It.IsAny<PazaramaMultipleInvoiceLinkRequest>()))
            .ReturnsAsync(BuildSuccessResponse());

        var sut = CreateSut();
        var request = new PazaramaMultipleInvoiceLinkRequest(
            OrderId: "ORD-789",
            InvoiceLink: "https://fatura3.pdf",
            DeliveryCompanyId: "DC-2",
            TrackingNumber: "TRK-2",
            OrderItemIds: new List<string> { "ITEM-A" });

        await sut.UploadMultipleInvoiceLinkAsync(request);

        _apiClientMock.Verify(a => a.PostAsync("order/multiple-invoice-link", It.Is<PazaramaMultipleInvoiceLinkRequest>(
            r => r.OrderId == "ORD-789" && r.OrderItemIds.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task UploadMultipleInvoiceLinkAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("order/multiple-invoice-link", It.IsAny<PazaramaMultipleInvoiceLinkRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse());

        var sut = CreateSut();
        var request = new PazaramaMultipleInvoiceLinkRequest(
            OrderId: "ORD-456",
            InvoiceLink: "https://fatura.pdf",
            OrderItemIds: new List<string> { "ITEM-1" });

        var result = await sut.UploadMultipleInvoiceLinkAsync(request);

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Mock service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<ILogger<MockPazaramaInvoiceService>> _mockLoggerMock = new();

    private MockPazaramaInvoiceService CreateMockSut() => new(_mockLoggerMock.Object);

    [Fact]
    public async Task MockService_UploadInvoiceLinkAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var request = new PazaramaInvoiceLinkRequest("https://fatura.pdf", "ORD-123");

        var result = await sut.UploadInvoiceLinkAsync(request);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MockService_UploadMultipleInvoiceLinkAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var request = new PazaramaMultipleInvoiceLinkRequest(
            OrderId: "ORD-456",
            InvoiceLink: "https://fatura.pdf",
            OrderItemIds: new List<string> { "ITEM-1" });

        var result = await sut.UploadMultipleInvoiceLinkAsync(request);

        result.Success.Should().BeTrue();
    }
}
