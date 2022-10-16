using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductManager _productManager;

        public ProductsController(ProductManager productManager)
        {
            _productManager = productManager;
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromForm]AddProductDto dto)
        {
            var result = await _productManager.AddProduct(dto);
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
