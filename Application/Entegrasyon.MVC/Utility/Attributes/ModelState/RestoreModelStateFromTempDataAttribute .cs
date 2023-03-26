using Entegrasyon.MVC.Utility.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.MVC.Utility.Attributes.ModelState;

public class RestoreModelStateFromTempDataAttribute : BaseModelTransferAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var controller = context.Controller as Controller;

        if(controller?.TempData[Key] is string serialisedModelState)
        {
            //Only Import if we are viewing
            if(context.Result is ViewResult)
            {
                context.ModelState.DeSerializeModelState(serialisedModelState);
            }
            else
            {
                //Otherwise remove it.
                controller.TempData.Remove(Key);
            }
        }
        base.OnActionExecuted(context);
    }
}