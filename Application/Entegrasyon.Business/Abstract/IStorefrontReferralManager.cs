using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontReferralManager
{
    Task<IDataResult<string>> GetOrCreateReferralCodeAsync(int tenantId, int customerId);
    Task<IResult> RegisterReferralAsync(int tenantId, string referralCode, int referredCustomerId);
    Task<IDataResult<List<StorefrontReferral>>> GetReferralsAsync(int tenantId, int customerId);
}
