using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Authorization;

namespace Entegrasyon.ApplicationBootstrap.Security;

/// <summary>
/// Iki katmanli yetkilendirme:
/// 1. Tenant'in paketi bu permission'i iceriyor mu? (IFeatureService)
/// 2. Kullanicinin bu permission'i var mi? (claims/role)
/// </summary>
public class TenantFeatureAuthorizationHandler(IFeatureService featureService) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.ToList())
        {
            if (requirement is PermissionRequirement permReq)
            {
                // Layer 1: Tenant feature check
                var featureEnabled = await featureService.IsFeatureEnabledAsync(permReq.Permission);
                if (!featureEnabled)
                {
                    context.Fail(new AuthorizationFailureReason(this, $"Feature '{permReq.Permission}' is not enabled for this tenant."));
                    return;
                }

                // Layer 2: User permission check
                if (context.User.IsInRole("Admin") || context.User.HasClaim("Permission", permReq.Permission))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}

/// <summary>
/// Permission-based authorization requirement.
/// </summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
