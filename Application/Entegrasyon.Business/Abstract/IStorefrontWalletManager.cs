using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontWalletManager
{
    Task<IDataResult<StorefrontWallet>> GetOrCreateWalletAsync(int tenantId, int customerId);
    Task<IResult> CreditAsync(int tenantId, int customerId, decimal amount, WalletTransactionType type, string? referenceId, string? description);
    Task<IResult> DebitAsync(int tenantId, int customerId, decimal amount, WalletTransactionType type, string? referenceId, string? description);
    Task<IDataResult<List<StorefrontWalletTransaction>>> GetTransactionsAsync(int tenantId, int customerId);
}
