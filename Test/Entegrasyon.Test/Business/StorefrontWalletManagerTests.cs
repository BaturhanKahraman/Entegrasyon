using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.Entity.Storefront;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class StorefrontWalletManagerTests : BaseTest
{
    private readonly StorefrontWalletManager _sut;

    public StorefrontWalletManagerTests()
    {
        _sut = new StorefrontWalletManager(mockContextFactory.Object);
    }

    [Fact]
    public async Task GetOrCreateWalletAsync_CreatesWallet_WhenNotExists()
    {
        // Arrange
        IList<StorefrontWallet> wallets = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);

        // Act
        var result = await _sut.GetOrCreateWalletAsync(1, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Balance.Should().Be(0);
    }

    [Fact]
    public async Task GetOrCreateWalletAsync_ReturnsExisting_WhenExists()
    {
        // Arrange
        IList<StorefrontWallet> wallets = [new() { Id = 1, TenantId = 1, CustomerId = 10, Balance = 50 }];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);

        // Act
        var result = await _sut.GetOrCreateWalletAsync(1, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Balance.Should().Be(50);
    }

    [Fact]
    public async Task CreditAsync_WithValidAmount_ReturnsSuccess()
    {
        // Arrange
        var wallet = new StorefrontWallet { Id = 1, TenantId = 1, CustomerId = 10, Balance = 100 };
        IList<StorefrontWallet> wallets = [wallet];
        IList<StorefrontWalletTransaction> transactions = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);
        mockIntegrationDbContext.Setup(c => c.StorefrontWalletTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.CreditAsync(1, 10, 50, WalletTransactionType.Refund, null, "Iade");

        // Assert
        result.Success.Should().BeTrue();
        wallet.Balance.Should().Be(150);
    }

    [Fact]
    public async Task CreditAsync_WithZeroAmount_ReturnsError()
    {
        // Act
        var result = await _sut.CreditAsync(1, 10, 0, WalletTransactionType.Refund, null, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("sifirdan buyuk");
    }

    [Fact]
    public async Task CreditAsync_WithNegativeAmount_ReturnsError()
    {
        // Act
        var result = await _sut.CreditAsync(1, 10, -10, WalletTransactionType.Refund, null, null);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DebitAsync_WithSufficientBalance_ReturnsSuccess()
    {
        // Arrange
        var wallet = new StorefrontWallet { Id = 1, TenantId = 1, CustomerId = 10, Balance = 100 };
        IList<StorefrontWallet> wallets = [wallet];
        IList<StorefrontWalletTransaction> transactions = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);
        mockIntegrationDbContext.Setup(c => c.StorefrontWalletTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.DebitAsync(1, 10, 50, WalletTransactionType.OrderPayment, null, "Sipariş");

        // Assert
        result.Success.Should().BeTrue();
        wallet.Balance.Should().Be(50);
    }

    [Fact]
    public async Task DebitAsync_WithInsufficientBalance_ReturnsError()
    {
        // Arrange
        var wallet = new StorefrontWallet { Id = 1, TenantId = 1, CustomerId = 10, Balance = 30 };
        IList<StorefrontWallet> wallets = [wallet];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);

        // Act
        var result = await _sut.DebitAsync(1, 10, 50, WalletTransactionType.OrderPayment, null, null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Yetersiz");
    }

    [Fact]
    public async Task DebitAsync_WithNoWallet_ReturnsError()
    {
        // Arrange
        IList<StorefrontWallet> wallets = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);

        // Act
        var result = await _sut.DebitAsync(1, 10, 50, WalletTransactionType.OrderPayment, null, null);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DebitAsync_WithZeroAmount_ReturnsError()
    {
        // Act
        var result = await _sut.DebitAsync(1, 10, 0, WalletTransactionType.OrderPayment, null, null);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetTransactionsAsync_ReturnsTransactions()
    {
        // Arrange
        var wallet = new StorefrontWallet { Id = 1, TenantId = 1, CustomerId = 10, Balance = 100 };
        IList<StorefrontWallet> wallets = [wallet];
        IList<StorefrontWalletTransaction> transactions =
        [
            new() { Id = 1, WalletId = 1, Amount = 50, TransactionType = WalletTransactionType.Refund },
            new() { Id = 2, WalletId = 1, Amount = -20, TransactionType = WalletTransactionType.OrderPayment }
        ];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);
        mockIntegrationDbContext.Setup(c => c.StorefrontWalletTransactions).ReturnsDbSet(transactions);

        // Act
        var result = await _sut.GetTransactionsAsync(1, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithNoWallet_ReturnsEmptyList()
    {
        // Arrange
        IList<StorefrontWallet> wallets = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);

        // Act
        var result = await _sut.GetTransactionsAsync(1, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task DebitAsync_BalanceNeverGoesNegative()
    {
        // Arrange
        var wallet = new StorefrontWallet { Id = 1, TenantId = 1, CustomerId = 10, Balance = 0 };
        IList<StorefrontWallet> wallets = [wallet];
        mockIntegrationDbContext.Setup(c => c.StorefrontWallets).ReturnsDbSet(wallets);

        // Act
        var result = await _sut.DebitAsync(1, 10, 1, WalletTransactionType.OrderPayment, null, null);

        // Assert
        result.Success.Should().BeFalse();
        wallet.Balance.Should().Be(0); // Balance unchanged
    }
}
