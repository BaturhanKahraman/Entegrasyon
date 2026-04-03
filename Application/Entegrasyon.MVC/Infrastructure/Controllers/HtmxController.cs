using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Infrastructure.Controllers;

/// <summary>
/// Base controller with HTMX-aware helpers.
/// Eliminates repeated if(Request.IsHtmx()) patterns in action methods.
/// </summary>
public abstract class HtmxController : Controller
{
    /// <summary>
    /// Returns PartialView for HTMX requests, full View otherwise.
    /// </summary>
    protected IActionResult HtmxView(string viewName, object? model = null)
        => Request.IsHtmx() ? PartialView(viewName, model) : View(viewName, model);

    /// <summary>
    /// Returns PartialView for HTMX requests, full View (using default view name) otherwise.
    /// </summary>
    protected IActionResult HtmxView(object? model = null)
        => Request.IsHtmx() ? PartialView(model) : View(model);

    /// <summary>
    /// Handles the result of a mutation (POST) for HTMX and non-HTMX paths.
    /// HTMX: sends toast via HX-Trigger header, optionally triggers a refresh event.
    /// Non-HTMX: sets TempData toast and redirects to Index.
    /// </summary>
    protected IActionResult HtmxMutationResult(
        Entity.Results.IResult result,
        string successMessage,
        string? errorMessage = null,
        string? refreshEvent = null,
        string redirectAction = "Index")
    {
        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = successMessage, type = "success" });
                if (refreshEvent is not null)
                    Response.HtmxTrigger(refreshEvent);
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? errorMessage ?? "İşlem başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess(successMessage);
        else
            TempData.SetError(result.Message ?? errorMessage ?? "İşlem başarısız.");

        return RedirectToAction(redirectAction);
    }
}
