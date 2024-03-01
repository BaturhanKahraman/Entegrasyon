namespace Entegrasyon.MVC.Controllers;

using Entegrasyon.Business.Concrete;
using Entegrasyon.MVC.Utility.Attributes;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[Breadcrumb("Marka", ViewModels.BreadcrumbUsageType.Controller)]
public class BrandsController(BrandManager brandManager) : Controller
{
    [Breadcrumb("Liste")]
    public async Task<IActionResult> Index(int page,string search,int itemCount)
    {
        var result = await brandManager.GetBrandDetailPageable(new Entity.Dtos.Brand.GetCategoryDetailsPageDto(search));
        //var model = null;
        return View(result.Data);
    }

}
