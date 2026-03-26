using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure;
using Entegrasyon.AdminPanel.Infrastructure.Auth;
using Entegrasyon.AdminPanel.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AdminPanelDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AdminPanel") ?? "Data Source=adminpanel.db"));

builder.Services.AddDbContext<TemplateDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TemplateDb")));

builder.Services.AddScoped<TenantProvisioningService>();

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
    SeedData.Initialize(db);
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

public partial class Program { } // For integration tests
