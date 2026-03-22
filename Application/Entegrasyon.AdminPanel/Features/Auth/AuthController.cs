using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure.Auth;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.Auth;

public class AuthController(AdminPanelDbContext dbContext) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View("~/Features/Auth/Views/Login.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View("~/Features/Auth/Views/Login.cshtml", model);

        var user = await dbContext.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

        if (user is null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya parola hatalı.");
            return View("~/Features/Auth/Views/Login.cshtml", model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            IssuedUtc = DateTimeOffset.UtcNow
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
