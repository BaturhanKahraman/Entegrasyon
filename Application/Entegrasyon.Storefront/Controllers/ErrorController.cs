using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class ErrorController(ICategoryService categoryService) : Controller
{
    [Route("/hata/{statusCode}")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> Index(int statusCode)
    {
        ViewBag.StatusCode = statusCode;
        ViewBag.Message = statusCode switch
        {
            404 => "Aradiginiz sayfa bulunamadi.",
            403 => "Bu sayfaya erisim izniniz yok.",
            500 => "Bir hata olustu. Lutfen daha sonra tekrar deneyin.",
            503 => "Servis gecici olarak kullanilamaz durumdadir.",
            _ => "Beklenmeyen bir hata olustu."
        };

        if (statusCode == 404)
        {
            try
            {
                var treeResult = await categoryService.GetCategoryTreeAsync();
                if (treeResult.Success)
                    ViewBag.TopCategories = treeResult.Data.Take(8).ToList();
            }
            catch
            {
                // Don't let category loading failure break the error page
            }
        }

        return View();
    }
}
