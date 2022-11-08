using Entegrasyon.Business.Utility;
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
        private readonly ILogger<ApplicationSetupController> _logger;

        public ApplicationSetupController(TrendyolCategories trendyolCategories, TrendyolBrands trendyolBrands, TrendyolCargoCompanies trendyolCargoCompanies, ILogger<ApplicationSetupController> logger)
        {
            _trendyolCategories = trendyolCategories;
            _trendyolBrands = trendyolBrands;
            _trendyolCargoCompanies = trendyolCargoCompanies;
            _logger = logger;
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
        [HttpGet]
        public IActionResult TryLogger()
        {
            _logger.LogInformation("Information eklendi");
            _logger.LogWarning("Bu warninging txt dosyasına yazılması gerekiyor.");
            _logger.LogError("Bu errorun da dosyaya yazılması gerekiyor");
            return Ok();
        }
    }
}
