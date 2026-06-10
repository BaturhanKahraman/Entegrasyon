using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Roles;

[Authorize]
public class RoleController(IRoleService roleService) : Controller
{
    [HttpGet("/roles")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Roller");
        ViewData.SetActiveNav("roles");

        var roles = await roleService.GetRolesWithClaimsAsync();

        if (Request.IsHtmx())
            return PartialView("Partials/_RoleTable", roles);

        return View(roles);
    }

    [HttpGet("/roles/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Rol");
        ViewData.SetActiveNav("roles");
        ViewData.SetBreadcrumb(("Roller", "/roles"), ("Yeni Rol", null));

        ViewBag.AllPermissions = AppPermissions.GetAllPermissions();
        return View();
    }

    [HttpPost("/roles/create")]
    public async Task<IActionResult> Create(string name, List<string> permissions)
    {
        var dto = new AddRoleDto(name, permissions);
        var result = await roleService.AddRole(dto);

        if (result.Success)
            TempData.SetSuccess("Rol basariyla eklendi.");
        else
            TempData.SetError(result.Message ?? "Rol eklenirken bir hata olustu.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/roles/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData.SetPageTitle("Rol Duzenle");
        ViewData.SetActiveNav("roles");
        ViewData.SetBreadcrumb(("Roller", "/roles"), ("Duzenle", null));

        var roles = await roleService.GetRolesWithClaimsAsync();
        var role = roles.FirstOrDefault(r => r.Id == id);

        if (role is null)
        {
            TempData.SetError("Rol bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewBag.AllPermissions = AppPermissions.GetAllPermissions();
        return View(role);
    }

    [HttpPost("/roles/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, string name, List<string> permissions)
    {
        var dto = new EditRoleDto(id, name, permissions);
        var result = await roleService.UpdateRole(dto);

        if (result.Success)
            TempData.SetSuccess("Rol basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Rol guncellenirken bir hata olustu.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/roles/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await roleService.DeleteRole(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Rol silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Rol basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Rol silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
