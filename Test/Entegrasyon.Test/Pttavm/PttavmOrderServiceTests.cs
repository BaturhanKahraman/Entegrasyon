using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

/// <summary>
/// PttavmOrderService unit testleri.
/// </summary>
public class PttavmOrderServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPttavmCatalogApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<PttavmOrderService>> _mockLogger = new();

    private PttavmOrderService CreateSut() => new(
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

    // ── SearchOrders Tests ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchOrdersAsync_ExceedsMaxDateRange_ReturnsError()
    {
        var sut = CreateSut();
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 3, 1); // > 40 days

        var result = await sut.SearchOrdersAsync(start, end, false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("40");
    }

    [Fact]
    public async Task SearchOrdersAsync_EndBeforeStart_ReturnsError()
    {
        var sut = CreateSut();
        var start = new DateTime(2026, 2, 1);
        var end = new DateTime(2026, 1, 1);

        var result = await sut.SearchOrdersAsync(start, end, false);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SearchOrdersAsync_SuccessfulResponse_ReturnsOrders()
    {
        var orders = new List<PttavmOrder>
        {
            new("ORD-001", "kargo_yapilmasi_bekleniyor", 100, 10, DateTime.UtcNow, null, "Ali", "Veli")
        };

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("orders/search"))))
            .ReturnsAsync(CreateJsonResponse(orders));

        var sut = CreateSut();
        var result = await sut.SearchOrdersAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, false);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
    }

    [Fact]
    public async Task SearchOrdersAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.SearchOrdersAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, false);

        result.Success.Should().BeFalse();
    }

    // ── GetOrderDetail Tests ────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderDetailAsync_EmptyOrderId_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.GetOrderDetailAsync("");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderDetailAsync_SuccessfulResponse_ReturnsDetail()
    {
        var detail = new PttavmOrderDetail("ORD-001", "tamamlandi", 100, 10, DateTime.UtcNow, null, "Ali", "Veli", "Adres1", "Adres2");

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("orders/ORD-001"))))
            .ReturnsAsync(CreateJsonResponse(detail));

        var sut = CreateSut();
        var result = await sut.GetOrderDetailAsync("ORD-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.SiparisNo.Should().Be("ORD-001");
    }

    // ── GetCargoInfos Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCargoInfosAsync_SuccessfulResponse_ReturnsList()
    {
        var infos = new List<PttavmCargoInfo>
        {
            new("P1", 1, "1", "REF1", "gondericisine_teslim_edildi", null)
        };

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("cargo-infos"))))
            .ReturnsAsync(CreateJsonResponse(infos));

        var sut = CreateSut();
        var result = await sut.GetCargoInfosAsync("ORD-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
    }

    // ── GetCargoProfiles Tests ──────────────────────────────────────────────

    [Fact]
    public async Task GetCargoProfilesAsync_SuccessfulResponse_ReturnsList()
    {
        var response = new PttavmCargoProfileResponse(new List<PttavmCargoProfile>
        {
            new(1, "PTT Kargo", "Standart", "birincil")
        });

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("cargo-profiles"))))
            .ReturnsAsync(CreateJsonResponse(response));

        var sut = CreateSut();
        var result = await sut.GetCargoProfilesAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
    }
}
