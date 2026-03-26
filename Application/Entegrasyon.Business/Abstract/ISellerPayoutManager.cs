using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface ISellerPayoutManager
{
    Task<IDataResult<SellerBalance>> GetBalanceAsync(int sellerId);
    Task<IDataResult<List<SellerTransaction>>> GetTransactionsAsync(int sellerId);
    Task<IResult> RequestPayoutAsync(int sellerId, decimal amount);
    Task<IDataResult<List<PayoutRequest>>> GetPayoutRequestsAsync(int sellerId);

    // Admin
    Task<IDataResult<List<PayoutRequest>>> GetAllPendingPayoutsAsync(int tenantId);
    Task<IResult> ProcessPayoutAsync(int payoutId, bool approve, string? note);
}
