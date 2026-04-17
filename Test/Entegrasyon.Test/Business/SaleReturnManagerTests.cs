using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class SaleReturnManagerTests : BaseTest
{
    private readonly SaleReturnManager _sut;
    private readonly Mock<IOfficeStockManager> _mockStock;

    public SaleReturnManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator.Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>())).Returns(Task.CompletedTask);

        _mockStock = new Mock<IOfficeStockManager>();
        _mockStock
            .Setup(s => s.IncreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));
        _mockStock
            .Setup(s => s.DecreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        _sut = new SaleReturnManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object,
            _mockStock.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static SaleItem MakeSaleItem(int qty = 3, int returned = 0, decimal price = 100m, double tax = 18)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            UnitPrice = price,
            TaxPercentage = tax,
            Quantity = qty,
            ReturnedQuantity = returned,
            ProductTitle = "Test Ürün"
        };

    private static Sale MakeSale(SaleItem? item = null)
    {
        var si = item ?? MakeSaleItem();
        return new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = "S202601010001",
            BranchOfficeId = 1,
            SaleStatus = SaleStatus.Completed,
            SaleItems = [si]
        };
    }

    private CreateSaleReturnDto MakeCreateDto(Sale sale, int qty = 1) => new(
        SaleId: sale.Id,
        OrderId: null,
        ReturnedByUserId: Guid.NewGuid(),
        Source: ReturnSource.InPerson,
        ReturnReasonId: 1,
        CustomReason: null,
        RefundPaymentMethodId: null,
        Note: null,
        Items: [new SaleReturnItemDto(sale.SaleItems.First().Id, null, qty, "Hasar")]);

    private void SetupSales(List<Sale> sales) =>
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);

    private void SetupReturns(List<SaleReturn> returns) =>
        mockIntegrationDbContext.Setup(x => x.SaleReturns).ReturnsDbSet(returns);

    private void SetupReturnItems(List<SaleReturnItem> items) =>
        mockIntegrationDbContext.Setup(x => x.SaleReturnItems).ReturnsDbSet(items);

    private void SetupSave() =>
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ── CREATE ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReturnAsync_PartialReturn_SetsPartialStatus()
    {
        var si = MakeSaleItem(qty: 3);
        var sale = MakeSale(si);
        SetupSales([sale]);
        SetupSave();
        mockIntegrationDbContext.Setup(x => x.SaleReturns.Add(It.IsAny<SaleReturn>()));

        var result = await _sut.CreateReturnAsync(MakeCreateDto(sale, qty: 1));

        result.Success.Should().BeTrue();
        si.ReturnedQuantity.Should().Be(1);
        sale.SaleStatus.Should().Be(SaleStatus.PartialReturn);
    }

    [Fact]
    public async Task CreateReturnAsync_AllReturned_SetsFullReturnStatus()
    {
        var si = MakeSaleItem(qty: 2);
        var sale = MakeSale(si);
        SetupSales([sale]);
        SetupSave();
        mockIntegrationDbContext.Setup(x => x.SaleReturns.Add(It.IsAny<SaleReturn>()));

        var result = await _sut.CreateReturnAsync(MakeCreateDto(sale, qty: 2));

        result.Success.Should().BeTrue();
        sale.SaleStatus.Should().Be(SaleStatus.FullReturn);
    }

    [Fact]
    public async Task CreateReturnAsync_ExceedsAvailable_ReturnsError()
    {
        var si = MakeSaleItem(qty: 2, returned: 1);
        var sale = MakeSale(si);
        SetupSales([sale]);

        var result = await _sut.CreateReturnAsync(MakeCreateDto(sale, qty: 2));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("maksimum 1 adet");
    }

    [Fact]
    public async Task CreateReturnAsync_CancelledSale_ReturnsError()
    {
        var sale = MakeSale();
        sale.SaleStatus = SaleStatus.Cancelled;
        SetupSales([sale]);

        var result = await _sut.CreateReturnAsync(MakeCreateDto(sale));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("İptal edilmiş");
    }

    [Fact]
    public async Task CreateReturnAsync_Draft_WhenSubmitImmediatelyFalse()
    {
        var si = MakeSaleItem(qty: 3);
        var sale = MakeSale(si);
        SetupSales([sale]);
        SetupSave();

        SaleReturn? captured = null;
        mockIntegrationDbContext.Setup(x => x.SaleReturns.Add(It.IsAny<SaleReturn>()))
            .Callback<SaleReturn>(r => captured = r);

        var dto = MakeCreateDto(sale) with { SubmitImmediately = false };
        var result = await _sut.CreateReturnAsync(dto);

        result.Success.Should().BeTrue();
        captured!.ReturnStatus.Should().Be(ReturnStatus.Draft);
    }

    [Fact]
    public async Task CreateReturnAsync_CalculatesRefundWithVat()
    {
        var si = MakeSaleItem(qty: 5, price: 100m, tax: 18);
        var sale = MakeSale(si);
        SetupSales([sale]);
        SetupSave();

        SaleReturn? captured = null;
        mockIntegrationDbContext.Setup(x => x.SaleReturns.Add(It.IsAny<SaleReturn>()))
            .Callback<SaleReturn>(r => captured = r);

        var result = await _sut.CreateReturnAsync(MakeCreateDto(sale, qty: 2));

        result.Success.Should().BeTrue();
        captured!.RefundAmount.Should().Be(236m); // 2 * 100 * 1.18
    }

    // ── APPROVE (artık stok eklemez) ─────────────────────────────────────

    [Fact]
    public async Task ApproveReturnAsync_DoesNotTouchStock()
    {
        var sr = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Pending };
        SetupReturns([sr]);
        SetupSave();

        var result = await _sut.ApproveReturnAsync(1, Guid.NewGuid());

        result.Success.Should().BeTrue();
        sr.ReturnStatus.Should().Be(ReturnStatus.Approved);
        _mockStock.Verify(s => s.IncreaseStockAtomicAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApproveReturnAsync_NotPending_ReturnsError()
    {
        var sr = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Approved };
        SetupReturns([sr]);

        var result = await _sut.ApproveReturnAsync(1, Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bekleyen");
    }

    // ── SUBMIT ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitReturnAsync_Draft_TransitionsToPending()
    {
        var sr = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Draft };
        SetupReturns([sr]);
        SetupSave();

        var result = await _sut.SubmitReturnAsync(1, Guid.NewGuid());

        result.Success.Should().BeTrue();
        sr.ReturnStatus.Should().Be(ReturnStatus.Pending);
    }

    [Fact]
    public async Task SubmitReturnAsync_NotDraft_ReturnsError()
    {
        var sr = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Pending };
        SetupReturns([sr]);

        var result = await _sut.SubmitReturnAsync(1, Guid.NewGuid());

        result.Success.Should().BeFalse();
    }

    // ── COMPLETE ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteReturnAsync_PartialItems_OnlyRestoresSelected()
    {
        var si1 = MakeSaleItem();
        var si2 = MakeSaleItem();
        var item1 = new SaleReturnItem { Id = 10, SaleItem = si1, SaleItemId = si1.Id, Quantity = 1 };
        var item2 = new SaleReturnItem { Id = 20, SaleItem = si2, SaleItemId = si2.Id, Quantity = 1 };
        var sr = new SaleReturn
        {
            Id = 1,
            ReturnStatus = ReturnStatus.Approved,
            SaleId = Guid.NewGuid(),
            Items = [item1, item2]
        };
        SetupReturns([sr]);
        SetupSave();

        var dto = new CompleteSaleReturnDto(1, Guid.NewGuid(), BranchOfficeId: 1, ItemIdsToRestore: [10]);
        var result = await _sut.CompleteReturnAsync(dto);

        result.Success.Should().BeTrue();
        sr.ReturnStatus.Should().Be(ReturnStatus.Completed);
        item1.RestoredToStock.Should().BeTrue();
        item2.RestoredToStock.Should().BeFalse();
        _mockStock.Verify(s => s.IncreaseStockAtomicAsync(
            1, si1.ProductVariantId, 1, StockMovementType.Return, "SaleReturn", "1"), Times.Once);
    }

    [Fact]
    public async Task CompleteReturnAsync_EmptyList_CompletesWithoutStock()
    {
        var si = MakeSaleItem();
        var item = new SaleReturnItem { Id = 10, SaleItem = si, SaleItemId = si.Id, Quantity = 1 };
        var sr = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Approved, Items = [item] };
        SetupReturns([sr]);
        SetupSave();

        var dto = new CompleteSaleReturnDto(1, Guid.NewGuid(), BranchOfficeId: 1, ItemIdsToRestore: []);
        var result = await _sut.CompleteReturnAsync(dto);

        result.Success.Should().BeTrue();
        sr.ReturnStatus.Should().Be(ReturnStatus.Completed);
        item.RestoredToStock.Should().BeFalse();
        _mockStock.Verify(s => s.IncreaseStockAtomicAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CompleteReturnAsync_Idempotent_SkipsAlreadyRestored()
    {
        var si = MakeSaleItem();
        var item = new SaleReturnItem
        {
            Id = 10, SaleItem = si, SaleItemId = si.Id, Quantity = 1,
            RestoredToStock = true, RestoredAt = DateTimeOffset.UtcNow
        };
        var sr = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Approved, Items = [item] };
        SetupReturns([sr]);
        SetupSave();

        var dto = new CompleteSaleReturnDto(1, Guid.NewGuid(), BranchOfficeId: 1, ItemIdsToRestore: [10]);
        await _sut.CompleteReturnAsync(dto);

        _mockStock.Verify(s => s.IncreaseStockAtomicAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CompleteReturnAsync_NotApproved_ReturnsError()
    {
        var sr = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Pending, Items = [] };
        SetupReturns([sr]);

        var dto = new CompleteSaleReturnDto(1, Guid.NewGuid(), BranchOfficeId: 1, ItemIdsToRestore: []);
        var result = await _sut.CompleteReturnAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("onaylanmış");
    }

    // ── CANCEL ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelReturnAsync_PendingSaleReturn_RecalcsSaleStatus()
    {
        var si = MakeSaleItem(qty: 3, returned: 2);
        var sale = MakeSale(si);
        var sr = new SaleReturn
        {
            Id = 1,
            SaleId = sale.Id,
            ReturnStatus = ReturnStatus.Pending,
            Items = [new SaleReturnItem { Id = 10, SaleItem = si, SaleItemId = si.Id, Quantity = 2 }]
        };
        SetupReturns([sr]);
        SetupSales([sale]);
        SetupSave();

        var dto = new CancelSaleReturnDto(1, Guid.NewGuid(), "Yanlış girildi");
        var result = await _sut.CancelReturnAsync(dto);

        result.Success.Should().BeTrue();
        sr.ReturnStatus.Should().Be(ReturnStatus.Cancelled);
        sr.CancellationReason.Should().Be("Yanlış girildi");
        si.ReturnedQuantity.Should().Be(0);
        sale.SaleStatus.Should().Be(SaleStatus.Completed);
    }

    [Fact]
    public async Task CancelReturnAsync_WhenItemsRestored_DecreasesStock()
    {
        var si = MakeSaleItem();
        var item = new SaleReturnItem
        {
            Id = 10, SaleItem = si, SaleItemId = si.Id, Quantity = 1,
            RestoredToStock = true, RestoredAt = DateTimeOffset.UtcNow
        };
        var sr = new SaleReturn
        {
            Id = 1,
            ReturnStatus = ReturnStatus.Approved,
            RestoreBranchOfficeId = 1,
            Items = [item]
        };
        SetupReturns([sr]);
        SetupSave();

        var dto = new CancelSaleReturnDto(1, Guid.NewGuid(), "İptal lazım");
        var result = await _sut.CancelReturnAsync(dto);

        result.Success.Should().BeTrue();
        item.RestoredToStock.Should().BeFalse();
        _mockStock.Verify(s => s.DecreaseStockAtomicAsync(
            1, si.ProductVariantId, 1, StockMovementType.Return, "SaleReturn", "1"), Times.Once);
    }

    // ── REJECT ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectReturnAsync_RollsBackReturnedQuantity()
    {
        var si = MakeSaleItem(qty: 3, returned: 2);
        var sale = MakeSale(si);
        var sr = new SaleReturn
        {
            Id = 1,
            SaleId = sale.Id,
            ReturnStatus = ReturnStatus.Pending,
            Items = [new SaleReturnItem { Id = 10, SaleItem = si, SaleItemId = si.Id, Quantity = 2 }]
        };
        SetupReturns([sr]);
        SetupSales([sale]);
        SetupSave();

        var result = await _sut.RejectReturnAsync(1, Guid.NewGuid(), "Geçersiz talep");

        result.Success.Should().BeTrue();
        sr.ReturnStatus.Should().Be(ReturnStatus.Rejected);
        si.ReturnedQuantity.Should().Be(0);
        sale.SaleStatus.Should().Be(SaleStatus.Completed);
    }

    // ── RESTORE ITEM ─────────────────────────────────────────────────────

    [Fact]
    public async Task RestoreItemToStockAsync_AlreadyRestored_ReturnsSuccess()
    {
        var si = MakeSaleItem();
        var item = new SaleReturnItem
        {
            Id = 10, SaleReturn = new SaleReturn { Id = 1, ReturnStatus = ReturnStatus.Completed },
            SaleItem = si, SaleItemId = si.Id, Quantity = 1,
            RestoredToStock = true
        };
        SetupReturnItems([item]);

        var result = await _sut.RestoreItemToStockAsync(10, Guid.NewGuid(), 1);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("zaten");
        _mockStock.Verify(s => s.IncreaseStockAtomicAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
