using System.Security.Claims;
using Amazon.Runtime.Internal;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.User;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.Utility.Constants;
using Entegrasyon.MVC.Utility.Services;
using Entegrasyon.MVC.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Results;
using Shared.User.Dto;

namespace Entegrasyon.MVC.Controllers
{
    public class AuthController(AuthManager authManager, IHttpContextAccessor httpContextAccessor)
        : Controller
    {
        private readonly HttpContext _httpContext = httpContextAccessor.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        [HttpGet]
        [RestoreModelStateFromTempData]
        public IActionResult Login(string returnUrl = null)
        {
            if(User.Identity!.IsAuthenticated)
            {
                return Redirect(returnUrl ?? "~/");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SetTempDataModelState]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(!ModelState.IsValid)
                return RedirectToAction(nameof(Login));

            var result = await authManager.LoginAsync(model.UserName,model.Password);
            if(!result.Success)
            {
                ModelState.AddModelError(string.Empty,result.Message);
                return RedirectToAction(nameof(Login));
            }

            if(result is SuccessDataResult<LoginNewPasswordDto> dto)
            {
                return RedirectToAction(nameof(CreateNewPassword),new { userId = dto.Data.UserId });
            }
            //login işlemi
            var user = (result as SuccessDataResult<UserLoginSuccessDto>)!.Data;
            var claims = new List<Claim>()
            {
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (ClaimTypes.Name,user.Name),
                new (ClaimTypes.Surname,user.Surname),
                new (ClaimTypes.GivenName,user.Username)
            };
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)));
            user.Roles.SelectMany(r=>r.Claims).ToList().ForEach(x =>
            {
                claims.Add(new Claim(StringConstant.Permission,x.Name));
            });

            var claimsIdentity = new ClaimsIdentity(
                claims,CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrincipal = new ClaimsPrincipal(claimsIdentity);
            await _httpContext.SignInAsync(claimPrincipal);
            if(!string.IsNullOrEmpty(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);
            return RedirectToAction("Index","Home");
        }

        [HttpGet]
        [RestoreModelStateFromTempData]
        public IActionResult CreateNewPassword(Guid? userId)
        {
            if(userId == null)
                return NotFound();
            var model = new CreatePasswordViewModel() { UserId = userId.Value };
            return View(model);
        }

        [HttpPost]
        [SetTempDataModelState]
        public async Task<IActionResult> CreateNewPassword(CreatePasswordViewModel model)
        {
            if(!ModelState.IsValid)
                return RedirectToAction(nameof(CreateNewPassword));
            var result = await authManager.CreatePassword(model.Password,model.UserId);
            if(!result.Success)
            {
                ModelState.AddModelError("",result.Message);
                return RedirectToAction(nameof(CreateNewPassword));
            }
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId,string userName)
        {
            if(userId == null)
            {
                return NotFound();
            }

            var model = new ResetPasswordViewModel()
            {
                UserId = userId,
                UserName = userName
            };
            return PartialView("Partials/Modals/Auth/_ResetPasswordForm",model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if(!ModelState.IsValid)
                return Json(new ErrorResult("Gönderdiğiniz veride bir hata var lütfen tekrar deneyin."));
            var result = await authManager.AssignNewPassword(model.TemporaryPassword,model.UserId);
            return Json(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _httpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}