using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Shared.Extensions;

public static class LocalViewExtension
{
    public static bool ContainsAll<T>(this LocalView<T> localView, IEnumerable<T> items)
    where T:class
    {
        return items.All(localView.Contains);
    }
}