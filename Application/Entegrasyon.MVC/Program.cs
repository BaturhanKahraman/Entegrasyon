using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.ApplicationBootstrap.Logger;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
// using Entegrasyon.Business.Notifications; // TODO: Faz 7'de notification delivery eklenince aktifle
using Entegrasyon.Business.Notifications.SignalR;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Entegrasyon.MVC.Infrastructure.ExceptionHandlers;
using Entegrasyon.MVC.Infrastructure.Filters;
using Entegrasyon.MVC.Infrastructure.Middleware;
using System.Net.ServerSentEvents;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + Feature Folders ────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    // Global CSRF protection — all POST/PUT/DELETE auto-validated
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Custom filters
    options.Filters.Add<TenantActionFilter>();
    options.Filters.Add<AutoValidationFilter>();
});

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
});

// ── Cookie Authentication ────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/error/403";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Entegrasyon.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // HTMX-aware: return 401 header instead of redirect for HTMX requests
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Headers.ContainsKey("HX-Request"))
            {
                context.Response.StatusCode = 401;
                context.Response.Headers.Append("HX-Redirect", "/auth/login");
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Headers.ContainsKey("HX-Request"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// ── Authorization — AppPermissions (aynen Blazor'dakiyle aynı) ───────────
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.GetAllPermissions())
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});
builder.Services.AddScoped<IAuthorizationHandler, TenantFeatureAuthorizationHandler>();

// ── Application Dependencies (Business, DataAccess, Entity) ─────────────
builder.Services.AddConfigurations(builder.Configuration);
builder.Host.UseDefaultServiceProvider((host, options) =>
{
    options.ValidateOnBuild = host.HostingEnvironment.IsDevelopment();
    options.ValidateScopes = host.HostingEnvironment.IsDevelopment();
});

// Background service hataları uygulamayı durdurmasın
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddClients();

// Background service'ler: Production/Staging'de etkin, Development'ta devre dışı
// (Blazor ile aynı anda çalışırken çift polling önlenir)
if (!builder.Environment.IsDevelopment())
    builder.Services.AddBackgroundServices();

builder.Services.AddStorefrontServices();
builder.Services.AddStorageServices(builder.Configuration);
builder.Services.AddCustomDbContext(builder.Configuration);

// ── Caching ──────────────────────────────────────────────────────────────
// Redis opsiyonel — bağlantı yoksa in-memory cache'e fallback
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(opt =>
    {
        opt.Configuration = redisConnection;
        opt.InstanceName = "MVC:";
        opt.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        {
            EndPoints = { redisConnection },
            AbortOnConnectFail = false,      // Bağlantı başarısızsa çökmez
            ConnectTimeout = 3000,
            SyncTimeout = 3000,
            ConnectRetry = 2
        };
    });
}
builder.Services.AddDistributedMemoryCache(); // Redis yoksa veya düşerse fallback
builder.Services.AddMemoryCache();

// ── Output Cache ─────────────────────────────────────────────────────────
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("CategoryTree", b => b
        .Expire(TimeSpan.FromMinutes(30))
        .Tag("categories"));

    options.AddPolicy("ProductList", b => b
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByQuery("page", "search", "sort")
        .Tag("products"));

    options.AddPolicy("Dashboard", b => b
        .Expire(TimeSpan.FromMinutes(2))
        .Tag("dashboard"));
});

// ── Rate Limiting ────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Marketplace sync — per tenant
    options.AddPolicy("marketplace-sync", context =>
    {
        var tenantId = context.User?.FindFirst("TenantId")?.Value ?? "anon";
        return System.Threading.RateLimiting.RateLimitPartition
            .GetSlidingWindowLimiter(tenantId, _ =>
                new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6
                });
    });

    // Login — per IP
    options.AddPolicy("login", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition
            .GetFixedWindowLimiter(ip, _ =>
                new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15)
                });
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        if (context.HttpContext.Request.Headers.ContainsKey("HX-Request"))
        {
            context.HttpContext.Response.ContentType = "text/html";
            await context.HttpContext.Response.WriteAsync(
                "<div class='alert alert-warning'>Cok fazla istek gonderdiniz. Lutfen biraz bekleyin.</div>", ct);
        }
    };
});

