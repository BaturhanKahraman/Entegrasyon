using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class BranchOfficesController : ControllerBase
    {
        private readonly BranchOfficeManager _branchOfficeManager;

        public BranchOfficesController(BranchOfficeManager branchOfficeManager)
        {
            _branchOfficeManager = branchOfficeManager;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var result = await _branchOfficeManager.GetBranchList();
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result.Message);
        }
        
        [HttpPost]
        public async Task<IActionResult> AddBranch(BranchOfficeAddDto officedto)
        {
            var result =await _branchOfficeManager.AddBranch(officedto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        // POST api/<BranchesController>
        [HttpGet]
        public async Task<IActionResult> GetBranchDetail(int branchId)
        {
            var result = await _branchOfficeManager.GetBranchDetailById(branchId);
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateBranch(BranchOfficeEditDto branchOfficeDto)
        {
            var result = await _branchOfficeManager.Update(branchOfficeDto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var result = await _branchOfficeManager.Delete(id);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
