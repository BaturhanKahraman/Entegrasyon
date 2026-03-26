using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ISellerCommissionManager
{
    Task<decimal> GetCommissionRateAsync(int sellerId, int? categoryId);
    Task<IResult> RecordSaleCommissionAsync(int sellerId, Guid orderId, decimal saleAmount, decimal commissionRate);
    Task<IResult> ProcessOrderCommissionsAsync(Guid orderId);
}
