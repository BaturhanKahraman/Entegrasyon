using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface ISellerCommissionManager
{
    Task<decimal> GetCommissionRateAsync(int sellerId, int? categoryId);
    Task<IResult> RecordSaleCommissionAsync(int sellerId, Guid orderId, decimal saleAmount, decimal commissionRate);
    Task<IResult> ProcessOrderCommissionsAsync(Guid orderId);
    Task<IDataResult<List<SellerCommission>>> GetCommissionsAsync(int tenantId);
    Task<IResult> UpdateCommissionRateAsync(int commissionId, decimal newRate);
}
