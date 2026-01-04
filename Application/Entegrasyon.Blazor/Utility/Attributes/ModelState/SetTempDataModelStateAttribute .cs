using Entegrasyon.Blazor.Utility.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.Blazor.Utility.Attributes.ModelState;

public class SetTempDataModelStateAttribute : BaseModelTransferAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.ModelState.IsValid) return;
        if (context.Result is RedirectResult or RedirectToActionResult or RedirectToRouteResult)
        {
            if (context.Controller is Controller controller)
            {
                controller.TempData[Key] = context.ModelState.SerializeModelState();
            }
        }
        base.OnActionExecuted(context);
    }
}