using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.Reports;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Entegrasyon.UnitTest.Reports;

/// <summary>
/// /reports/inventory (GET) wiring'i — esnafın 4 envanter sorusunun controller tarafı:
///  - Filtre parametreleri (branchOfficeId, stockFilter, startDate, endDate, unsoldOnly)
///    InventoryReportFilterDto'ya geçer (manager bunları hesaplar).
///  - View form state'ini koruması için ViewBag'e yansıtılır
///    (BranchOfficeId, StockFilter, StartDate, EndDate, UnsoldOnly).
///  - Şube dropdown'u dolu gelsin diye ViewBag.Branches = GetBranchList().Data.
///  - Salt-okuma rapor → read-path log istisnası (AddLog her çağrıda yok).
/// </summary>
public class InventoryReportControllerTests
{
    private readonly Mock<IReportManager> _reportManager = new();
    private readonly Mock<ICustomerManager> _customerManager = new();
    private readonly Mock<IStorefrontReturnManager> _returnManager = new();
    private readonly Mock<IShipmentTrackingManager> _shipmentManager = new();
    private readonly Mock<IBranchOfficeManager> _branchManager = new();
    private readonly Mock<IStockTransferRequestManager> _transferManager = new();

    private static readonly InventoryReportDto EmptyReport =
        new(new InventoryReportSummaryDto(0, 0, 0, 0, 0m), []);

    private ReportController CreateSut()
    {
        _reportManager
            .Setup(m => m.GetInventoryReportAsync(It.IsAny<InventoryReportFilterDto>()))
            .ReturnsAsync(EmptyReport);
        _branchManager
            .Setup(m => m.GetBranchList(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<BranchOffice>>(
                [new BranchOffice { Id = 7, Name = "Ana Depo" }]));

        var controller = new ReportController(
            _reportManager.Object,
            _customerManager.Object,
            _returnManager.Object,
            _shipmentManager.Object,
            _branchManager.Object,
            _transferManager.Object);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.ViewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary());
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task Inventory_passes_all_filters_into_report_dto()
    {
        var sut = CreateSut();
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 31);

        await sut.Inventory(branchOfficeId: 7, stockFilter: StockFilter.LowStock,
            startDate: start, endDate: end, unsoldOnly: true);

        _reportManager.Verify(m => m.GetInventoryReportAsync(
            It.Is<InventoryReportFilterDto>(f =>
                f.BranchOfficeId == 7
                && f.StockFilter == StockFilter.LowStock
                && f.StartDate == start
                && f.EndDate == end
                && f.UnsoldOnly)), Times.Once);
    }

    [Fact]
    public async Task Inventory_reflects_filters_to_viewbag_for_form_state()
    {
        var sut = CreateSut();
        var start = new DateOnly(2026, 2, 1);
        var end = new DateOnly(2026, 2, 28);

        var result = await sut.Inventory(branchOfficeId: 7, stockFilter: StockFilter.OutOfStock,
            startDate: start, endDate: end, unsoldOnly: true) as ViewResult;

        result.Should().NotBeNull();
        // ViewBag dinamik → FluentAssertions çağırmadan önce tipli local'e al.
        int? branchOfficeId = sut.ViewBag.BranchOfficeId;
        StockFilter stockFilter = sut.ViewBag.StockFilter;
        DateOnly? viewStart = sut.ViewBag.StartDate;
        DateOnly? viewEnd = sut.ViewBag.EndDate;
        bool unsoldOnly = sut.ViewBag.UnsoldOnly;

        branchOfficeId.Should().Be(7);
        stockFilter.Should().Be(StockFilter.OutOfStock);
        viewStart.Should().Be(start);
        viewEnd.Should().Be(end);
        unsoldOnly.Should().BeTrue();
    }

    [Fact]
    public async Task Inventory_loads_branch_list_into_viewbag()
    {
        var sut = CreateSut();

        await sut.Inventory();

        _branchManager.Verify(m => m.GetBranchList(It.IsAny<CancellationToken>()), Times.Once);
        var branches = (IEnumerable<BranchOffice>?)sut.ViewBag.Branches;
        branches.Should().NotBeNull();
        branches!.Should().ContainSingle(b => b.Id == 7);
    }

    [Fact]
    public async Task Inventory_defaults_no_period_and_unsold_off()
    {
        var sut = CreateSut();

        await sut.Inventory();

        _reportManager.Verify(m => m.GetInventoryReportAsync(
            It.Is<InventoryReportFilterDto>(f =>
                f.BranchOfficeId == null
                && f.StockFilter == StockFilter.All
                && f.StartDate == null
                && f.EndDate == null
                && !f.UnsoldOnly)), Times.Once);
        bool unsoldOnly = sut.ViewBag.UnsoldOnly;
        unsoldOnly.Should().BeFalse();
    }
}
