using Microsoft.AspNetCore.Mvc.Razor;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

public class FeatureViewLocationExpander : IViewLocationExpander
{
    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        // {0} = view name, {1} = controller name
        return new[]
        {
            "/Features/{1}/Views/{0}.cshtml",
            "/Features/{1}/Views/Partials/{0}.cshtml",
            "/Shared/Views/{0}.cshtml",
            "/Shared/Views/Partials/{0}.cshtml"
        }.Concat(viewLocations);
    }

    public void PopulateValues(ViewLocationExpanderContext context) { }
}
