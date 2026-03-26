using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Blazor.Middleware;

/// <summary>
/// Blazor Server icin subdomain-based tenant resolution middleware.
/// Storefront TenantResolutionMiddleware pattern'ini takip eder.
/// </summary>
public class BlazorTenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SkipPrefixes =
        ["/_blazor", "/_framework", "/css", "/js", "/images", "/lib", "/favicon.ico"];

    public async Task InvokeAsync(
        HttpContext context,
        ITenantRegistry tenantRegistry,
        ITenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? "";
        if (SkipPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);

        if (string.IsNullOrEmpty(subdomain))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Tenant bulunamadi.");
            return;
        }

        var tenant = await tenantRegistry.GetBySubdomainAsync(subdomain);

        if (tenant is null || !tenant.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Tenant bulunamadi veya pasif.");
            return;
        }

        tenantContext.Initialize(tenant);
        await next(context);
    }

    /// <summary>
    /// "acme.app.entegrasyon.com" -> "acme"
    /// "localhost" -> null
    /// </summary>
    internal static string? ExtractSubdomain(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            System.Net.IPAddress.TryParse(host, out _))
            return null;

        var parts = host.Split('.');
        if (parts.Length < 3)
            return null;

        return parts[0];
    }
}
