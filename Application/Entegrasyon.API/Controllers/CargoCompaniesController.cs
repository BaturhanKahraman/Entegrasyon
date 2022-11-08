using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CargoCompaniesController : ControllerBase
    {
        private readonly CargoCompaniesManager _cargoCompanyManager;
        public CargoCompaniesController(CargoCompaniesManager cargoCompanyManager)
        {
            _cargoCompanyManager = cargoCompanyManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetCargoCompanies()
        {
            var result = await _cargoCompanyManager.GetCargoCompanies();
            if(result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost]
        public async Task<IActionResult> AddCargoCompany(AddCargoCompanyDto cargoCompanyDto)
        {
            var result = await _cargoCompanyManager.AddCargoCompany(cargoCompanyDto);
            if(result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateCargoCompany(CargoCompany cargoCompany)
        {
            var result = await _cargoCompanyManager.UpdateCargoCompany(cargoCompany);
            if(result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteCargoCompany(CargoCompany cargoCompany)
        {
            var result = await _cargoCompanyManager.DeleteCargoCompany(cargoCompany);
            if(result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
