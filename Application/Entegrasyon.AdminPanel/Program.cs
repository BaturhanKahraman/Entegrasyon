using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure;
using Entegrasyon.AdminPanel.Infrastructure.Auth;
using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AdminPanelDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AdminPanel")
        ?? "Host=192.168.1.78;Port=5432;Database=AdminPanelDb;Username=baturhan;Password=DiHRrP6dY8nC*M"));

builder.Services.AddDbContext<TemplateDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TemplateDb")));

builder.Services.AddScoped<TenantProvisioningService>();

builder.Services.AddHttpClient("TrendyolPublic", client =>
{
    client.BaseAddress = new Uri("https://api.trendyol.com/sapigw/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

if (builder.Configuration.GetValue("MasterCatalog:EnableAutoSync", false))
{
    builder.Services.AddHostedService<MasterCatalogSyncService>();
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminPanelDbContext>();
    db.Database.Migrate();
    var mainConnStr = builder.Configuration.GetConnectionString("TemplateDb");
    var snapshotDir = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "docs", "trendyol");
    SeedData.Initialize(db, mainConnStr, Directory.Exists(snapshotDir) ? snapshotDir : null);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

internal partial class Program { } // internal — Blazor'daki public Program ile çakışmasın
