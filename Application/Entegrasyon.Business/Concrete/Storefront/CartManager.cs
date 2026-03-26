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
            return new ErrorResult("Sepet bulunamadi.");

        var variant = await dbContext.ProductVariants
            .Include(v => v.BranchOfficeStocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == productVariantId);

        if (variant is null)
            return new ErrorResult("Urun varyanti bulunamadi.");

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

    public async Task<IDataResult<CartDto>> GetCartDtoAsync(Guid cartId, decimal freeShippingThreshold, decimal flatShippingRate)
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
            return new ErrorDataResult<CartDto>(null!, "Sepet bulunamadi.");

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
        var shippingCost = subTotal >= freeShippingThreshold ? 0m : flatShippingRate;
        var grandTotal = subTotal + shippingCost;

        var dto = new CartDto(
            Id: cart.Id,
            Items: items,
            CouponCode: cart.CouponCode,
            SubTotal: subTotal,
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
            return new ErrorResult("Sepet ogesi bulunamadi.");

        if (quantity <= 0)
            return new ErrorResult("Miktar sifirdan buyuk olmalidir.");

        var variant = await dbContext.ProductVariants
            .Include(v => v.BranchOfficeStocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == productVariantId);

        if (variant is null)
            return new ErrorResult("Urun varyanti bulunamadi.");

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
            return new ErrorResult("Sepet ogesi bulunamadi.");

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
}
