using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

[Authorize]
public class WishlistController(
    IStorefrontTenantContext tenant,
    IStorefrontWishlistManager wishlistManager,
    IProductService productService) : Controller
{
    private int GetCustomerId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await wishlistManager.GetWishlistAsync(tenant.TenantId, GetCustomerId());
        ViewBag.WishlistItems = result.Success ? result.Data : [];
        ViewBag.SeoTitle = $"Favorilerim | {tenant.Settings.StoreName}";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] WishlistRequest request)
    {
        var result = await wishlistManager.AddToWishlistAsync(tenant.TenantId, GetCustomerId(), request.ProductId);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Sil([FromBody] WishlistRequest request)
    {
        var result = await wishlistManager.RemoveFromWishlistAsync(tenant.TenantId, GetCustomerId(), request.ProductId);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> Kontrol([FromQuery] Guid productId)
    {
        var isInWishlist = await wishlistManager.IsInWishlistAsync(tenant.TenantId, GetCustomerId(), productId);
        return Json(new { inWishlist = isInWishlist });
    }
}

public record WishlistRequest(Guid ProductId);
