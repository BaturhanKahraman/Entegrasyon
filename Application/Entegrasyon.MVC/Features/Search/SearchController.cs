using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.ApplicationBootstrap.Security;
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

        // Admin rolü tüm UI'de permission check'i bypass eder
        // (PermissionGuardTagHelper + TenantFeatureAuthorizationHandler ile aynı pattern)
        HashSet<string> userPermissions;
        if (User.IsInRole("Admin"))
        {
            userPermissions = [.. AppPermissions.GetAllPermissions()];
        }
        else
        {
            // AuthController "Permission" claim type'ını ekliyor
            userPermissions = User.Claims
                .Where(c => string.Equals(c.Type, "Permission", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .ToHashSet();
        }

        IReadOnlyCollection<string>? sourceFilter = null;
        if (!string.IsNullOrWhiteSpace(sources))
            sourceFilter = sources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var response = await globalSearchManager.SearchAsync(q, userPermissions, sourceFilter, limit, ct);
        return Json(response);
    }
}
