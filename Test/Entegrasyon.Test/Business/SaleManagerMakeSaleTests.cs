using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class SaleManagerMakeSaleTests : BaseTest
{
    private readonly SaleManager _sut;
    private readonly Mock<IOfficeStockManager> _mockOfficeStockManager;
    private readonly Mock<ILogger<SaleManager>> _mockLogger;
    private readonly SaleMapper _mapper;

    public SaleManagerMakeSaleTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<MakeSaleDto>()))
            .Returns(Task.CompletedTask);

        _mockOfficeStockManager = new Mock<IOfficeStockManager>();
        _mockOfficeStockManager
            .Setup(s => s.DecreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        _mockOfficeStockManager
            .Setup(s => s.IncreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        _mockLogger = new Mock<ILogger<SaleManager>>();
        _mapper = new SaleMapper();

        _sut = new SaleManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            _mockLogger.Object,
            _mapper,
            MockValidator.Object,
            _mockOfficeStockManager.Object);
    }

    private void SetupDbContextForMakeSale(List<Sale> existingSales, List<ProductVariant>? variants = null)
    {
        variants ??= [];

        mockIntegrationDbContext
            .Setup(x => x.Sales)
            .ReturnsDbSet(existingSales);

        mockIntegrationDbContext
            .Setup(x => x.ProductVariants)
            .ReturnsDbSet(variants);

        var capturedSales = new List<Sale>();
        mockIntegrationDbContext
            .Setup(x => x.Sales.Add(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSales.Add(s));

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static MakeSaleDto BuildDto(List<SalePaymentDto>? payments = null, List<SaleItemDto>? items = null)
    {
        var variantId = Guid.NewGuid();
        return new MakeSaleDto(
            SalePersonId: Guid.NewGuid(),
            CustomerId: 1,
            GeneralDiscount: 0,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: items ?? [new SaleItemDto(variantId, 18, 0, 100m, 2, "")],
            Payments: payments ?? [new SalePaymentDto(1, 200m, null, null)]);
    }

    [Fact]
    public async Task MakeSale_WithSplitPayment_CreatesSaleWithMultiplePayments()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var items = new List<SaleItemDto> { new(variantId, 18, 0, 100m, 1, "") };
        var payments = new List<SalePaymentDto>
        {
            new(1, 50m, null, null),
            new(2, 50m, null, null)
        };
        var dto = new MakeSaleDto(Guid.NewGuid(), 1, 0, 1, SaleSource.POS, null, items, payments);

        Sale? capturedSale = null;
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());
        mockIntegrationDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant>());
        mockIntegrationDbContext.Setup(x => x.Sales.Add(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSale = s);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.MakeSale(dto);

        // Assert
        result.Success.Should().BeTrue();
        capturedSale.Should().NotBeNull();
        capturedSale!.Payments.Should().HaveCount(2);
        capturedSale.Payments.Sum(p => p.Amount).Should().Be(100m);
        capturedSale.Payments.Should().Contain(p => p.PaymentMethodId == 1 && p.Amount == 50m);
        capturedSale.Payments.Should().Contain(p => p.PaymentMethodId == 2 && p.Amount == 50m);
    }

    [Fact]
    public async Task MakeSale_CalculatesCorrectChangeGiven_ForCashPayment()
    {
        // Arrange — müşteri 200 veriyor, toplam 180, üstü 20 olmalı
        var variantId = Guid.NewGuid();
        var items = new List<SaleItemDto> { new(variantId, 0, 0, 180m, 1, "") };
        var payments = new List<SalePaymentDto> { new(1, 180m, 200m, null) };
        var dto = new MakeSaleDto(Guid.NewGuid(), 1, 0, 1, SaleSource.POS, null, items, payments);

        Sale? capturedSale = null;
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());
        mockIntegrationDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant>());
        mockIntegrationDbContext.Setup(x => x.Sales.Add(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSale = s);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.MakeSale(dto);

        // Assert
        result.Success.Should().BeTrue();
        capturedSale!.Payments.First().ChangeGiven.Should().Be(20m);
    }

    [Fact]
    public async Task MakeSale_GeneratesSequentialSaleNumbers()
    {
        // Arrange — simulate first sale of the day
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prefix = $"S{today:yyyyMMdd}";

        Sale? firstCapture = null;
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());
        mockIntegrationDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant>());
        mockIntegrationDbContext.Setup(x => x.Sales.Add(It.IsAny<Sale>()))
            .Callback<Sale>(s => firstCapture = s);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.MakeSale(BuildDto());

        // Assert
        result.Success.Should().BeTrue();
        firstCapture!.SaleNumber.Should().Be($"{prefix}0001");
    }

    [Fact]
    public async Task MakeSale_GeneratesIncrementedSaleNumber_WhenPreviousSaleExists()
    {
        // Arrange — bir önceki satış bu günün ilk satışı olmuş
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prefix = $"S{today:yyyyMMdd}";
        var existingSales = new List<Sale>
        {
            new() { Id = Guid.NewGuid(), SaleNumber = $"{prefix}0001", SaleDate = DateTimeOffset.UtcNow }
        };

        Sale? capturedSale = null;
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(existingSales);
        mockIntegrationDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant>());
        mockIntegrationDbContext.Setup(x => x.Sales.Add(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSale = s);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.MakeSale(BuildDto());

        // Assert
        result.Success.Should().BeTrue();
        capturedSale!.SaleNumber.Should().Be($"{prefix}0002");
    }

    [Fact]
    public async Task MakeSale_SetsSaleStatusCompleted()
    {
        // Arrange
        Sale? capturedSale = null;
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());
        mockIntegrationDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant>());
        mockIntegrationDbContext.Setup(x => x.Sales.Add(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSale = s);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.MakeSale(BuildDto());

        // Assert
        result.Success.Should().BeTrue();
        capturedSale!.SaleStatus.Should().Be(SaleStatus.Completed);
    }

    [Fact]
    public async Task MakeSale_StockDecreaseFailure_ReturnsError()
    {
        // Arrange
        _mockOfficeStockManager
            .Setup(s => s.DecreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ErrorDataResult<StockMovement>(null!, "Yetersiz stok."));

        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());
        mockIntegrationDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant>());

        // Act
        var result = await _sut.MakeSale(BuildDto());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Yetersiz stok");
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSaleDetailAsync_NotFound_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());

        // Act
        var result = await _sut.GetSaleDetailAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task CancelSaleAsync_AlreadyCancelled_ReturnsError()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new()
            {
                Id = saleId,
                SaleNumber = "S202601010001",
                SaleDate = DateTimeOffset.UtcNow,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Cancelled,
                SaleItems = new List<SaleItem>()
            }
        };
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);

        // Act
        var result = await _sut.CancelSaleAsync(saleId, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten iptal");
    }

    [Fact]
    public async Task CancelSaleAsync_NotToday_ReturnsError()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new()
            {
                Id = saleId,
                SaleNumber = "S202501010001",
                SaleDate = DateTimeOffset.UtcNow.AddDays(-1), // dünkü satış
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Completed,
                SaleItems = new List<SaleItem>()
            }
        };
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);

        // Act
        var result = await _sut.CancelSaleAsync(saleId, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bugünkü");
    }

    [Fact]
    public async Task CancelSaleAsync_NotFound_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());

        // Act
        var result = await _sut.CancelSaleAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task CancelSaleAsync_TodaySale_CancelsSaleAndRestoresStock()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var sale = new Sale
        {
            Id = saleId,
            SaleNumber = $"S{DateTime.UtcNow:yyyyMMdd}0001",
            SaleDate = DateTimeOffset.UtcNow,
            BranchOfficeId = 1,
            SaleStatus = SaleStatus.Completed,
            SaleItems = new List<SaleItem>
            {
                new() { Id = Guid.NewGuid(), ProductVariantId = variantId, Quantity = 3 }
            }
        };
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale> { sale });
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.CancelSaleAsync(saleId, Guid.NewGuid());

        // Assert
        result.Success.Should().BeTrue();
        sale.SaleStatus.Should().Be(SaleStatus.Cancelled);
        _mockOfficeStockManager.Verify(s => s.IncreaseStockAtomicAsync(
            1, variantId, 3, StockMovementType.Return, "SaleCancellation", null), Times.Once);
    }
}
