using System.Security.Claims;
using System.Text.Json;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.MVC.Infrastructure.Middleware;

/// <summary>
/// /api/ prefix'li endpoint'lerde Authorization: Bearer header'ını doğrular.
/// Bearer ent_xxxx → tenant API key (IApiKeyManager).
/// Bearer dev_xxxx → desktop device key (IDeviceManager).
/// MVC sayfalarını etkilemez; /api/device/register bootstrap path'i exempt.
/// </summary>
public class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApiKeyManager apiKeyManager, IDeviceManager deviceManager)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/device/register"))
        {
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorized(context, "Authorization header gerekli. Format: Bearer ent_xxxx veya Bearer dev_xxxx");
            return;
        }

        var plainKey = authHeader["Bearer ".Length..].Trim();

        if (plainKey.StartsWith("dev_", StringComparison.Ordinal))
        {
            await AuthenticateDevice(context, deviceManager, plainKey);
            return;
        }

        if (plainKey.StartsWith("ent_", StringComparison.Ordinal))
        {
            await AuthenticateApiKey(context, apiKeyManager, plainKey);
            return;
        }

        await WriteUnauthorized(context, "Bilinmeyen anahtar formatı — dev_ veya ent_ prefix'i gerekir.");
    }

    private async Task AuthenticateApiKey(HttpContext context, IApiKeyManager apiKeyManager, string plainKey)
    {
        var apiKey = await apiKeyManager.ValidateKeyAsync(plainKey);
        if (apiKey is null)
        {
            await WriteUnauthorized(context, "Geçersiz veya süresi dolmuş API anahtarı.");
            return;
        }

        var scopes = JsonSerializer.Deserialize<string[]>(apiKey.Scopes) ?? [];
        var claims = new List<Claim>
        {
            new("ApiKeyId", apiKey.Id.ToString()),
            new("TenantId", apiKey.TenantId.ToString()),
            new(ClaimTypes.AuthenticationMethod, "ApiKey"),
        };
        foreach (var scope in scopes)
            claims.Add(new Claim("Scope", scope));

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
        _ = apiKeyManager.UpdateLastUsedAsync(apiKey.Id);

        await next(context);
    }

    private async Task AuthenticateDevice(HttpContext context, IDeviceManager deviceManager, string plainKey)
    {
        var device = await deviceManager.ValidateKeyAsync(plainKey);
        if (device is null)
        {
            await WriteUnauthorized(context, "Geçersiz veya iptal edilmiş cihaz anahtarı.");
            return;
        }

        var claims = new List<Claim>
        {
            new("DeviceId", device.Id.ToString()),
            new("DeviceName", device.Name),
            new("TenantId", device.TenantId.ToString()),
            new(ClaimTypes.AuthenticationMethod, "Device"),
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Device"));
        _ = deviceManager.UpdateLastSeenAsync(device.Id);

        await next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
