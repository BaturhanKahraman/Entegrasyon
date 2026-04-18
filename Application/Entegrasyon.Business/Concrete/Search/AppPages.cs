namespace Entegrasyon.Business.Concrete.Search;

public sealed record AppPage(
    string Title,
    string Url,
    string Icon,
    IReadOnlyList<string> Keywords,
    string? RequiredPermission);

public static class AppPages
{
    public static readonly AppPage[] All =
    [
        new("Dashboard",            "/",                     "ti-dashboard",        ["dashboard", "anasayfa", "home"], null),
        new("Ürünler",              "/products",             "ti-package",          ["urun", "product", "stok", "katalog"], "Permissions.Products.View"),
        new("Ürün Ekle",            "/products/add",         "ti-plus",             ["urun ekle", "yeni urun", "add product"], "Permissions.Products.Create"),
        new("Kategoriler",          "/categories",           "ti-category",         ["kategori", "category"], "Permissions.Categories.View"),
        new("Markalar",             "/brands",               "ti-award",            ["marka", "brand"], "Permissions.Brands.View"),
        new("Müşteriler",           "/customers",            "ti-users",            ["musteri", "customer", "client"], "Permissions.Customers.View"),
        new("POS Terminal",         "/pos",                  "ti-device-desktop",   ["pos", "kasa", "satis ekrani"], "Permissions.Sales.Create"),
        new("Satışlar",             "/sales",                "ti-receipt",          ["satis", "sale", "siparis", "order"], "Permissions.Sales.View"),
        new("İade Yönetimi",        "/returns",              "ti-arrow-back-up",    ["iade", "return", "refund"], "Permissions.Orders.View"),
        new("Sipariş Hazırlama",    "/picking",              "ti-barcode",          ["siparis hazirlama", "picking"], "Permissions.Orders.View"),
        new("Kargo Takip",          "/shipping",             "ti-truck",            ["kargo", "shipping", "teslimat"], "Permissions.Cargo.View"),
        new("Toplu İşlem",          "/bulk-operations",      "ti-layers-subtract",  ["toplu", "bulk"], "Permissions.Products.Edit"),
        new("Depolar",              "/branch-offices",       "ti-building-warehouse",["depo", "subelik", "warehouse", "branch"], "Permissions.BranchOffices.View"),
        new("Stok Hareketleri",     "/stock-movements",      "ti-exchange",         ["stok hareketi", "stock movement"], "Permissions.Products.View"),
        new("Pazaryeri Şablonları", "/matched-entities",     "ti-template",         ["sablon", "template", "eslesme"], "Permissions.Marketplace.View"),
        new("Senkronizasyon",       "/marketplace/sync",     "ti-refresh",          ["senkron", "sync"], "Permissions.Marketplace.View"),
        new("Ürün Eşleştirme",      "/marketplace/matching", "ti-link",             ["eslestirme", "matching"], "Permissions.Marketplace.View"),
        new("Komisyon Oranları",    "/marketplace/commission-rates", "ti-percentage",["komisyon", "commission"], "Permissions.Marketplace.View"),
        new("Entegrasyon Durumu",   "/integrations/health",  "ti-heartbeat",        ["entegrasyon", "integration", "saglik", "health"], "Permissions.Integrations.View"),
        new("İndirimler",           "/discounts",            "ti-percentage",       ["indirim", "discount", "kupon", "coupon"], "Permissions.Products.View"),
        new("Sadakat Programı",     "/loyalty",              "ti-gift",             ["sadakat", "loyalty", "puan"], "Permissions.Customers.View"),
        new("Hediye Kartları",      "/gift-cards",           "ti-gift",             ["hediye", "gift card"], "Permissions.Customers.View"),
        new("Raporlar",             "/reports",              "ti-chart-bar",        ["rapor", "report"], null),
        new("Bildirimler",          "/notifications",        "ti-bell",             ["bildirim", "notification"], null),
        new("Sohbet",               "/chat",                 "ti-message",          ["chat", "sohbet", "mesaj"], null),
        new("Kullanıcılar",         "/users",                "ti-user-shield",      ["kullanici", "user"], "Permissions.Users.View"),
        new("Roller",               "/roles",                "ti-shield",           ["rol", "role", "yetki"], "Permissions.Users.View"),
        new("Ayarlar",              "/settings",             "ti-settings",         ["ayar", "setting", "konfigurasyon"], null),
        new("Profil",               "/profile",              "ti-user-circle",      ["profil", "profile"], null),
        new("Loglar",               "/logs",                 "ti-list",             ["log", "gunluk"], "Permissions.Logs.View")
    ];
}
