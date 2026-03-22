using Entegrasyon.PrintAgent.Configuration;
using Microsoft.Extensions.Options;

namespace Entegrasyon.PrintAgent;

public class ApiKeyMiddleware(RequestDelegate next, IOptions<AgentConfiguration> config)
{
    private const string ApiKeyHeader = "X-Agent-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // /health endpoint'i auth gerektirmez
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey)
            || providedKey != config.Value.ApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Geçersiz API key" });
            return;
        }

        await next(context);
    }
}
