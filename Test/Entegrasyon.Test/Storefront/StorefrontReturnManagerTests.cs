using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontReturnManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontReturnManager _sut;

    public StorefrontReturnManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontReturnManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task CreateReturnRequestAsync_EmptyReason_ReturnsError()
    {
        // Act
        var result = await _sut.CreateReturnRequestAsync(1, Guid.NewGuid(), 1, "", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task CreateReturnRequestAsync_OrderNotFound_ReturnsError()
    {
        // Arrange
        var orders = new List<Order>();
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);

        // Act
        var result = await _sut.CreateReturnRequestAsync(1, Guid.NewGuid(), 1, "Urun kusurlu", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task CreateReturnRequestAsync_OrderNotDelivered_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new() { Id = orderId, CustomerId = 1, StorefrontOrderStatus = OrderStatus.Shipped }
        };
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);

        // Act
        var result = await _sut.CreateReturnRequestAsync(1, orderId, 1, "Urun kusurlu", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("teslim edilmis");
    }

    [Fact]
    public async Task CreateReturnRequestAsync_WrongCustomer_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new() { Id = orderId, CustomerId = 999, StorefrontOrderStatus = OrderStatus.Delivered }
        };
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);

        // Act
        var result = await _sut.CreateReturnRequestAsync(1, orderId, 1, "Urun kusurlu", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task CreateReturnRequestAsync_AlreadyRequested_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new() { Id = orderId, CustomerId = 1, StorefrontOrderStatus = OrderStatus.Delivered }
        };
        var returns = new List<StorefrontReturnRequest>
        {
            new() { Id = 1, TenantId = 1, OrderId = orderId, CustomerId = 1, Reason = "Test" }
        };
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);
        _mockDbContext.Setup(x => x.StorefrontReturnRequests).ReturnsDbSet(returns);

        // Act
        var result = await _sut.CreateReturnRequestAsync(1, orderId, 1, "Urun kusurlu", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten");
    }

    [Fact]
    public async Task CreateReturnRequestAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new() { Id = orderId, CustomerId = 1, StorefrontOrderStatus = OrderStatus.Delivered }
        };
        var returns = new List<StorefrontReturnRequest>();
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(orders);
        _mockDbContext.Setup(x => x.StorefrontReturnRequests).ReturnsDbSet(returns);

        // Act
        var result = await _sut.CreateReturnRequestAsync(1, orderId, 1, "Urun kusurlu", "Aciklama");

        // Assert
        result.Success.Should().BeTrue();
        _mockDbContext.Verify(x => x.StorefrontReturnRequests.Add(It.Is<StorefrontReturnRequest>(
            r => r.Reason == "Urun kusurlu" && r.Status == ReturnStatus.Pending)), Times.Once);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCustomerReturnsAsync_ReturnsCustomerReturns()
    {
        // Arrange
        var returns = new List<StorefrontReturnRequest>
        {
            new() { Id = 1, TenantId = 1, CustomerId = 1, Reason = "Test1" },
            new() { Id = 2, TenantId = 1, CustomerId = 2, Reason = "Test2" },
            new() { Id = 3, TenantId = 1, CustomerId = 1, Reason = "Test3" },
        };
        _mockDbContext.Setup(x => x.StorefrontReturnRequests).ReturnsDbSet(returns);

        // Act
        var result = await _sut.GetCustomerReturnsAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(r => r.CustomerId == 1);
    }

    [Fact]
    public async Task GetAllReturnsAsync_ReturnsAllTenantReturns()
    {
        // Arrange
        var returns = new List<StorefrontReturnRequest>
        {
            new() { Id = 1, TenantId = 1, Reason = "Test1" },
            new() { Id = 2, TenantId = 2, Reason = "Test2" },
            new() { Id = 3, TenantId = 1, Reason = "Test3" },
        };
        _mockDbContext.Setup(x => x.StorefrontReturnRequests).ReturnsDbSet(returns);

        // Act
        var result = await _sut.GetAllReturnsAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateReturnStatusAsync_NotFound_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontReturnRequests.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((StorefrontReturnRequest?)null);

        // Act
        var result = await _sut.UpdateReturnStatusAsync(999, ReturnStatus.Approved, "Note", 100m);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task UpdateReturnStatusAsync_Approve_ReturnsSuccess()
    {
        // Arrange
        var returnRequest = new StorefrontReturnRequest
        {
            Id = 1, TenantId = 1, Status = ReturnStatus.Pending, Reason = "Test"
        };
        _mockDbContext.Setup(x => x.StorefrontReturnRequests.FindAsync(1))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _sut.UpdateReturnStatusAsync(1, ReturnStatus.Approved, "Onaylandi", 150m);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("onaylandi");
        returnRequest.Status.Should().Be(ReturnStatus.Approved);
        returnRequest.ReviewNote.Should().Be("Onaylandi");
        returnRequest.RefundAmount.Should().Be(150m);
        returnRequest.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateReturnStatusAsync_Reject_ReturnsSuccess()
    {
        // Arrange
        var returnRequest = new StorefrontReturnRequest
        {
            Id = 1, TenantId = 1, Status = ReturnStatus.Pending, Reason = "Test"
        };
        _mockDbContext.Setup(x => x.StorefrontReturnRequests.FindAsync(1))
            .ReturnsAsync(returnRequest);

        // Act
        var result = await _sut.UpdateReturnStatusAsync(1, ReturnStatus.Rejected, "Reddedildi", null);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("reddedildi");
        returnRequest.Status.Should().Be(ReturnStatus.Rejected);
    }
}
