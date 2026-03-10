using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Entegrasyon.ApplicationBootstrap.Extensions;

public static class LocalViewExtension
{
    public static bool ContainsAll<T>(this LocalView<T> localView, IEnumerable<T> items)
    where T:class
    {
        return items.All(localView.Contains);
    }
}
