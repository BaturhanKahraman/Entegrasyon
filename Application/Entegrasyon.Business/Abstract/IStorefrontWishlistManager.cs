using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontWishlistManager
{
    Task<IDataResult<List<StorefrontWishlistItem>>> GetWishlistAsync(int tenantId, int customerId);
    Task<IResult> AddToWishlistAsync(int tenantId, int customerId, Guid productId);
    Task<IResult> RemoveFromWishlistAsync(int tenantId, int customerId, Guid productId);
    Task<bool> IsInWishlistAsync(int tenantId, int customerId, Guid productId);
}
