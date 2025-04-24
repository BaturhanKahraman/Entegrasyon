using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.ViewModels.User;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.User;

namespace Entegrasyon.MVC.Controllers
{
    [Authorize]
    public class UsersController(
        ApplicationUserManager userManager,
        BranchOfficeManager branchOfficeManager,
        RoleService roleManager)
        : Controller
    {
        public async Task<IActionResult> Index(int pageIndex = 0,int pageSize = 50)
        {
            var result = await userManager.GetPaginatedUserDetails(pageIndex,pageSize);
            return View(result.Data);
        }
        [HttpGet]
        [RestoreModelStateFromTempData]
        public async Task<IActionResult> Edit(Guid? id,CancellationToken token)
        
        {
            if (id == null)
                return NotFound();
            var user = await userManager.GetUserById(id.Value);
            if (user == null)
                return NotFound();
            var model = user.Adapt<UserEditViewModel>();
            model.BranchOffices ??= (await branchOfficeManager.GetBranchList(token)).Data
                .Select(b => new SelectListItem(b.Name,b.Id.ToString())).ToList();
            model.Roles ??= (await roleManager.GetRolesSelectList(token))
                .Select(role => new SelectListItem(role.Name,role.Id.ToString())).ToList();
            return View(model);
        }

        [HttpPost]
        [SetTempDataModelState]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Edit));
            //var dto = new UserEditDto(model.Id, model.BranchOfficeId, model.Name, model.Surname, model.UserName,
            //    model.Email, model.RoleId);
            var dto = model.Adapt<UserEditDto>();
            var result = await userManager.EditUser(dto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty,result.Message);
                return RedirectToAction(nameof(Edit));
            }
                
            return RedirectToAction(); //detay
        }

        [HttpGet]
        [RestoreModelStateFromTempData]
        public async Task<IActionResult> Add(CancellationToken token)//UserAddViewModel model
        {
            var model = new UserAddViewModel();
            model.BranchOffices ??= (await branchOfficeManager.GetBranchList(token)).Data
                .Select(b => new SelectListItem(b.Name, b.Id.ToString())).ToList();
            model.Roles ??= (await roleManager.GetRolesSelectList(token))
                .Select(role => new SelectListItem(role.Name, role.Id.ToString())).ToList();

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SetTempDataModelState]
        public async Task<IActionResult> Add(UserAddViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Add),model);
            }

            var dto = model.Adapt<AddUserDto>();
            var result = await userManager.AddUser(dto);
            if(result.Success)
            {
                return RedirectToAction("Index");
            }
            ModelState.AddModelError(string.Empty,"result.Message");
            return RedirectToAction(nameof(Add),model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();
            var result = await userManager.GetUserDetails(id);
            if (!result.Success)
                return BadRequest(result.Message);
            var model = result.Adapt<UserDetailViewModel>();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetPassive(IFormCollection fc)
        {
            if (!fc.ContainsKey("id"))
                return BadRequest();
            bool isConvertible = Guid.TryParse(fc["id"].ToString(),out var userId);
            if(!isConvertible)
                return BadRequest();
            await userManager.SetPassive(userId);
            return RedirectToAction(nameof(Details),new {id=userId.ToString()});
        }

        [HttpPost]
        public async Task<IActionResult> Delete(IFormCollection fc)
        {
            if(!fc.ContainsKey("id"))
                return BadRequest();
            bool isConvertible = Guid.TryParse(fc["id"].ToString(),out var userId);
            if (!isConvertible)
                return BadRequest();
            var result = await userManager.SoftDelete(userId);
            return Json(result);
        }
    }
}
