using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class ErrorController : Controller
{
    [Route("/hata/{statusCode}")]
    [ResponseCache(NoStore = true)]
    public IActionResult Index(int statusCode)
    {
        ViewBag.StatusCode = statusCode;
        ViewBag.Message = statusCode switch
        {
            404 => "Aradaginiz sayfa bulunamadi.",
            403 => "Bu sayfaya erisim izniniz yok.",
            500 => "Bir hata olustu. Lutfen daha sonra tekrar deneyin.",
            503 => "Servis gecici olarak kullanilamaz durumdadir.",
            _ => "Beklenmeyen bir hata olustu."
        };

        return View();
    }
}
