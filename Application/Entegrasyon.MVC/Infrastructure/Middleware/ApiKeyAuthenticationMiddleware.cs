using System.Security.Claims;
using System.Text.Json;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.MVC.Infrastructure.Middleware;

/// <summary>
/// API Key authentication middleware.
/// Authorization: Bearer ent_xxxx header'ı ile gelen istekleri doğrular.
/// Sadece /api/ prefix'li endpoint'lerde çalışır — MVC sayfalarını etkilemez.
/// </summary>
public class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApiKeyManager apiKeyManager)
    {
        // Sadece /api/ prefix'li endpoint'lerde çalış
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Authorization header gerekli. Format: Bearer ent_xxxx" }));
            return;
        }

        var plainKey = authHeader["Bearer ".Length..].Trim();
        var apiKey = await apiKeyManager.ValidateKeyAsync(plainKey);

        if (apiKey is null)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Gecersiz veya suresi dolmus API anahtari." }));
            return;
        }

        // Scope'ları claim olarak ekle
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

        // LastUsedAt güncelle (fire-and-forget)
        _ = apiKeyManager.UpdateLastUsedAsync(apiKey.Id);

        await next(context);
    }
}
