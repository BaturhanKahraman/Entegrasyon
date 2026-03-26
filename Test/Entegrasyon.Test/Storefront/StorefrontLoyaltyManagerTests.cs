using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontLoyaltyManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontLoyaltyManager _sut;

    public StorefrontLoyaltyManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontLoyaltyManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task GetBalanceAsync_NewCustomer_CreatesRecordWithZeroBalance()
    {
        // Arrange
        var loyaltyPoints = new List<StorefrontLoyaltyPoints>();
        _mockDbContext.Setup(x => x.StorefrontLoyaltyPoints).ReturnsDbSet(loyaltyPoints);

        // Act
        var result = await _sut.GetBalanceAsync(1, 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.CurrentBalance.Should().Be(0);
        result.Data.CustomerId.Should().Be(42);
    }

    [Fact]
    public async Task GetBalanceAsync_ExistingCustomer_ReturnsExistingBalance()
    {
        // Arrange
        var loyaltyPoints = new List<StorefrontLoyaltyPoints>
        {
            new() { Id = 1, TenantId = 1, CustomerId = 42, TotalEarned = 500, TotalSpent = 100, CurrentBalance = 400 }
        };
        _mockDbContext.Setup(x => x.StorefrontLoyaltyPoints).ReturnsDbSet(loyaltyPoints);

        // Act
        var result = await _sut.GetBalanceAsync(1, 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.CurrentBalance.Should().Be(400);
    }

    [Fact]
    public async Task EarnPointsAsync_ValidPoints_AddsToBalance()
    {
        // Arrange
        var existing = new StorefrontLoyaltyPoints
        {
            Id = 1, TenantId = 1, CustomerId = 42, TotalEarned = 100, TotalSpent = 0, CurrentBalance = 100
        };
        var loyaltyPoints = new List<StorefrontLoyaltyPoints> { existing };
        var transactions = new List<StorefrontLoyaltyTransaction>();
        _mockDbContext.Setup(x => x.StorefrontLoyaltyPoints).ReturnsDbSet(loyaltyPoints);
        _mockDbContext.Setup(x => x.StorefrontLoyaltyTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.EarnPointsAsync(1, 42, 50, "Purchase", null, "Alisveris puani");

        // Assert
        result.Success.Should().BeTrue();
        existing.TotalEarned.Should().Be(150);
        existing.CurrentBalance.Should().Be(150);
    }

    [Fact]
    public async Task EarnPointsAsync_ZeroPoints_ReturnsError()
    {
        // Act
        var result = await _sut.EarnPointsAsync(1, 42, 0, "Purchase", null, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("sifirdan buyuk");
    }

    [Fact]
    public async Task EarnPointsAsync_NegativePoints_ReturnsError()
    {
        // Act
        var result = await _sut.EarnPointsAsync(1, 42, -10, "Purchase", null, null);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemPointsAsync_SufficientBalance_DeductsPoints()
    {
        // Arrange
        var existing = new StorefrontLoyaltyPoints
        {
            Id = 1, TenantId = 1, CustomerId = 42, TotalEarned = 1000, TotalSpent = 0, CurrentBalance = 1000
        };
        var loyaltyPoints = new List<StorefrontLoyaltyPoints> { existing };
        var transactions = new List<StorefrontLoyaltyTransaction>();
        var settings = new List<StorefrontSettings>
        {
            new() { Id = 1, TenantId = 1, LoyaltyMinRedemption = 500, StoreName = "Test", CompanyName = "Test",
                     CompanyTaxOffice = "Test", CompanyTaxNumber = "123", ContactPhone = "555", ContactEmail = "t@t.com",
                     Address = "Test", City = "Test" }
        };
        _mockDbContext.Setup(x => x.StorefrontLoyaltyPoints).ReturnsDbSet(loyaltyPoints);
        _mockDbContext.Setup(x => x.StorefrontLoyaltyTransactions).ReturnsDbSet(transactions);
        _mockDbContext.Setup(x => x.StorefrontSettings).ReturnsDbSet(settings);

        // Act
        var result = await _sut.RedeemPointsAsync(1, 42, 500, null);

        // Assert
        result.Success.Should().BeTrue();
        existing.TotalSpent.Should().Be(500);
        existing.CurrentBalance.Should().Be(500);
    }

    [Fact]
    public async Task RedeemPointsAsync_BelowMinimum_ReturnsError()
    {
        // Arrange
        var existing = new StorefrontLoyaltyPoints
        {
            Id = 1, TenantId = 1, CustomerId = 42, TotalEarned = 1000, TotalSpent = 0, CurrentBalance = 1000
        };
        var loyaltyPoints = new List<StorefrontLoyaltyPoints> { existing };
        var settings = new List<StorefrontSettings>
        {
            new() { Id = 1, TenantId = 1, LoyaltyMinRedemption = 500, StoreName = "Test", CompanyName = "Test",
                     CompanyTaxOffice = "Test", CompanyTaxNumber = "123", ContactPhone = "555", ContactEmail = "t@t.com",
                     Address = "Test", City = "Test" }
        };
        _mockDbContext.Setup(x => x.StorefrontLoyaltyPoints).ReturnsDbSet(loyaltyPoints);
        _mockDbContext.Setup(x => x.StorefrontSettings).ReturnsDbSet(settings);

        // Act
        var result = await _sut.RedeemPointsAsync(1, 42, 100, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Minimum");
    }

    [Fact]
    public async Task RedeemPointsAsync_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var existing = new StorefrontLoyaltyPoints
        {
            Id = 1, TenantId = 1, CustomerId = 42, TotalEarned = 200, TotalSpent = 0, CurrentBalance = 200
        };
        var loyaltyPoints = new List<StorefrontLoyaltyPoints> { existing };
        var settings = new List<StorefrontSettings>
        {
            new() { Id = 1, TenantId = 1, LoyaltyMinRedemption = 100, StoreName = "Test", CompanyName = "Test",
                     CompanyTaxOffice = "Test", CompanyTaxNumber = "123", ContactPhone = "555", ContactEmail = "t@t.com",
                     Address = "Test", City = "Test" }
        };
        _mockDbContext.Setup(x => x.StorefrontLoyaltyPoints).ReturnsDbSet(loyaltyPoints);
        _mockDbContext.Setup(x => x.StorefrontSettings).ReturnsDbSet(settings);

        // Act
        var result = await _sut.RedeemPointsAsync(1, 42, 500, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Yetersiz");
    }

    [Fact]
    public async Task RedeemPointsAsync_ZeroPoints_ReturnsError()
    {
        // Act
        var result = await _sut.RedeemPointsAsync(1, 42, 0, null);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetTransactionsAsync_ReturnsOrderedByDate()
    {
        // Arrange
        var transactions = new List<StorefrontLoyaltyTransaction>
        {
            new() { Id = 1, TenantId = 1, CustomerId = 42, Points = 50, TransactionType = "Welcome", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { Id = 2, TenantId = 1, CustomerId = 42, Points = 100, TransactionType = "Purchase", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { Id = 3, TenantId = 1, CustomerId = 99, Points = 50, TransactionType = "Welcome", CreatedAt = DateTimeOffset.UtcNow }
        };
        _mockDbContext.Setup(x => x.StorefrontLoyaltyTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.GetTransactionsAsync(1, 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data[0].Points.Should().Be(100); // newer first
    }
}
