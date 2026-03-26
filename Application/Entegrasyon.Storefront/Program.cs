using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Storefront.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddCustomDbContext(builder.Configuration);
builder.Services.AddStorefrontServices(); // will fail until Task 7, that's OK
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/giris";
        options.LogoutPath = "/cikis";
        options.AccessDeniedPath = "/giris";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Storefront.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddControllersWithViews();
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = "Storefront.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/hata/500");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000"
});
app.UseResponseCaching();
app.UseSession();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/hata/{0}");

app.MapControllerRoute("home", "/",
    new { controller = "Home", action = "Index" });
app.MapControllerRoute("categories", "/kategoriler",
    new { controller = "Catalog", action = "Categories" });
app.MapControllerRoute("category", "/kategori/{slug}",
    new { controller = "Catalog", action = "Category" });
app.MapControllerRoute("subcategory", "/kategori/{parentSlug}/{slug}",
    new { controller = "Catalog", action = "Category" });
app.MapControllerRoute("product", "/urun/{slug}",
    new { controller = "Product", action = "Detail" });
app.MapControllerRoute("brand", "/marka/{slug}",
    new { controller = "Catalog", action = "Brand" });
app.MapControllerRoute("allProducts", "/urunler",
    new { controller = "Catalog", action = "AllProducts" });
app.MapControllerRoute("search", "/arama",
    new { controller = "Catalog", action = "Search" });
app.MapControllerRoute("searchSuggest", "/api/arama/oneri",
    new { controller = "Catalog", action = "SearchSuggest" });
app.MapControllerRoute("popularSearches", "/api/arama/populer",
    new { controller = "Catalog", action = "PopularSearches" });
app.MapControllerRoute("newProducts", "/yeni-urunler",
    new { controller = "Catalog", action = "NewProducts" });
app.MapControllerRoute("bestSellers", "/cok-satanlar",
    new { controller = "Catalog", action = "BestSellers" });
app.MapControllerRoute("login", "/giris",
    new { controller = "Auth", action = "Login" });
app.MapControllerRoute("register", "/kayit",
    new { controller = "Auth", action = "Register" });
app.MapControllerRoute("logout", "/cikis",
    new { controller = "Auth", action = "Logout" });
app.MapControllerRoute("forgotPassword", "/sifremi-unuttum",
    new { controller = "Auth", action = "ForgotPassword" });
app.MapControllerRoute("resetPassword", "/sifre-sifirla",
    new { controller = "Auth", action = "ResetPassword" });
app.MapControllerRoute("confirmEmail", "/email-dogrula",
    new { controller = "Auth", action = "ConfirmEmail" });
app.MapControllerRoute("orderDetail", "/hesabim/siparis/{id}",
    new { controller = "Account", action = "OrderDetail" });
app.MapControllerRoute("orderCancel", "/hesabim/siparis/{id}/iptal",
    new { controller = "Account", action = "CancelOrder" });
app.MapControllerRoute("orderInvoice", "/hesabim/siparis/{id}/fatura",
    new { controller = "Account", action = "DownloadInvoice" });
app.MapControllerRoute("accountReturns", "/hesabim/iadelerim",
    new { controller = "Account", action = "Returns" });
app.MapControllerRoute("accountCreateReturn", "/hesabim/iade-talebi/{orderId}",
    new { controller = "Account", action = "CreateReturn" });
app.MapControllerRoute("accountBuyAgain", "/hesabim/tekrar-satin-al",
    new { controller = "Account", action = "BuyAgain" });
app.MapControllerRoute("accountSecurity", "/hesabim/guvenlik",
    new { controller = "Account", action = "Security" });
app.MapControllerRoute("accountExportData", "/hesabim/veri-indir",
    new { controller = "Account", action = "ExportData" });
app.MapControllerRoute("account", "/hesabim/{action=Index}",
    new { controller = "Account" });
app.MapControllerRoute("tracking", "/siparis-takip",
    new { controller = "Tracking", action = "Index" });
app.MapControllerRoute("trackingResult", "/siparis-takip/sonuc",
    new { controller = "Tracking", action = "Result" });
app.MapControllerRoute("cart", "/sepet",
    new { controller = "Cart", action = "Index" });
app.MapControllerRoute("cartApi", "/api/sepet/{action}",
    new { controller = "Cart" });
app.MapControllerRoute("paymentCallback", "/odeme/callback",
    new { controller = "Checkout", action = "Callback" });
app.MapControllerRoute("checkout", "/odeme/{action=Index}",
    new { controller = "Checkout" });
app.MapControllerRoute("contact", "/iletisim",
    new { controller = "Contact", action = "Index" });
app.MapControllerRoute("wishlist", "/favorilerim",
    new { controller = "Wishlist", action = "Index" });
app.MapControllerRoute("wishlistApi", "/api/favori/{action}",
    new { controller = "Wishlist" });
app.MapControllerRoute("newsletterApi", "/newsletter",
    new { controller = "Newsletter", action = "Subscribe" });
app.MapControllerRoute("reviewApi", "/urun/{slug}/yorum",
    new { controller = "Product", action = "AddReview" });
app.MapControllerRoute("compare", "/karsilastir",
    new { controller = "Compare", action = "Index" });
app.MapControllerRoute("stockNotifyApi", "/api/stok-bildirim",
    new { controller = "StockNotification", action = "Subscribe" });
app.MapControllerRoute("legal", "/{slug}",
    new { controller = "Page", action = "Show" });
app.MapControllerRoute("robots", "/robots.txt",
    new { controller = "Seo", action = "Robots" });
app.MapControllerRoute("sitemap", "/sitemap.xml",
    new { controller = "Seo", action = "Sitemap" });
app.MapControllerRoute("error", "/hata/{statusCode}",
    new { controller = "Error", action = "Index" });

app.Run();

public partial class Program { }
