using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Extensions;
using Entegrasyon.MVC.Utility.Attributes;
using Entegrasyon.MVC.Utility.Mapper;
using Entegrasyon.MVC.Utility.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using Shared.FileStorage;
using Shared.Logger.Serilog;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews();
#if DEBUG
mvcBuilder.AddRazorRuntimeCompilation();
#endif
builder.Services.AddServerSideBlazor();

builder.Services.AddLogging();
builder.Services.AddConfigurations(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddApplicationDependencies();
builder.Services.AddClients();
builder.Services.AddAutoMapper(x => x.AddProfile<CustomMapProfile>());
builder.Services.AddFileStorageCore();
builder.Services.AddBackgroundServices();
builder.Services.AddLocalFileStorage(builder.Configuration.GetSection("LocalFileStorageOptions"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(x =>
{
    x.SlidingExpiration = true;
    x.ExpireTimeSpan = TimeSpan.FromHours(1);
    x.LoginPath = "/auth/login";

    x.AccessDeniedPath = "/access-denied";
    x.LogoutPath = "/auth/logout";
});
builder.Services.AddAuthorization();
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = "redis";
    opt.InstanceName = "DemoInstance";
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(x =>
{
    x.IdleTimeout = TimeSpan.FromHours(1);
    x.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddCustomDbContext();
builder.AddSerilogWithLoggerProvider(builder.Configuration);
builder.Services.AddResponseCaching();
builder.Services.AddScoped<IMenuService,MenuService>();
var app = builder.Build();
app.Lifetime.ApplicationStarted.Register(async () =>
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var lifeTimeHandler = serviceScope.ServiceProvider.GetRequiredService<ApplicationLifetimeManager>();
    await lifeTimeHandler.ApplyStartActions();
    await serviceScope.DisposeAsync();
});
// Configure the HTTP request pipeline.
if(!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithRedirects("~/home/error?code={0}");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
    app.UseDeveloperExceptionPage();
app.UseResponseCaching();
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(),"node_modules")),
    RequestPath = "/vendor"
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();