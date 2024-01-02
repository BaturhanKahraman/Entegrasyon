using Entegrasyon.MVC.Utility.Objects;
using System.Collections.ObjectModel;

namespace Entegrasyon.MVC.Utility.Storage;

public static class MenuStorage
{
    public static ReadOnlyDictionary<string, NavigationItem> Menus = new(
        new Dictionary<string, NavigationItem>
        {
            ["product"] = new NavigationItem("Ürünler", "Products", "package", 10),
            ["category"] = new NavigationItem("Kategori", "Categories", "package", 20),
            ["sale"] = new NavigationItem("Satış", "Sales", "credit-card", 30),
            ["customer"] = new NavigationItem("Müşteriler", "Customers", "dollar-sign", 40),
            ["order"] = new NavigationItem("Siparişler", "Orders", "shopping-bag", 50),
            ["user"] = new NavigationItem("Kullanıcılar", "Users", "users", 60),
            ["report"] = new NavigationItem("Raporlar", "Reports", "pie-chart", 70),
            ["branchOffice"] = new NavigationItem("Depo/Ofis", "Offices", "printer", 80),
            ["setting"] = new NavigationItem("Ayarlar", "Settings", "settings", 100),
            ["cargo"] = new NavigationItem("Kargo", "Cargos", "truck", 90),
            ["brand"] = new NavigationItem("Marka", "Brands", "truck", 33),
        });
}