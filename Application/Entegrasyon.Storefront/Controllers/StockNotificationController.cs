using Entegrasyon.Business.Abstract;
using Entegrasyon.Storefront.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class StockNotificationController(
    IStorefrontTenantContext tenant,
    IStorefrontStockNotificationManager stockNotificationManager) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] StockNotifyRequest request)
    {
        if (request is null || request.ProductVariantId == Guid.Empty || string.IsNullOrWhiteSpace(request.Email))
            return Json(new { success = false, message = "Gecersiz istek." });

        var result = await stockNotificationManager.SubscribeAsync(
            tenant.TenantId, request.ProductVariantId, request.Email);

        return Json(new { success = result.Success, message = result.Message });
    }
}

public record StockNotifyRequest(Guid ProductVariantId, string Email);
