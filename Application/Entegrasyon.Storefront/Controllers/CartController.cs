using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class CartController(
    IStorefrontTenantContext tenant,
    ICartManager cartManager,
    IStorefrontCouponManager couponManager) : Controller
{
    private async Task<Guid> GetOrCreateCartIdAsync()
    {
        var cartIdStr = HttpContext.Session.GetString("CartId");
        if (cartIdStr != null && Guid.TryParse(cartIdStr, out var existing))
            return existing;

        int? customerId = User.Identity?.IsAuthenticated == true
            ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
            : null;

        var result = await cartManager.GetOrCreateCartAsync(tenant.TenantId, customerId, HttpContext.Session.Id);
        var cartId = result.Data.Id;
        HttpContext.Session.SetString("CartId", cartId.ToString());
        return cartId;
    }

    private int? GetCustomerId()
        => User.Identity?.IsAuthenticated == true
            ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
            : null;

    private async Task<IActionResult> GetCartWithCouponAsync(Guid cartId)
    {
        var s = tenant.Settings;
        var summary = await cartManager.GetCartSummaryAsync(cartId);
        decimal discountAmount = 0;

        // Check if cart has a coupon and validate it
        var tempCart = await cartManager.GetCartDtoAsync(cartId, s.FreeShippingThreshold, s.FlatShippingRate);
        if (tempCart.Success && !string.IsNullOrWhiteSpace(tempCart.Data.CouponCode))
        {
            var couponResult = await couponManager.ValidateCouponAsync(
                tempCart.Data.CouponCode, summary.Data.Total, GetCustomerId());
            if (couponResult.Success)
                discountAmount = couponResult.Data.DiscountAmount;
            else
                await cartManager.RemoveCouponAsync(cartId); // Remove invalid coupon
        }

        var cartResult = await cartManager.GetCartDtoAsync(cartId, s.FreeShippingThreshold, s.FlatShippingRate, discountAmount);
        return Json(new { success = true, cart = cartResult.Data });
    }

    public async Task<IActionResult> Index()
    {
        var cartId = await GetOrCreateCartIdAsync();
        var s = tenant.Settings;

        // Calculate discount if coupon exists
        var summary = await cartManager.GetCartSummaryAsync(cartId);
        decimal discountAmount = 0;
        var tempCart = await cartManager.GetCartDtoAsync(cartId, s.FreeShippingThreshold, s.FlatShippingRate);
        if (tempCart.Success && !string.IsNullOrWhiteSpace(tempCart.Data.CouponCode))
        {
            var couponResult = await couponManager.ValidateCouponAsync(
                tempCart.Data.CouponCode, summary.Data.Total, GetCustomerId());
            if (couponResult.Success)
                discountAmount = couponResult.Data.DiscountAmount;
            else
                await cartManager.RemoveCouponAsync(cartId);
        }

        var cartResult = await cartManager.GetCartDtoAsync(cartId, s.FreeShippingThreshold, s.FlatShippingRate, discountAmount);

        ViewBag.Cart = cartResult.Success ? cartResult.Data : null;
        ViewBag.SeoTitle = $"Sepetim | {s.StoreName}";
        ViewBag.FreeShippingThreshold = s.FreeShippingThreshold;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] AddToCartDto dto)
    {
        var cartId = await GetOrCreateCartIdAsync();
        var result = await cartManager.AddToCartAsync(cartId, dto.ProductVariantId, dto.Quantity);

        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        var summary = await cartManager.GetCartSummaryAsync(cartId);
        return Json(new { success = true, itemCount = summary.Data.ItemCount, total = summary.Data.Total });
    }

    [HttpPost]
    public async Task<IActionResult> Guncelle([FromBody] AddToCartDto dto)
    {
        var cartId = await GetOrCreateCartIdAsync();
        var result = await cartManager.UpdateQuantityAsync(cartId, dto.ProductVariantId, dto.Quantity);
        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        return await GetCartWithCouponAsync(cartId);
    }

    [HttpPost]
    public async Task<IActionResult> Sil([FromBody] AddToCartDto dto)
    {
        var cartId = await GetOrCreateCartIdAsync();
        await cartManager.RemoveItemAsync(cartId, dto.ProductVariantId);

        return await GetCartWithCouponAsync(cartId);
    }

    [HttpGet]
    public async Task<IActionResult> Ozet()
    {
        var cartIdStr = HttpContext.Session.GetString("CartId");
        if (cartIdStr == null || !Guid.TryParse(cartIdStr, out var cartId))
            return Json(new CartSummaryDto(0, 0));

        var result = await cartManager.GetCartSummaryAsync(cartId);
        return Json(result.Success ? result.Data : new CartSummaryDto(0, 0));
    }

    [HttpPost]
    public async Task<IActionResult> KuponUygula([FromBody] CouponApplyDto dto)
    {
        var cartId = await GetOrCreateCartIdAsync();
        var summary = await cartManager.GetCartSummaryAsync(cartId);

        var result = await couponManager.ValidateCouponAsync(dto.Code, summary.Data.Total, GetCustomerId());
        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        await cartManager.ApplyCouponAsync(cartId, dto.Code);

        var s = tenant.Settings;
        var cartResult = await cartManager.GetCartDtoAsync(
            cartId, s.FreeShippingThreshold, s.FlatShippingRate, result.Data.DiscountAmount);
        return Json(new { success = true, discount = result.Data.DiscountAmount, description = result.Data.Description, cart = cartResult.Data });
    }

    [HttpPost]
    public async Task<IActionResult> KuponKaldir()
    {
        var cartId = await GetOrCreateCartIdAsync();
        await cartManager.RemoveCouponAsync(cartId);

        var s = tenant.Settings;
        var cartResult = await cartManager.GetCartDtoAsync(cartId, s.FreeShippingThreshold, s.FlatShippingRate);
        return Json(new { success = true, cart = cartResult.Data });
    }
}
