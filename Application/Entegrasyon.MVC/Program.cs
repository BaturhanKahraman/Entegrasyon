using Entegrasyon.MVC.Infrastructure.Validation;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
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
using Entegrasyon.MVC.Infrastructure.BranchOffices;
using Entegrasyon.MVC.Infrastructure.Security;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + Feature Folders ────────────────────────────────────────────────
// (Türkçe doğrulama mesajları)
builder.Services.AddControllersWithViews(options =>
{
    // Global CSRF protection — all POST/PUT/DELETE auto-validated
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Custom filters
    options.Filters.Add<TenantActionFilter>();
    options.Filters.Add<AutoValidationFilter>();

    // Para alanları için esnek decimal binder (T111): invariant + Türkçe biçimi tolere eder.
    // Default decimal binder'dan önce çalışsın diye başa eklenir.
    options.ModelBinderProviders.Insert(0,
        new Entegrasyon.MVC.Infrastructure.ModelBinding.FlexibleDecimalModelBinderProvider());

    // Türkçe doğrulama mesajları (implicit required dahil)
    options.ModelMetadataDetailsProviders.Add(new TurkishValidationMetadataProvider());

    // Model binding (tip/parse) hata mesajları — Türkçe
    var mp = options.ModelBindingMessageProvider;
    mp.SetValueIsInvalidAccessor(v => $"'{v}' değeri geçersiz.");
    mp.SetValueMustNotBeNullAccessor(_ => "Bu alan boş olamaz.");
    mp.SetMissingBindRequiredValueAccessor(f => $"'{f}' alanı için değer gönderilmedi.");
    mp.SetMissingKeyOrValueAccessor(() => "Bir değer gereklidir.");
    mp.SetAttemptedValueIsInvalidAccessor((v, f) => $"'{v}' değeri '{f}' alanı için geçersiz.");
    mp.SetUnknownValueIsInvalidAccessor(f => $"'{f}' alanına gönderilen değer geçersiz.");
    mp.SetValueMustBeANumberAccessor(f => $"'{f}' alanı bir sayı olmalıdır.");
    mp.SetNonPropertyValueMustBeANumberAccessor(() => "Değer bir sayı olmalıdır.");
})
.AddSessionStateTempDataProvider();

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
        // "Beni hatırla" işaretlenmediğindeki taban ömür (login'de ExpiresUtc ile override edilir).
        // SlidingExpiration: aktif kullanıcı erken atılmaz, her istekte pencere yenilenir.
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Entegrasyon.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Oturum geçersizleştirme (auto-logout) + HTMX-aware yönlendirmeler.
        // ValidatePrincipal her istekte SecurityStamp/IsActive/IsDeleted'i DB ile doğrular;
        // pasif/silinen/şifresi-sıfırlanan kullanıcının cookie'si reddedilir.
        // EventsType, options.Events lambda'larının yerine geçer (scoped DI gerektiği için).
        options.EventsType = typeof(SecurityStampCookieEvents);
    });

builder.Services.AddScoped<SecurityStampCookieEvents>();

// LastSeenAt throttle — singleton (in-memory ConcurrentDictionary, kullanıcı başına izole).
// Kullanıcı başına en fazla 1 dk'da bir DB-write; her istekte senkron yazımı önler.
builder.Services.AddSingleton(new LastSeenThrottle(TimeSpan.FromMinutes(1)));

// ── Authorization — AppPermissions ────────────────────────────────────────
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

// ── Hybrid Cache (.NET 10) ──────────────────────────────────────────────
// L1 (in-memory) + L2 (Redis) otomatik. Stampede protection dahili.
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});

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

    // Device register — per IP. Invite-code brute-force'a karşı agresif limit.
    options.AddPolicy("device-register", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition
            .GetFixedWindowLimiter(ip, _ =>
                new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
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
// TODO: MVC-specific notification delivery servisi eklenecek

// ── Exception Handling ───────────────────────────────────────────────────
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
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
builder.Services.AddScoped<Entegrasyon.Business.Abstract.ICurrentUserContext, Entegrasyon.MVC.Infrastructure.CurrentUserContext>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// ── Request Localization ────────────────────────────────────────────────
// Model binder decimal/DateTime parse'inin host OS culture'indan bagimsiz
// olmasi icin InvariantCulture'i zorluyoruz. Formlardan gelen "1234.56"
// her ortamda ayni sekilde parse edilir. UI culture ise Turkce kalir
// (view'lardaki ToString'ler icin — ancak bizim kod genelde explicit
// InvariantCulture kullanir).
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var invariant = CultureInfo.InvariantCulture;
    var turkish = new CultureInfo("tr-TR");
    options.DefaultRequestCulture = new RequestCulture(culture: invariant, uiCulture: turkish);
    options.SupportedCultures = [invariant];
    options.SupportedUICultures = [turkish];
});

