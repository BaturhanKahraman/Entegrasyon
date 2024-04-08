using AutoMapper;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.ViewModels.Brand;
using Shared.Entity;

namespace Entegrasyon.MVC.Controllers;

using Entegrasyon.Business.Concrete;
using Entegrasyon.MVC.Utility.Attributes;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[Breadcrumb("Marka", ViewModels.BreadcrumbUsageType.Controller)]
public class BrandsController(BrandManager brandManager, IMapper mapper) : Controller
{
    [Breadcrumb("Liste")]
    public async Task<IActionResult> Index(int page, string search, int itemCount)
    {
        var result = await brandManager.GetBrandDetailPageable(new GetCategoryDetailsPageDto(search,page-1,itemCount));
        var model = mapper.Map<Pageable<BrandListDetailViewModel>>(result.Data);
        return View(model);
    }
    [Breadcrumb("Detay")]
    public async Task<IActionResult> Detail(int brandId)
    {
        await Task.Yield();
        return View();
    }
    [Breadcrumb("Düzenle")]
    [RestoreModelStateFromTempData]
    public async Task<IActionResult> Edit(int brandId)
    {
        await Task.Yield();
        var model = new BrandEditViewModel();
        return View(model);
    }
    [HttpPost]
    [SetTempDataModelState]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BrandEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Edit),vm);
        await Task.Yield();
        throw new NotImplementedException();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(BrandDeleteViewModel vm)
    {
        await Task.Yield();
        throw new NotImplementedException();
    }
}
