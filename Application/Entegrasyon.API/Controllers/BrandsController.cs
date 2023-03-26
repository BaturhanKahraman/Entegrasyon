using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Mvc;
using Shared.DTO;

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
            var result = await _brandManager.GetBrandListDetails();
            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpPost]
        public async Task<IActionResult> AddBrand(AddBrandDto addBrandDto)
        {
            var result =await _brandManager.AddBrand(addBrandDto);
            if (result.Success)
                return Ok(result);
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
       
        [HttpGet]
        public async Task<IActionResult> GetBrandDetailsPage([FromQuery]GetCategoryDetailsPageDto dto)
        {
            var result = await _brandManager.GetCategoryDetailPageable(dto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpGet]
        public async Task<IActionResult> GetBrand(int id)
        {
            var result = await _brandManager.GetBrandById(id);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpPut]
        public async Task<IActionResult> EditBrand(Brand brand)
        {
            var result = await _brandManager.UpdateBrand(brand);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpGet]
        public async Task<IActionResult> GetBrandDetail(int id)
        {
            var result = await _brandManager.GetBrandDetail(id);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var result = await _brandManager.DeleteBrand(id);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
