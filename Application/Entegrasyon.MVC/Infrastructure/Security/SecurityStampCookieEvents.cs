using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Entegrasyon.MVC.Infrastructure.Security;

/// <summary>
/// Her authenticated istekte cookie'deki kimliği DB ile doğrular: kullanıcı hâlâ aktif/silinmemiş mi
/// ve SecurityStamp eşleşiyor mu? Pasifleştirilen / silinen / şifresi sıfırlanan kullanıcının
/// aktif oturumu burada düşürülür (RejectPrincipal + SignOut) → otomatik logout.
///
/// Scoped servis bağımlılığı (ISecurityStampValidator) gerektirdiğinden DI'a kayıtlı; cookie
/// options'ında <c>EventsType = typeof(SecurityStampCookieEvents)</c> ile bağlanır.
/// </summary>
public sealed class SecurityStampCookieEvents(ISecurityStampValidator validator)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
            return;

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            // Kimlik claim'i bozuk → güvenli tarafta kal, oturumu düşür.
            await RejectAsync(context);
            return;
        }

        var stamp = principal.FindFirstValue(StringConstants.SecurityStampClaimType);
        if (!await validator.IsValidAsync(userId, stamp, context.HttpContext.RequestAborted))
            await RejectAsync(context);
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    // EventsType set edildiğinde options.Events lambda'ları yok sayılır; mevcut HTMX-aware
    // yönlendirme davranışını korumak için bu iki override burada yaşar.
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Headers.ContainsKey("HX-Request"))
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("HX-Redirect", "/auth/login");
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Headers.ContainsKey("HX-Request"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
