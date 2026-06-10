using System.Security.Claims;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.MVC.Infrastructure.Security;

/// <summary>
/// Her authenticated istekte kullanıcının LastSeenAt damgasını günceller — admin detay sayfasındaki
/// "online/offline" ve "ne süredir aktif" göstergesini besler.
///
/// Perf-bilinçli ve SecurityStamp doğrulamasından AYRI:
/// - Throttle: kullanıcı başına en fazla 1 dk'da bir DB-write (LastSeenThrottle, in-memory).
/// - Yazım tek SQL <c>ExecuteUpdate</c> (full-entity yüklemez, no-tracking footgun yok, RowVersion tetiklemez).
/// - Fire-and-forget değil ama best-effort: yazım hatası isteği bozmaz (sadece ILogger).
/// Authentication'dan SONRA çalışmalı (claims hazır olsun).
/// </summary>
public sealed class LastSeenMiddleware(
    RequestDelegate next,
    LastSeenThrottle throttle,
    ILogger<LastSeenMiddleware> logger)
{
    private static readonly string[] SkippedPathPrefixes =
    [
        "/lib/", "/css/", "/js/", "/images/", "/favicon", "/health/"
    ];

    public async Task InvokeAsync(HttpContext context, IDbContextFactory<IntegrationDbContext> contextFactory)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !IsSkippedPath(context.Request.Path)
            && Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            && throttle.ShouldWrite(userId, DateTimeOffset.UtcNow))
        {
            await UpdateLastSeenAsync(contextFactory, userId, context.RequestAborted);
        }

        await next(context);
    }

    private async Task UpdateLastSeenAsync(
        IDbContextFactory<IntegrationDbContext> contextFactory, Guid userId, CancellationToken token)
    {
        try
        {
            await using var db = await contextFactory.CreateDbContextAsync(token);
            await db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastSeenAt, DateTimeOffset.UtcNow), token);
        }
        catch (OperationCanceledException)
        {
            // İstek iptal edildi — LastSeenAt best-effort, sessizce geç.
        }
        catch (Exception ex)
        {
            // LastSeenAt yazımı asla isteği bozmamalı; developer-facing log yeterli.
            logger.LogWarning(ex, "LastSeenAt guncellenemedi {UserId}", userId);
        }
    }

    private static bool IsSkippedPath(PathString path)
    {
        foreach (var prefix in SkippedPathPrefixes)
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
