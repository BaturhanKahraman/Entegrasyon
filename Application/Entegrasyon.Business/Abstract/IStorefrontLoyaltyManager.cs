using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontLoyaltyManager
{
    Task<IDataResult<StorefrontLoyaltyPoints>> GetBalanceAsync(int tenantId, int customerId);
    Task<IResult> EarnPointsAsync(int tenantId, int customerId, int points, string type, string? referenceId, string? description);
    Task<IResult> RedeemPointsAsync(int tenantId, int customerId, int points, string? referenceId);
    Task<IDataResult<List<StorefrontLoyaltyTransaction>>> GetTransactionsAsync(int tenantId, int customerId, int count = 20);
}
