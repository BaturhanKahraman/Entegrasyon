using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class NewsletterController(
    IStorefrontTenantContext tenant,
    IStorefrontNewsletterManager newsletterManager) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] NewsletterRequest request)
    {
        var result = await newsletterManager.SubscribeAsync(tenant.TenantId, request.Email, request.Name);
        return Json(new { success = result.Success, message = result.Message });
    }
}

public record NewsletterRequest(string Email, string? Name);
