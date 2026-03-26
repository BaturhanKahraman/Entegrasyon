using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.Storefront.ViewComponents;

public class MegaMenuViewComponent(
    ICategoryService categoryService,
    IStorefrontTenantContext tenant,
    IMemoryCache cache) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cacheKey = $"storefront:megamenu:{tenant.TenantId}";
        if (!cache.TryGetValue(cacheKey, out List<CategoryTreeDto>? categories))
        {
            var result = await categoryService.GetCategoryTreeAsync();
            categories = result.Success ? result.Data : new List<CategoryTreeDto>();
            cache.Set(cacheKey, categories, TimeSpan.FromMinutes(30));
        }

        return View(categories);
    }
}
