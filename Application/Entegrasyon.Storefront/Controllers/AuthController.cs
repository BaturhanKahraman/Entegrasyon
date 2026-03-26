using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class AuthController(
    IStorefrontTenantContext tenant,
    IStorefrontAuthManager authManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(StorefrontLoginDto dto, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/");

        var result = await authManager.LoginAsync(tenant.TenantId, dto.Email, dto.Password);
        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var auth = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.CustomerId.ToString()),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Name, auth.Customer.FullName ?? $"{auth.Customer.Name} {auth.Customer.Surname}"),
            new("TenantId", auth.TenantId.ToString()),
            new("AuthId", auth.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = dto.RememberMe,
                ExpiresUtc = dto.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            });

        return Redirect(returnUrl ?? "/");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(StorefrontRegisterDto dto)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/");

        var registerDto = dto with { TenantId = tenant.TenantId };
        var result = await authManager.RegisterAsync(registerDto);
        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        // Auto login after register
        var auth = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.CustomerId.ToString()),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Name, $"{dto.Name} {dto.Surname}"),
            new("TenantId", auth.TenantId.ToString()),
            new("AuthId", auth.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Redirect("/hesabim");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        await authManager.RequestPasswordResetAsync(tenant.TenantId, email);
        // Always show success to prevent email enumeration
        ViewBag.Success = true;
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ViewBag.Error = "Sifreler uyusmuyor.";
            ViewBag.Token = token;
            return View();
        }
        var result = await authManager.ResetPasswordAsync(tenant.TenantId, token, password);
        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            ViewBag.Token = token;
            return View();
        }
        ViewBag.Success = true;
        return View();
    }

    public async Task<IActionResult> ConfirmEmail(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return View(model: (object)"Gecersiz link.");

        var result = await authManager.ConfirmEmailAsync(tenant.TenantId, token);
        return View(model: result.Success ? (object)"Email adresiniz dogrulandi!" : result.Message);
    }
}
