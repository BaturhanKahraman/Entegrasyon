using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Infrastructure.Filters;

/// <summary>
/// Action'lara eklendiğinde AutoValidationFilter'ı devre dışı bırakır.
/// Wizard gibi çok adımlı formlarda kendi validation'ını yapan action'lar için kullanılır.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SkipAutoValidationAttribute : Attribute;

/// <summary>
/// POST isteklerinde ModelState geçersizse, controller action'a girmeden
/// HTMX için partial view veya normal view döner.
/// Bu sayede her action'da if (!ModelState.IsValid) yazmaya gerek kalmaz.
/// </summary>
public class AutoValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // SkipAutoValidation attribute'u varsa filtreyi atla
        var endpoint = context.ActionDescriptor.EndpointMetadata;
        if (endpoint.Any(m => m is SkipAutoValidationAttribute))
            return;

        if (!context.ModelState.IsValid
            && context.HttpContext.Request.Method is "POST" or "PUT")
        {
            var controller = (Controller)context.Controller;

            // İlk ViewModel parametresini bul
            var model = context.ActionArguments.Values
                .FirstOrDefault(v => v?.GetType().Name.EndsWith("Vm") == true);

            if (context.HttpContext.Request.Headers.ContainsKey("HX-Request"))
            {
                // HTMX → form partial döndür
                context.Result = controller.PartialView("Partials/_Form", model);
            }
            else
            {
                // Bug fix: View(model) action'ın default view'ını input DTO/null ile render
                // ediyordu; view GET'in yüklediği zengin modele bağımlıysa NRE/500 oluyordu
                // (örn. Customer Edit, "Vm" yerine "Dto" parametresi -> model=null -> @Model.Id).
                // PRG: validation mesajını TempData'ya yaz, geldiği sayfaya geri yönlendir.
                var messages = string.Join(" • ", context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct());
                controller.TempData.SetError(string.IsNullOrWhiteSpace(messages)
                    ? "Lütfen formu kontrol edip tekrar deneyin."
                    : messages);
                var back = context.HttpContext.Request.Headers.Referer.FirstOrDefault();
                context.Result = new RedirectResult(string.IsNullOrWhiteSpace(back) ? "/" : back);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
