namespace Entegrasyon.MVC.Controllers;

using Entegrasyon.Business.Concrete;
using Entegrasyon.MVC.Utility.Attributes;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[Breadcrumb("Marka", ViewModels.BreadcrumbUsageType.Controller)]
public class BrandsController : Controller
{
    private readonly BrandManager _brandManager;

    public BrandsController(BrandManager brandManager)
    {
        _brandManager = brandManager;
    }

    [Breadcrumb("Liste")]
    public async Task<IActionResult> Index(int page,string search,int itemCount)
    {
        var result = await _brandManager.GetBrandDetailPageable(new Entity.Dtos.Brand.GetCategoryDetailsPageDto(search));
        var model = 
        return View(result.Data);
    }

}
