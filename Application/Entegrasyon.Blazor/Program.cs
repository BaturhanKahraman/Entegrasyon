using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.SignalR;
using Entegrasyon.Business.Channels;
using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Blazor.Utility.Notifications;
using Entegrasyon.Blazor.Utility.Services;
using Entegrasyon.Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Entegrasyon.ApplicationBootstrap.Logger;
using Entegrasyon.Blazor.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomEnd;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
});

builder.Services.AddLogging();
builder.Services.AddConfigurations(builder.Configuration);
builder.Host.UseDefaultServiceProvider((host, options) =>
{
    options.ValidateOnBuild = host.HostingEnvironment.IsDevelopment();
    options.ValidateScopes = host.HostingEnvironment.IsDevelopment();
});
builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddClients();
builder.Services.AddBackgroundServices();

// Event channels and background services are registered via AddEventChannels() and AddBackgroundServices()

builder.Services.AddStorageServices(builder.Configuration);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Entegrasyon.ApplicationBootstrap.Security.AppPermissions.GetAllPermissions())
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
builder.Services.AddSingleton<InProcessNotificationDeliveryService>();
builder.Services.AddSingleton<INotificationDeliveryService>(
    sp => sp.GetRequiredService<InProcessNotificationDeliveryService>());
builder.Services.AddSingleton<INotificationChannel>(
    sp => sp.GetRequiredService<InProcessNotificationDeliveryService>());
builder.Services.AddHostedService<NotificationEventPublisher>();

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
app.MapTrendyolWebhooks();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
