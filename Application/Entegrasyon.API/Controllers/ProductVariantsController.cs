using Entegrasyon.Business.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ProductVariantsController : Controller
{
    private readonly ProductVariantManager _productVariantManager;

    public ProductVariantsController(ProductVariantManager productVariantManager)
    {
        _productVariantManager = productVariantManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductVariantsBySearchText(string fullTextSearch)
    {
        var result = await _productVariantManager.GetProductVariantsBySearchText(fullTextSearch);
        if(result.Success)
            return Ok(result);
        return BadRequest(result.Message);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductVariantByBarcode(string barcode)
    {
        var result = await _productVariantManager.GetProductVariantByBarcode(barcode);
        if(result.Success)
            return Ok(result);
        return BadRequest(result.Message);
        
    }
}