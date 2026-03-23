using System.Net;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Kargo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Kargo;

/// <summary>
/// ArasKargoService unit testleri.
/// </summary>
public class ArasKargoServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ArasKargoClient> _mockClient;
    private readonly Mock<ILogger<ArasKargoService>> _mockLogger = new();

    public ArasKargoServiceTests()
    {
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockClientLogger = new Mock<ILogger<ArasKargoClient>>();

        _mockClient = new Mock<ArasKargoClient>(
            mockHttpClientFactory.Object,
            mockConfiguration.Object,
            mockClientLogger.Object);
    }

    private ArasKargoService CreateSut() => new(
        _mockClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    // ── CreateShipmentAsync Tests ───────────────────────────────────────────

    [Fact]
    public async Task CreateShipmentAsync_ValidRequest_ReturnsSuccess()
    {
        var request = CreateValidShipmentRequest();
        var expected = new ArasKargoOrderResult("0", "Basarili", "BRK-123456");

        _mockClient
            .Setup(x => x.SetOrderAsync(It.IsAny<ArasKargoShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ResultCode.Should().Be("0");
        result.Data.BarcodeNumber.Should().Be("BRK-123456");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyIntegrationCode_ReturnsError()
    {
        var request = CreateValidShipmentRequest() with { IntegrationCode = "" };

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("IntegrationCode");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyReceiverName_ReturnsError()
    {
        var request = CreateValidShipmentRequest() with { ReceiverName = "" };

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("alici");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyReceiverPhone_ReturnsError()
    {
        var request = CreateValidShipmentRequest() with { ReceiverPhone = "" };

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("telefon");
    }

    [Fact]
    public async Task CreateShipmentAsync_EmptyReceiverAddress_ReturnsError()
    {
        var request = CreateValidShipmentRequest() with { ReceiverAddress = "" };

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("adres");
    }

    [Fact]
    public async Task CreateShipmentAsync_ZeroPieceCount_ReturnsError()
    {
        var request = CreateValidShipmentRequest() with { PieceCount = 0 };

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("parca");
    }

    [Fact]
    public async Task CreateShipmentAsync_ClientThrows_ReturnsError()
    {
        var request = CreateValidShipmentRequest();

        _mockClient
            .Setup(x => x.SetOrderAsync(It.IsAny<ArasKargoShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var sut = CreateSut();
        var result = await sut.CreateShipmentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Hata");
    }

    // ── CancelShipmentAsync Tests ───────────────────────────────────────────

    [Fact]
    public async Task CancelShipmentAsync_ValidCode_ReturnsSuccess()
    {
        _mockClient
            .Setup(x => x.CancelDispatchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("INT-001");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelShipmentAsync_EmptyCode_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("integrationCode");
    }

    [Fact]
    public async Task CancelShipmentAsync_ClientReturnsFalse_ReturnsError()
    {
        _mockClient
            .Setup(x => x.CancelDispatchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("INT-001");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CancelShipmentAsync_ClientThrows_ReturnsError()
    {
        _mockClient
            .Setup(x => x.CancelDispatchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var sut = CreateSut();
        var result = await sut.CancelShipmentAsync("INT-001");

        result.Success.Should().BeFalse();
    }

    // ── TrackShipmentAsync Tests ────────────────────────────────────────────

    [Fact]
    public async Task TrackShipmentAsync_ValidCode_ReturnsResult()
    {
        var tracking = new ArasKargoTrackingResult("INT-001", "Teslim Edildi", "Ali Veli", "2026-03-23", "BRK-123");

        _mockClient
            .Setup(x => x.GetQueryJsonAsync<ArasKargoTrackingResult>(
                ArasKargoQueryType.CargoInformation, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracking);

        var sut = CreateSut();
        var result = await sut.TrackShipmentAsync("INT-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be("Teslim Edildi");
    }

    [Fact]
    public async Task TrackShipmentAsync_EmptyCode_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.TrackShipmentAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("integrationCode");
    }

    // ── GetShipmentMovementsAsync Tests ─────────────────────────────────────

    [Fact]
    public async Task GetShipmentMovementsAsync_ValidCode_ReturnsList()
    {
        var movements = new List<ArasKargoMovement>
        {
            new("2026-03-23", "Kabul", "Istanbul", "Kargo kabul edildi"),
            new("2026-03-23", "Aktarma", "Ankara", "Transfer merkezinde")
        };

        _mockClient
            .Setup(x => x.GetQueryJsonAsync<List<ArasKargoMovement>>(
                ArasKargoQueryType.CargoMovementInformation, It.IsAny<string?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements);

        var sut = CreateSut();
        var result = await sut.GetShipmentMovementsAsync("INT-001");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShipmentMovementsAsync_EmptyCode_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetShipmentMovementsAsync("");

        result.Success.Should().BeFalse();
    }

    // ── GetShipmentsByDateRangeAsync Tests ──────────────────────────────────

    [Fact]
    public async Task GetShipmentsByDateRangeAsync_ValidDates_ReturnsList()
    {
        var shipments = new List<ArasKargoShipmentSummary>
        {
            new("INT-001", "Teslim Edildi", "Ali Veli", "2026-03-23")
        };

        _mockClient
            .Setup(x => x.GetQueryJsonAsync<List<ArasKargoShipmentSummary>>(
                ArasKargoQueryType.CargoWaybillBetweenDate, null, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipments);

        var sut = CreateSut();
        var result = await sut.GetShipmentsByDateRangeAsync(
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 23));

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetShipmentsByDateRangeAsync_StartAfterEnd_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetShipmentsByDateRangeAsync(
            new DateTime(2026, 3, 23), new DateTime(2026, 3, 1));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("tarih");
    }

    // ── GetUndeliveredShipmentsAsync Tests ──────────────────────────────────

    [Fact]
    public async Task GetUndeliveredShipmentsAsync_ReturnsSuccess()
    {
        var shipments = new List<ArasKargoShipmentSummary>
        {
            new("INT-002", "Dagitimda", "Ayse Fatma", "2026-03-22")
        };

        _mockClient
            .Setup(x => x.GetQueryJsonAsync<List<ArasKargoShipmentSummary>>(
                ArasKargoQueryType.CargoUndelivered, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipments);

        var sut = CreateSut();
        var result = await sut.GetUndeliveredShipmentsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUndeliveredShipmentsAsync_ClientThrows_ReturnsError()
    {
        _mockClient
            .Setup(x => x.GetQueryJsonAsync<List<ArasKargoShipmentSummary>>(
                ArasKargoQueryType.CargoUndelivered, null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var sut = CreateSut();
        var result = await sut.GetUndeliveredShipmentsAsync();

        result.Success.Should().BeFalse();
    }

    // ── MockArasKargoService Tests ──────────────────────────────────────────

    [Fact]
    public async Task MockService_CreateShipment_ReturnsSuccess()
    {
        var mockLogger = new Mock<ILogger<MockArasKargoService>>();
        var mockService = new MockArasKargoService(mockLogger.Object);

        var request = CreateValidShipmentRequest();
        var result = await mockService.CreateShipmentAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ResultCode.Should().Be("0");
    }

    [Fact]
    public async Task MockService_CancelShipment_ReturnsSuccess()
    {
        var mockLogger = new Mock<ILogger<MockArasKargoService>>();
        var mockService = new MockArasKargoService(mockLogger.Object);

        var result = await mockService.CancelShipmentAsync("INT-001");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MockService_TrackShipment_ReturnsSuccess()
    {
        var mockLogger = new Mock<ILogger<MockArasKargoService>>();
        var mockService = new MockArasKargoService(mockLogger.Object);

        var result = await mockService.TrackShipmentAsync("INT-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ArasKargoShipmentRequest CreateValidShipmentRequest() => new(
        IntegrationCode: "INT-001",
        ReceiverName: "Ali Veli",
        ReceiverPhone: "05551234567",
        ReceiverCityName: "Istanbul",
        ReceiverTownName: "Kadikoy",
        ReceiverAddress: "Test Mahallesi Test Sokak No:1",
        PieceCount: 1,
        Description: "Test gonderisi");
}
