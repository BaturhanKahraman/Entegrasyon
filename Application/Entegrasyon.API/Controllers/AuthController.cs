using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthManager _authManager;

        public AuthController(AuthManager authManager)
        {
            _authManager = authManager;
        }

        // GET: api/<AuthController>
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authManager.LoginAsync(dto.UserName, dto.Password);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpPost]
        public async Task<IActionResult> AssignFirstPassword(AssignFirstPasswordDto dto)
        {
            var result = await _authManager.TakeNewPasswordAsync(dto.Password, dto.UserId);
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> LogOut(string userId)
        {
            var result = await _authManager.LogOut(userId);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
