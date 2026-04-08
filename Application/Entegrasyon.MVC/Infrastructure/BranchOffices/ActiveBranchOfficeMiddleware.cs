using System.Security.Claims;

namespace Entegrasyon.MVC.Infrastructure.BranchOffices;

/// <summary>
/// Her authenticated request'te aktif şube ofisinin hâlâ geçerli olduğunu doğrular.
/// Stale ise (ofis silinmiş, junction üyeliği kaldırılmış, vb.) sessizce HQ'ya reset eder.
///
/// Authentication pipeline'dan SONRA + Session middleware'inden SONRA çalışmalı.
/// Skip edilenler: anonymous requests, /auth/*, static files, /health/*.
/// </summary>
public sealed class ActiveBranchOfficeMiddleware(
    RequestDelegate next,
    ILogger<ActiveBranchOfficeMiddleware> logger)
{
    private static readonly string[] SkippedPathPrefixes =
    [
        "/auth/",
        "/health/",
        "/lib/",
        "/css/",
        "/js/",
        "/images/",
        "/favicon"
    ];

    public async Task InvokeAsync(HttpContext context, IActiveBranchOfficeAccessor accessor)
    {
        // Anonymous veya whitelist path → direkt geç
        if (context.User.Identity?.IsAuthenticated != true || IsSkippedPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            // Auth claims bozuk — middleware'in sorumluluğu değil, pipeline devam etsin
            await next(context);
            return;
        }

        try
        {
            await accessor.ValidateAndResetIfStaleAsync(userId, context.RequestAborted);
        }
        catch (Exception ex)
        {
            // Session validation kritik değil — log'la ve devam et, kullanıcı deneyimi aksamasın
            logger.LogWarning(ex,
                "ActiveBranchOfficeMiddleware validation failed for user {UserId}, continuing pipeline",
                userId);
        }

        await next(context);
    }

    private static bool IsSkippedPath(PathString path)
    {
        if (!path.HasValue) return false;
        var value = path.Value!;
        foreach (var prefix in SkippedPathPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
