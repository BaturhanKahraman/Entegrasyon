using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class MarketplaceFaz4Tests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory = new();
    private readonly Mock<IntegrationDbContext> _mockDbContext;

    public MarketplaceFaz4Tests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
    }

    // --- Commission Tests ---

    [Fact]
    public async Task GetCommissionRateAsync_CategoryOverride_ReturnsCategoryRate()
    {
        // Arrange
        var commissions = new List<SellerCommission>
        {
            new() { SellerId = 1, CategoryId = 10, CommissionRate = 15 }
        };
        _mockDbContext.Setup(x => x.SellerCommissions).ReturnsDbSet(commissions);

        var manager = new SellerCommissionManager(_mockContextFactory.Object);

        // Act
        var rate = await manager.GetCommissionRateAsync(1, 10);

        // Assert
        rate.Should().Be(15);
    }

    [Fact]
    public async Task GetCommissionRateAsync_NoCategoryOverride_FallsBackToSellerDefault()
    {
        // Arrange
        _mockDbContext.Setup(x => x.SellerCommissions).ReturnsDbSet(new List<SellerCommission>());
        var seller = new Seller
        {
            Id = 1, DefaultCommissionRate = 12,
            StoreName = "S", CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);

        var manager = new SellerCommissionManager(_mockContextFactory.Object);

        // Act
        var rate = await manager.GetCommissionRateAsync(1, null);

        // Assert
        rate.Should().Be(12);
    }

    [Fact]
    public async Task GetCommissionRateAsync_NoSellerFound_Returns10AsDefault()
    {
        // Arrange
        _mockDbContext.Setup(x => x.SellerCommissions).ReturnsDbSet(new List<SellerCommission>());
        _mockDbContext.Setup(x => x.Sellers.FindAsync(999)).ReturnsAsync((Seller?)null);

        var manager = new SellerCommissionManager(_mockContextFactory.Object);

        // Act
        var rate = await manager.GetCommissionRateAsync(999, null);

        // Assert
        rate.Should().Be(10);
    }

    // --- Payout Tests ---

    [Fact]
    public async Task RequestPayoutAsync_ValidAmount_CreatesRequest()
    {
        // Arrange
        var balance = new SellerBalance
        {
            SellerId = 1, CurrentBalance = 1000, PendingAmount = 0
        };
        var seller = new Seller
        {
            Id = 1, Iban = "TR123",
            StoreName = "S", CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance> { balance });
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);
        _mockDbContext.Setup(x => x.PayoutRequests).ReturnsDbSet(new List<PayoutRequest>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerPayoutManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RequestPayoutAsync(1, 500);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RequestPayoutAsync_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var balance = new SellerBalance
        {
            SellerId = 1, CurrentBalance = 100, PendingAmount = 50
        };
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance> { balance });

        var manager = new SellerPayoutManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RequestPayoutAsync(1, 200);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RequestPayoutAsync_ZeroAmount_ReturnsError()
    {
        // Arrange
        var manager = new SellerPayoutManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RequestPayoutAsync(1, 0);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessPayoutAsync_Approve_UpdatesBalanceCorrectly()
    {
        // Arrange
        var payout = new PayoutRequest
        {
            Id = 1, SellerId = 1, Amount = 500, Status = PayoutStatus.Pending, Iban = "TR123"
        };
        var balance = new SellerBalance
        {
            SellerId = 1, CurrentBalance = 1000, PendingAmount = 500, TotalPaidOut = 0
        };
        _mockDbContext.Setup(x => x.PayoutRequests).ReturnsDbSet(new List<PayoutRequest> { payout });
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance> { balance });
        _mockDbContext.Setup(x => x.SellerTransactions).ReturnsDbSet(new List<SellerTransaction>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerPayoutManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ProcessPayoutAsync(1, approve: true, note: null);

        // Assert
        result.Success.Should().BeTrue();
        payout.Status.Should().Be(PayoutStatus.Completed);
        balance.CurrentBalance.Should().Be(500);
        balance.TotalPaidOut.Should().Be(500);
        balance.PendingAmount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessPayoutAsync_Reject_ReleasesPendingAmount()
    {
        // Arrange
        var payout = new PayoutRequest
        {
            Id = 1, SellerId = 1, Amount = 300, Status = PayoutStatus.Pending, Iban = "TR123"
        };
        var balance = new SellerBalance
        {
            SellerId = 1, CurrentBalance = 1000, PendingAmount = 300
        };
        _mockDbContext.Setup(x => x.PayoutRequests).ReturnsDbSet(new List<PayoutRequest> { payout });
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance> { balance });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerPayoutManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ProcessPayoutAsync(1, approve: false, note: "Reddedildi");

        // Assert
        result.Success.Should().BeTrue();
        payout.Status.Should().Be(PayoutStatus.Rejected);
        balance.PendingAmount.Should().Be(0);
        balance.CurrentBalance.Should().Be(1000); // not changed
    }

    [Fact]
    public async Task ProcessPayoutAsync_AlreadyProcessed_ReturnsError()
    {
        // Arrange
        var payout = new PayoutRequest
        {
            Id = 1, SellerId = 1, Amount = 500, Status = PayoutStatus.Completed, Iban = "TR123"
        };
        _mockDbContext.Setup(x => x.PayoutRequests).ReturnsDbSet(new List<PayoutRequest> { payout });

        var manager = new SellerPayoutManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ProcessPayoutAsync(1, approve: true, note: null);

        // Assert
        result.Success.Should().BeFalse();
    }

    // --- Seller Order Tests ---

    [Fact]
    public async Task GetSellerOrdersAsync_ReturnsOnlySellerOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new()
            {
                Id = Guid.NewGuid(), OrderNumber = "SF-001",
                OrderItems = new List<OrderItem>
                {
                    new() { SellerId = 1, Quantity = 1, UnitPrice = 100 }
                }
            },
            new()
            {
                Id = Guid.NewGuid(), OrderNumber = "SF-002",
                OrderItems = new List<OrderItem>
                {
                    new() { SellerId = 2, Quantity = 1, UnitPrice = 200 }
                }
            }
        };
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);

        var manager = new SellerOrderManager(_mockContextFactory.Object);

        // Act
        var result = await manager.GetSellerOrdersAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].OrderNumber.Should().Be("SF-001");
    }

    [Fact]
    public async Task GetSellerOrderDetailAsync_WrongSeller_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new()
            {
                Id = orderId,
                OrderItems = new List<OrderItem>
                {
                    new() { SellerId = 2, Quantity = 1, UnitPrice = 100 }
                }
            }
        };
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);

        var manager = new SellerOrderManager(_mockContextFactory.Object);

        // Act
        var result = await manager.GetSellerOrderDetailAsync(1, orderId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetSellerOrderDetailAsync_ValidSeller_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new()
            {
                Id = orderId,
                OrderNumber = "SF-TEST",
                OrderItems = new List<OrderItem>
                {
                    new() { SellerId = 1, Quantity = 2, UnitPrice = 50 }
                }
            }
        };
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);

        var manager = new SellerOrderManager(_mockContextFactory.Object);

        // Act
        var result = await manager.GetSellerOrderDetailAsync(1, orderId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.OrderNumber.Should().Be("SF-TEST");
    }

    [Fact]
    public async Task UpdateSellerOrderStatusAsync_WrongSeller_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new()
            {
                Id = orderId,
                OrderItems = new List<OrderItem>
                {
                    new() { SellerId = 2, Quantity = 1, UnitPrice = 100 }
                }
            }
        };
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);

        var manager = new SellerOrderManager(_mockContextFactory.Object);

        // Act
        var result = await manager.UpdateSellerOrderStatusAsync(1, orderId, "Shipped");

        // Assert
        result.Success.Should().BeFalse();
    }

    // --- MarketplaceEnabled Tests ---

    [Fact]
    public void StorefrontSettings_MarketplaceEnabled_DefaultIsFalse()
    {
        // Arrange & Act
        var settings = new StorefrontSettings();

        // Assert
        settings.MarketplaceEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task RecordSaleCommissionAsync_ValidData_UpdatesBalance()
    {
        // Arrange
        var balance = new SellerBalance
        {
            SellerId = 1, CurrentBalance = 0, TotalEarned = 0
        };
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance> { balance });
        _mockDbContext.Setup(x => x.SellerTransactions).ReturnsDbSet(new List<SellerTransaction>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerCommissionManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RecordSaleCommissionAsync(1, Guid.NewGuid(), 1000m, 10m);

        // Assert
        result.Success.Should().BeTrue();
        balance.CurrentBalance.Should().Be(900m); // 1000 - 10% = 900
        balance.TotalEarned.Should().Be(900m);
    }

    [Fact]
    public async Task RecordSaleCommissionAsync_NoBalance_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance>());

        var manager = new SellerCommissionManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RecordSaleCommissionAsync(999, Guid.NewGuid(), 1000m, 10m);

        // Assert
        result.Success.Should().BeFalse();
    }
}
