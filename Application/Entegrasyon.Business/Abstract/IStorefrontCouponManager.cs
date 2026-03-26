using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontCouponManager
{
    Task<IDataResult<CouponValidationResult>> ValidateCouponAsync(string code, decimal cartTotal, int? customerId);
}

public record CouponValidationResult(
    int VoucherId, string Code, decimal DiscountAmount, string Description);
