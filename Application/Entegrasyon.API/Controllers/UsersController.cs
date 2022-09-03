using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationUserManager _userManager;

        public UsersController(ApplicationUserManager userManager)
        {
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(AddUserDto dto)
        {
            var result =await _userManager.AddUser(dto);
            if (result.Success)
                return Created("",dto);
            return BadRequest(result.Message);
        }
    }
}
