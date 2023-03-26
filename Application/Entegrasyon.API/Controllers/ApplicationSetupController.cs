using Entegrasyon.Business.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ApplicationSetupController : ControllerBase
    {
        private readonly ILogger<ApplicationSetupController> _logger;

        public ApplicationSetupController(ILogger<ApplicationSetupController> logger)
        {
            _logger = logger;
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
