using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.DiscountVouchers;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontCouponManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontCouponManager _sut;

    public StorefrontCouponManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontCouponManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task ValidateCouponAsync_ValidPercentCoupon_ReturnsDiscount()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 1,
            Code = "TEST10",
            IsActive = true,
            Percentage = 10,
            Amount = 0,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("TEST10", 200m, null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.DiscountAmount.Should().Be(20m);
        result.Data.Code.Should().Be("TEST10");
    }

    [Fact]
    public async Task ValidateCouponAsync_ValidFixedAmountCoupon_ReturnsDiscount()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 2,
            Code = "FLAT50",
            IsActive = true,
            Percentage = 0,
            Amount = 50m,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("FLAT50", 200m, null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.DiscountAmount.Should().Be(50m);
    }

    [Fact]
    public async Task ValidateCouponAsync_FixedAmountExceedsCart_CapsToCartTotal()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 3,
            Code = "BIG100",
            IsActive = true,
            Percentage = 0,
            Amount = 100m,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("BIG100", 30m, null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.DiscountAmount.Should().Be(30m);
    }

    [Fact]
    public async Task ValidateCouponAsync_ExpiredCoupon_ReturnsError()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 4,
            Code = "EXPIRED",
            IsActive = true,
            Percentage = 10,
            Amount = 0,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(-1)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("EXPIRED", 200m, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("suresi dolmus");
    }

    [Fact]
    public async Task ValidateCouponAsync_InactiveCoupon_ReturnsError()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 5,
            Code = "INACTIVE",
            IsActive = false,
            Percentage = 10,
            Amount = 0,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("INACTIVE", 200m, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("gecerli degil");
    }

    [Fact]
    public async Task ValidateCouponAsync_NonExistentCode_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher>());

        // Act
        var result = await _sut.ValidateCouponAsync("NOTEXIST", 200m, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Gecersiz kupon kodu");
    }

    [Fact]
    public async Task ValidateCouponAsync_EmptyCode_ReturnsError()
    {
        // Act
        var result = await _sut.ValidateCouponAsync("", 200m, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos olamaz");
    }

    [Fact]
    public async Task ValidateCouponAsync_CustomerSpecificCoupon_WrongCustomer_ReturnsError()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 6,
            Code = "VIP10",
            IsActive = true,
            Percentage = 10,
            Amount = 0,
            CustomerId = 42,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("VIP10", 200m, customerId: 99);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("hesabiniza tanimli degil");
    }

    [Fact]
    public async Task ValidateCouponAsync_CustomerSpecificCoupon_CorrectCustomer_ReturnsDiscount()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 7,
            Code = "VIP10",
            IsActive = true,
            Percentage = 10,
            Amount = 0,
            CustomerId = 42,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("VIP10", 200m, customerId: 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.DiscountAmount.Should().Be(20m);
    }

    [Fact]
    public async Task ValidateCouponAsync_NoPercentageNoAmount_ReturnsError()
    {
        // Arrange
        var voucher = new DiscountVoucher
        {
            Id = 8,
            Code = "ZERO",
            IsActive = true,
            Percentage = 0,
            Amount = 0,
            ExpiringDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _mockDbContext.Setup(x => x.DiscountVouchers).ReturnsDbSet(new List<DiscountVoucher> { voucher });

        // Act
        var result = await _sut.ValidateCouponAsync("ZERO", 200m, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("gecerli bir indirim icermiyor");
    }
}
