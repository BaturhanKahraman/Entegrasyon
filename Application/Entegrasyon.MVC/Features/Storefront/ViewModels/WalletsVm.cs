using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class WalletsVm
{
    public List<WalletSummary> Wallets { get; set; } = [];

    public record WalletSummary(int CustomerId, decimal Balance, int TransactionCount, DateTimeOffset LastActivity);
}

public class WalletDetailVm
{
    public StorefrontWallet Wallet { get; set; } = null!;
    public List<StorefrontWalletTransaction> Transactions { get; set; } = [];
}
