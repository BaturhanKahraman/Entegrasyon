using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
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
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetPaginatedUserDetailList(int pageIndex=0,int itemNumber=50)
        {
            var result = await _applicationUserManager.GetPaginatedUserDetails(pageIndex,itemNumber);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        //yanlış. tekil kullanıcı detayı yerine refactor edilecek.
        [HttpGet]
        public async Task<IActionResult> GetUserDetailList()
        {
            var result = await _applicationUserManager.GetUserDetailList();
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
