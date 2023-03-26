using Entegrasyon.Business.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TrendyolBrandImportsController : ControllerBase
    {
        private readonly TrendyolBrandImporterService _service;

        public TrendyolBrandImportsController(TrendyolBrandImporterService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> QueueImport()
        {
            var result =await _service.QueueImporting();
            return Ok(result);
        }
    }
}
