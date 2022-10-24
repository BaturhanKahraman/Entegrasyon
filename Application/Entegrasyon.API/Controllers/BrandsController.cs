using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Products;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly BrandManager _brandManager;

        public BrandsController(BrandManager brandManager)
        {
            _brandManager = brandManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBrands()
        {
            var result = await _brandManager.GetBrands();
            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpPost]
        public async Task<IActionResult> AddBrand(AddBrandDto addBrandDto)
        {
            var result =await _brandManager.AddBrand(addBrandDto);
            if (result.Success)
                return Ok();
            return BadRequest(result.Message);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateBrand(Brand brand)
        {
            var result = await _brandManager.UpdateBrand(brand);
            if(result.Success)
                return Ok();
            return BadRequest(result.Message);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBrand(Brand brand)
        {
            var result = await _brandManager.DeleteBrand(brand);
            if(result.Success)
                return Ok();
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetBrandDetailsPage([FromQuery]GetCategoryDetailsPageDto dto)
        {
            var result = await _brandManager.GetCategoryDetailPageable(dto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
