using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class AuthController(
    IStorefrontTenantContext tenant,
    IStorefrontAuthManager authManager,
    IStorefrontEmailService emailService,
    IStorefrontReferralManager referralManager) : Controller
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
            // Record failed login if auth record exists (email found but wrong password)
            if (result.Data is not null && result.Data.Id > 0)
            {
                await authManager.RecordLoginAttemptAsync(
                    result.Data.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    false, result.Message);
            }

            ViewBag.Error = result.Message;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var auth = result.Data;

        // Record successful login
        await authManager.RecordLoginAttemptAsync(
            auth.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            true);

        // Check if 2FA is enabled
        if (auth.TwoFactorEnabled)
        {
            // Store auth info temporarily in session for 2FA verification
            HttpContext.Session.SetInt32("Pending2FA_AuthId", auth.Id);
            HttpContext.Session.SetInt32("Pending2FA_CustomerId", auth.CustomerId);
            HttpContext.Session.SetString("Pending2FA_Email", auth.Email);
            HttpContext.Session.SetString("Pending2FA_Name", auth.Customer.FullName ?? $"{auth.Customer.Name} {auth.Customer.Surname}");
            HttpContext.Session.SetInt32("Pending2FA_TenantId", auth.TenantId);
            HttpContext.Session.SetString("Pending2FA_ReturnUrl", returnUrl ?? "/");
            if (dto.RememberMe)
                HttpContext.Session.SetString("Pending2FA_RememberMe", "true");

            return Redirect("/giris/2fa");
        }

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
    public IActionResult Register(string? @ref = null)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/");
        ViewBag.ReferralCode = @ref;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(StorefrontRegisterDto dto, string? referralCode = null)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/");

        var registerDto = dto with { TenantId = tenant.TenantId };
        var result = await authManager.RegisterAsync(registerDto);
        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            ViewBag.ReferralCode = referralCode;
            return View();
        }

        // Send verification email (fire-and-forget, don't block registration)
        var authData = result.Data;
        _ = emailService.SendEmailVerificationAsync(
            authData.Email, dto.Name, authData.EmailConfirmationToken!,
            tenant.Settings.StoreName, tenant.Domain.DomainName);

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

        // Process referral code if provided
        if (!string.IsNullOrWhiteSpace(referralCode))
        {
            _ = referralManager.RegisterReferralAsync(tenant.TenantId, referralCode, auth.CustomerId);
        }

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
        var resetResult = await authManager.RequestPasswordResetAsync(tenant.TenantId, email);
        if (resetResult.Success)
        {
            // Fire-and-forget password reset email
            _ = emailService.SendPasswordResetAsync(
                email, email, resetResult.Data,
                tenant.Settings.StoreName, tenant.Domain.DomainName);
        }
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

    [HttpGet]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        // Read the external authentication result
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (authenticateResult?.Principal is null)
            return RedirectToAction("Login");

        var claims = authenticateResult.Principal.Claims.ToList();
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "";
        var surname = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(externalId))
        {
            ViewBag.Error = "Sosyal giris sirasinda e-posta bilgisi alinamadi.";
            return View("Login");
        }

        // Determine provider from the identity
        var provider = authenticateResult.Principal.Identity?.AuthenticationType ?? "External";

        var result = await authManager.ExternalLoginAsync(
            tenant.TenantId, provider, externalId, email, name, surname);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View("Login");
        }

        var auth = result.Data;

        // Sign in with application cookie
        var appClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.CustomerId.ToString()),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Name, $"{name} {surname}".Trim()),
            new("TenantId", auth.TenantId.ToString()),
            new("AuthId", auth.Id.ToString())
        };

        var identity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        return Redirect(returnUrl ?? "/");
    }

    public async Task<IActionResult> ConfirmEmail(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return View(model: (object)"Gecersiz link.");

        var result = await authManager.ConfirmEmailAsync(tenant.TenantId, token);
        return View(model: result.Success ? (object)"Email adresiniz dogrulandi!" : result.Message);
    }

    [HttpGet]
    public IActionResult TwoFactor()
    {
        var authId = HttpContext.Session.GetInt32("Pending2FA_AuthId");
        if (authId is null)
            return RedirectToAction("Login");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactor(string code, bool useRecovery = false)
    {
        var authId = HttpContext.Session.GetInt32("Pending2FA_AuthId");
        var customerId = HttpContext.Session.GetInt32("Pending2FA_CustomerId");
        var email = HttpContext.Session.GetString("Pending2FA_Email");
        var name = HttpContext.Session.GetString("Pending2FA_Name");
        var tenantId = HttpContext.Session.GetInt32("Pending2FA_TenantId");
        var returnUrl = HttpContext.Session.GetString("Pending2FA_ReturnUrl") ?? "/";
        var rememberMe = HttpContext.Session.GetString("Pending2FA_RememberMe") == "true";

        if (authId is null || customerId is null || email is null || name is null || tenantId is null)
        {
            return RedirectToAction("Login");
        }

        Entity.Results.IResult result;
        if (useRecovery)
            result = await authManager.VerifyRecoveryCodeAsync(authId.Value, code);
        else
            result = await authManager.Verify2FAAsync(authId.Value, code);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        // Clear 2FA session data
        HttpContext.Session.Remove("Pending2FA_AuthId");
        HttpContext.Session.Remove("Pending2FA_CustomerId");
        HttpContext.Session.Remove("Pending2FA_Email");
        HttpContext.Session.Remove("Pending2FA_Name");
        HttpContext.Session.Remove("Pending2FA_TenantId");
        HttpContext.Session.Remove("Pending2FA_ReturnUrl");
        HttpContext.Session.Remove("Pending2FA_RememberMe");

        // Sign in
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customerId.Value.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new("TenantId", tenantId.Value.ToString()),
            new("AuthId", authId.Value.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            });

        return Redirect(returnUrl);
    }
}
