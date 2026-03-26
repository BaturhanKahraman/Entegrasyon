using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontReturnManager
{
    Task<IResult> CreateReturnRequestAsync(int tenantId, Guid orderId, int customerId, string reason, string? description);
    Task<IDataResult<List<StorefrontReturnRequest>>> GetCustomerReturnsAsync(int tenantId, int customerId);
    Task<IDataResult<List<StorefrontReturnRequest>>> GetAllReturnsAsync(int tenantId);
    Task<IResult> UpdateReturnStatusAsync(int id, ReturnStatus status, string? reviewNote, decimal? refundAmount);
}
