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
    private readonly Mock<IOfficeStockManager> _mockOfficeStockManager;

    public SaleReturnManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<CreateSaleReturnDto>()))
            .Returns(Task.CompletedTask);

        _mockOfficeStockManager = new Mock<IOfficeStockManager>();
        _mockOfficeStockManager
            .Setup(s => s.IncreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        _sut = new SaleReturnManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object,
            _mockOfficeStockManager.Object);
    }

    private static Sale BuildCompletedSale(
        Guid? saleId = null,
        int branchOfficeId = 1,
        List<SaleItem>? items = null)
    {
        var id = saleId ?? Guid.NewGuid();
        return new Sale
        {
            Id = id,
            SaleNumber = $"S202601010001",
            SaleDate = DateTimeOffset.UtcNow,
            BranchOfficeId = branchOfficeId,
            SaleStatus = SaleStatus.Completed,
            SaleItems = items ?? new List<SaleItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = Guid.NewGuid(),
                    UnitPrice = 100m,
                    TaxPercentage = 18,
                    Quantity = 3,
                    ReturnedQuantity = 0,
                    ProductTitle = "Test Ürün"
                }
            }
        };
    }

    private void SetupForCreateReturn(List<Sale> sales)
    {
        mockIntegrationDbContext
            .Setup(x => x.Sales)
            .ReturnsDbSet(sales);

        mockIntegrationDbContext
            .Setup(x => x.SaleReturns.Add(It.IsAny<SaleReturn>()));

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private void SetupForApproveReject(List<SaleReturn> returns)
    {
        mockIntegrationDbContext
            .Setup(x => x.SaleReturns)
            .ReturnsDbSet(returns);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // -------------------------------------------------------------------------
    // CreateReturnAsync Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateReturnAsync_PartialReturn_UpdatesReturnedQuantityAndPartialStatus()
    {
        // Arrange
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            UnitPrice = 100m,
            TaxPercentage = 18,
            Quantity = 3,
            ReturnedQuantity = 0,
            ProductTitle = "Test Ürün"
        };
        var sale = BuildCompletedSale(items: new List<SaleItem> { saleItem });

        // Only 1 of 3 returned — partial
        sale.SaleItems = new List<SaleItem> { saleItem };
        SetupForCreateReturn(new List<Sale> { sale });

        var dto = new CreateSaleReturnDto(
            SaleId: sale.Id,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Ürün hasarlı",
            RefundPaymentMethodId: null,
            Note: null,
            Items: new List<SaleReturnItemDto>
            {
                new(saleItem.Id, Quantity: 1, Reason: "Hasar")
            });

        // Act
        var result = await _sut.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        saleItem.ReturnedQuantity.Should().Be(1);
        sale.SaleStatus.Should().Be(SaleStatus.PartialReturn);
    }

    [Fact]
    public async Task CreateReturnAsync_AllItemsReturned_SetsFullReturnStatus()
    {
        // Arrange
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            UnitPrice = 100m,
            TaxPercentage = 18,
            Quantity = 2,
            ReturnedQuantity = 0,
            ProductTitle = "Test Ürün"
        };
        var sale = BuildCompletedSale(items: new List<SaleItem> { saleItem });
        SetupForCreateReturn(new List<Sale> { sale });

        var dto = new CreateSaleReturnDto(
            SaleId: sale.Id,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Ürün beklentileri karşılamadı",
            RefundPaymentMethodId: null,
            Note: null,
            Items: new List<SaleReturnItemDto>
            {
                new(saleItem.Id, Quantity: 2, Reason: null)
            });

        // Act
        var result = await _sut.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        saleItem.ReturnedQuantity.Should().Be(2);
        sale.SaleStatus.Should().Be(SaleStatus.FullReturn);
    }

    [Fact]
    public async Task CreateReturnAsync_ExceedsAvailableQuantity_ReturnsError()
    {
        // Arrange
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            UnitPrice = 100m,
            TaxPercentage = 18,
            Quantity = 2,
            ReturnedQuantity = 1, // 1 already returned, only 1 available
            ProductTitle = "Test Ürün"
        };
        var sale = BuildCompletedSale(items: new List<SaleItem> { saleItem });
        SetupForCreateReturn(new List<Sale> { sale });

        var dto = new CreateSaleReturnDto(
            SaleId: sale.Id,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Hasarlı",
            RefundPaymentMethodId: null,
            Note: null,
            Items: new List<SaleReturnItemDto>
            {
                new(saleItem.Id, Quantity: 2, Reason: null) // requesting 2, but only 1 available
            });

        // Act
        var result = await _sut.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("maksimum 1 adet iade edilebilir");
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReturnAsync_CancelledSale_ReturnsError()
    {
        // Arrange
        var sale = BuildCompletedSale();
        sale.SaleStatus = SaleStatus.Cancelled;
        SetupForCreateReturn(new List<Sale> { sale });

        var dto = new CreateSaleReturnDto(
            SaleId: sale.Id,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Hasarlı",
            RefundPaymentMethodId: null,
            Note: null,
            Items: new List<SaleReturnItemDto> { new(Guid.NewGuid(), 1, null) });

        // Act
        var result = await _sut.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("İptal edilmiş satış");
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReturnAsync_FullyReturnedSale_ReturnsError()
    {
        // Arrange
        var sale = BuildCompletedSale();
        sale.SaleStatus = SaleStatus.FullReturn;
        SetupForCreateReturn(new List<Sale> { sale });

        var dto = new CreateSaleReturnDto(
            SaleId: sale.Id,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Hasarlı",
            RefundPaymentMethodId: null,
            Note: null,
            Items: new List<SaleReturnItemDto> { new(Guid.NewGuid(), 1, null) });

        // Act
        var result = await _sut.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("tamamı zaten iade edilmiş");
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReturnAsync_CalculatesRefundAmountWithVat()
    {
        // Arrange — 2 adet, 100 TL, %18 KDV → 2 * 100 * 1.18 = 236
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            UnitPrice = 100m,
            TaxPercentage = 18,
            Quantity = 5,
            ReturnedQuantity = 0,
            ProductTitle = "Test Ürün"
        };
        var sale = BuildCompletedSale(items: new List<SaleItem> { saleItem });

        SaleReturn? capturedReturn = null;
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale> { sale });
        mockIntegrationDbContext.Setup(x => x.SaleReturns.Add(It.IsAny<SaleReturn>()))
            .Callback<SaleReturn>(r => capturedReturn = r);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dto = new CreateSaleReturnDto(
            SaleId: sale.Id,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Test",
            RefundPaymentMethodId: null,
            Note: null,
            Items: new List<SaleReturnItemDto>
            {
                new(saleItem.Id, Quantity: 2, Reason: null)
            });

        // Act
        var result = await _sut.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        capturedReturn.Should().NotBeNull();
        capturedReturn!.RefundAmount.Should().Be(236m); // 2 * 100 * 1.18
    }

    // -------------------------------------------------------------------------
    // ApproveReturnAsync Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApproveReturnAsync_SetsApprovedStatusAndCallsStockIncrease()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variantId,
            Quantity = 3,
            ReturnedQuantity = 2
        };
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = "S202601010001",
            BranchOfficeId = 1,
            SaleItems = new List<SaleItem> { saleItem }
        };
        var saleReturn = new SaleReturn
        {
            Id = 1,
            Sale = sale,
            SaleId = sale.Id,
            ReturnStatus = ReturnStatus.Pending,
            Items = new List<SaleReturnItem>
            {
                new() { Id = 1, SaleItem = saleItem, SaleItemId = saleItem.Id, Quantity = 2 }
            }
        };
        SetupForApproveReject(new List<SaleReturn> { saleReturn });

        // Act
        var result = await _sut.ApproveReturnAsync(1, Guid.NewGuid());

        // Assert
        result.Success.Should().BeTrue();
        saleReturn.ReturnStatus.Should().Be(ReturnStatus.Approved);
        _mockOfficeStockManager.Verify(s => s.IncreaseStockAtomicAsync(
            1, variantId, 2, StockMovementType.Return, It.IsAny<string>(), null), Times.Once);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveReturnAsync_AlreadyProcessed_ReturnsError()
    {
        // Arrange
        var saleReturn = new SaleReturn
        {
            Id = 1,
            ReturnStatus = ReturnStatus.Approved,
            Sale = new Sale { SaleNumber = "S202601010001", BranchOfficeId = 1, SaleItems = new List<SaleItem>() },
            Items = new List<SaleReturnItem>()
        };
        SetupForApproveReject(new List<SaleReturn> { saleReturn });

        // Act
        var result = await _sut.ApproveReturnAsync(1, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten işlenmiş");
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // RejectReturnAsync Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RejectReturnAsync_RollsBackReturnedQuantity()
    {
        // Arrange
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            Quantity = 3,
            ReturnedQuantity = 2 // was set to 2 when return was created
        };
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = "S202601010001",
            BranchOfficeId = 1,
            SaleStatus = SaleStatus.PartialReturn,
            SaleItems = new List<SaleItem> { saleItem }
        };
        var saleReturn = new SaleReturn
        {
            Id = 1,
            Sale = sale,
            SaleId = sale.Id,
            ReturnStatus = ReturnStatus.Pending,
            Items = new List<SaleReturnItem>
            {
                new() { Id = 1, SaleItem = saleItem, SaleItemId = saleItem.Id, Quantity = 2 }
            }
        };
        SetupForApproveReject(new List<SaleReturn> { saleReturn });

        // Act
        var result = await _sut.RejectReturnAsync(1, Guid.NewGuid(), "Geçersiz talep");

        // Assert
        result.Success.Should().BeTrue();
        saleReturn.ReturnStatus.Should().Be(ReturnStatus.Rejected);
        saleItem.ReturnedQuantity.Should().Be(0); // rolled back from 2 to 0
        sale.SaleStatus.Should().Be(SaleStatus.Completed); // no more returns
        _mockOfficeStockManager.Verify(s => s.IncreaseStockAtomicAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
