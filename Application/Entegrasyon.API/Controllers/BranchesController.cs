using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly BranchOfficeManager _branchOfficeManager;

        public BranchesController(BranchOfficeManager branchOfficeManager)
        {
            _branchOfficeManager = branchOfficeManager;
        }

        // GET: api/<BranchesController>
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

        // GET api/<BranchesController>/5
        [HttpPost]
        public async Task<IActionResult> AddBranch(BranchOffice office)
        {
            var result =await _branchOfficeManager.AddBranch(office);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        // POST api/<BranchesController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<BranchesController>/5
        [HttpPut("{id}")]
        public void Put(int id,[FromBody] string value)
        {
        }

        // DELETE api/<BranchesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
