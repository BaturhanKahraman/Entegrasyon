using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface ISellerManager
{
    // Registration
    Task<IDataResult<Seller>> RegisterSellerAsync(int tenantId, int customerId, SellerRegistrationDto dto);
    Task<IDataResult<Seller>> GetSellerByCustomerIdAsync(int tenantId, int customerId);
    Task<IDataResult<Seller>> GetSellerBySlugAsync(int tenantId, string slug);

    // Admin
    Task<IDataResult<List<Seller>>> GetAllSellersAsync(int tenantId);
    Task<IResult> ApproveSellerAsync(int sellerId);
    Task<IResult> RejectSellerAsync(int sellerId, string reason);
    Task<IResult> SuspendSellerAsync(int sellerId, string reason);

    // Profile
    Task<IResult> UpdateSellerProfileAsync(int sellerId, SellerProfileDto dto);
}
