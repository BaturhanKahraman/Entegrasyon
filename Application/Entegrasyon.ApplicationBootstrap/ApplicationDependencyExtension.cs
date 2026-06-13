using System.Diagnostics;
using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.ApplicationBootstrap.MasterCatalog;
using Entegrasyon.Business.BackgroundServices;
using Scrutor;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Business.Concrete.Invoicing;
using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.Business.Concrete.Temu;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Business.Concrete.Trendyol.EFatura;
using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Concrete.BulkOperations;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Concrete.POS;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Emails;
using Entegrasyon.Business.Notifications.SignalR;
using Entegrasyon.Business.Notifications.Sms;
using Microsoft.AspNetCore.SignalR;
using Entegrasyon.ApplicationBootstrap.FileStorage;
using Entegrasyon.ApplicationBootstrap.Tenants;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Labels;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.ApplicationBootstrap
{
    public static class ApplicationBootstrapExtensions
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            // ── Scrutor convention scan ──────────────────────────────────────────────
            // Automatically registers all standard IXxx → Xxx Scoped pairs from the
            // Business assembly. Naming-exception services are registered explicitly
            // below and override via last-wins. Mock filter defensive — artik Business
            // projesinde hic Mock class'i yok (Faz 3 + Faz 7 temizligi sonrasi), ama
            // ileride yanlislikla Mock* isimli class eklenirse Scrutor tarafindan
            // yakalanip register edilmemesini garanti eder.
            services.Scan(scan => scan
                .FromAssemblyOf<NotificationManager>()      // Entegrasyon.Business assembly
                .AddClasses(classes => classes
                    .InNamespaces(
                        "Entegrasyon.Business.Concrete",
                        "Entegrasyon.Business.Concrete.Auth",
                        "Entegrasyon.Business.Concrete.BulkOperations",
                        "Entegrasyon.Business.Concrete.Import",
                        "Entegrasyon.Business.Concrete.Invoicing",
                        "Entegrasyon.Business.Concrete.Kargo",
                        "Entegrasyon.Business.Concrete.POS",
                        "Entegrasyon.Business.Concrete.Printing",
                        "Entegrasyon.Business.Concrete.Search",
                        "Entegrasyon.Business.Concrete.Shipping",
                        "Entegrasyon.Business.Concrete.Stock",
                        "Entegrasyon.Business.Concrete.Storefront",
                        "Entegrasyon.Business.Notifications"
                    )
                    .Where(t =>
                        !t.Name.StartsWith("Mock")                   &&  // defensive — Business'te artik Mock class yok
                        !t.Name.EndsWith("CategoryImporter")         &&  // concrete-only, no interface
                        !t.Name.EndsWith("MappingValidator")         &&  // concrete-only, no interface
                        !t.Name.EndsWith("Cache")                    &&  // may need explicit lifetime
                        !t.Name.EndsWith("InvoiceBuilder")           &&  // concrete-only
                        !t.Name.EndsWith("InvoiceClient")            &&  // brand-named, keep manual
                        !t.Name.EndsWith("KargoClient")              &&  // concrete-only, conditional registration
                        !t.Name.EndsWith("KargoService")             &&  // conditional mock/real, keep manual
                        !typeof(IHostedService).IsAssignableFrom(t)      // never auto-register hosted
                    )
                )
                .AsMatchingInterface()      // strips I-prefix: BranchOfficeManager → IBranchOfficeManager
                .WithScopedLifetime()
            );
            // ─────────────────────────────────────────────────────────────────────────

            // Search source'ları ISearchSource arabirimine kayıt (GlobalSearchManager IEnumerable<ISearchSource> alır)
            services.AddScoped<Entegrasyon.Business.Abstract.Search.ISearchSource, Entegrasyon.Business.Concrete.Search.PageSearchSource>();
            services.AddScoped<Entegrasyon.Business.Abstract.Search.ISearchSource, Entegrasyon.Business.Concrete.Search.ProductSearchSource>();
            services.AddScoped<Entegrasyon.Business.Abstract.Search.ISearchSource, Entegrasyon.Business.Concrete.Search.CustomerSearchSource>();
            services.AddScoped<Entegrasyon.Business.Abstract.Search.ISearchSource, Entegrasyon.Business.Concrete.Search.CategorySearchSource>();
            services.AddScoped<Entegrasyon.Business.Abstract.Search.ISearchSource, Entegrasyon.Business.Concrete.Search.BrandSearchSource>();

            // Task 3.1: ICurrentUserContext fallback (MVC katmanı Program.cs'de override eder)
            services.TryAddScoped<Entegrasyon.Business.Abstract.ICurrentUserContext,
                Entegrasyon.Business.Concrete.Auth.NullCurrentUserContext>();

            services.AddScoped<ITenantContext, HttpTenantContext>();
            services.AddScoped<TenantMemoryCache>();
            services.AddSingleton<ITenantRegistryDataSource, AdminPanelTenantDataSource>();
            services.AddSingleton<ITenantRegistry, TenantRegistryService>();
            services.AddSingleton<IFeatureDataSource, AdminPanelFeatureDataSource>();
            services.AddScoped<IFeatureService, FeatureService>();

            services.AddScoped<ApplicationLifetimeManager>();
            services.AddScoped<Security.AdminPermissionSeeder>();
            //services.AddScoped<DbContext,IntegrationDbContext>();

            services.AddSingleton<IRandomGenerator, RandomGenerator>();
            services.AddSingleton<IPrintBatchProgressBroadcaster, Entegrasyon.Business.Concrete.Printing.PrintBatchProgressBroadcaster>();
            //services.AddUserServices<ApplicationUser, RootLogin, RootRole, RootClaim, IntegrationDbContext>();

            // Mapperly mappers (singletons — stateless source-generated mappers)
            services.AddSingleton<CargoCompanyMapper>();
            services.AddSingleton<BranchOfficeMapper>();
            services.AddSingleton<BrandMapper>();
            services.AddSingleton<SaleMapper>();
            services.AddSingleton<HelpRequestMapper>();

            // Brand auto-match (explicit — IBrandAutoMatchService naming exception vs Scrutor)
            services.AddScoped<IBrandAutoMatchService, BrandAutoMatchService>();

            // NAMING EXCEPTION: IXxxService → XxxManager (Scrutor cannot match)
            services.AddScoped<ICategoryService, CategoryManager>();
            services.AddScoped<IProductService, ProductManager>();
            services.AddScoped<ILabelService, LabelManager>();
            services.AddScoped<ILabelTemplateService, LabelTemplateManager>();
            // NAMING EXCEPTION: "Import" vs "Importer" suffix
            services.AddScoped<ITrendyolCategoryImportService, TrendyolCategoryImporterService>();

            // Toplu İşlem servisleri (interface-registered, BulkOperationManager injects by interface)
            services.AddScoped<IExcelParser, ExcelParser>();
            services.AddScoped<ICsvParser, CsvParser>();
            services.AddScoped<IProductImportValidator, ProductImportValidator>();

            // Etiket & Fiş servisleri
            services.AddSingleton<ILabelGenerator, ZplLabelGenerator>();
            services.AddSingleton<IReceiptGenerator, EscPosReceiptGenerator>();

            // Trendyol servisleri — UseMock flag kaldirildi, WireMock.Net testlerde
            // HTTP mock saglar (bkz. Test/Entegrasyon.IntegrationTest/Fixtures/WireMockStubs/).
            services.AddScoped<TrendyolCategoryImporter>();
            services.AddScoped<TrendyolMappingValidator>();
            services.AddScoped<TrendyolSupplierAddressCache>();

            services.AddScoped<ITrendyolProductService, TrendyolProductService>();
            services.AddScoped<ITrendyolStockPriceService, TrendyolStockPriceService>();
            services.AddScoped<ITrendyolOrderService, TrendyolOrderService>();
            services.AddScoped<ITrendyolInvoiceService, TrendyolInvoiceService>();
            services.AddScoped<IMarketplaceSearchService, TrendyolMarketplaceSearchService>();
            services.AddScoped<IMarketplaceCategoryAttributeProvider, TrendyolCategoryAttributeProvider>();
            services.AddScoped<ITrendyolAttributeCatalog, TrendyolAttributeCatalog>();

            // Hepsiburada servisleri — UseMock flag kaldirildi, WireMock.Net testlerde
            // HTTP mock saglar (bkz. Test/Entegrasyon.IntegrationTest/Fixtures/WireMockStubs/).
            services.AddScoped<HepsiburadaCategoryImporter>();
            services.AddScoped<HepsiburadaMappingValidator>();

            services.AddScoped<IHepsiburadaProductService, HepsiburadaProductService>();
            services.AddScoped<IHepsiburadaListingService, HepsiburadaListingService>();
            services.AddScoped<IHepsiburadaOrderService, HepsiburadaOrderService>();
            services.AddScoped<IHepsiburadaClaimService, HepsiburadaClaimService>();
            services.AddScoped<IHepsiburadaQnAService, HepsiburadaQnAService>();

            // Amazon servisleri
            services.AddSingleton<IAmazonTokenManager, AmazonTokenManager>();   // MUST be Singleton (multi-tenant cache)
            services.AddScoped<AmazonMappingValidator>();

            // Amazon servisleri — UseMock flag kaldirildi (Faz 3.5).
            services.AddScoped<IAmazonCatalogService, AmazonCatalogService>();
            services.AddScoped<IAmazonProductTypeService, AmazonProductTypeService>();
            services.AddScoped<IAmazonListingService, AmazonListingService>();
            services.AddScoped<IAmazonProductService, AmazonProductService>();
            services.AddScoped<IAmazonOrderService, AmazonOrderService>();
            services.AddScoped<IAmazonFeedService, AmazonFeedService>();

            // N11 servisleri
            services.AddScoped<N11MappingValidator>();
            // IN11SoapClient + N11CategoryImporter her zaman kayıtlı —
            // REST servisleri delete/ship için SOAP'a fallback yapar.
            services.AddScoped<IN11SoapClient, N11SoapClient>();
            services.AddScoped<N11CategoryImporter>();
            services.AddScoped<N11RestCategoryImporter>();

            // N11 servisleri — UseMock flag kaldirildi (Faz 3.3). REST varsayilan,
            // SOAP flag ile legacy stack secilebilir. WireMock.Net integration testlerde
            // HTTP mock saglar.
            var useN11Soap = configuration.GetValue<bool>("N11:UseSoap", false);
            if (useN11Soap)
            {
                // SOAP (legacy) — tam SOAP stack
                services.AddScoped<IN11ProductMapper, N11ProductMapper>();
                services.AddScoped<IN11ProductService, N11ProductService>();
                services.AddScoped<IN11StockPriceService, N11StockPriceService>();
                services.AddScoped<IN11OrderService, N11OrderService>();
                services.AddScoped<IN11ClaimService, N11ClaimService>();
            }
            else
            {
                // REST (yeni varsayilan)
                services.AddScoped<IN11RestClient, N11RestClient>();
                services.AddScoped<IN11ProductService, N11RestProductService>();
                services.AddScoped<IN11StockPriceService, N11RestStockPriceService>();
                services.AddScoped<IN11OrderService, N11RestOrderService>();
                services.AddScoped<IN11ClaimService, N11ClaimService>();
            }

            // Pazarama servisleri — UseMock flag kaldirildi (Faz 3.4).
            services.AddScoped<PazaramaMappingValidator>();
            services.AddScoped<IPazaramaApiClient, PazaramaApiClient>();
            services.AddScoped<IPazaramaProductService, PazaramaProductService>();
            services.AddScoped<IPazaramaStockPriceService, PazaramaStockPriceService>();
            services.AddScoped<IPazaramaOrderService, PazaramaOrderService>();
            services.AddScoped<IPazaramaRefundService, PazaramaRefundService>();
            services.AddScoped<PazaramaCategoryImporter>();

            // PttAVM — UseMock flag kaldirildi (Faz 3.6).
            services.AddScoped<IPttavmCatalogApiClient, PttavmCatalogApiClient>();
            services.AddScoped<IPttavmProductService, PttavmProductService>();
            services.AddScoped<IPttavmStockPriceService, PttavmStockPriceService>();
            services.AddScoped<IPttavmShipmentApiClient, PttavmShipmentApiClient>();
            services.AddScoped<IPttavmOrderService, PttavmOrderService>();
            services.AddScoped<IPttavmShippingService, PttavmShippingService>();
            services.AddScoped<IPttavmInvoiceService, PttavmInvoiceService>();

            services.AddScoped<PttavmCategoryImporter>();
            services.AddScoped<PttavmMappingValidator>();

            // Çiçeksepeti servisleri
            services.AddScoped<ICiceksepetiCategoryImporter, CiceksepetiCategoryImporter>();
            services.AddScoped<CiceksepetiCategoryImporter>();
            services.AddScoped<CiceksepetiMappingValidator>();

            // Cicceksepeti servisleri — UseMock flag kaldirildi (Faz 3.7).
            services.AddScoped<ICiceksepetiApiClient, CiceksepetiApiClient>();
            services.AddScoped<ICiceksepetiCategoryService, CiceksepetiCategoryService>();
            services.AddScoped<ICiceksepetiProductMapper, CiceksepetiProductMapper>();
            services.AddScoped<ICiceksepetiProductService, CiceksepetiProductService>();
            services.AddScoped<ICiceksepetiStockPriceService, CiceksepetiStockPriceService>();
            services.AddScoped<ICiceksepetiOrderService, CiceksepetiOrderService>();
            services.AddScoped<ICiceksepetiInvoiceService, CiceksepetiInvoiceService>();
            services.AddScoped<ICiceksepetiReturnService, CiceksepetiReturnService>();
            services.AddScoped<ICiceksepetiQnAService, CiceksepetiQnAService>();

            // Temu servisleri — UseMock flag kaldirildi (Faz 3.8).
            services.AddScoped<ITemuApiClient, TemuApiClient>();
            services.AddScoped<TemuCategoryImporter>();

            // Kargo servisleri — UseMock flag'leri kaldirildi (Faz 7).
            // Yurtici Kargo
            services.AddScoped<IYurticiKargoClient, YurticiKargoClient>();
            services.AddScoped<IYurticiKargoService, YurticiKargoService>();

            // Surat Kargo
            services.AddScoped<ISuratKargoClient, SuratKargoClient>();
            services.AddScoped<ISuratKargoService, SuratKargoService>();

            // Aras Kargo
            services.AddScoped<ArasKargoClient>();
            services.AddScoped<IArasKargoService, ArasKargoService>();

            // Trendyol e-Fatura servisleri — UseMock flag kaldirildi (Faz 7).
            services.AddScoped<ITrendyolEFaturaApiClient, TrendyolEFaturaApiClient>();
            services.AddScoped<ITrendyolEFaturaService, TrendyolEFaturaService>();
            services.AddScoped<TrendyolEFaturaInvoiceBuilder>();

            // Shipping Tracking — multi-registration (ICargoTrackingAdapter), kept explicit
            services.AddScoped<ICargoTrackingAdapter, Entegrasyon.Business.Concrete.Shipping.ArasTrackingAdapter>();
            services.AddScoped<ICargoTrackingAdapter, Entegrasyon.Business.Concrete.Shipping.SuratTrackingAdapter>();
            services.AddScoped<ICargoTrackingAdapter, Entegrasyon.Business.Concrete.Shipping.YurticiTrackingAdapter>();
            // IShipmentTrackingManager → ShipmentTrackingManager: covered by scan (Concrete.Shipping namespace)

            // E-Fatura / E-Arsiv (genel amacli) servisleri — UseMock flag kaldirildi (Faz 7).
            // IEInvoiceManager → EInvoiceManager Scrutor scan ile otomatik kayitli.
            services.AddScoped<IEInvoiceIntegratorClient, ParasutInvoiceClient>();

            services.AddSingleton<ProductMapper>();
            services.AddSingleton<UserMapper>();
            services.AddSingleton<BrandMatchMapper>();
            services.AddSingleton<CategoryMapper>();
            services.AddSingleton<CategoryAttributeMapper>();
            services.AddSingleton<CustomerMapper>();

            // Master Catalog Import
            services.AddScoped<IMasterCatalogImportService, MasterCatalogImportService>();

            services.AddEventChannels();
            services.AddValidators();
            return services;
        }
        public static IServiceCollection AddClients(this IServiceCollection services)
        {
            // Marketplace HTTP clientlari — hepsi Polly resilience handler ile korunur.
            // Base URL'ler runtime'da MarketPlace entity'sinden okunarak override edilir
            // (client.BaseAddress = marketplace.BaseUrl). Burada sadece fallback URL'ler
            // ve policy register ediliyor.
            //
            // Resilience defaults (AddStandardResilienceHandler custom):
            //   - AttemptTimeout: 60s  (default 30s — Amazon Feed/N11 SOAP icin yetersizdi)
            //   - TotalRequestTimeout: 120s (retry'lari kapsiyor)
            //   - Retry: 2 attempt, exponential backoff (default 3 — POST idempotency riskini azalt)
            //   - CircuitBreaker: failure ratio 0.5, sampling 150s (Polly: SamplingDuration >= 2 * AttemptTimeout)
            static void ConfigureMarketplacePolicy(
                Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions o)
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                o.Retry.MaxRetryAttempts = 2;
                o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                o.Retry.UseJitter = true;
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(150);
                o.CircuitBreaker.FailureRatio = 0.5;
            }

            services.AddHttpClient(StringConstants.TrendyolApi, x =>
                    x.BaseAddress = new Uri("https://apigw.trendyol.com/integration/"))
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.TrendyolEFaturaApi, x =>
                    x.BaseAddress = new Uri("https://apigw.trendyol.com/integration/"))
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.HepsiburadaApi, x =>
                    x.BaseAddress = new Uri("https://mpop.hepsiburada.com/product/"))
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.N11RestApi, x =>
                    x.BaseAddress = new Uri("https://api.n11.com/"))
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.N11SoapApi, x =>
                    x.BaseAddress = new Uri("https://api.n11.com/"))
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.PazaramaApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.AmazonApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.PttavmCatalogApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.PttavmShipmentApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.CiceksepetiApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            services.AddHttpClient(StringConstants.TemuApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            // Kargo clientlari — Polly ile korunur, base URL runtime'da set edilir.
            services.AddHttpClient(StringConstants.SuratKargoApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);
            services.AddHttpClient(StringConstants.YurticiKargoApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);
            services.AddHttpClient(StringConstants.ArasKargoApi)
                .AddStandardResilienceHandler(ConfigureMarketplacePolicy);

            // Ollama — local LLM, uzun timeout, retry yok (local network, retry anlamsiz)
            services.AddHttpClient(StringConstants.OllamaApi, x =>
            {
                x.BaseAddress = new Uri("http://localhost:11434/");
                x.Timeout = TimeSpan.FromSeconds(120);
            });

            // Webhook — kisa timeout, retry yok (kullanici webhook'u yavassa beklemeyelim)
            services.AddHttpClient(StringConstants.WebhookApi, x =>
            {
                x.Timeout = TimeSpan.FromSeconds(10);
            });

            return services;
        }
        public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            // AdminPanelDbContext factory — master catalog import için
            var adminPanelConnectionString = configuration.GetConnectionString("AdminPanel")
                ?? "Host=192.168.1.78;Port=5432;Database=AdminPanelDb;Username=baturhan;Password=DiHRrP6dY8nC*M";
            services.AddDbContextFactory<AdminPanelDbContext>(options =>
                options.UseNpgsql(adminPanelConnectionString));

            // Fallback connection string — development/single-tenant modu icin
            var fallbackConnectionString = configuration.GetConnectionString("Main")
                ?? configuration.GetConnectionString("DefaultConnection");

            services.AddScoped<IDbContextFactory<IntegrationDbContext>>(sp =>
            {
                var tenantContext = sp.GetRequiredService<ITenantContext>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                Func<string> connectionStringProvider = () => tenantContext.IsInitialized
                    ? tenantContext.ConnectionString
                    : fallbackConnectionString
                        ?? throw new InvalidOperationException(
                            "Tenant context is not initialized and no fallback ConnectionString configured.");

                return new TenantDbContextFactory(connectionStringProvider, loggerFactory);
            });

            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            // NOTE: Notification dispatcher'ları (InProcessEventDispatcher, OutboxDispatcher) artık
            // AddNotification() içinde register ediliyor — AddBackgroundServices() Development'ta
            // bypass'lanır ama notification akışı her iki ortamda da çalışmalı (multi-device read sync,
            // bell badge updates vb. dev'de de gerekli). Marketplace polling servisleri burada kalır.

            services.AddHostedService<CategoryImportBackgroundService>();
            services.AddHostedService<TrendyolBatchStatusPollingService>();
            services.AddHostedService<TrendyolProductStatusSyncService>();
            services.AddHostedService<TrendyolOrderPollingService>();
            services.AddHostedService<N11OrderPollingService>();
            services.AddHostedService<N11TaskPollingService>();
            services.AddHostedService<DashboardRefreshService>();
            services.AddHostedService<HepsiburadaStatusPollingService>();
            services.AddHostedService<HepsiburadaStockPriceSyncService>();
            services.AddHostedService<HepsiburadaOrderPollingService>();
            services.AddHostedService<AmazonOrderPollingService>();
            services.AddHostedService<AmazonStockPriceSyncService>();
            services.AddHostedService<AmazonFeedStatusPollingService>();
            services.AddHostedService<AmazonListingStatusPollingService>();
            services.AddHostedService<PazaramaBatchStatusPollingService>();
            services.AddHostedService<PazaramaOrderPollingService>();
            services.AddHostedService<PazaramaRefundPollingService>();
            services.AddHostedService<CiceksepetiBatchStatusPollingService>();
            services.AddHostedService<CiceksepetiOrderPollingService>();
            services.AddHostedService<CiceksepetiStockPriceSyncService>();
            services.AddHostedService<PttavmProductTrackingPollingService>();
            services.AddHostedService<PttavmStockPriceSyncService>();
            services.AddHostedService<PttavmOrderPollingService>();
            services.AddHostedService<N11StockPriceSyncService>();
            services.AddHostedService<PazaramaStockPriceSyncService>();
            services.AddHostedService<TrendyolEFaturaStatusPollingService>();
            services.AddHostedService<ShipmentStatusUpdateService>();
            return services;
        }
        public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MinioOptions>(configuration.GetSection("Minio"));
            services.AddSingleton<IMinioFileStorage, MinioFileStorage>();
            services.AddSingleton<IImageProcessingService, ImageProcessingService>();
            return services;
        }

        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration? configuration = null)
        {
            services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);
            if (configuration is not null)
            {
                services.Configure<Entegrasyon.Business.Concrete.Auth.BatchTokenOptions>(
                    configuration.GetSection(Entegrasyon.Business.Concrete.Auth.BatchTokenOptions.SectionName));

                // Task 0.12: OutboxDispatcher polling/retry ayarları
                services.Configure<OutboxDispatchOptions>(
                    configuration.GetSection(OutboxDispatchOptions.SectionName));

                // Task 3.1: Notification feature flags
                services.Configure<Entegrasyon.Business.FeatureFlags.NotificationFeatureFlags>(
                    configuration.GetSection(Entegrasyon.Business.FeatureFlags.NotificationFeatureFlags.SectionName));

                // Task 5.1: Web Push VAPID config (admin dashboard)
                services.Configure<Entegrasyon.Business.Notifications.WebPush.WebPushOptions>(
                    configuration.GetSection(Entegrasyon.Business.Notifications.WebPush.WebPushOptions.SectionName));

                // Task 3.1: ICurrentUserContext — fallback null-object; MVC katmanı Program.cs'de override eder
                services.TryAddScoped<Entegrasyon.Business.Abstract.ICurrentUserContext,
                    Entegrasyon.Business.Concrete.Auth.NullCurrentUserContext>();
            }
            return services;
        }
        public static IServiceCollection AddCustomUserIdProvider(this IServiceCollection services)
        {
            return services.AddSingleton<IUserIdProvider, ApplicationUserIdProvider>();
        }

        public static IServiceCollection AddSignalRSettings(this IServiceCollection services)
            => services.AddCustomUserIdProvider();

        public static IServiceCollection AddNotification(this IServiceCollection services)
        {
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<ISmsSender, SmsSender>();

            // NotificationManager IEnumerable<INotificationSender> olarak inject eder
            services.AddScoped<INotificationSender, EmailSender>();
            services.AddScoped<INotificationSender, SmsSender>();

            // INotificationRecipientResolver → NotificationRecipientResolver: covered by scan (Notifications namespace)

            // Task 0.14: Ephemeral event channel — registered here (not in AddBackgroundServices) so it is
            // always available in both Development and Production. AddBackgroundServices is skipped in dev.
            services.AddSingleton<System.Threading.Channels.Channel<Entegrasyon.Business.Channels.Events.BaseEvent>>(
                _ => System.Threading.Channels.Channel.CreateBounded<Entegrasyon.Business.Channels.Events.BaseEvent>(
                    new System.Threading.Channels.BoundedChannelOptions(1000)
                    {
                        FullMode = System.Threading.Channels.BoundedChannelFullMode.DropOldest,
                        SingleReader = true,
                        SingleWriter = false
                    }));

            // Task 0.14: IEventBus — Scoped because InMemoryEventBus depends on ITenantContext (Scoped)
            services.AddScoped<IEventBus, InMemoryEventBus>();

            // Bug fix (smoke-test): Notification dispatcher'ları AddNotification içinde register edilmeli
            // çünkü AddBackgroundServices Development'ta bypass'lanıyor ama notification akışı (bell badge,
            // multi-device read sync, outbox→handler→bildirim) dev'de de çalışmalı.
            services.AddSingleton<InProcessEventDispatcher>();
            services.AddHostedService(sp => sp.GetRequiredService<InProcessEventDispatcher>());

            services.AddSingleton<OutboxDispatcher>();
            services.AddHostedService(sp => sp.GetRequiredService<OutboxDispatcher>());

            // Task 2.3: IDomainEventHandler<T> — Scrutor auto-registration for all notification handlers
            services.Scan(scan => scan
                .FromAssemblyOf<Entegrasyon.Business.Notifications.Handlers.ProductAddedNotificationHandler>()
                .AddClasses(c => c.AssignableTo(typeof(Entegrasyon.Business.Notifications.Handlers.IDomainEventHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Task 4.2: ISseConnectionRegistry — Singleton (process-scoped connection state)
            services.AddSingleton<Entegrasyon.Business.Notifications.Sse.ISseConnectionRegistry, Entegrasyon.Business.Notifications.Sse.SseConnectionRegistry>();

            // Task 4.3: SseNotificationSender — Scoped (matches other INotificationSender lifetimes)
            services.AddScoped<INotificationSender, Entegrasyon.Business.Notifications.Sse.SseNotificationSender>();

            // Task 5.2: AdminPushSubscriptionManager — Web Push subscription CRUD for admin users
            services.AddScoped<Entegrasyon.Business.Notifications.WebPush.IAdminPushSubscriptionManager,
                               Entegrasyon.Business.Notifications.WebPush.AdminPushSubscriptionManager>();

            // Task 5.3: PushServiceClient — singleton (thread-safe HTTP client wrapper)
            // The if-guard allows startup without VAPID keys (dev / placeholder mode).
            services.AddSingleton<Lib.Net.Http.WebPush.PushServiceClient>(sp =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Entegrasyon.Business.Notifications.WebPush.WebPushOptions>>().Value;
                var client = new Lib.Net.Http.WebPush.PushServiceClient();
                if (!string.IsNullOrEmpty(opts.VapidPublicKey) && !string.IsNullOrEmpty(opts.VapidPrivateKey))
                {
                    client.DefaultAuthentication = new Lib.Net.Http.WebPush.Authentication.VapidAuthentication(
                        opts.VapidPublicKey, opts.VapidPrivateKey)
                    {
                        Subject = opts.VapidSubject
                    };
                }
                return client;
            });

            // Task 5.3: AdminWebPushSender — sends Web Push to all admin subscribers
            services.AddScoped<INotificationSender, Entegrasyon.Business.Notifications.WebPush.AdminWebPushSender>();

            return services;
        }

        public static IServiceCollection AddStorefrontServices(this IServiceCollection services)
        {
            // Singleton — cannot be auto-scanned with Scoped lifetime
            services.AddSingleton<IStorefrontTenantResolver, StorefrontTenantResolver>();

            // NAMING EXCEPTION: IPaymentGatewayService → IyzicoPaymentService (provider-branded impl)
            services.AddScoped<IPaymentGatewayService, IyzicoPaymentService>();

            // All other IXxxManager/IXxxService storefront services are covered by the Scrutor scan
            // (Entegrasyon.Business.Concrete.Storefront namespace in AddApplicationDependencies)

            services.AddHostedService<AbandonedCartBackgroundService>();
            return services;
        }
    }
}
