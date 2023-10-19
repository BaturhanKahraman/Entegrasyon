using Entegrasyon.MVC.Utility.Constants;
using Entegrasyon.MVC.Utility.Objects;
using Entegrasyon.MVC.Utility.Storage;
using System.Security.Claims;
using System.Text.Json;

namespace Entegrasyon.MVC.Utility.Services;

public class MenuService : IMenuService
{
    private const string CACHE_KEY = "Menu";
    private readonly IHttpContextAccessor _accessor;
    private readonly ISession _session;

    public MenuService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
        _session = _accessor.HttpContext.Session ?? throw new Exception("Session'a ulaşılamadı!");
        if(!_session.IsAvailable)
            throw new Exception("Session yüklenemedi!");
    }

    public void CreateMenu(ClaimsPrincipal user)
    {
        if(user == null)
            throw new ArgumentNullException(nameof(user));
        if(!user.HasClaim(c => c.Type == StringConstant.Permission))
            throw new Exception("Eksik claim?");
        List<NavigationItem> navigationItems;
        var permissionKeys = user.Claims
            .Where(x => x.Type == StringConstant.Permission)
            .GroupBy(x => x.Value.Split('.')[0])
            .Select(x => x.Key);
        navigationItems = MenuStorage.Menus
            .Where(x => permissionKeys.Contains(x.Key))
            .Select(kvp => kvp.Value)
            .ToList();
        _session.SetString(CACHE_KEY,JsonSerializer.Serialize(navigationItems));
    }

    public List<NavigationItem> GetMenu()
    {
        string cachedJson = _session.GetString(CACHE_KEY);
        if(string.IsNullOrEmpty(cachedJson))
            throw new Exception("CreateMenu çağırılmamış");
        return JsonSerializer.Deserialize<List<NavigationItem>>(cachedJson);
    }
}