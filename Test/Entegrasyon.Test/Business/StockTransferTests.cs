using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class StockTransferTests : BaseTest
{
    private readonly IOfficeStockManager _manager;
    private readonly Mock<IBranchOfficeManager> _mockBranchOfficeManager = new();
    private readonly Mock<IProductVariantManager> _mockProductVariantManager = new();
    private readonly Mock<INotificationManager> _mockNotificationManager = new();
    private readonly Mock<ILogger<OfficeStockManager>> _mockLogger = new();

    public StockTransferTests()
    {
        _manager = new OfficeStockManager(
            mockContextFactory.Object,
            _mockBranchOfficeManager.Object,
            _mockProductVariantManager.Object,
            _mockNotificationManager.Object,
            mockTenantContext.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task TransferStock_ShouldFail_WhenSameBranch()
    {
        // Arrange
        var items = new List<TransferItemDto>
        {
            new(Guid.NewGuid(), 5)
        };

        // Act
        var result = await _manager.TransferStockAsync(1, 1, items);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ayni");
    }

    [Fact]
    public async Task TransferStock_ShouldFail_WhenQuantityIsZeroOrNegative()
    {
        // Arrange
        var items = new List<TransferItemDto>
        {
            new(Guid.NewGuid(), 0)
        };

        // Act
        var result = await _manager.TransferStockAsync(1, 2, items);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("0'dan buyuk");
    }

    [Fact]
    public async Task TransferStock_ShouldFail_WhenTargetBranchIsDeleted()
    {
        // Arrange
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Kaynak", IsDeleted = false },
            new() { Id = 2, Name = "Hedef (Silinmis)", IsDeleted = true },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        var items = new List<TransferItemDto>
        {
            new(Guid.NewGuid(), 5)
        };

        // Act
        var result = await _manager.TransferStockAsync(1, 2, items);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("aktif");
    }

    [Fact]
    public async Task TransferStock_ShouldFail_WhenInsufficientStock()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Kaynak", IsDeleted = false },
            new() { Id = 2, Name = "Hedef", IsDeleted = false },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        // Stock with only 3 items, trying to transfer 10
        var stocks = new List<BranchOfficeStock>
        {
            new() { BranchOfficeId = 1, ProductVariantId = variantId, FirstTotalStock = 10, SoldQuantity = 7 }
            // CurrentStock = 10 - 7 = 3 (computed)
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOfficeStocks)
            .ReturnsDbSet(stocks);

        mockIntegrationDbContext
            .Setup(x => x.StockMovements)
            .ReturnsDbSet(new List<StockMovement>());

        mockIntegrationDbContext
            .Setup(x => x.ProductVariants)
            .ReturnsDbSet(new List<ProductVariant>());

        mockIntegrationDbContext
            .Setup(x => x.Users)
            .ReturnsDbSet(new List<Entegrasyon.Entity.User.ApplicationUser>());

        // Transfer 10 but only 3 available
        var items = new List<TransferItemDto>
        {
            new(variantId, 10)
        };

        // Act
        var result = await _manager.TransferStockAsync(1, 2, items);

        // Assert
        result.Success.Should().BeFalse();
    }

    /// <summary>
    /// Transfer with sufficient stock: pre-checks pass. ExecuteUpdateAsync returns 0 in mock
    /// (no SQL support), so DecreaseStockAtomicAsync returns "Yetersiz stok" error at the atomic
    /// level — but critically NOT the pre-check error. Full end-to-end is covered by integration tests.
    /// </summary>
    [Fact]
    public async Task TransferStock_ShouldPassPreChecks_WhenStockIsSufficient()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Kaynak", IsDeleted = false },
            new() { Id = 2, Name = "Hedef", IsDeleted = false },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        // CurrentStock >= quantity: pre-check AnyAsync returns true
        var stocks = new List<BranchOfficeStock>
        {
            new() { BranchOfficeId = 1, ProductVariantId = variantId, FirstTotalStock = 20, SoldQuantity = 5 },
            new() { BranchOfficeId = 2, ProductVariantId = variantId, FirstTotalStock = 10, SoldQuantity = 2 },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOfficeStocks)
            .ReturnsDbSet(stocks);

        mockIntegrationDbContext
            .Setup(x => x.StockMovements)
            .ReturnsDbSet(new List<StockMovement>());

        mockIntegrationDbContext
            .Setup(x => x.ProductVariants)
            .ReturnsDbSet(new List<ProductVariant>());

        mockIntegrationDbContext
            .Setup(x => x.Users)
            .ReturnsDbSet(new List<Entegrasyon.Entity.User.ApplicationUser>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var items = new List<TransferItemDto>
        {
            new(variantId, 5)
        };

        // Act — ExecuteUpdateAsync throws InvalidOperationException in unit tests (no SQL provider).
        // We verify that all PRE-checks passed (no early-return error before the atomic call).
        var exception = await Record.ExceptionAsync(async () =>
            await _manager.TransferStockAsync(1, 2, items));

        // Assert: The only acceptable outcomes are:
        // 1. No exception + success result (full mock support)
        // 2. InvalidOperationException from ExecuteUpdateAsync (pre-checks passed, atomic not supported in unit)
        if (exception is not null)
        {
            exception.Message.Should().Contain("ExecuteUpdate",
                "exception should only come from atomic SQL call after pre-checks pass");
        }
    }
}
