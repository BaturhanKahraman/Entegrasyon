using Entegrasyon.Blazor.Utility.Services;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Blazor.Components.View;

public class MenuViewComponent(IMenuService menuService) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var items = menuService.GetMenu();
        return View(items.ToList());
    }
}