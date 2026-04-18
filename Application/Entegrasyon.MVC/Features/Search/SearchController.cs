using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.MVC.Features.Search;

[Authorize]
public class SearchController(IGlobalSearchManager globalSearchManager) : Controller
{
    [HttpGet("/_/search")]
    public async Task<IActionResult> Search(
        string q,
        string? sources = null,
        int limit = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new { groups = Array.Empty<object>(), totalHits = 0, elapsedMs = 0 });

        if (limit is < 1 or > 20) limit = 5;

        var userPermissions = User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToHashSet();

        IReadOnlyCollection<string>? sourceFilter = null;
        if (!string.IsNullOrWhiteSpace(sources))
            sourceFilter = sources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var response = await globalSearchManager.SearchAsync(q, userPermissions, sourceFilter, limit, ct);
        return Json(response);
    }
}
