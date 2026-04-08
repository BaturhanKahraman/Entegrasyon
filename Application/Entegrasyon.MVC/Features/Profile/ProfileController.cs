using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Entegrasyon.MVC.Features.Profile.ViewModels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Users;
using System.Security.Claims;

namespace Entegrasyon.MVC.Features.Profile;

[Authorize]
public class ProfileController(
    IAuthService authService,
    IApplicationUserManager userManager,
    IApplicationLogManager applicationLogManager) : Controller
{
    [HttpGet("/profile")]
    public IActionResult Index()
    {
        ViewData.SetPageTitle("Profil");
        ViewData.SetActiveNav("profile");

        var fullName = User.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var parts = fullName.Split(' ', 2);

        ViewBag.UserName = User.Identity?.Name ?? "";
        ViewBag.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        ViewBag.Name = parts.Length > 0 ? parts[0] : "";
        ViewBag.Surname = parts.Length > 1 ? parts[1] : "";

        return View();
    }

    [HttpPost("/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string name, string surname)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            TempData.SetError("Kullanici kimlik bilgisi alinamadi.");
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateProfileDto { Name = name, Surname = surname };
        var result = await userManager.UpdateOwnProfile(userId, dto);

        if (result.Success)
            TempData.SetSuccess("Profil bilgileriniz guncellendi.");
        else
            TempData.SetError(result.Message ?? "Profil guncellenemedi.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/profile/change-password")]
    public IActionResult ChangePassword()
    {
        ViewData.SetPageTitle("Sifre Degistir");
        ViewData.SetActiveNav("profile");
        ViewData.SetBreadcrumb(("Profil", "/profile"), ("Sifre Degistir", null));

        return View(new ChangePasswordVm());
    }

    [HttpPost("/profile/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm vm)
    {
        ViewData.SetPageTitle("Sifre Degistir");
        ViewData.SetActiveNav("profile");
        ViewData.SetBreadcrumb(("Profil", "/profile"), ("Sifre Degistir", null));

        if (!ModelState.IsValid)
            return View(vm);

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            TempData.SetError("Kullanici kimlik bilgisi alinamadi.");
            return View(vm);
        }

        var dto = new ChangePasswordDto
        {
            CurrentPassword = vm.OldPassword,
            NewPassword = vm.NewPassword,
            ConfirmPassword = vm.ConfirmPassword
        };

        var result = await authService.ChangeOwnPassword(userId, dto);

        if (result.Success)
        {
            TempData.SetSuccess("Sifreniz basariyla degistirildi.");
            return RedirectToAction(nameof(ChangePassword));
        }

        TempData.SetError(result.Message ?? "Sifre degistirilemedi.");
        return View(vm);
    }

    [HttpGet("/profile/activity")]
    public async Task<IActionResult> Activity(int page = 0)
    {
        ViewData.SetPageTitle("Aktivite Gecmisi");
        ViewData.SetActiveNav("profile");
        ViewData.SetBreadcrumb(("Profil", "/profile"), ("Aktivite Gecmisi", null));

        var result = await applicationLogManager.GetPaginatedLogs(pageIndex: page, itemCount: 50);
        ViewBag.Logs = result.Success ? result.Data : null;

        return View();
    }
}
