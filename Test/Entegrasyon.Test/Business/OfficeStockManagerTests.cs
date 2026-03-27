using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Business;

public class OfficeStockManagerTests : BaseTest
{
    private readonly IOfficeStockManager _manager;
    private readonly Mock<IBranchOfficeManager> _mockBranchOfficeManager = new();
    private readonly Mock<IProductVariantManager> _mockProductVariantManager = new();
    private readonly Mock<INotificationManager> _mockNotificationManager = new();
    private readonly Mock<ILogger<OfficeStockManager>> _mockLogger = new();

    public OfficeStockManagerTests()
    {
        _manager = new OfficeStockManager(
            mockContextFactory.Object,
            _mockBranchOfficeManager.Object,
            _mockProductVariantManager.Object,
            _mockNotificationManager.Object,
            new EventChannel<StockPriceChangedEvent>(),
            mockTenantContext.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void CheckIfProductCountZero_ShouldReturnError_WhenAllStocksAreZero()
    {
        // Arrange
        var stocks = new[]
        {
            new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 0 },
            new AddBranchOfficeStockDto { BranchOfficeId = 2, FirstTotalStock = 0 },
        };

        // Act
        var result = _manager.CheckIfProductCountZero(stocks);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Lütfen en az bir stok girin.");
    }

    [Fact]
    public void CheckIfProductCountZero_ShouldReturnSuccess_WhenAtLeastOneStockIsNonZero()
    {
        // Arrange
        var stocks = new[]
        {
            new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 0 },
            new AddBranchOfficeStockDto { BranchOfficeId = 2, FirstTotalStock = 10 },
        };

        // Act
        var result = _manager.CheckIfProductCountZero(stocks);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void CheckIfProductCountZero_ShouldReturnSuccess_WhenInputIsNull()
    {
        // Act
        var result = _manager.CheckIfProductCountZero(null!);

        // Assert
        result.Success.Should().BeTrue();
    }
}
