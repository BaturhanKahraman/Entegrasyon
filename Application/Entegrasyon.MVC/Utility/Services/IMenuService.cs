using System.Security.Claims;
using Entegrasyon.MVC.Utility.Objects;

namespace Entegrasyon.MVC.Utility.Services
{
    public interface IMenuService
    {
        Task<List<NavigationItem>> GetOrCreateMenu(ClaimsPrincipal user);
    }
}