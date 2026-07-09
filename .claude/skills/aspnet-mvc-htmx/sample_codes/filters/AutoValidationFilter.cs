// Infrastructure/Filters/AutoValidationFilter.cs
// POST/PUT'ta ModelState invalid'se:
//   - HTMX  -> Partials/_Form partial dön (input altında hatalar)
//   - Normal -> TempData.SetError + Referer'a Redirect (PRG; View(null model) NRE'sini önler)
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.MVC.Infrastructure.Filters;

/// <summary>
/// Action'a eklendiğinde AutoValidationFilter atlanır.
/// Wizard step'leri gibi kendi validation'ını yapan action'lar için.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SkipAutoValidationAttribute : Attribute;

public class AutoValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var endpoint = context.ActionDescriptor.EndpointMetadata;
        if (endpoint.Any(m => m is SkipAutoValidationAttribute))
            return;

        if (!context.ModelState.IsValid
            && context.HttpContext.Request.Method is "POST" or "PUT")
        {
            var controller = (Controller)context.Controller;

            // İlk Vm parametresini bul (convention: ViewModel'lar "Vm" ile biter)
            var model = context.ActionArguments.Values
                .FirstOrDefault(v => v?.GetType().Name.EndsWith("Vm") == true);

            if (context.HttpContext.Request.Headers.ContainsKey("HX-Request"))
            {
                // HTMX akışı: form partial'ı ModelState ile render
                context.Result = controller.PartialView("Partials/_Form", model);
            }
            else
            {
                // PRG: TempData.SetError + Referer'a redirect
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

// ── Program.cs kayıt ────────────────────────────────────────────────────
//
// builder.Services.AddControllersWithViews(options =>
// {
//     options.Filters.Add<AutoValidationFilter>();
//     options.Filters.Add<TenantActionFilter>();
// });
