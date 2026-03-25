using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Storefront.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddCustomDbContext(builder.Configuration);
builder.Services.AddStorefrontServices(); // will fail until Task 7, that's OK
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
