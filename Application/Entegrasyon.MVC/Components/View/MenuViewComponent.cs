using Entegrasyon.MVC.Utility.Services;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Components.View;

public class MenuViewComponent(IMenuService menuService) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var items = menuService.GetMenu();
        return View(items.ToList());
    }
}