// ── Active Branch Office (session + middleware) ─────────────────────────
builder.Services.AddScoped<IActiveBranchOfficeAccessor, ActiveBranchOfficeAccessor>();

// =====================================================================
var app = builder.Build();
// =====================================================================

// CLI: --migrate flag ile sadece EF Core migration calistirip cikis.
// NOT: DI'da IntegrationDbContext DOGRUDAN kayitli degil — multi-tenant icin
// IDbContextFactory<IntegrationDbContext> kayitli. CLI'da HTTP istegi yok →
// tenant context initialize degil → factory fallback ConnectionStrings:Main kullanir.
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
    await using var db = await contextFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations applied successfully.");
    return;
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var lifeTimeHandler = serviceScope.ServiceProvider.GetRequiredService<ApplicationLifetimeManager>();
    await lifeTimeHandler.ApplyStartActions();

    // Admin role permission drift'ini önle: AppPermissions.GetAllPermissions() listesine eklenen
    // yeni claim'leri Admin role'e idempotent şekilde senkronize eder. Migration sonrası çalışır.
    var adminPermissionSeeder = serviceScope.ServiceProvider
        .GetRequiredService<Entegrasyon.ApplicationBootstrap.Security.AdminPermissionSeeder>();
    await adminPermissionSeeder.EnsureAdminPermissionsAsync();

    // Development offline mode: WireMock container varsa marketplace BaseUrl'lerini
    // WireMock URL'ine ata. Staging/Production'da ASLA calistirilmaz (production
    // DB'de marketplace URL'lerini bozar).
    if (app.Environment.IsDevelopment())
    {
        var devSeederLogger = app.Services.GetRequiredService<ILogger<Program>>();
        await Entegrasyon.MVC.Infrastructure.DevMode.DevWireMockSeeder
            .SeedAsync(app.Services, app.Configuration, devSeederLogger);

        // Fresh dev DB'de admin login'i garanti et: bilinen parola (admin / 123456789).
        // SADECE Development — prod HasData onboarding akisina dokunmaz. Idempotent.
        await Entegrasyon.MVC.Infrastructure.DevMode.DevAdminSeeder
            .SeedAsync(app.Services, devSeederLogger);
    }
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

// Her HTTP istegi icin tek satir yapilandirilmis log (Elapsed ms + RequestPath + StatusCode).
// Loki'de propertiesAsLabels=[RequestPath,StatusCode] ile label olur → TL latency'i query'leyebilir.
// Statik dosyalar zaten OTel filter'inda eleniyor; burada da gurultuyu azaltmak icin elenir.
Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(app, options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isStaticAsset =
            path.StartsWith("/lib/") || path.StartsWith("/css/") || path.StartsWith("/js/") ||
            path.StartsWith("/images/") || path.StartsWith("/favicon");
        if (ex is not null || httpContext.Response.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;
        if (isStaticAsset)
            return Serilog.Events.LogEventLevel.Verbose; // MinimumLevel.Default=Debug ile elenir
        if (elapsed > 1000)
            return Serilog.Events.LogEventLevel.Warning; // yavas istek → dikkat cek
        return Serilog.Events.LogEventLevel.Information;
    };
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});

app.UseRequestLocalization();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseAuthorization();
app.UseOutputCache();
app.UseSession();

// Active branch office validation — her authenticated request'te stale check
// UseAuthentication + UseSession sonrası, endpoint'lerden önce
app.UseMiddleware<ActiveBranchOfficeMiddleware>();

// LastSeenAt güncelleme — admin detay sayfası canlı takibi besler.
// Throttle'lı (kullanıcı başına 1 dk'da bir, tek SQL ExecuteUpdate); auth'dan sonra çalışır.
app.UseMiddleware<LastSeenMiddleware>();

app.UseAntiforgery();

// ── Endpoints ────────────────────────────────────────────────────────────

// TODO: SSE bildirim stream'i şimdilik devre dışı — sayfa geçişlerinde uygulamayı blokluyor.
// İleride SignalR veya düzgün SSE implementasyonu ile değiştirilecek.

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
