using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

/// <summary>
/// Marka / kategori / özellik / özellik değeri eşleştirme ekranlarındaki
/// Tom Select autocomplete dropdown'larını besler. Endpoint'ler JSON döner.
/// </summary>
[Authorize]
public class SuggestionsController(IMarketplaceSearchService searchService) : Controller
{
    [HttpGet("/marketplace/sync/suggestions/brands")]
    public async Task<IActionResult> Brands(int mp = 1, string q = "", CancellationToken ct = default)
    {
        var result = await searchService.SearchBrandsAsync(mp, q, ct);
        if (!result.Success)
            return Ok(Array.Empty<object>());

        var items = result.Data.Select(b => new
        {
            id = b.Id,
            name = b.Name,
            label = $"{b.Name} (ID: {b.Id})"
        });
        return Ok(items);
    }

    [HttpGet("/marketplace/sync/suggestions/categories")]
    public async Task<IActionResult> Categories(int mp = 1, string q = "", CancellationToken ct = default)
    {
        var result = await searchService.SearchCategoriesAsync(mp, q, ct);
        if (!result.Success)
            return Ok(Array.Empty<object>());

        var items = result.Data.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            label = c.FullPath ?? c.Name,
            fullPath = c.FullPath ?? c.Name,
            isLeaf = c.IsLeaf
        });
        return Ok(items);
    }

    [HttpGet("/marketplace/sync/suggestions/attributes")]
    public async Task<IActionResult> Attributes(int mp = 1, string q = "", CancellationToken ct = default)
    {
        var result = await searchService.SearchAttributesAsync(mp, q, ct);
        if (!result.Success)
            return Ok(Array.Empty<object>());

        var items = result.Data.Select(a => new
        {
            id = a.Id,
            name = a.Name,
            label = a.Name
        });
        return Ok(items);
    }

    [HttpGet("/marketplace/sync/suggestions/attributes/values")]
    public async Task<IActionResult> AttributeValues(int mp = 1, int mpCategoryId = 0, int mpAttributeId = 0, string q = "", CancellationToken ct = default)
    {
        if (mpAttributeId <= 0)
            return Ok(Array.Empty<object>());

        // Kategori biliniyorsa kategori-scoped ara (aynı özelliğin farklı kategorilerdeki
        // değerleri karışmasın); bilinmiyorsa eski global davranışa düş.
        var result = mpCategoryId > 0
            ? await searchService.SearchAttributeValuesForCategoryAsync(mp, mpCategoryId, mpAttributeId, q, ct)
            : await searchService.SearchAttributeValuesAsync(mp, mpAttributeId, q, ct);

        if (!result.Success)
            return Ok(Array.Empty<object>());

        var items = result.Data.Select(v => new
        {
            id = v.Id,
            name = v.Name,
            label = v.Name
        });
        return Ok(items);
    }
}
