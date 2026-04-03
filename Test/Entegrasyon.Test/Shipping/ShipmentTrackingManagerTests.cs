using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Shipping;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Shipping;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.Test.Shipping;

public class ShipmentTrackingManagerTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ILogger<ShipmentTrackingManager>> _mockLogger = new();
    private readonly Mock<ICargoTrackingAdapter> _mockArasAdapter = new();
    private readonly Mock<ICargoTrackingAdapter> _mockSuratAdapter = new();
    private readonly Mock<ICargoTrackingAdapter> _mockYurticiAdapter = new();

    public ShipmentTrackingManagerTests()
    {
        _mockArasAdapter.SetupGet(x => x.CargoCompanyId).Returns(18);
        _mockSuratAdapter.SetupGet(x => x.CargoCompanyId).Returns(13);
        _mockYurticiAdapter.SetupGet(x => x.CargoCompanyId).Returns(17);

        MockValidator = new Mock<IFluentValidator>();
        MockValidator.Setup(x => x.Validate(It.IsAny<TrackShipmentDto>()))
            .ReturnsAsync(new ValidationResult());
    }

    private ShipmentTrackingManager CreateSut()
    {
        var adapters = new List<ICargoTrackingAdapter>
        {
            _mockArasAdapter.Object,
            _mockSuratAdapter.Object,
            _mockYurticiAdapter.Object
        };

        return new ShipmentTrackingManager(
            mockContextFactory.Object,
            adapters,
            MockValidator.Object,
            mockApplicationLogger.Object,
            _mockLogger.Object);
    }

    // ── TrackShipment Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task TrackShipment_ArasKargo_ValidTracking_ReturnsSuccess()
    {
        var dto = new TrackShipmentDto("INT-001", 18);
        var trackingDto = new ShipmentTrackingDto(
            0, null, 18, "Aras Kargo", "INT-001",
            ShipmentStatus.InTransit, DateTimeOffset.UtcNow,
            null, null, "Ali Veli", null,
            new List<ShipmentStatusHistoryDto>());

        _mockArasAdapter.Setup(x => x.GetTrackingInfoAsync(dto.TrackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<ShipmentTrackingDto>(trackingDto));

        mockIntegrationDbContext.Setup(x => x.ShipmentTrackings).ReturnsDbSet(new List<ShipmentTracking>());
        mockIntegrationDbContext.Setup(x => x.CargoCompanies).ReturnsDbSet(new List<CargoCompany>
        {
            new() { Id = 18, Name = "Aras Kargo Marketplace", Code = "ARASMP" }
        });

        var sut = CreateSut();
        var result = await sut.TrackShipmentAsync(dto);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TrackingNumber.Should().Be("INT-001");
        result.Data.CurrentStatus.Should().Be(ShipmentStatus.InTransit);
    }

    [Fact]
    public async Task TrackShipment_SuratKargo_ValidTracking_ReturnsSuccess()
    {
        var dto = new TrackShipmentDto("TRK-001", 13);
        var trackingDto = new ShipmentTrackingDto(
            0, null, 13, "Surat Kargo", "TRK-001",
            ShipmentStatus.Delivered, DateTimeOffset.UtcNow,
            null, new DateTimeOffset(2026, 3, 23, 0, 0, 0, TimeSpan.Zero),
            "Ayse Fatma", null,
            new List<ShipmentStatusHistoryDto>());

        _mockSuratAdapter.Setup(x => x.GetTrackingInfoAsync(dto.TrackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<ShipmentTrackingDto>(trackingDto));

        mockIntegrationDbContext.Setup(x => x.ShipmentTrackings).ReturnsDbSet(new List<ShipmentTracking>());
        mockIntegrationDbContext.Setup(x => x.CargoCompanies).ReturnsDbSet(new List<CargoCompany>
        {
            new() { Id = 13, Name = "Surat Kargo Marketplace", Code = "SURATMP" }
        });

        var sut = CreateSut();
        var result = await sut.TrackShipmentAsync(dto);

        result.Success.Should().BeTrue();
        result.Data!.CurrentStatus.Should().Be(ShipmentStatus.Delivered);
    }

    [Fact]
    public async Task TrackShipment_YurticiKargo_ValidTracking_ReturnsSuccess()
    {
        var dto = new TrackShipmentDto("CARGO-001", 17);
        var trackingDto = new ShipmentTrackingDto(
            0, null, 17, "Yurtici Kargo", "CARGO-001",
            ShipmentStatus.InTransit, DateTimeOffset.UtcNow,
            null, null, null, null,
            new List<ShipmentStatusHistoryDto>());

        _mockYurticiAdapter.Setup(x => x.GetTrackingInfoAsync(dto.TrackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<ShipmentTrackingDto>(trackingDto));

        mockIntegrationDbContext.Setup(x => x.ShipmentTrackings).ReturnsDbSet(new List<ShipmentTracking>());
        mockIntegrationDbContext.Setup(x => x.CargoCompanies).ReturnsDbSet(new List<CargoCompany>
        {
            new() { Id = 17, Name = "Yurtici Kargo Marketplace", Code = "YKMP" }
        });

        var sut = CreateSut();
        var result = await sut.TrackShipmentAsync(dto);

        result.Success.Should().BeTrue();
        result.Data!.TrackingNumber.Should().Be("CARGO-001");
    }

    [Fact]
    public async Task TrackShipment_UnknownCargoCompany_ReturnsError()
    {
        var dto = new TrackShipmentDto("UNKNOWN-001", 999);

        mockIntegrationDbContext.Setup(x => x.ShipmentTrackings).ReturnsDbSet(new List<ShipmentTracking>());

        var sut = CreateSut();
        var result = await sut.TrackShipmentAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("adapter");
    }

    [Fact]
    public async Task TrackShipment_ValidationFails_ReturnsError()
    {
        var dto = new TrackShipmentDto("", 18);

        MockValidator.Setup(x => x.Validate(It.IsAny<TrackShipmentDto>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("TrackingNumber", "Takip numarasi bos olamaz") }));

        var sut = CreateSut();
        var result = await sut.TrackShipmentAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Takip numarasi");
    }

    // ── GetAllShipments Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllShipments_WithFilters_ReturnsFilteredResults()
    {
        var shipments = new List<ShipmentTracking>
        {
            new()
            {
                Id = 1, TrackingNumber = "TRK-001", CargoCompanyId = 18,
                CurrentStatus = ShipmentStatus.InTransit,
                CargoCompany = new CargoCompany { Id = 18, Name = "Aras Kargo" },
                StatusHistory = new List<ShipmentStatusHistory>()
            },
            new()
            {
                Id = 2, TrackingNumber = "TRK-002", CargoCompanyId = 13,
                CurrentStatus = ShipmentStatus.Delivered,
                CargoCompany = new CargoCompany { Id = 13, Name = "Surat Kargo" },
                StatusHistory = new List<ShipmentStatusHistory>()
            },
            new()
            {
                Id = 3, TrackingNumber = "TRK-003", CargoCompanyId = 18,
                CurrentStatus = ShipmentStatus.Failed,
                CargoCompany = new CargoCompany { Id = 18, Name = "Aras Kargo" },
                StatusHistory = new List<ShipmentStatusHistory>()
            }
        };

        mockIntegrationDbContext.Setup(x => x.ShipmentTrackings).ReturnsDbSet(shipments);

        var request = new Entegrasyon.Entity.Requests.ShipmentPaginatedRequest
        {
            Status = ShipmentStatus.InTransit
        };

        var sut = CreateSut();
        var result = await sut.GetAllShipmentsAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data!.Items[0].TrackingNumber.Should().Be("TRK-001");
    }

    // ── RefreshTrackingStatus Tests ───────────────────────────────────────────

    [Fact]
    public async Task RefreshTrackingStatus_UpdatesStatusAndHistory()
    {
        var existingTracking = new ShipmentTracking
        {
            Id = 1,
            TrackingNumber = "INT-001",
            CargoCompanyId = 18,
            CurrentStatus = ShipmentStatus.PickedUp,
            CargoCompany = new CargoCompany { Id = 18, Name = "Aras Kargo" },
            StatusHistory = new List<ShipmentStatusHistory>()
        };

        mockIntegrationDbContext.Setup(x => x.ShipmentTrackings).ReturnsDbSet(new List<ShipmentTracking> { existingTracking });
        mockIntegrationDbContext.Setup(x => x.ShipmentStatusHistories).ReturnsDbSet(new List<ShipmentStatusHistory>());

        var updatedHistory = new List<ShipmentStatusHistoryDto>
        {
            new(ShipmentStatus.PickedUp, "Kabul edildi", "Istanbul", DateTimeOffset.UtcNow.AddDays(-1)),
            new(ShipmentStatus.InTransit, "Transfer", "Ankara", DateTimeOffset.UtcNow)
        };

        _mockArasAdapter.Setup(x => x.GetStatusHistoryAsync("INT-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<ShipmentStatusHistoryDto>>(updatedHistory));

        var sut = CreateSut();
        var result = await sut.RefreshTrackingStatusAsync(1);

        result.Success.Should().BeTrue();
        existingTracking.CurrentStatus.Should().Be(ShipmentStatus.InTransit);
    }

    // ── GetCargoSummary Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCargoSummary_ReturnsCorrectCounts()
    {
        var shipments = new List<ShipmentTracking>
        {
            new() { Id = 1, TrackingNumber = "T1", CargoCompanyId = 18, CurrentStatus = ShipmentStatus.InTransit, StatusHistory = new List<ShipmentStatusHistory>() },
            new() { Id = 2, TrackingNumber = "T2", CargoCompanyId = 18, CurrentStatus = ShipmentStatus.InTransit, StatusHistory = new List<ShipmentStatusHistory>() },
            new() { Id = 3, TrackingNumber = "T3", CargoCompanyId = 13, CurrentStatus = ShipmentStatus.Delivered, StatusHistory = new List<ShipmentStatusHistory>() },
            new() { Id = 4, TrackingNumber = "T4", CargoCompanyId = 17, CurrentStatus = ShipmentStatus.Failed, StatusHistory = new List<ShipmentStatusHistory>() },
            new() { Id = 5, TrackingNumber = "T5", CargoCompanyId = 18, CurrentStatus = ShipmentStatus.Created, StatusHistory = new List<ShipmentStatusHistory>() }
        };

        mockIntegrationDbContext.Setup(x => x.ShipmentTrackings).ReturnsDbSet(shipments);

        var sut = CreateSut();
        var result = await sut.GetCargoSummaryAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalShipments.Should().Be(5);
        result.Data.InTransitCount.Should().Be(2);
        result.Data.DeliveredCount.Should().Be(1);
        result.Data.FailedCount.Should().Be(1);
        result.Data.PendingCount.Should().Be(1); // Created
    }
}
