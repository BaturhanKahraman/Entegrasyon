using Entegrasyon.Blazor.Utility.Constants;
using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.Blazor.Utility.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class,AllowMultiple = false,Inherited = false)]
    public class BreadcrumbAttribute : ActionFilterAttribute
    {
        private readonly string? name;
        private readonly Type? resourceType;
        private readonly string? titleKey;
        private readonly BreadcrumbUsageType bcUsageType;

        public BreadcrumbAttribute(string name,BreadcrumbUsageType type = BreadcrumbUsageType.Action)
        {
            this.name = name;
            bcUsageType = type;
        }

        public BreadcrumbAttribute(string titleKey,Type resource,BreadcrumbUsageType type = BreadcrumbUsageType.Action)
        {
            this.titleKey = titleKey;
            resourceType = resource;
            bcUsageType = type;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if(context.Result is not ViewResult vr)
                return;
            var routeData = context.HttpContext.GetRouteData();
            List<BreadcrumbModel> models = vr.ViewData[StringConstant.Breadcrumb] as List<BreadcrumbModel> ?? new List<BreadcrumbModel>();
            if(!string.IsNullOrEmpty(name))
            {
                models.Add(new BreadcrumbModel(name,GetFullUrl(routeData),bcUsageType));
            }
            else if(!string.IsNullOrEmpty(titleKey) && resourceType is not null)
            {
                throw new NotImplementedException();
            }
            vr.ViewData[StringConstant.Breadcrumb] = models;
        }

        private string GetFullUrl(RouteData routeData)
        {
            string controller = routeData.Values["controller"]!.ToString()!;
            string action = bcUsageType == BreadcrumbUsageType.Action
                ? routeData.Values["action"]!.ToString()!
                : string.Empty;
            return @$"/{controller}/{action}";
        }
    }
}
