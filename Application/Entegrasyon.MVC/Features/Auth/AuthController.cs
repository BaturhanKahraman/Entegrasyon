using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.Auth.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Auth;

[AllowAnonymous]
public class AuthController(
    IAuthService authService,
    ITenantContext tenantContext) : Controller
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
            ModelState.AddModelError("", result.Message ?? "Giris basarisiz.");
            return View(vm);
        }

        // Kullanıcı ilk kez giriş yapıyor — şifre oluşturma gerekli
        if (result is SuccessDataResult<LoginNewPasswordDto> newPasswordResult)
        {
            TempData.SetWarning("Lutfen yeni sifrenizi olusturun.");
            return RedirectToAction(nameof(ResetPassword),
                new { userId = newPasswordResult.Data.UserId });
        }

        // Başarılı giriş — claims oluştur
        if (result is not SuccessDataResult<UserLoginSuccessDto> successResult)
        {
            ModelState.AddModelError("", "Beklenmeyen bir hata olustu.");
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
        claims.AddRange(user.Roles?.Select(r => new Claim(ClaimTypes.Role, r.Name)) ?? []);
        claims.AddRange(permissions.Distinct().Select(p => new Claim("Permission", p)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = vm.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

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
        ViewData.SetPageTitle("Sifre Sifirla");
        return View(new PasswordResetVm { UserId = userId });
    }
}
