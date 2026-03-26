using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontGiftCardManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontGiftCardManager _sut;

    public StorefrontGiftCardManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontGiftCardManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task CreateGiftCardAsync_ValidAmount_ReturnsSuccess()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>();
        var transactions = new List<StorefrontGiftCardTransaction>();
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);
        _mockDbContext.Setup(x => x.StorefrontGiftCardTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.CreateGiftCardAsync(1, 100m, 42, "test@test.com", "Ali", "Iyi ki varsin!");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Code.Should().HaveLength(16);
        result.Data.InitialAmount.Should().Be(100m);
        result.Data.RemainingAmount.Should().Be(100m);
        result.Data.Status.Should().Be(GiftCardStatus.Active);
    }

    [Fact]
    public async Task CreateGiftCardAsync_ZeroAmount_ReturnsError()
    {
        // Act
        var result = await _sut.CreateGiftCardAsync(1, 0, null, null, null, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("sifirdan buyuk");
    }

    [Fact]
    public async Task CreateGiftCardAsync_NegativeAmount_ReturnsError()
    {
        // Act
        var result = await _sut.CreateGiftCardAsync(1, -50, null, null, null, null);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UseGiftCardAsync_SufficientBalance_DeductsAndReturnsRemaining()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>
        {
            new()
            {
                Id = 1, TenantId = 1, Code = "ABCDEFGHIJKLMNOP",
                InitialAmount = 200, RemainingAmount = 200,
                Status = GiftCardStatus.Active,
                ExpiresAt = DateTimeOffset.UtcNow.AddMonths(6)
            }
        };
        var transactions = new List<StorefrontGiftCardTransaction>();
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);
        _mockDbContext.Setup(x => x.StorefrontGiftCardTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.UseGiftCardAsync(1, "ABCDEFGHIJKLMNOP", 50m, Guid.NewGuid());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(150m);
    }

    [Fact]
    public async Task UseGiftCardAsync_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>
        {
            new()
            {
                Id = 1, TenantId = 1, Code = "ABCDEFGHIJKLMNOP",
                InitialAmount = 100, RemainingAmount = 30,
                Status = GiftCardStatus.Active,
                ExpiresAt = DateTimeOffset.UtcNow.AddMonths(6)
            }
        };
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);

        // Act
        var result = await _sut.UseGiftCardAsync(1, "ABCDEFGHIJKLMNOP", 50m, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("yetersiz");
    }

    [Fact]
    public async Task UseGiftCardAsync_ExpiredCard_ReturnsError()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>
        {
            new()
            {
                Id = 1, TenantId = 1, Code = "ABCDEFGHIJKLMNOP",
                InitialAmount = 100, RemainingAmount = 100,
                Status = GiftCardStatus.Active,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);

        // Act
        var result = await _sut.UseGiftCardAsync(1, "ABCDEFGHIJKLMNOP", 50m, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task CheckBalanceAsync_ActiveCard_ReturnsBalance()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>
        {
            new()
            {
                Id = 1, TenantId = 1, Code = "ABCDEFGHIJKLMNOP",
                InitialAmount = 200, RemainingAmount = 150,
                Status = GiftCardStatus.Active,
                ExpiresAt = DateTimeOffset.UtcNow.AddMonths(6)
            }
        };
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);

        // Act
        var result = await _sut.CheckBalanceAsync(1, "ABCDEFGHIJKLMNOP");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(150m);
    }

    [Fact]
    public async Task CheckBalanceAsync_EmptyCode_ReturnsError()
    {
        // Act
        var result = await _sut.CheckBalanceAsync(1, "");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task CheckBalanceAsync_NonExistentCard_ReturnsError()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>();
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);

        // Act
        var result = await _sut.CheckBalanceAsync(1, "NONEXISTENTCODE1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task CheckBalanceAsync_ExpiredCard_ReturnsError()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>
        {
            new()
            {
                Id = 1, TenantId = 1, Code = "ABCDEFGHIJKLMNOP",
                InitialAmount = 100, RemainingAmount = 100,
                Status = GiftCardStatus.Expired,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);

        // Act
        var result = await _sut.CheckBalanceAsync(1, "ABCDEFGHIJKLMNOP");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("suresi dolmus");
    }

    [Fact]
    public async Task UseGiftCardAsync_ExactBalance_SetsStatusToUsed()
    {
        // Arrange
        var giftCards = new List<StorefrontGiftCard>
        {
            new()
            {
                Id = 1, TenantId = 1, Code = "ABCDEFGHIJKLMNOP",
                InitialAmount = 100, RemainingAmount = 100,
                Status = GiftCardStatus.Active,
                ExpiresAt = DateTimeOffset.UtcNow.AddMonths(6)
            }
        };
        var transactions = new List<StorefrontGiftCardTransaction>();
        _mockDbContext.Setup(x => x.StorefrontGiftCards).ReturnsDbSet(giftCards);
        _mockDbContext.Setup(x => x.StorefrontGiftCardTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.UseGiftCardAsync(1, "ABCDEFGHIJKLMNOP", 100m, Guid.NewGuid());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(0m);
        giftCards[0].Status.Should().Be(GiftCardStatus.Used);
    }
}
