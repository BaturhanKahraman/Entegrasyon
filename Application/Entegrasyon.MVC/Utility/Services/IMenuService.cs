using System.Security.Claims;
using Entegrasyon.MVC.Utility.Objects;

namespace Entegrasyon.MVC.Utility.Services
{
    public interface IMenuService
    {
        List<NavigationItem> GetMenu();

        void CreateMenu(ClaimsPrincipal user);
    }
}