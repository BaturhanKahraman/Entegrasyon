using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Auth;

/// <summary>
/// Salt-okuma oturum doğrulayıcısı. Her authenticated istekte çağrılır → AddLog ile spam yapma,
/// sadece ILogger (developer-facing). Hata durumunda fail-closed: doğrulama yapılamazsa oturumu düşür.
/// </summary>
public sealed class SecurityStampValidator(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<SecurityStampValidator> logger) : ISecurityStampValidator
{
    public async Task<bool> IsValidAsync(Guid userId, string? securityStamp, CancellationToken token = default)
    {
        // Cookie'de damga yoksa (eski cookie / login akışı stamp yazmamış) → geçersiz say.
        if (string.IsNullOrEmpty(securityStamp))
            return false;

        await using var context = await contextFactory.CreateDbContextAsync(token);

        // Sadece gereken alanları çek — full entity değil. By-PK lookup, indexli.
        var dbState = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.IsDeleted, u.SecurityStamp })
            .FirstOrDefaultAsync(token);

        if (dbState is null || !dbState.IsActive || dbState.IsDeleted)
        {
            logger.LogInformation(
                "Oturum dogrulama reddedildi (kullanici yok/pasif/silinmis). UserId={UserId}", userId);
            return false;
        }

        if (!string.Equals(dbState.SecurityStamp, securityStamp, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Oturum dogrulama reddedildi (SecurityStamp degismis). UserId={UserId}", userId);
            return false;
        }

        return true;
    }
}
