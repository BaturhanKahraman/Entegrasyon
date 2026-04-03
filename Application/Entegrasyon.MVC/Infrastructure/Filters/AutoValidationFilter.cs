using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.MVC.Infrastructure.Filters;

/// <summary>
/// POST isteklerinde ModelState geçersizse, controller action'a girmeden
/// HTMX için partial view veya normal view döner.
/// Bu sayede her action'da if (!ModelState.IsValid) yazmaya gerek kalmaz.
/// </summary>
public class AutoValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
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
                // Normal → aynı view döndür
                context.Result = controller.View(model);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
