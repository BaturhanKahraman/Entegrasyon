using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface ICartManager
{
    Task<IDataResult<Cart>> GetOrCreateCartAsync(int tenantId, int? customerId, string? sessionId);
    Task<IResult> AddToCartAsync(Guid cartId, Guid productVariantId, int quantity);
    Task<IDataResult<CartDto>> GetCartDtoAsync(Guid cartId, decimal freeShippingThreshold, decimal flatShippingRate, decimal discountAmount = 0m);
    Task<IResult> UpdateQuantityAsync(Guid cartId, Guid productVariantId, int quantity);
    Task<IResult> RemoveItemAsync(Guid cartId, Guid productVariantId);
    Task<IResult> ClearCartAsync(Guid cartId);
    Task<IDataResult<CartSummaryDto>> GetCartSummaryAsync(Guid cartId);
    Task<IResult> MergeCartsAsync(string sessionId, int customerId, int tenantId);
    Task<IResult> ApplyCouponAsync(Guid cartId, string couponCode);
    Task<IResult> RemoveCouponAsync(Guid cartId);

    // Save for Later
    Task<IResult> SaveForLaterAsync(Guid cartId, Guid productVariantId, int customerId, int tenantId);
    Task<IDataResult<List<SavedCartItemDto>>> GetSavedItemsAsync(int tenantId, int customerId);
    Task<IResult> MoveToCartAsync(int tenantId, int customerId, Guid productVariantId, Guid cartId);
}
