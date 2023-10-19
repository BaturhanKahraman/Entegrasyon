using Entegrasyon.MVC.Utility.Services;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Components.View;

public class MenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    public MenuViewComponent(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public IViewComponentResult Invoke()
    {
        var items = _menuService.GetMenu();
        return View(items);
    }
}