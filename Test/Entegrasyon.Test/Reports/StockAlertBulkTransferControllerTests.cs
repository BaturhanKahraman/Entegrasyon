using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.Reports;
using Entegrasyon.MVC.Features.Reports.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Entegrasyon.UnitTest.Reports;

/// <summary>
/// /reports/stock-alerts/bulk-transfer wiring'i:
///  - Seçili satırlar kaynak şube başına gruplanır → her grup için ayrı CreateAsync
///    (StockTransferRequestManager.CreateAsync tek source/target alır).
///  - Hedef şube tek (formdan); her satırın kaynak şubesi alert satırından gelir.
///  - PRG: TempData.SetSuccess + RedirectToAction(StockAlerts).
///  - Boş seçim / oturumsuz / hedef=kaynak gibi durumlar hata + redirect.
///  - Kullanıcı kimliği claim'den (NameIdentifier) okunur.
/// </summary>
public class StockAlertBulkTransferControllerTests
{
    private readonly Mock<IReportManager> _reportManager = new();
    private readonly Mock<ICustomerReportManager> _customerReportManager = new();
    private readonly Mock<IStorefrontReturnManager> _returnManager = new();
    private readonly Mock<IShipmentTrackingManager> _shipmentManager = new();
    private readonly Mock<IBranchOfficeManager> _branchManager = new();
    private readonly Mock<ISupplierReturnNotificationManager> _supplierNotifyManager = new();
    private readonly Mock<IShipmentDelayNotificationManager> _delayNotifyManager = new();
    private readonly Mock<IStockTransferRequestManager> _transferManager = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private ReportController CreateSut(bool withUser = true)
    {
        var controller = new ReportController(
            _reportManager.Object,
            _customerReportManager.Object,
            _returnManager.Object,
            _shipmentManager.Object,
            _branchManager.Object,
            _supplierNotifyManager.Object,
            _delayNotifyManager.Object,
            _transferManager.Object);

        var httpContext = new DefaultHttpContext();
        if (withUser)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.ViewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary());
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static StockAlertBulkTransferVm Vm(int targetBranch, params (Guid variant, int source, int qty)[] lines) =>
        new()
        {
            TargetBranchOfficeId = targetBranch,
            Lines = lines.Select(l => new StockAlertBulkTransferLine
            {
                ProductVariantId = l.variant,
                SourceBranchOfficeId = l.source,
                Quantity = l.qty
            }).ToList()
        };

    [Fact]
    public async Task BulkTransfer_groups_lines_by_source_branch_one_request_per_source()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var v3 = Guid.NewGuid();
        // İki kaynak şube (1 ve 2), hepsi hedef 9'a
        var vm = Vm(9,
            (v1, source: 1, qty: 5),
            (v2, source: 1, qty: 3),
            (v3, source: 2, qty: 7));

        _transferManager
            .Setup(m => m.CreateAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlyList<TransferItemDto>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<int>(1));

        var sut = CreateSut();

        var result = await sut.BulkTransfer(vm);

        // Kaynak 1 → 2 item; kaynak 2 → 1 item; 2 ayrı talep
        _transferManager.Verify(m => m.CreateAsync(
            1, 9,
            It.Is<IReadOnlyList<TransferItemDto>>(items => items.Count == 2),
            UserId, It.IsAny<CancellationToken>()), Times.Once);
        _transferManager.Verify(m => m.CreateAsync(
            2, 9,
            It.Is<IReadOnlyList<TransferItemDto>>(items => items.Count == 1
                && items[0].ProductVariantId == v3 && items[0].Quantity == 7),
            UserId, It.IsAny<CancellationToken>()), Times.Once);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(ReportController.StockAlerts));
    }

    [Fact]
    public async Task BulkTransfer_empty_selection_redirects_without_calling_manager()
    {
        var sut = CreateSut();

        var result = await sut.BulkTransfer(Vm(9));

        _transferManager.Verify(m => m.CreateAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<TransferItemDto>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task BulkTransfer_skips_lines_with_nonpositive_quantity_or_empty_variant()
    {
        var v1 = Guid.NewGuid();
        var vm = Vm(9,
            (v1, source: 1, qty: 5),
            (Guid.Empty, source: 1, qty: 4),   // geçersiz varyant → atla
            (Guid.NewGuid(), source: 1, qty: 0)); // miktar 0 → atla

        _transferManager
            .Setup(m => m.CreateAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlyList<TransferItemDto>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<int>(1));

        var sut = CreateSut();

        await sut.BulkTransfer(vm);

        _transferManager.Verify(m => m.CreateAsync(
            1, 9,
            It.Is<IReadOnlyList<TransferItemDto>>(items => items.Count == 1 && items[0].ProductVariantId == v1),
            UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkTransfer_no_user_session_sets_error_and_does_not_transfer()
    {
        var sut = CreateSut(withUser: false);

        var result = await sut.BulkTransfer(Vm(9, (Guid.NewGuid(), 1, 5)));

        _transferManager.Verify(m => m.CreateAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<TransferItemDto>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task BulkTransfer_target_equals_source_is_rejected_for_that_group()
    {
        var v1 = Guid.NewGuid();
        // Hedef 1, kaynak 1 → geçersiz (aynı şube)
        var vm = Vm(1, (v1, source: 1, qty: 5));

        var sut = CreateSut();

        await sut.BulkTransfer(vm);

        _transferManager.Verify(m => m.CreateAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<TransferItemDto>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkTransfer_manager_failure_surfaces_error_message()
    {
        _transferManager
            .Setup(m => m.CreateAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlyList<TransferItemDto>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorDataResult<int>(0, "Kaynak şube aktif değil veya bulunamadı."));

        var sut = CreateSut();

        var result = await sut.BulkTransfer(Vm(9, (Guid.NewGuid(), 1, 5)));

        result.Should().BeOfType<RedirectToActionResult>();
    }
}
