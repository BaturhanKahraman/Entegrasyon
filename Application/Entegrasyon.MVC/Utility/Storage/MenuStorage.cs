using Entegrasyon.MVC.Utility.Objects;

namespace Entegrasyon.MVC.Utility.Storage;

public static class MenuStorage
{
    public static Dictionary<string,NavigationItem> Menus = new()
    {
        ["product"] = new NavigationItem("Ürünler","Products"),
        ["sale"] = new NavigationItem("Satış","Sales"),
        ["branchOffice"] = new NavigationItem("Depo/Ofis","Offices"),
        ["category"] = new NavigationItem("Kategori","Categories"),
        ["customer"] = new NavigationItem("Müşteriler","Customers"),
        ["order"] = new NavigationItem("Siparişler","Orders"),
        ["user"] = new NavigationItem("Kullanıcılar","Users"),
        ["cargo"] = new NavigationItem("Cargo","Cargos")
    };
}