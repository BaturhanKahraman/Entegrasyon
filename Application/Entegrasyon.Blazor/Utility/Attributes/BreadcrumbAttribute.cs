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
        private readonly string _name;
        private readonly Type _resourceType;
        private readonly string _titleKey;
        private readonly BreadcrumbUsageType _bcUsageType;

        public BreadcrumbAttribute(string name,BreadcrumbUsageType type = BreadcrumbUsageType.Action)
        {
            _name = name;
            _bcUsageType = type;
        }

        public BreadcrumbAttribute(string titleKey,Type resource,BreadcrumbUsageType type = BreadcrumbUsageType.Action)
        {
            _titleKey = titleKey;
            _resourceType = resource;
            _bcUsageType = type;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if(context.Result is not ViewResult vr)
                return;
            var routeData = context.HttpContext.GetRouteData();
            List<BreadcrumbModel> models = vr.ViewData[StringConstant.Breadcrumb] as List<BreadcrumbModel> ?? new List<BreadcrumbModel>();
            if(!string.IsNullOrEmpty(_name))
            {
                models.Add(new BreadcrumbModel(_name,GetFullUrl(routeData),_bcUsageType));
            }
            else if(!string.IsNullOrEmpty(_titleKey) && _resourceType is not null)
            {
                throw new NotImplementedException();
            }
            vr.ViewData[StringConstant.Breadcrumb] = models;
        }

        private string GetFullUrl(RouteData routeData)
        {
            string controller = routeData.Values["controller"].ToString();
            string action = _bcUsageType == BreadcrumbUsageType.Action
                ? routeData.Values["action"].ToString()
                : string.Empty;
            return @$"/{controller}/{action}";
        }
    }
}