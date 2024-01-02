using System.Security.Claims;
using Entegrasyon.MVC.Utility.Objects;

namespace Entegrasyon.MVC.Utility.Services
{
    public interface IMenuService
    {
        IEnumerable<NavigationItem> GetMenu();

        void CreateMenu(ClaimsPrincipal user);
    }
}