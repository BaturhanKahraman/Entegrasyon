using Entegrasyon.Business.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly AddTrendyolCategories _addTrendyol;

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, AddTrendyolCategories addTrendyol)
        {
            _logger = logger;
            _addTrendyol = addTrendyol;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
             await _addTrendyol.AddCategories();
             return null;
        }
    }
}