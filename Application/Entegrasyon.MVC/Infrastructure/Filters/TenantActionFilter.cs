using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.MVC.Infrastructure.Filters;

/// <summary>
/// Claims'ten TenantId okur ve ITenantContext'i initialize eder.
/// Middleware zaten tenant set etmişse, bu filter defense-in-depth olarak çalışır.
/// </summary>
public class TenantActionFilter(
    ITenantContext tenantContext,
    ITenantRegistry tenantRegistry) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!tenantContext.IsInitialized)
        {
            var tenantIdClaim = context.HttpContext.User.FindFirst("TenantId")?.Value;
            if (tenantIdClaim is not null && int.TryParse(tenantIdClaim, out var tid))
            {
                var tenant = await tenantRegistry.GetByIdAsync(tid);
                if (tenant is not null)
                    tenantContext.Initialize(tenant);
            }
        }

        await next();
    }
}
