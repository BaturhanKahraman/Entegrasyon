using Entegrasyon.MVC.Utility.Constants;
using Entegrasyon.MVC.Utility.Objects;
using Entegrasyon.MVC.Utility.Storage;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.MVC.Utility.Services;

public class MenuService : IMenuService
{
    private const string CacheKey = "Menu+{0}";
    private readonly IHttpContextAccessor _accessor;
    private readonly IMemoryCache _menuCache;

    public MenuService(IHttpContextAccessor accessor, IMemoryCache menuCache)
    {
        _accessor = accessor;
        _menuCache = menuCache;
    }

    private List<NavigationItem> CreateMenuItems(ClaimsPrincipal user)
    {
        if(user == null)
            throw new ArgumentNullException(nameof(user));
        if(!user.HasClaim(c => c.Type == StringConstant.Permission))
            throw new Exception("Eksik claim?");
        var permissionKeys = user.Claims
            .Where(x => x.Type == StringConstant.Permission)
            .GroupBy(x => x.Value.Split('.')[0])
            .Select(x => x.Key);
        return MenuStorage.Menus
            .Where(x => permissionKeys.Contains(x.Key))
            .Select(kvp => kvp.Value)
            .ToList();
    }

    public IEnumerable<NavigationItem> GetMenu()
    {
        var user = _accessor.HttpContext!.User;
        string cacheKey = string.Format(CacheKey, user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        bool isCached = _menuCache.TryGetValue(cacheKey,out List<NavigationItem> navItems);
        if (isCached) return navItems;
        navItems = CreateMenuItems(user);
        _menuCache.Set(cacheKey, navItems);
        return navItems;
    }
}