using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Test.Hepsiburada;

/// <summary>
/// HepsiburadaOrderService unit testleri.
/// Sipariş listeleme, detay, paketleme, iptal, fatura islemleri test edilir.
/// </summary>
public class HepsiburadaOrderServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHepsiburadaApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<HepsiburadaOrderService>> _mockLogger = new();

    public HepsiburadaOrderServiceTests()
    {
        // merchantId icin marketplace kaydı
        var marketplace = new Entity.MarketPlace
        {
            Id = MarketPlaceConstants.HepsiburadaMarketPlaceId,
            SellerId = "test-merchant-id"
        };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<Entity.MarketPlace> { marketplace });
    }

    private HepsiburadaOrderService CreateSut() => new(
        mockContextFactory.Object,
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

    private static HttpResponseMessage ErrorResponse(HttpStatusCode status = HttpStatusCode.BadRequest) =>
        new(status) { Content = new StringContent("Error", System.Text.Encoding.UTF8, "application/json") };

    // ── Test 1: GetOrdersAsync — success ────────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_Success_ReturnsParsedOrders()
    {
        var orderDto = new HepsiburadaOrderDto(
            "ORD-001", "2024-01-05", "Delivered", 199.90m, null, null, null);

        var apiResponse = new HepsiburadaOrderListResponse(
            new List<HepsiburadaOrderDto> { orderDto }, 1, 1);

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("/orders/merchantid/"))))
            .ReturnsAsync(CreateJsonResponse(apiResponse));

        var sut = CreateSut();
        var result = await sut.GetOrdersAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].OrderNumber.Should().Be("ORD-001");
    }

    // ── Test 2: GetOrdersAsync — date params included in URL ────────────────

    [Fact]
    public async Task GetOrdersAsync_WithDates_BuildsCorrectUrl()
    {
        string? capturedUrl = null;

        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(new HepsiburadaOrderListResponse(null, 0, 0)));

        var sut = CreateSut();
        var beginDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        await sut.GetOrdersAsync(beginDate, endDate, offset: 10, limit: 25);

        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("limit=25");
        capturedUrl.Should().Contain("offset=10");
        capturedUrl.Should().Contain("begindate=2024-01-01");
        capturedUrl.Should().Contain("enddate=2024-01-31");
    }

    // ── Test 3: GetOrdersAsync — API error ──────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.GetOrdersAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Sipariş API hatası");
    }

    // ── Test 4: GetOrderAsync — success ─────────────────────────────────────

    [Fact]
    public async Task GetOrderAsync_Success_ReturnsOrder()
    {
        var order = new HepsiburadaOrderDto(
            "ORD-002", "2024-02-10", "Shipped", 350m, null, null, null);

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("ordernumber/ORD-002"))))
            .ReturnsAsync(CreateJsonResponse(order));

        var sut = CreateSut();
        var result = await sut.GetOrderAsync("ORD-002");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.OrderNumber.Should().Be("ORD-002");
    }

    // ── Test 5: GetOrderAsync — API error ───────────────────────────────────

    [Fact]
    public async Task GetOrderAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(ErrorResponse(HttpStatusCode.NotFound));

        var sut = CreateSut();
        var result = await sut.GetOrderAsync("NONEXISTENT");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Sipariş bulunamadı");
    }

    // ── Test 6: CreatePackageAsync — success ────────────────────────────────

    [Fact]
    public async Task CreatePackageAsync_Success_ReturnsPackageResponse()
    {
        var packageResponse = new HepsiburadaPackageResponse("PKG-001", "BARCODE-001");

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/packages/merchantid/")),
                It.IsAny<HepsiburadaPackageRequest>()))
            .ReturnsAsync(CreateJsonResponse(packageResponse));

        var request = new HepsiburadaPackageRequest(
            new List<HepsiburadaPackageLineItem>
            {
                new("LINE-001", 1, null)
            }, 1, 2.5m);

        var sut = CreateSut();
        var result = await sut.CreatePackageAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PackageNumber.Should().Be("PKG-001");
    }

    // ── Test 7: CreatePackageAsync — API error ──────────────────────────────

    [Fact]
    public async Task CreatePackageAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HepsiburadaPackageRequest>()))
            .ReturnsAsync(ErrorResponse());

        var request = new HepsiburadaPackageRequest(
            new List<HepsiburadaPackageLineItem>(), 1, 1m);

        var sut = CreateSut();
        var result = await sut.CreatePackageAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Paketleme hatası");
    }

    // ── Test 8: CancelLineItemAsync — success ──────────────────────────────

    [Fact]
    public async Task CancelLineItemAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("cancelbymerchant")),
                It.IsAny<HepsiburadaCancelRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });

        var sut = CreateSut();
        var result = await sut.CancelLineItemAsync("LINE-001", 1);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("iptal edildi");
    }

    // ── Test 9: CancelLineItemAsync — API error ────────────────────────────

    [Fact]
    public async Task CancelLineItemAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HepsiburadaCancelRequest>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.CancelLineItemAsync("LINE-999", 1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("İptal hatası");
    }

    // ── Test 10: AddInvoiceAsync — success ──────────────────────────────────

    [Fact]
    public async Task AddInvoiceAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/invoice")),
                It.IsAny<HepsiburadaInvoiceRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });

        var invoice = new HepsiburadaInvoiceRequest("INV-001", "2024-01-15", null);

        var sut = CreateSut();
        var result = await sut.AddInvoiceAsync("LINE-001", invoice);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Fatura eklendi");
    }

    // ── Test 11: AddInvoiceAsync — API error ────────────────────────────────

    [Fact]
    public async Task AddInvoiceAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HepsiburadaInvoiceRequest>()))
            .ReturnsAsync(ErrorResponse());

        var invoice = new HepsiburadaInvoiceRequest("INV-001", "2024-01-15", null);

        var sut = CreateSut();
        var result = await sut.AddInvoiceAsync("LINE-001", invoice);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Fatura ekleme hatası");
    }

    // ── Test 12: GetOrdersAsync — exception handling ────────────────────────

    [Fact]
    public async Task GetOrdersAsync_Exception_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection timeout"));

        var sut = CreateSut();
        var result = await sut.GetOrdersAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection timeout");
    }
}
