using Entegrasyon.Business.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TempBarcodesController : ControllerBase
    {
        private readonly TempBarcodeManager _barcodeManager;

        public TempBarcodesController(TempBarcodeManager barcodeManager)
        {
            _barcodeManager = barcodeManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetTempBarcode()=>Ok(await _barcodeManager.GetBarcodeResult());
        
    }
}
