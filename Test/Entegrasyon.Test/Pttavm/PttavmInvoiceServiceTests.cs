using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

/// <summary>
/// PttavmInvoiceService unit testleri.
/// </summary>
public class PttavmInvoiceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPttavmCatalogApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<PttavmInvoiceService>> _mockLogger = new();

    private PttavmInvoiceService CreateSut() => new(
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

    // ── SendInvoice Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task SendInvoiceAsync_EmptyOrderId_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.SendInvoiceAsync("", new List<int> { 1 }, null, null);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendInvoiceAsync_EmptyLineItems_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.SendInvoiceAsync("ORD-001", new List<int>(), null, null);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendInvoiceAsync_WithUrl_ReturnsSuccess()
    {
        var response = new PttavmInvoiceResult(true, null);

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("invoice")), It.IsAny<PttavmInvoiceRequest>()))
            .ReturnsAsync(CreateJsonResponse(response));

        var sut = CreateSut();
        var result = await sut.SendInvoiceAsync("ORD-001", new List<int> { 1, 2 }, "https://example.com/fatura.pdf", null);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendInvoiceAsync_WithBase64_ReturnsSuccess()
    {
        var response = new PttavmInvoiceResult(true, null);

        _mockApiClient
            .Setup(x => x.PostAsync(It.Is<string>(u => u.Contains("invoice")), It.IsAny<PttavmInvoiceRequest>()))
            .ReturnsAsync(CreateJsonResponse(response));

        var sut = CreateSut();
        var result = await sut.SendInvoiceAsync("ORD-001", new List<int> { 1 }, null, "base64encodeddata");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendInvoiceAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<PttavmInvoiceRequest>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.SendInvoiceAsync("ORD-001", new List<int> { 1 }, "https://url.pdf", null);

        result.Success.Should().BeFalse();
    }
}
