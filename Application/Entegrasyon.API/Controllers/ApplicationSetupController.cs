using Entegrasyon.Business.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ApplicationSetupController : ControllerBase
    {
        private readonly TrendyolCategories _trendyolCategories;
        private readonly TrendyolBrands _trendyolBrands;
        private readonly TrendyolCargoCompanies _trendyolCargoCompanies;

        public ApplicationSetupController(TrendyolCategories trendyolCategories, TrendyolBrands trendyolBrands, TrendyolCargoCompanies trendyolCargoCompanies)
        {
            _trendyolCategories = trendyolCategories;
            _trendyolBrands = trendyolBrands;
            _trendyolCargoCompanies = trendyolCargoCompanies;
        }

        [HttpGet]
        public async Task<IActionResult> InstallCategoriesFromTrendyol()
        {
            try
            {
                await _trendyolCategories.AddCategories();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> InstallBrandsFromTrendyol()
        {
            try
            {
                await _trendyolBrands.AddBrandsFromTrendyol();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> InstallCargoCompaniesFromTrendyol()
        {
            try
            {
                await _trendyolCargoCompanies.AddCargoCompanies();
            }
            catch(Exception e)
            {
                return BadRequest(e);
            }
            return Ok();
        }
    }
}
