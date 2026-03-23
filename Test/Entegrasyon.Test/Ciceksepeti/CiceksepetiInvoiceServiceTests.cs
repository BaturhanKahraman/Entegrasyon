using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiInvoiceService unit testleri.
/// </summary>
public class CiceksepetiInvoiceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICiceksepetiApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<CiceksepetiInvoiceService>> _mockLogger = new();

    private CiceksepetiInvoiceService CreateSut() => new(
        _mockApiClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("", System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request", System.Text.Encoding.UTF8, "application/json") };

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendInvoiceAsync_UsesSendRawAsync_NotPostAsync()
    {
        // Arrange — SendRawAsync is set up; PostAsync is NOT set up (would throw if called)
        _mockApiClient
            .Setup(x => x.SendRawAsync(
                It.Is<string>(p => p.Contains("Branch/SendInvoiceMail")),
                HttpMethod.Post,
                It.IsAny<HttpContent?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiInvoiceRequest(
            Items: new List<CiceksepetiInvoiceItem>
            {
                new(OrderItemId: 1001, Document: null, DocumentUrl: "https://invoice.example.com/1001.pdf")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SendInvoiceAsync(request);

        // Assert — SendRawAsync must have been called (not PostAsync)
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.SendRawAsync(
                It.Is<string>(p => p.Contains("Branch/SendInvoiceMail")),
                HttpMethod.Post,
                It.IsAny<HttpContent?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockApiClient.Verify(
            x => x.PostAsync(
                It.IsAny<string>(),
                It.IsAny<CiceksepetiInvoiceRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendInvoiceAsync_SuccessResponse_ReturnsSuccess()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.SendRawAsync(
                It.IsAny<string>(),
                HttpMethod.Post,
                It.IsAny<HttpContent?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiInvoiceRequest(
            Items: new List<CiceksepetiInvoiceItem>
            {
                new(OrderItemId: 2001, Document: "base64encodeddata", DocumentUrl: null)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SendInvoiceAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendInvoiceAsync_ErrorResponse_ReturnsError()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.SendRawAsync(
                It.IsAny<string>(),
                HttpMethod.Post,
                It.IsAny<HttpContent?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorResponse());

        var request = new CiceksepetiInvoiceRequest(
            Items: new List<CiceksepetiInvoiceItem>
            {
                new(OrderItemId: 9999, Document: null, DocumentUrl: null)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SendInvoiceAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }
}
