using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationUserManager _applicationUserManager;
        
        public UsersController(ApplicationUserManager applicationUserManager)
        {
            _applicationUserManager = applicationUserManager;
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(AddUserDto dto)
        {
            var result =await _applicationUserManager.AddUser(dto);
            if (result.Success)
                return Created("",dto);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetPaginatedUserDetailList(int page=1,int itemNumber=50)
        {
            var result = await _applicationUserManager.GetPaginatedUserDetails(null,page,itemNumber);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetUserDetailList()
        {
            var result = await _applicationUserManager.GetUserDetails();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetUserCount()
        {
            var result = await _applicationUserManager.GetUserCount();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        
    }
}
