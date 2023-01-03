using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Sale;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        private readonly SaleManager _saleManager;

        public SalesController(SaleManager saleManager)
        {
            _saleManager = saleManager;
        }

        [HttpPost]
        public async Task<IActionResult> AddSale(MakeSaleDto dto)
        {
            var result = await _saleManager.MakeSale(dto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result);
        }
    }
}
