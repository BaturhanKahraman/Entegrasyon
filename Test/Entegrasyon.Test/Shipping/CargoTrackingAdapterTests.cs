using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Business.Concrete.Shipping;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Shipping;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Shipping;

public class CargoTrackingAdapterTests
{
    // ── Aras Adapter Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ArasAdapter_GetTrackingInfo_MapsStatusCorrectly()
    {
        var mockAras = new Mock<IArasKargoService>();
        var mockLogger = new Mock<ILogger<ArasTrackingAdapter>>();

        mockAras.Setup(x => x.TrackShipmentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<ArasKargoTrackingResult>(
                new ArasKargoTrackingResult("INT-001", "Teslim Edildi", "Ali Veli", "2026-03-23", "BRK-123")));

        mockAras.Setup(x => x.GetShipmentMovementsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<ArasKargoMovement>>(new List<ArasKargoMovement>
            {
                new("2026-03-22", "Kabul", "Istanbul", "Kargo kabul edildi"),
                new("2026-03-23", "Teslim Edildi", "Ankara", "Teslim edildi")
            }));

        var adapter = new ArasTrackingAdapter(mockAras.Object, mockLogger.Object);

        adapter.CargoCompanyId.Should().Be(18); // ARASMP seed Id

        var result = await adapter.GetTrackingInfoAsync("INT-001");
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TrackingNumber.Should().Be("INT-001");
        result.Data.CurrentStatus.Should().Be(ShipmentStatus.Delivered);
    }

    [Fact]
    public async Task ArasAdapter_GetTrackingInfo_ServiceFails_ReturnsError()
    {
        var mockAras = new Mock<IArasKargoService>();
        var mockLogger = new Mock<ILogger<ArasTrackingAdapter>>();

        mockAras.Setup(x => x.TrackShipmentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorDataResult<ArasKargoTrackingResult>(null!, "Baglanti hatasi"));

        var adapter = new ArasTrackingAdapter(mockAras.Object, mockLogger.Object);
        var result = await adapter.GetTrackingInfoAsync("INT-001");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ArasAdapter_GetStatusHistory_ReturnsHistoryList()
    {
        var mockAras = new Mock<IArasKargoService>();
        var mockLogger = new Mock<ILogger<ArasTrackingAdapter>>();

        mockAras.Setup(x => x.GetShipmentMovementsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<ArasKargoMovement>>(new List<ArasKargoMovement>
            {
                new("2026-03-22", "Kabul", "Istanbul", "Kargo kabul edildi"),
                new("2026-03-23", "Dagitimda", "Ankara", "Dagitimda")
            }));

        var adapter = new ArasTrackingAdapter(mockAras.Object, mockLogger.Object);
        var result = await adapter.GetStatusHistoryAsync("INT-001");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Location.Should().Be("Istanbul");
    }

    // ── Surat Adapter Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task SuratAdapter_GetTrackingInfo_MapsStatusCorrectly()
    {
        var mockSurat = new Mock<ISuratKargoService>();
        var mockLogger = new Mock<ILogger<SuratTrackingAdapter>>();

        mockSurat.Setup(x => x.QueryShipmentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<SuratKargoTrackingResult>(
                new SuratKargoTrackingResult("TRK-001", "Dagitimda",
                    new DateTime(2026, 3, 25),
                    new List<SuratKargoMovement>
                    {
                        new(new DateTime(2026, 3, 22), "Istanbul", "Kabul"),
                        new(new DateTime(2026, 3, 23), "Ankara", "Transfer")
                    })));

        var adapter = new SuratTrackingAdapter(mockSurat.Object, mockLogger.Object);

        adapter.CargoCompanyId.Should().Be(13); // SURATMP seed Id

        var result = await adapter.GetTrackingInfoAsync("TRK-001");
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TrackingNumber.Should().Be("TRK-001");
        result.Data.CurrentStatus.Should().Be(ShipmentStatus.OutForDelivery);
    }

    // ── Yurtici Adapter Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task YurticiAdapter_GetTrackingInfo_MapsOperationCode()
    {
        var mockYurtici = new Mock<IYurticiKargoService>();
        var mockLogger = new Mock<ILogger<YurticiTrackingAdapter>>();

        mockYurtici.Setup(x => x.QueryShipmentAsync(It.IsAny<YurticiQueryShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<YurticiShipmentInfo>>(new List<YurticiShipmentInfo>
            {
                new("CARGO-001", "INV-001", 4, "Teslim Edildi", new DateTime(2026, 3, 23), "Ali Veli", 1)
            }));

        var adapter = new YurticiTrackingAdapter(mockYurtici.Object, mockLogger.Object);

        adapter.CargoCompanyId.Should().Be(17); // YKMP seed Id

        var result = await adapter.GetTrackingInfoAsync("CARGO-001");
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CurrentStatus.Should().Be(ShipmentStatus.Delivered);
    }

    [Fact]
    public async Task YurticiAdapter_GetTrackingInfo_EmptyResponse_ReturnsError()
    {
        var mockYurtici = new Mock<IYurticiKargoService>();
        var mockLogger = new Mock<ILogger<YurticiTrackingAdapter>>();

        mockYurtici.Setup(x => x.QueryShipmentAsync(It.IsAny<YurticiQueryShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<YurticiShipmentInfo>>(new List<YurticiShipmentInfo>()));

        var adapter = new YurticiTrackingAdapter(mockYurtici.Object, mockLogger.Object);
        var result = await adapter.GetTrackingInfoAsync("CARGO-001");

        result.Success.Should().BeFalse();
    }
}
