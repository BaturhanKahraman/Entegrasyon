using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Storefront.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> StaticPrefixes = ["/css", "/js", "/images", "/lib", "/favicon.ico"];

    public async Task InvokeAsync(
        HttpContext context,
        IStorefrontTenantResolver tenantResolver,
        IStorefrontTenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? "";
        if (StaticPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var host = context.Request.Host.Host;
        var tenantInfo = await tenantResolver.ResolveAsync(host);

        if (tenantInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Magaza bulunamadi.");
            return;
        }

        if (tenantInfo.Settings.IsMaintenanceMode)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "3600";
            await context.Response.WriteAsync(
                tenantInfo.Settings.MaintenanceMessage ?? "Sitemiz bakim modundadir.");
            return;
        }

        tenantContext.Initialize(tenantInfo);
        await next(context);
    }
}
