using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Users;

[Authorize]
public class UserController(
    IApplicationUserManager userManager,
    IRoleService roleService) : Controller
{
    [HttpGet("/users")]
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData.SetPageTitle("Kullanicilar");
        ViewData.SetActiveNav("users");

        var result = await userManager.GetPaginatedUserDetails(page - 1, 20);

        if (Request.IsHtmx())
            return PartialView("Partials/_UserTable", result.Data);

        return View(result.Data);
    }

    [HttpGet("/users/create")]
    public async Task<IActionResult> Create()
    {
        ViewData.SetPageTitle("Yeni Kullanici");
        ViewData.SetActiveNav("users");
        ViewData.SetBreadcrumb(("Kullanicilar", "/users"), ("Yeni Kullanici", null));

        var roles = await roleService.GetRolesSelectList();
        ViewBag.Roles = roles;
        return View();
    }

    [HttpPost("/users/create")]
    public async Task<IActionResult> Create(AddUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewData.SetPageTitle("Yeni Kullanici");
            ViewData.SetActiveNav("users");
            ViewData.SetBreadcrumb(("Kullanicilar", "/users"), ("Yeni Kullanici", null));

            var roles = await roleService.GetRolesSelectList();
            ViewBag.Roles = roles;
            return View(dto);
        }

        var result = await userManager.AddUser(dto);

        if (result.Success)
            TempData.SetSuccess("Kullanici basariyla eklendi.");
        else
            TempData.SetError(result.Message ?? "Kullanici eklenirken bir hata olustu.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/users/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await userManager.GetUserEditDetail(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Kullanici bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Kullanici Duzenle");
        ViewData.SetActiveNav("users");
        ViewData.SetBreadcrumb(("Kullanicilar", "/users"), ("Duzenle", null));

        var roles = await roleService.GetRolesSelectList();
        ViewBag.Roles = roles;
        return View(result.Data);
    }

    [HttpPost("/users/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, UserEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewData.SetPageTitle("Kullanici Duzenle");
            ViewData.SetActiveNav("users");
            ViewData.SetBreadcrumb(("Kullanicilar", "/users"), ("Duzenle", null));

            var roles = await roleService.GetRolesSelectList();
            ViewBag.Roles = roles;

            var detail = await userManager.GetUserEditDetail(id);
            return View(detail.Data);
        }

        var result = await userManager.EditUser(dto);

        if (result.Success)
            TempData.SetSuccess("Kullanici basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Kullanici guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/users/{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await userManager.SoftDelete(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Kullanici silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Kullanici basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Kullanici silinemedi.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/users/{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await userManager.ToggleActive(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Kullanici durumu degistirildi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Islem basarisiz.", type = "danger" });
            return StatusCode(422);
        }

        return RedirectToAction(nameof(Index));
    }
}
