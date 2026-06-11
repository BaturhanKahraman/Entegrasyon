using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.Auth.ViewModels;
using Entegrasyon.MVC.Infrastructure.BranchOffices;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Auth;

[AllowAnonymous]
public class AuthController(
    IAuthService authService,
    ITenantContext tenantContext,
    IActiveBranchOfficeAccessor activeBranchOfficeAccessor) : Controller
{
    [HttpGet("/auth/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(returnUrl ?? "/");

        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost("/auth/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await authService.LoginAsync(vm.Username, vm.Password);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Giriş başarısız.");
            return View(vm);
        }

        // Kullanıcı ilk kez giriş yapıyor — şifre oluşturma gerekli
        if (result is SuccessDataResult<LoginNewPasswordDto> newPasswordResult)
        {
            TempData.SetWarning("Lütfen yeni şifrenizi oluşturun.");
            return RedirectToAction(nameof(ResetPassword),
                new { userId = newPasswordResult.Data.UserId });
        }

        // Başarılı giriş — claims oluştur
        if (result is not SuccessDataResult<UserLoginSuccessDto> successResult)
        {
            ModelState.AddModelError("", "Beklenmeyen bir hata oluştu.");
            return View(vm);
        }

        var user = successResult.Data;

        // Permission'ları rollerden topla
        var permissions = new List<string>();
        if (user.Roles is not null)
        {
            foreach (var role in user.Roles)
            {
                if (role.RoleClaims is not null)
                {
                    permissions.AddRange(role.RoleClaims
                        .Where(rc => !string.IsNullOrEmpty(rc.Permission))
                        .Select(rc => rc.Permission!));
                }
            }
        }

        // Claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, $"{user.Name} {user.Surname}"),
            new("TenantId", (tenantContext.IsInitialized ? tenantContext.TenantId : 1).ToString())
        };
        // Oturum geçersizleştirme damgası — her istekte DB ile karşılaştırılır (auto-logout).
        if (!string.IsNullOrEmpty(user.SecurityStamp))
            claims.Add(new Claim(StringConstants.SecurityStampClaimType, user.SecurityStamp));
        claims.AddRange(user.Roles?.Select(r => new Claim(ClaimTypes.Role, r.Name)) ?? []);
        claims.AddRange(permissions.Distinct().Select(p => new Claim("Permission", p)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                // "Beni hatırla" işaretliyse kalıcı + pratikte sonsuz (1 yıl; SlidingExpiration ile
                // aktif kullanıcıda sürekli yenilenir). Değilse oturum çerezi olarak 1 güne kadar geçerli.
                IsPersistent = vm.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(
                    vm.RememberMe ? TimeSpan.FromDays(365) : TimeSpan.FromDays(1))
            });

        // Aktif şube ofisini Session'a yerleştir.
        // Öncelik: RememberLastBranchOffice → DefaultBranchOfficeId → HQ.
        // user.Id kullanıyoruz çünkü current request'te HttpContext.User hâlâ anonymous (sign-in bir sonraki request'te aktif olur).
        await activeBranchOfficeAccessor.ResolveAndStoreAsync(user.Id);

        return Redirect(vm.ReturnUrl ?? "/");
    }

    [HttpPost("/auth/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/auth/logout")]
    public async Task<IActionResult> LogoutGet()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/auth/reset-password")]
    public IActionResult ResetPassword(string? userId = null)
    {
        ViewData.SetPageTitle("Şifre Sifirla");
        return View(new PasswordResetVm { UserId = userId });
    }

    [HttpPost("/auth/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(PasswordResetVm vm)
    {
        ViewData.SetPageTitle("Şifre Sifirla");

        if (!ModelState.IsValid)
            return View(vm);

        if (!Guid.TryParse(vm.UserId, out var userId))
        {
            ModelState.AddModelError("", Messages.LoginFailedWrongPassword);
            return View(vm);
        }

        var dto = new SetInitialPasswordDto(userId, vm.TemporaryPassword, vm.NewPassword, vm.ConfirmPassword);
        var result = await authService.SetInitialPasswordAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? Messages.ProcessFailed);
            return View(vm);
        }

        // PRG — başarı sonrası GET'e yönlendir (geri tuşu güvenli).
        TempData.SetSuccess(result.Message ?? Messages.InitialPasswordSet);
        return RedirectToAction(nameof(Login));
    }
}
