using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.MVC.Utility.Attributes.ModelState;

public class BaseModelTransferAttribute : ActionFilterAttribute
{
    protected const string Key = nameof(BaseModelTransferAttribute);
}