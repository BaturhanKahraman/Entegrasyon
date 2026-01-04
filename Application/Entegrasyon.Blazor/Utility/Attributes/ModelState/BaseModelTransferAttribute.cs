using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.Blazor.Utility.Attributes.ModelState;

public class BaseModelTransferAttribute : ActionFilterAttribute
{
    protected const string Key = nameof(BaseModelTransferAttribute);
}