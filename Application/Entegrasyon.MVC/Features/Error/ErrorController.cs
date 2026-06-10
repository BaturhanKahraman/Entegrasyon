using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Error;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("error/{statusCode:int}")]
    public IActionResult Index(int statusCode)
    {
        Response.StatusCode = statusCode;
        ViewData.SetPageTitle(statusCode switch
        {
            403 => "Erisim Engellendi",
            404 => "Sayfa bulunamadı",
            429 => "Cok Fazla Istek",
            _ => "Sunucu Hatasi"
        });

        return statusCode switch
        {
            403 => View("~/Views/Error/403.cshtml"),
            404 => View("~/Views/Error/404.cshtml"),
            429 => View("~/Views/Error/429.cshtml"),
            _ => View("~/Views/Error/500.cshtml")
        };
    }
}
