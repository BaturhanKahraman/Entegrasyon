using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Results;
using Shared.User;
using Shared.User.Services;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleManager<RootRole,RootClaim> _roleManager;
        private readonly ApplicationRoleManager _applicationRoleManager;

        public RolesController(ApplicationRoleManager applicationRoleManager,IRoleManager<RootRole,RootClaim> roleManager)
        {
            _applicationRoleManager = applicationRoleManager;
            _roleManager = roleManager;
        }

        [HttpGet] 
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.GetRoles();
            if(roles == null)
            {
                return NotFound("Rol bulunamadı.");
            }
            return Ok(new SuccessDataResult<IEnumerable<RootRole>>(roles));
        }
        [HttpGet]
        public async Task<IActionResult> GetRoleClaims()
        {
            var roleClaims = await _roleManager.GetRoleClaims();
            if(roleClaims == null)
            {
                return NotFound("Getirilecek yetki bulunamadı.");
            }
            return Ok(new SuccessDataResult<IEnumerable<RootClaim>>(roleClaims));
        }
        [HttpPost]
        public async Task<IActionResult> AddRole(AddRoleDto roledto)
        {
            var result = await _applicationRoleManager.AddRole(roledto);
            if(result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result.Message);
        }


    }
}
