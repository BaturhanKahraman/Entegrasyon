using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Components.View.Breadcrumb
{
    public class BreadcrumbViewComponent:ViewComponent
    {
        public async Task InvokeAsync()
        {
            await Task.Yield();
        }
    }
}
