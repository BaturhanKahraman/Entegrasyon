using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class CartManager(IDbContextFactory<IntegrationDbContext> contextFactory) : ICartManager
{
    public async Task<IDataResult<Cart>> GetOrCreateCartAsync(int tenantId, int? customerId, string? sessionId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        Cart? cart = null;

        if (customerId.HasValue)
        {
            cart = await dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CustomerId == customerId);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            cart = await dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.SessionId == sessionId);
        }

        if (cart is not null)
            return new SuccessDataResult<Cart>(cart);

        cart = new Cart
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            SessionId = sessionId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<Cart>(cart);
    }

    public async Task<IResult> AddToCartAsync(Guid cartId, Guid productVariantId, int quantity)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var cart = await dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart is null)
            return new ErrorResult("Sepet bulunamadı.");

        var variant = await dbContext.ProductVariants
            .Include(v => v.BranchOfficeStocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == productVariantId);

        if (variant is null)
            return new ErrorResult("Urun varyanti bulunamadı.");

        var availableStock = variant.BranchOfficeStocks.Sum(s => s.CurrentStock);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductVariantId == productVariantId);
        var totalRequested = quantity + (existingItem?.Quantity ?? 0);

        if (totalRequested > availableStock)
            return new ErrorResult($"Yetersiz stok. Mevcut: {availableStock}");

        if (existingItem is not null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cartId,
                ProductVariantId = productVariantId,
                Quantity = quantity,
                UnitPrice = variant.SalePrice,
                AddedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Urun sepete eklendi.");
    }

    public async Task<IDataResult<CartDto>> GetCartDtoAsync(Guid cartId, decimal freeShippingThreshold, decimal flatShippingRate, decimal discountAmount = 0m)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var cart = await dbContext.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart is null)
            return new ErrorDataResult<CartDto>(null!, "Sepet bulunamadı.");

        var items = cart.Items.Select(i =>
        {
            var mainImage = i.ProductVariant.Images
                .OrderBy(img => img.DisplayOrder)
                .FirstOrDefault();

            var variantInfo = i.ProductVariant.ProductVariantAttributes is { Count: > 0 }
                ? string.Join(", ", i.ProductVariant.ProductVariantAttributes.Select(a => a.ToString()))
                : null;

            return new CartItemDto(
                ProductVariantId: i.ProductVariantId,
                ProductTitle: i.ProductVariant.Product?.Title ?? "",
                ImageUrl: mainImage?.StorageKey,
                VariantInfo: variantInfo,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                LineTotal: i.Quantity * i.UnitPrice,
                AvailableStock: i.ProductVariant.BranchOfficeStocks.Sum(s => s.CurrentStock));
        }).ToList();

        var subTotal = items.Sum(i => i.LineTotal);
        var appliedDiscount = Math.Min(discountAmount, subTotal);
        var afterDiscount = subTotal - appliedDiscount;
        var shippingCost = afterDiscount >= freeShippingThreshold ? 0m : flatShippingRate;
        var grandTotal = afterDiscount + shippingCost;

        var dto = new CartDto(
            Id: cart.Id,
            Items: items,
            CouponCode: cart.CouponCode,
            SubTotal: subTotal,
            DiscountAmount: appliedDiscount,
            ShippingCost: shippingCost,
            GrandTotal: grandTotal,
            ItemCount: items.Sum(i => i.Quantity));

        return new SuccessDataResult<CartDto>(dto);
    }

    public async Task<IResult> UpdateQuantityAsync(Guid cartId, Guid productVariantId, int quantity)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var item = await dbContext.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductVariantId == productVariantId);

        if (item is null)
            return new ErrorResult("Sepet ogesi bulunamadı.");

        if (quantity <= 0)
            return new ErrorResult("Miktar sifirdan buyuk olmalidir.");

        var variant = await dbContext.ProductVariants
            .Include(v => v.BranchOfficeStocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == productVariantId);

        if (variant is null)
            return new ErrorResult("Urun varyanti bulunamadı.");

        var availableStock = variant.BranchOfficeStocks.Sum(s => s.CurrentStock);
        if (quantity > availableStock)
            return new ErrorResult($"Yetersiz stok. Mevcut: {availableStock}");

        item.Quantity = quantity;
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Miktar guncellendi.");
    }

    public async Task<IResult> RemoveItemAsync(Guid cartId, Guid productVariantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var item = await dbContext.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductVariantId == productVariantId);

        if (item is null)
            return new ErrorResult("Sepet ogesi bulunamadı.");

        dbContext.CartItems.Remove(item);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Urun sepetten cikarildi.");
    }

    public async Task<IResult> ClearCartAsync(Guid cartId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var items = await dbContext.CartItems
            .Where(i => i.CartId == cartId)
            .ToListAsync();

        dbContext.CartItems.RemoveRange(items);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Sepet temizlendi.");
    }

    public async Task<IDataResult<CartSummaryDto>> GetCartSummaryAsync(Guid cartId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var items = await dbContext.CartItems
            .Where(i => i.CartId == cartId)
            .AsNoTracking()
            .ToListAsync();

        var itemCount = items.Sum(i => i.Quantity);
        var total = items.Sum(i => i.Quantity * i.UnitPrice);

        return new SuccessDataResult<CartSummaryDto>(new CartSummaryDto(itemCount, total));
    }

    public async Task<IResult> MergeCartsAsync(string sessionId, int customerId, int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sessionCart = await dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.SessionId == sessionId && c.CustomerId == null);

        if (sessionCart is null || !sessionCart.Items.Any())
            return new SuccessResult("Birlestirilecek oturum sepeti yok.");

        var customerCart = await dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CustomerId == customerId);

        if (customerCart is null)
        {
            customerCart = new Cart
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customerId,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            };
            dbContext.Carts.Add(customerCart);
        }

        foreach (var sessionItem in sessionCart.Items)
        {
            var existingItem = customerCart.Items
                .FirstOrDefault(i => i.ProductVariantId == sessionItem.ProductVariantId);

            if (existingItem is not null)
            {
                existingItem.Quantity += sessionItem.Quantity;
            }
            else
            {
                customerCart.Items.Add(new CartItem
                {
                    CartId = customerCart.Id,
                    ProductVariantId = sessionItem.ProductVariantId,
                    Quantity = sessionItem.Quantity,
                    UnitPrice = sessionItem.UnitPrice,
                    AddedAt = DateTimeOffset.UtcNow
                });
            }
        }

        // Remove session cart items and mark cart as deleted
        dbContext.CartItems.RemoveRange(sessionCart.Items);
        sessionCart.IsDeleted = true;
        sessionCart.DeletedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Sepetler birlestirildi.");
    }

    public async Task<IResult> ApplyCouponAsync(Guid cartId, string couponCode)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var cart = await dbContext.Carts.AsTracking().FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart is null)
            return new ErrorResult("Sepet bulunamadı.");

        cart.CouponCode = couponCode;
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Kupon uygulandi.");
    }

    public async Task<IResult> RemoveCouponAsync(Guid cartId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var cart = await dbContext.Carts.AsTracking().FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart is null)
            return new ErrorResult("Sepet bulunamadı.");

        cart.CouponCode = null;
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Kupon kaldirildi.");
    }

    public async Task<IResult> SaveForLaterAsync(Guid cartId, Guid productVariantId, int customerId, int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var cartItem = await dbContext.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductVariantId == productVariantId);

        if (cartItem is null)
            return new ErrorResult("Sepet ogesi bulunamadı.");

        // Check if already saved
        var alreadySaved = await dbContext.StorefrontSavedCartItems
            .AnyAsync(s => s.TenantId == tenantId && s.CustomerId == customerId && s.ProductVariantId == productVariantId);

        if (!alreadySaved)
        {
            dbContext.StorefrontSavedCartItems.Add(new StorefrontSavedCartItem
            {
                TenantId = tenantId,
                CustomerId = customerId,
                ProductVariantId = productVariantId,
                OriginalPrice = cartItem.UnitPrice,
                SavedAt = DateTimeOffset.UtcNow
            });
        }

        // Remove from cart
        dbContext.CartItems.Remove(cartItem);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Urun sonra almak uzere kaydedildi.");
    }

    public async Task<IDataResult<List<SavedCartItemDto>>> GetSavedItemsAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var items = await dbContext.StorefrontSavedCartItems
            .Include(s => s.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(s => s.ProductVariant)
                .ThenInclude(v => v.Images)
            .Where(s => s.TenantId == tenantId && s.CustomerId == customerId)
            .OrderByDescending(s => s.SavedAt)
            .AsNoTracking()
            .ToListAsync();

        var dtos = items.Select(s =>
        {
            var mainImage = s.ProductVariant.Images
                .OrderBy(img => img.DisplayOrder)
                .FirstOrDefault();

            return new SavedCartItemDto(
                ProductVariantId: s.ProductVariantId,
                ProductTitle: s.ProductVariant.Product?.Title ?? "",
                ImageUrl: mainImage?.StorageKey,
                OriginalPrice: s.OriginalPrice,
                CurrentPrice: s.ProductVariant.SalePrice,
                SavedAt: s.SavedAt);
        }).ToList();

        return new SuccessDataResult<List<SavedCartItemDto>>(dtos);
    }

    public async Task<IResult> MoveToCartAsync(int tenantId, int customerId, Guid productVariantId, Guid cartId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var savedItem = await dbContext.StorefrontSavedCartItems
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.CustomerId == customerId && s.ProductVariantId == productVariantId);

        if (savedItem is null)
            return new ErrorResult("Kaydedilmis urun bulunamadı.");

        // Get current price from variant
        var variant = await dbContext.ProductVariants
            .Include(v => v.BranchOfficeStocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == productVariantId);

        if (variant is null)
            return new ErrorResult("Urun varyanti bulunamadı.");

        var availableStock = variant.BranchOfficeStocks.Sum(s => s.CurrentStock);
        if (availableStock <= 0)
            return new ErrorResult("Urun stokta yok.");

        // Add to cart
        var existingCartItem = await dbContext.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductVariantId == productVariantId);

        if (existingCartItem is not null)
        {
            existingCartItem.Quantity += 1;
        }
        else
        {
            dbContext.CartItems.Add(new CartItem
            {
                CartId = cartId,
                ProductVariantId = productVariantId,
                Quantity = 1,
                UnitPrice = variant.SalePrice,
                AddedAt = DateTimeOffset.UtcNow
            });
        }

        // Remove from saved
        dbContext.StorefrontSavedCartItems.Remove(savedItem);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Urun sepete tasinidi.");
    }
}
