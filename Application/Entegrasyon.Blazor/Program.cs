using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.SignalR;
using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Blazor.Middleware;
using Entegrasyon.Blazor.Utility;
using Entegrasyon.Blazor.Utility.Notifications;
using Entegrasyon.Blazor.Utility.Services;
using Entegrasyon.Blazor.Services;
using Entegrasyon.Blazor.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.ResponseCompression;
using MudBlazor.Services;
using Entegrasyon.ApplicationBootstrap.Logger;
using Entegrasyon.Blazor.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Web App services (.NET 8 unified pattern)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2);
        options.DisconnectedCircuitMaxRetained = 500;
    });

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

// Background service hataları uygulamayı durdurmasın (özellikle dev ortamında DB yoksa)
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});
builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddClients();
builder.Services.AddBackgroundServices();
builder.Services.AddStorefrontServices();

// Event channels and background services are registered via AddEventChannels() and AddBackgroundServices()

builder.Services.AddStorageServices(builder.Configuration);

builder.Services.AddAuthentication(options =>
{
    // Blazor Server custom AuthenticationStateProvider kullanır.
    // Bu scheme sadece middleware pipeline'ın çalışması için gerekli.
    // LoginPath yok — auth redirect Blazor'ın AuthorizeRouteView + RedirectToLogin ile yapılır.
    options.DefaultScheme = "BlazorServer";
}).AddCookie("BlazorServer");
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Entegrasyon.ApplicationBootstrap.Security.AppPermissions.GetAllPermissions())
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(
                new Entegrasyon.ApplicationBootstrap.Security.PermissionRequirement(permission)));
    }
});
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    Entegrasyon.ApplicationBootstrap.Security.TenantFeatureAuthorizationHandler>();
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
    opt.InstanceName = "DemoInstance";
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCustomDbContext(builder.Configuration);
builder.Services.AddScoped<CircuitHandler, TenantCircuitHandler>();
builder.AddSerilogWithLoggerProvider(builder.Configuration);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
builder.Services.AddAntiforgery();
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

app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});

app.UseMiddleware<BlazorTenantResolutionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHub<NotificationHub>("/NotificationHub");
app.MapTrendyolWebhooks();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