// ── Health Checks ────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Main")
            ?? "Host=localhost;Database=IntegrationDb",
        name: "postgresql",
        tags: ["db", "ready"]);

// ── SignalR ──────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors = false;
        options.MaximumReceiveMessageSize = 102400;
        options.StreamBufferCapacity = 10;
    }
});
builder.Services.AddSignalRSettings();
builder.Services.AddNotification();
// TODO: Faz 7'de MVC-specific notification delivery servisi eklenecek
// (Blazor'daki InProcessNotificationDeliveryService + NotificationEventPublisher
// Blazor projesine bağlı — MVC'ye taşınması veya Business'a refactor edilmesi gerekecek)

// ── Exception Handling ───────────────────────────────────────────────────
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<HtmxExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        var tenantId = context.HttpContext.User.FindFirst("TenantId")?.Value;
        if (tenantId is not null)
            context.ProblemDetails.Extensions["tenantId"] = tenantId;
    };
});

// ── OpenTelemetry ────────────────────────────────────────────────────────
var otelEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Entegrasyon.MVC"))
        .AddSource("Entegrasyon.MVC")
        .AddSource("Entegrasyon.Business")
        .AddAspNetCoreInstrumentation(opt =>
        {
            opt.Filter = ctx =>
            {
                var path = ctx.Request.Path.Value ?? "";
                return !path.StartsWith("/lib/") && !path.StartsWith("/css/") &&
                       !path.StartsWith("/js/") && !path.StartsWith("/images/") &&
                       !path.StartsWith("/favicon") && !path.EndsWith(".js") &&
                       !path.EndsWith(".css") && !path.EndsWith(".woff") &&
                       !path.EndsWith(".woff2") && !path.EndsWith(".png") &&
                       !path.EndsWith(".jpg") && !path.EndsWith(".svg");
            };
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint)))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Entegrasyon.MVC"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Entegrasyon.Business")
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint)));

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Entegrasyon.MVC"));
    logging.AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint));
});

// ── Serilog ──────────────────────────────────────────────────────────────
builder.AddSerilogWithLoggerProvider(builder.Configuration);

// ── Session (POS cart state) ─────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.Name = "Entegrasyon.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── Misc ─────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery();

// =====================================================================
var app = builder.Build();
// =====================================================================

// CLI: --migrate flag ile sadece EF Core migration calistirip cikis
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations applied successfully.");
    return;
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var lifeTimeHandler = serviceScope.ServiceProvider.GetRequiredService<ApplicationLifetimeManager>();
    await lifeTimeHandler.ApplyStartActions();
});

// ── Middleware Pipeline ──────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});

app.UseMiddleware<TenantResolutionMiddleware>();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseSession();
app.UseAntiforgery();

// ── Endpoints ────────────────────────────────────────────────────────────

// SSE: Bildirim stream'i (SignalR NotificationHub yerine — tek yönlü, hafif)
app.MapGet("/notifications/stream", async (
    HttpContext context,
    IServiceScopeFactory scopeFactory,
    CancellationToken ct) =>
{
    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userId is null) return Results.Unauthorized();

    async IAsyncEnumerable<SseItem<string>> GetNotifications(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var uid = Guid.Parse(userId);
        DateTimeOffset lastCheck = DateTimeOffset.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(3000, cancellationToken);

            using var scope = scopeFactory.CreateScope();
            var notificationManager = scope.ServiceProvider
                .GetRequiredService<Entegrasyon.Business.Abstract.INotificationManager>();

            var recent = await notificationManager.GetNotificationsForUser(uid, onlyUnread: true);
            var newOnes = recent?.Where(n => n.CreatedAt > lastCheck).ToList();

            if (newOnes is { Count: > 0 })
            {
                lastCheck = DateTimeOffset.UtcNow;
                foreach (var n in newOnes)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(
                        new { n.Id, Header = n.Header ?? "", Content = n.Content ?? "" });
                    yield return new SseItem<string>(json, eventType: "notification");
                }
            }
        }
    }

    return TypedResults.ServerSentEvents(GetNotifications(ct));
}).RequireAuthorization();

// Chat hala SignalR (bidirectional gerekli)
app.MapHub<ChatHub>("/ChatHub");

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
