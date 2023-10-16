using Entegrasyon.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Results;
using System.Security.Policy;

namespace Entegrasyon.MVC.Utility.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,AllowMultiple = false,Inherited = false)]
    public class BreadcrumbActionFilter : ActionFilterAttribute
    {
        private const string Breadcrumb = "Breadcrumb";

        public string Name { get; }
        public string TitleKey { get; }
        public Type ResourceType { get; }
        public BreadcrumbActionFilter(string name)
        {
            Name = name;
        }
        public BreadcrumbActionFilter(string titleKey,Type resource)
        {
            TitleKey = titleKey;
            ResourceType = resource;
        }
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var routeData = context.HttpContext.GetRouteData();

            List<BreadcrumbModel> models = context.HttpContext.Items[Breadcrumb] as List<BreadcrumbModel> ?? new List<BreadcrumbModel>();
            if(!string.IsNullOrEmpty(Name))
            {
                models.Add(new BreadcrumbModel(Name,GetFullUrl(routeData)));
            }
            else if(!string.IsNullOrEmpty(TitleKey) && ResourceType is not null)
            {
                throw new NotImplementedException();
            }
            context.HttpContext.Items[Breadcrumb] = models;
        }

        private string GetFullUrl(RouteData routeData)
        {
            string controller = routeData.Values["controller"].ToString();
            string action = routeData.Values["action"].ToString();
            return @$"/{controller}/{action}";
        }
    }
}
