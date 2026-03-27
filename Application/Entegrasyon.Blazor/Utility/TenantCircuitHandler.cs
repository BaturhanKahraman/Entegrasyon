using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Entegrasyon.Blazor.Utility;

/// <summary>
/// Blazor circuit reconnect'te ITenantContext'i yeniden initialize eder.
/// Claims'teki TenantId kullanilarak ITenantRegistry'den tenant bilgisi cekilir.
/// Development modda fallback tenant kullanilir.
/// </summary>
public sealed class TenantCircuitHandler(
    ITenantContext tenantContext,
    ITenantRegistry tenantRegistry,
    AuthenticationStateProvider authStateProvider,
    IConfiguration configuration,
    IWebHostEnvironment environment) : CircuitHandler
{
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        if (tenantContext.IsInitialized)
            return;

        try
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            var tenantIdClaim = authState.User.FindFirst("TenantId")?.Value;

            if (tenantIdClaim is not null && int.TryParse(tenantIdClaim, out var tenantId))
            {
                var tenant = await tenantRegistry.GetByIdAsync(tenantId);
                if (tenant is not null && tenant.IsActive)
                {
                    tenantContext.Initialize(tenant);
                    return;
                }
            }

            // Development fallback: claims'te tenant yoksa (henüz login olmamış)
            if (environment.IsDevelopment() && !tenantContext.IsInitialized)
            {
                var fallbackCs = configuration.GetConnectionString("Main")
                    ?? configuration.GetConnectionString("DefaultConnection");

                if (!string.IsNullOrEmpty(fallbackCs))
                {
                    tenantContext.Initialize(new TenantRegistryEntry(
                        TenantId: 1,
                        Subdomain: "dev",
                        CompanyName: "Development",
                        ConnectionString: fallbackCs,
                        IsActive: true,
                        LicenseType: "Enterprise"));
                }
            }
        }
        catch
        {
            // Circuit handler'da hata yutulmali — tenant context bos kalirsa
            // sayfa zaten login'e yonlendirilecek
        }
    }
}
