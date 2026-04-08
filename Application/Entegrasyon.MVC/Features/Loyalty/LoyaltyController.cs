using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Loyalty;

[Authorize]
public class LoyaltyController(
    IStorefrontLoyaltyManager loyaltyManager,
    ITenantContext tenantContext) : HtmxController
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    [HttpGet("/loyalty")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Sadakat Programi");
        ViewData.SetActiveNav("loyalty");
        ViewData.SetBreadcrumb(("Sadakat Programi", null));

        var dashboard = await loyaltyManager.GetDashboardAsync(TenantId);
        return View(dashboard);
    }
}
