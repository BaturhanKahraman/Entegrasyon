using System.Net;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolInvoiceService unit tests — verifies invoice link send/delete
/// and invoice file upload.
/// </summary>
public class TrendyolInvoiceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<TrendyolInvoiceService>> _loggerMock = new();

    private TrendyolInvoiceService CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlace(string? sellerId = "12345")
    {
        var mp = new MarketPlace { Id = TrendyolMarketPlaceId, Name = "Trendyol", SellerId = sellerId };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { mp });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SendInvoiceLinkAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendInvoiceLinkAsync_HappyPath_ReturnsSuccess()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        var result = await sut.SendInvoiceLinkAsync(1001, "https://invoices.example.com/inv-001.pdf");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Fatura");
    }

    [Fact]
    public async Task SendInvoiceLinkAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.SendInvoiceLinkAsync(1001, "https://invoices.example.com/inv.pdf");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SellerId");
    }

    [Fact]
    public async Task SendInvoiceLinkAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid request")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SendInvoiceLinkAsync(1001, "https://invoices.example.com/inv.pdf");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("hatasi");
    }

    [Fact]
    public async Task SendInvoiceLinkAsync_UsesCorrectUrlWithSellerId()
    {
        // Arrange
        SetupMarketPlace("99999");

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((url, _) => capturedUrl = url)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        await sut.SendInvoiceLinkAsync(1001, "https://example.com/inv.pdf");

        // Assert
        capturedUrl.Should().Contain("99999");
        capturedUrl.Should().Contain("seller-invoice-links");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DeleteInvoiceLinkAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteInvoiceLinkAsync_HappyPath_ReturnsSuccess()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteInvoiceLinkAsync(serviceSourceId: 5001, customerId: 2001);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("silindi");
    }

    [Fact]
    public async Task DeleteInvoiceLinkAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteInvoiceLinkAsync(5001, 2001);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteInvoiceLinkAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteInvoiceLinkAsync(5001, 2001);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteInvoiceLinkAsync_UsesCorrectUrlWithIds()
    {
        // Arrange
        SetupMarketPlace("12345");

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        await sut.DeleteInvoiceLinkAsync(serviceSourceId: 5001, customerId: 2001);

        // Assert
        capturedUrl.Should().Contain("5001");
        capturedUrl.Should().Contain("2001");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UploadInvoiceFileAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UploadInvoiceFileAsync_HappyPath_ReturnsSuccess()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        // Act
        var result = await sut.UploadInvoiceFileAsync(1001, stream, "application/pdf");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("yuklendi");
    }

    [Fact]
    public async Task UploadInvoiceFileAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await sut.UploadInvoiceFileAsync(1001, stream, "application/pdf");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UploadInvoiceFileAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid file")
            });

        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await sut.UploadInvoiceFileAsync(1001, stream, "application/pdf");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("hatasi");
    }
}
