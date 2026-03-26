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
app.MapControllerRoute("account", "/hesabim/{action=Index}",
    new { controller = "Account" });
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
