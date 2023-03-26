using AutoMapper;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.ViewModels.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.User;
using Shared.User.Services;

namespace Entegrasyon.MVC.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationUserManager _userManager;
        private readonly BranchOfficeManager _branchOfficeManager;
        private readonly IRoleManager<RootRole,RootClaim> _roleManager;
        private readonly IMapper _mapper;

        public UsersController(ApplicationUserManager userManager,IMapper mapper,BranchOfficeManager branchOfficeManager,IRoleManager<RootRole,RootClaim> roleManager)
        {
            _userManager = userManager;
            _mapper = mapper;
            _branchOfficeManager = branchOfficeManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(int pageIndex = 0,int pageSize = 50)
        {
            var result = await _userManager.GetPaginatedUserDetails(pageIndex,pageSize);
            return View(result.Data);
        }
        [HttpGet]
        [RestoreModelStateFromTempData]
        public async Task<IActionResult> Edit(Guid? id)
        
        {
            if (id == null)
                return NotFound();
            var user = await _userManager.GetUserById(id.Value);
            if (user == null)
                return NotFound();
            var model = _mapper.Map<UserEditViewModel>(user);
            model.BranchOffices ??= (await _branchOfficeManager.GetBranchList()).Data
                .Select(b => new SelectListItem(b.Name,b.Id.ToString())).ToList();
            model.Roles ??= (await _roleManager.GetRolesSelectList())
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
            var dto = _mapper.Map<UserEditDto>(model);
            var result = await _userManager.EditUser(dto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty,result.Message);
                return RedirectToAction(nameof(Edit));
            }
                
            return RedirectToAction(); //detay
        }

        [HttpGet]
        [RestoreModelStateFromTempData]
        public async Task<IActionResult> Add()//UserAddViewModel model
        {
            var model = new UserAddViewModel();
            model.BranchOffices ??= (await _branchOfficeManager.GetBranchList()).Data
                .Select(b => new SelectListItem(b.Name, b.Id.ToString())).ToList();
            model.Roles ??= (await _roleManager.GetRolesSelectList())
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

            var dto = _mapper.Map<AddUserDto>(model);
            var result = await _userManager.AddUser(dto);
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
            var result = await _userManager.GetUserDetails(id);
            if (!result.Success)
                return BadRequest(result.Message);
            var model = _mapper.Map<UserDetailViewModel>(result.Data);
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
            await _userManager.SetPassive(userId);
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
            var result = await _userManager.SoftDelete(userId);
            return Json(result);
        }
    }
}
