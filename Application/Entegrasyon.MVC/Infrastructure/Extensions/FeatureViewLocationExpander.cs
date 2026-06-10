using Microsoft.AspNetCore.Mvc.Razor;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

public class FeatureViewLocationExpander : IViewLocationExpander
{
    // Controller adı (tekil) → Feature klasör adı eşlemesi
    // MVC convention: ProductController → {1} = "Product"
    // Klasör yapımız: Features/Products/ (çoğul)
    private static readonly Dictionary<string, string> controllerToFolder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Product"] = "Products",
        // Ürün 360° aktivite sayfası: ProductActivityController → Products klasörü
        ["ProductActivity"] = "Products",
        ["Category"] = "Categories",
        ["Brand"] = "Brands",
        ["Customer"] = "Customers",
        ["Order"] = "Orders",
        ["User"] = "Users",
        ["Role"] = "Roles",
        ["Report"] = "Reports",
        ["Sale"] = "Sales",
        ["Notification"] = "Notifications",
        ["BranchOffice"] = "BranchOffices",
        ["BulkOperation"] = "BulkOperations",
        ["Attribute"] = "Attributes",
        ["Invoice"] = "Invoicing",
        // MarketplaceSync alt-controller'ları: hepsi aynı klasörde
        ["SyncOverview"] = "MarketplaceSync",
        ["ProductSync"] = "MarketplaceSync",
        ["CategorySync"] = "MarketplaceSync",
        ["AttributeSync"] = "MarketplaceSync",
        ["BrandMapping"] = "MarketplaceSync",
        ["BulkMatch"] = "MarketplaceSync",
        ["CommissionRates"] = "MarketplaceSync",
        ["StockMovement"] = "StockMovements",
        ["Discount"] = "Discounts",
        ["GiftCard"] = "GiftCards",
    };

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        var controllerName = context.Values.TryGetValue("controller", out var cn) ? cn ?? "" : "";
        var folder = controllerToFolder.TryGetValue(controllerName, out var f) ? f : controllerName;

        // {0} = view name, {1} = controller name (original)
        var locations = new List<string>
        {
            // Mapped folder (Products, Categories, etc.)
            $"/Features/{folder}/Views/{{0}}.cshtml",
            $"/Features/{folder}/Views/Partials/{{0}}.cshtml",
            // MarketplaceSync alt-klasörleri (Views/ProductSync/, Views/Overview/, etc.)
            $"/Features/{folder}/Views/{controllerName}/{{0}}.cshtml",
            $"/Features/{folder}/Views/{controllerName}/Partials/{{0}}.cshtml",
            // Original {1} pattern (exact match — Auth, Dashboard, POS, etc.)
            "/Features/{1}/Views/{0}.cshtml",
            "/Features/{1}/Views/Partials/{0}.cshtml",
            // Shared fallback
            "/Shared/Views/{0}.cshtml",
            "/Shared/Views/Partials/{0}.cshtml",
        };

        return locations.Concat(viewLocations);
    }

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // Controller adını context'e ekle (ExpandViewLocations'da kullanmak için)
        context.Values["controller"] = context.ActionContext.RouteData.Values["controller"]?.ToString() ?? "";
    }
}
