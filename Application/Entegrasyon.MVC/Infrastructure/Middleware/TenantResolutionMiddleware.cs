using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Serilog.Context;

namespace Entegrasyon.MVC.Infrastructure.Middleware;

/// <summary>
/// MVC icin subdomain-based tenant resolution middleware.
/// Subdomain'den tenant bilgisini cozumler.
/// Development modda tenant registry'de kayit yoksa fallback connection string kullanir.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SkipPrefixes =
        ["/lib", "/css", "/js", "/images", "/favicon.ico", "/health"];

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

        var env = context.RequestServices.GetService<IWebHostEnvironment>();
        var isDevelopment = env?.IsDevelopment() == true;
        var isTesting = env?.EnvironmentName == "Testing";

        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);

        // Fallback: subdomain yoksa (IP/localhost) config'den default tenant kullan
        if (string.IsNullOrEmpty(subdomain))
        {
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var defaultSubdomain = configuration["Tenant:DefaultSubdomain"];

            if (!string.IsNullOrEmpty(defaultSubdomain))
            {
                subdomain = defaultSubdomain;
            }
            else if (isDevelopment || isTesting)
            {
                subdomain = "dev";
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Tenant bulunamadı.");
                return;
            }
        }

        var tenant = await tenantRegistry.GetBySubdomainAsync(subdomain);

        // Development fallback: registry'de tenant yoksa fallback connection string ile devam et
        if (tenant is null || !tenant.IsActive)
        {
            if (isDevelopment || isTesting)
            {
                var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                var fallbackCs = configuration.GetConnectionString("Main")
                    ?? configuration.GetConnectionString("DefaultConnection");

                if (!string.IsNullOrEmpty(fallbackCs))
                {
                    tenant = new TenantRegistryEntry(
                        TenantId: 1,
                        Subdomain: "dev",
                        CompanyName: "Development",
                        ConnectionString: fallbackCs,
                        IsActive: true,
                        LicenseType: "Enterprise");
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Development: ConnectionStrings:Main yapilandirilmamis.");
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Tenant bulunamadı veya pasif.");
                return;
            }
        }

        tenantContext.Initialize(tenant);
        using (LogContext.PushProperty("TenantId", tenant.TenantId))
        using (LogContext.PushProperty("TenantSubdomain", tenant.Subdomain))
        {
            await next(context);
        }
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
