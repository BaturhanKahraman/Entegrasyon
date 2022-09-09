using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Mvc;
using Shared.User.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        //private readonly AuthManager _authManager;
        private readonly IUserManager<ApplicationUser> _userManager;
        private readonly ILoginManager<ApplicationUser> _loginManager;
        private readonly ApplicationLogManager _applicationLogManager;    

        public AuthController(IUserManager<ApplicationUser> userManager,ILoginManager<ApplicationUser> loginManager, ApplicationLogManager applicationLogManager)
        {
            _userManager = userManager;
            _loginManager = loginManager;
            _applicationLogManager = applicationLogManager;
        }

        // GET: api/<AuthController>
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _loginManager.LoginWithUserNameAsync(dto.UserName,dto.Password);
            if (result.Success)
            {
                await _applicationLogManager.AddLog("Başarıyla giriş yapıldı.",LogType.Auth);
                return Ok(result);
            }
            await _applicationLogManager.AddLog($"Giriş başarısız {result.Message}",LogType.Auth);
            return BadRequest(result.Message);
        }

        [HttpPost]
        public async Task<IActionResult> AssignFirstPassword(AssignFirstPasswordDto dto)
        {
            var result = await _userManager.CreateUserPasswordAsync(dto.Password,dto.UserId);
            if (result.Success)
            {
                await _applicationLogManager.AddLog("Şifre değiştirme yapıldı.",LogType.Auth);
                return Ok(result);  
            }
            await _applicationLogManager.AddLog($"Şifre değiştirme başarısız! {result.Message}",LogType.Auth);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> LogOut(string userId)
        {
            var result = await _loginManager.LogOutAsync(Guid.Parse(userId));
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
