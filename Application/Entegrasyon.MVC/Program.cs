using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.SignalR;
using Entegrasyon.DependencyResolver;
using Entegrasyon.MVC.Utility.Notifications;
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
builder.Host.UseDefaultServiceProvider((host,options) =>
{
    options.ValidateOnBuild = host.HostingEnvironment.IsDevelopment();
    options.ValidateScopes = host.HostingEnvironment.IsDevelopment();
});
builder.Services.AddApplicationDependencies();
builder.Services.AddModelViewMapping();
builder.Services.AddClients();
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddCustomDbContext();
builder.AddSerilogWithLoggerProvider(builder.Configuration);
builder.Services.AddResponseCaching();
builder.Services.AddSingleton<IMenuService,MenuService>();
builder.Services.AddSignalR();
builder.Services.AddSignalRSettings();
builder.Services.AddNotification();
builder.Services.AddSingleton<IBlazorNotificationSender, BlazorNotificationSender>();
builder.Services.AddSingleton<INotificationSender, BlazorNotificationSender>();
var app = builder.Build();
app.Lifetime.ApplicationStarted.Register(async () =>
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var lifeTimeHandler = serviceScope.ServiceProvider.GetRequiredService<ApplicationLifetimeManager>();
    await lifeTimeHandler.ApplyStartActions();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<NotificationHub>("/NotificationHub");
app.MapBlazorHub();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();