using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.SignalR;
using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Blazor.Utility.Notifications;
using Entegrasyon.Blazor.Utility.Services;
using Entegrasyon.Blazor.Services.Channels;
using Entegrasyon.Blazor.Services;
using Entegrasyon.Blazor.Services.BackgroundServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Shared.Logger.Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddLogging();
builder.Services.AddConfigurations(builder.Configuration);
builder.Host.UseDefaultServiceProvider((host, options) =>
{
    options.ValidateOnBuild = host.HostingEnvironment.IsDevelopment();
    options.ValidateScopes = host.HostingEnvironment.IsDevelopment();
});
builder.Services.AddApplicationDependencies();
builder.Services.AddClients();
builder.Services.AddBackgroundServices();

// Add event channels for in-process messaging
builder.Services.AddEventChannels();

// Add new background services that use Channels
builder.Services.AddHostedService<ProductSyncBackgroundService>();
builder.Services.AddHostedService<MarketplaceSyncBackgroundService>();
builder.Services.AddHostedService<TrendyolCategoryImportBackgroundService>();

// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(x =>
// {
//     x.SlidingExpiration = true;
//     x.ExpireTimeSpan = TimeSpan.FromHours(1);
//     x.LoginPath = "/auth/login";
//     x.AccessDeniedPath = "/access-denied";
//     x.LogoutPath = "/auth/logout";
// });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Shared.Extensions.Constants.AppPermissions.GetAllPermissions())
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("Admin") || ctx.User.HasClaim("Permission", permission))
        );
    }
});
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
    opt.InstanceName = "DemoInstance";
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCustomDbContext(builder.Configuration);
builder.AddSerilogWithLoggerProvider(builder.Configuration);
builder.Services.AddResponseCaching();
builder.Services.AddSingleton<IMenuService, MenuService>();

// SignalR configuration
builder.Services.AddSignalR(options =>
{
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors = false;
        options.MaximumReceiveMessageSize = 102400; // 100KB
        options.StreamBufferCapacity = 10;
    }
});
builder.Services.AddSignalRSettings();
builder.Services.AddNotification();
builder.Services.AddSingleton<IBlazorNotificationSender, BlazorNotificationSender>();
builder.Services.AddSingleton<INotificationSender, BlazorNotificationSender>();

// Register notification event publisher
builder.Services.AddSingleton<NotificationEventPublisher>();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var lifeTimeHandler = serviceScope.ServiceProvider.GetRequiredService<ApplicationLifetimeManager>();
    await lifeTimeHandler.ApplyStartActions();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithRedirects("~/error?code={0}");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseResponseCaching();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/NotificationHub");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
