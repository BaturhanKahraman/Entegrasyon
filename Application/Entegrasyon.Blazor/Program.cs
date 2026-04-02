using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.SignalR;
using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Blazor.Middleware;
using Entegrasyon.Blazor.Utility;
using Entegrasyon.Blazor.Utility.Chat;
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
using Microsoft.AspNetCore.DataProtection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

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

// ── OpenTelemetry ──────────────────────────────────────────────────────
// Auto-instrumentation: ASP.NET Core, HttpClient (marketplace API), EF Core (DB)
// OTLP exporter → Grafana Tempo (veya başka OTLP collector)
var otelEndpointRaw = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
var otelEndpoint = string.IsNullOrWhiteSpace(otelEndpointRaw) ? "http://localhost:4317" : otelEndpointRaw;
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
            .AddService("Entegrasyon.Blazor"))
        .AddSource("Entegrasyon.Blazor")     // custom ActivitySource for Blazor circuit errors
        .AddSource("Entegrasyon.Business")   // marketplace custom traces (TrendyolProductService, HepsiburadaProductService, N11, ProductSyncManager)
        .AddAspNetCoreInstrumentation(opt =>
        {
            // Gürültüyü filtrele — static files, _blazor, _framework trace'den çıkar
            opt.Filter = ctx =>
            {
                var path = ctx.Request.Path.Value ?? "";
                if (path.StartsWith("/_blazor") || path.StartsWith("/_framework") ||
                    path.StartsWith("/_content") || path.StartsWith("/css") ||
                    path.StartsWith("/js") || path.StartsWith("/fonts") ||
                    path.StartsWith("/favicon") || path.EndsWith(".js") ||
                    path.EndsWith(".css") || path.EndsWith(".woff") ||
                    path.EndsWith(".woff2") || path.EndsWith(".png") ||
                    path.EndsWith(".jpg") || path.EndsWith(".svg"))
                    return false;
                return true;
            };
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint)))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
            .AddService("Entegrasyon.Blazor"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Entegrasyon.Business")  // custom business metrics (product sync, API durations, orders)
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint)));

// OpenTelemetry Logging — ILogger çağrıları dashboard'ın "Yapılandırılmış" sekmesinde görünür
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
        .AddService("Entegrasyon.Blazor"));
    logging.AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint));
});
// ────────────────────────────────────────────────────────────────────────

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
// Blazor Server auth, ProtectedLocalStorage + AuthorizeRouteView ile calisir.
// HTTP-level authorization middleware Blazor endpoint'lerini BLOKLAMAMALI —
// AuthorizeRouteView circuit basladiktan sonra auth kontrolu yapar.
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();
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
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("Entegrasyon.Blazor");
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

// Chat services
builder.Services.AddSingleton<InProcessChatDeliveryService>();
builder.Services.AddSingleton<IChatDeliveryService>(
    sp => sp.GetRequiredService<InProcessChatDeliveryService>());
builder.Services.AddSingleton<UserOnlineStatusTracker>();
builder.Services.AddHostedService<ChatEventPublisher>();

var app = builder.Build();

// CLI: --migrate flag ile sadece EF Core migration çalıştırıp çıkar (deploy pipeline için)
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations applied successfully.");
    return;
}

// Testing ortamında (E2E) otomatik migration — DB boş başlıyor
if (app.Environment.EnvironmentName == "Testing")
{
    await using var scope = app.Services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    Console.WriteLine("Testing environment: migrations applied automatically.");
}

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

// Health endpoint — tenant middleware'den önce, auth gerektirmez
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/health"), healthApp =>
{
    healthApp.Run(async ctx =>
    {
        ctx.Response.ContentType = "text/plain";
        await ctx.Response.WriteAsync("healthy");
    });
});

app.UseMiddleware<BlazorTenantResolutionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapHub<NotificationHub>("/NotificationHub");
app.MapHub<ChatHub>("/ChatHub");
app.MapTrendyolWebhooks();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }

/// <summary>
/// Blazor Server'da HTTP-level authorization middleware'ini bypass eder.
/// Auth kontrolu AuthorizeRouteView tarafindan Blazor circuit icerisinde yapilir.
/// ProtectedLocalStorage (JS interop) HTTP pipeline'da okunamadigi icin
/// HTTP-level auth her zaman basarisiz olur — bu handler bunu onler.
/// </summary>
public class BlazorAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // Tum istekleri gecir — Blazor AuthorizeRouteView auth'u handle eder
        return next(context);
    }
}
