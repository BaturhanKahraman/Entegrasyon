using System.Net;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Storefront.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> StaticPrefixes = ["/css", "/js", "/images", "/lib", "/favicon.ico"];

    public async Task InvokeAsync(
        HttpContext context,
        IStorefrontTenantResolver tenantResolver,
        IStorefrontTenantContext storefrontTenantContext,
        ITenantContext tenantContext,
        ITenantRegistry tenantRegistry)
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
            context.Response.ContentType = "text/html; charset=utf-8";

            var s = tenantInfo.Settings;
            var storeName = WebUtility.HtmlEncode(s.StoreName);
            var maintenanceMsg = WebUtility.HtmlEncode(
                s.MaintenanceMessage ?? "Sitemiz bakim modundadir. Lutfen daha sonra tekrar deneyin.");
            var primaryColor = WebUtility.HtmlEncode(s.PrimaryColor);

            var html = $$"""
                <!DOCTYPE html>
                <html lang="tr">
                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                    <title>Bakim Modu - {{storeName}}</title>
                    <link rel="preconnect" href="https://fonts.googleapis.com" />
                    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap" rel="stylesheet" />
                    <style>
                        * { margin: 0; padding: 0; box-sizing: border-box; }
                        body {
                            font-family: 'Inter', system-ui, -apple-system, sans-serif;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            min-height: 100vh;
                            background: #f9fafb;
                            color: #333;
                        }
                        .container {
                            text-align: center;
                            max-width: 480px;
                            padding: 2rem;
                        }
                        .icon {
                            width: 80px;
                            height: 80px;
                            margin: 0 auto 1.5rem;
                            color: {{primaryColor}};
                        }
                        h1 {
                            font-size: 1.75rem;
                            font-weight: 700;
                            color: #111827;
                            margin-bottom: 0.75rem;
                        }
                        .message {
                            font-size: 1rem;
                            color: #6b7280;
                            line-height: 1.6;
                            margin-bottom: 1.5rem;
                        }
                        .badge {
                            display: inline-block;
                            background: {{primaryColor}};
                            color: white;
                            font-size: 0.75rem;
                            font-weight: 600;
                            padding: 0.25rem 0.75rem;
                            border-radius: 9999px;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                        }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <svg class="icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                                  d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        </svg>
                        <h1>{{storeName}}</h1>
                        <p class="message">{{maintenanceMsg}}</p>
                        <span class="badge">Bakim Modu</span>
                    </div>
                </body>
                </html>
                """;

            await context.Response.WriteAsync(html);
            return;
        }

        storefrontTenantContext.Initialize(tenantInfo);

        // Initialize general tenant context for DbContextFactory and managers
        var tenantEntry = await tenantRegistry.GetByIdAsync(tenantInfo.TenantId);
        if (tenantEntry is not null)
        {
            tenantContext.Initialize(tenantEntry);
        }

        await next(context);
    }
}
