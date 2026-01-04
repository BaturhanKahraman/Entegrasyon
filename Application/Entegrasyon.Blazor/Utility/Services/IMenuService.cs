using System.Security.Claims;
using Entegrasyon.Blazor.Utility.Objects;

namespace Entegrasyon.Blazor.Utility.Services
{
    public interface IMenuService
    {
        IEnumerable<NavigationItem> GetMenu();
    }
}