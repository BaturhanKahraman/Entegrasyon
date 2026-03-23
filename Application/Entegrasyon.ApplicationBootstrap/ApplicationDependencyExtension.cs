using System.Diagnostics;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Business.Concrete.Trendyol.EFatura;
using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.MapperProfiles;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Notifications.Emails;
using Entegrasyon.Business.Notifications.SignalR;
using Microsoft.AspNetCore.SignalR;
using Entegrasyon.ApplicationBootstrap.FileStorage;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Labels;

namespace Entegrasyon.ApplicationBootstrap
{
    public static class ApplicationBootstrapExtensions
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITenantContext, DefaultTenantContext>();

            services.AddScoped<ApplicationLifetimeManager>();
            //services.AddScoped<DbContext,IntegrationDbContext>();

            services.AddSingleton<IRandomGenerator, RandomGenerator>();
            //services.AddUserServices<ApplicationUser, RootLogin, RootRole, RootClaim, IntegrationDbContext>();

            services.AddScoped<IBranchOfficeManager,BranchOfficeManager>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<ICategoryService, CategoryManager>();
            services.AddScoped<ICategoryAttributeManager, CategoryAttributeManager>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IApplicationLogManager, ApplicationLogManager>();
            services.AddScoped<IApplicationUserManager, ApplicationUserManager>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICargoCompaniesManager,CargoCompaniesManager>();
            services.AddScoped<ICustomerManager,CustomerManager>();
            services.AddScoped<IProductService,ProductManager>();
            services.AddScoped<IOfficeStockManager,OfficeStockManager>();
            services.AddScoped<IProductVariantManager,ProductVariantManager>();
            services.AddScoped<IImageManager, ImageManager>();
            services.AddScoped<IDiscountVoucherManager,DiscountVoucherManager>();
            services.AddScoped<IAttributeKeyValueManager,AttributeKeyValueManager>();
            services.AddScoped<ISaleManager,SaleManager>();
            services.AddScoped<ITrendyolCategoryImportService, TrendyolCategoryImporterService>();
            services.AddScoped<ITrendyolBrandImporterService, TrendyolBrandImporterService>();
            services.AddScoped<IBarcodeService, BarcodeService>();
            services.AddScoped<IBrandMatchService,BrandMatchService>();
            services.AddScoped<ICategoryMatchService,CategoryMatchService>();
            services.AddScoped<ICategoryAttributeCategoryManager,CategoryAttributeCategoryManager>();
            services.AddScoped<ICategoryAttributeValueManager,CategoryAttributeValueManager>();
            services.AddScoped<INotificationManager, NotificationManager>();
            services.AddScoped<IProductSyncManager, ProductSyncManager>();
            services.AddScoped<IDiscountManager, DiscountManager>();
            services.AddScoped<IMarketPlaceManager, MarketPlaceManager>();
            services.AddScoped<IProductActivityLogger, ProductActivityLogger>();
            services.AddScoped<IMarketplaceOverrideManager, MarketplaceOverrideManager>();
            services.AddScoped<INotificationSettingManager, NotificationSettingManager>();
            services.AddScoped<IReportManager, ReportManager>();
            services.AddScoped<IApplicationSettingManager, ApplicationSettingManager>();

            // Etiket & Fiş servisleri
            services.AddSingleton<ILabelGenerator, ZplLabelGenerator>();
            services.AddSingleton<IReceiptGenerator, EscPosReceiptGenerator>();
            services.AddScoped<ILabelService, LabelManager>();
            services.AddScoped<ILabelTemplateService, LabelTemplateManager>();

            // Trendyol servisleri
            services.AddScoped<TrendyolCategoryImporter>();
            services.AddScoped<N11CategoryImporter>();
            services.AddScoped<TrendyolMappingValidator>();
            services.AddScoped<ITrendyolApiClient, TrendyolApiClient>();
            services.AddScoped<ITrendyolProductMapper, TrendyolProductMapper>();

            services.AddScoped<IOrderManager, OrderManager>();
            services.AddScoped<IDashboardManager, DashboardManager>();

            var useMock = configuration.GetValue<bool>("Trendyol:UseMock", true);
            services.AddScoped<TrendyolSupplierAddressCache>();

            if (useMock)
            {
                services.AddScoped<ITrendyolProductService, MockTrendyolProductService>();
                services.AddScoped<ITrendyolStockPriceService, MockTrendyolStockPriceService>();
                services.AddScoped<ITrendyolOrderService, MockTrendyolOrderService>();
                services.AddScoped<ITrendyolInvoiceService, MockTrendyolInvoiceService>();
                services.AddScoped<IMarketplaceSearchService, MockMarketplaceSearchService>();
            }
            else
            {
                services.AddScoped<ITrendyolProductService, TrendyolProductService>();
                services.AddScoped<ITrendyolStockPriceService, TrendyolStockPriceService>();
                services.AddScoped<ITrendyolOrderService, TrendyolOrderService>();
                services.AddScoped<ITrendyolInvoiceService, TrendyolInvoiceService>();
                services.AddScoped<IMarketplaceSearchService, TrendyolMarketplaceSearchService>();
            }

            // Hepsiburada servisleri
            services.AddScoped<IHepsiburadaApiClient, HepsiburadaApiClient>();
            services.AddScoped<HepsiburadaCategoryImporter>();
            services.AddScoped<HepsiburadaMappingValidator>();
            services.AddScoped<IHepsiburadaProductMapper, HepsiburadaProductMapper>();

            var useHbMock = configuration.GetValue<bool>("Hepsiburada:UseMock", true);
            if (useHbMock)
            {
                services.AddScoped<IHepsiburadaProductService, MockHepsiburadaProductService>();
                services.AddScoped<IHepsiburadaListingService, MockHepsiburadaListingService>();
                services.AddScoped<IHepsiburadaOrderService, MockHepsiburadaOrderService>();
            }
            else
            {
                services.AddScoped<IHepsiburadaProductService, HepsiburadaProductService>();
                services.AddScoped<IHepsiburadaListingService, HepsiburadaListingService>();
                services.AddScoped<IHepsiburadaOrderService, HepsiburadaOrderService>();
            }

            // Hepsiburada Q&A + Claim (mock/real ayrımı yok — her zaman real, API yoksa hata döner)
            services.AddScoped<IHepsiburadaQnAService, HepsiburadaQnAService>();
            services.AddScoped<IHepsiburadaClaimService, HepsiburadaClaimService>();

            // Amazon servisleri
            services.AddSingleton<IAmazonTokenManager, AmazonTokenManager>();
            services.AddScoped<IAmazonApiClient, AmazonApiClient>();
            services.AddScoped<AmazonMappingValidator>();
            services.AddScoped<IAmazonProductMapper, AmazonProductMapper>();

            var useAmazonMock = configuration.GetValue<bool>("Amazon:UseMock", true);
            if (useAmazonMock)
            {
                services.AddScoped<IAmazonCatalogService, MockAmazonCatalogService>();
                services.AddScoped<IAmazonProductTypeService, MockAmazonProductTypeService>();
                services.AddScoped<IAmazonListingService, MockAmazonListingService>();
                services.AddScoped<IAmazonProductService, MockAmazonProductService>();
                services.AddScoped<IAmazonOrderService, MockAmazonOrderService>();
                services.AddScoped<IAmazonFeedService, MockAmazonFeedService>();
            }
            else
            {
                services.AddScoped<IAmazonCatalogService, AmazonCatalogService>();
                services.AddScoped<IAmazonProductTypeService, AmazonProductTypeService>();
                services.AddScoped<IAmazonListingService, AmazonListingService>();
                services.AddScoped<IAmazonProductService, AmazonProductService>();
                services.AddScoped<IAmazonOrderService, AmazonOrderService>();
                services.AddScoped<IAmazonFeedService, AmazonFeedService>();
            }

            // N11 servisleri
            services.AddScoped<IN11SoapClient, N11SoapClient>();
            services.AddScoped<N11MappingValidator>();
            services.AddScoped<IN11ProductMapper, N11ProductMapper>();

            var useN11Mock = configuration.GetValue<bool>("N11:UseMock", true);
            if (useN11Mock)
            {
                services.AddScoped<IN11ProductService, MockN11ProductService>();
                services.AddScoped<IN11StockPriceService, MockN11StockPriceService>();
                services.AddScoped<IN11OrderService, MockN11OrderService>();
                services.AddScoped<IN11ClaimService, MockN11ClaimService>();
            }
            else
            {
                services.AddScoped<IN11ProductService, N11ProductService>();
                services.AddScoped<IN11StockPriceService, N11StockPriceService>();
                services.AddScoped<IN11OrderService, N11OrderService>();
                services.AddScoped<IN11ClaimService, N11ClaimService>();
            }

            // Pazarama servisleri
            services.AddScoped<PazaramaMappingValidator>();
            services.AddScoped<IPazaramaProductMapper, PazaramaProductMapper>();

            var usePazaramaMock = configuration.GetValue<bool>("Pazarama:UseMock", true);
            if (usePazaramaMock)
            {
                services.AddScoped<IPazaramaApiClient, MockPazaramaApiClient>();
                services.AddScoped<IPazaramaProductService, MockPazaramaProductService>();
                services.AddScoped<IPazaramaStockPriceService, MockPazaramaStockPriceService>();
                services.AddScoped<IPazaramaOrderService, MockPazaramaOrderService>();
                services.AddScoped<IPazaramaRefundService, MockPazaramaRefundService>();
            }
            else
            {
                services.AddScoped<IPazaramaApiClient, PazaramaApiClient>();
                services.AddScoped<IPazaramaProductService, PazaramaProductService>();
                services.AddScoped<IPazaramaStockPriceService, PazaramaStockPriceService>();
                services.AddScoped<IPazaramaOrderService, PazaramaOrderService>();
                services.AddScoped<IPazaramaRefundService, PazaramaRefundService>();
            }
            services.AddScoped<PazaramaCategoryImporter>();
            services.AddScoped<IPazaramaBrandService, PazaramaBrandService>();

            // PttAVM
            var usePttavmMock = configuration.GetValue<bool>("Pttavm:UseMock", true);
            if (usePttavmMock)
            {
                services.AddScoped<IPttavmCatalogApiClient, MockPttavmCatalogApiClient>();
                services.AddScoped<IPttavmProductService, MockPttavmProductService>();
                services.AddScoped<IPttavmStockPriceService, MockPttavmStockPriceService>();
                services.AddScoped<IPttavmShipmentApiClient, MockPttavmShipmentApiClient>();
                services.AddScoped<IPttavmOrderService, MockPttavmOrderService>();
                services.AddScoped<IPttavmShippingService, MockPttavmShippingService>();
                services.AddScoped<IPttavmInvoiceService, MockPttavmInvoiceService>();
            }
            else
            {
                services.AddScoped<IPttavmCatalogApiClient, PttavmCatalogApiClient>();
                services.AddScoped<IPttavmProductService, PttavmProductService>();
                services.AddScoped<IPttavmStockPriceService, PttavmStockPriceService>();
                services.AddScoped<IPttavmShipmentApiClient, PttavmShipmentApiClient>();
                services.AddScoped<IPttavmOrderService, PttavmOrderService>();
                services.AddScoped<IPttavmShippingService, PttavmShippingService>();
                services.AddScoped<IPttavmInvoiceService, PttavmInvoiceService>();
            }

            services.AddScoped<PttavmCategoryImporter>();
            services.AddScoped<IPttavmProductMapper, PttavmProductMapper>();
            services.AddScoped<PttavmMappingValidator>();

            // Çiçeksepeti servisleri
            var useCiceksepetiMock = configuration.GetValue<bool>("Ciceksepeti:UseMock", true);
            if (useCiceksepetiMock)
            {
                services.AddScoped<ICiceksepetiApiClient, MockCiceksepetiApiClient>();
            }
            else
            {
                services.AddScoped<ICiceksepetiApiClient, CiceksepetiApiClient>();
            }

            services.AddScoped<ICiceksepetiCategoryService, CiceksepetiCategoryService>();
            services.AddScoped<ICiceksepetiCategoryImporter, CiceksepetiCategoryImporter>();
            services.AddScoped<CiceksepetiCategoryImporter>();
            services.AddScoped<CiceksepetiMappingValidator>();
            services.AddScoped<ICiceksepetiProductMapper, CiceksepetiProductMapper>();
            services.AddScoped<ICiceksepetiProductService, CiceksepetiProductService>();
            services.AddScoped<ICiceksepetiStockPriceService, CiceksepetiStockPriceService>();
            services.AddScoped<ICiceksepetiOrderService, CiceksepetiOrderService>();
            services.AddScoped<ICiceksepetiInvoiceService, CiceksepetiInvoiceService>();
            services.AddScoped<ICiceksepetiReturnService, CiceksepetiReturnService>();
            services.AddScoped<ICiceksepetiQnAService, CiceksepetiQnAService>();

            // Trendyol e-Fatura servisleri
            var useEFaturaMock = configuration.GetValue<bool>("TrendyolEFatura:UseMock", true);
            if (useEFaturaMock)
            {
                services.AddScoped<ITrendyolEFaturaApiClient, MockTrendyolEFaturaApiClient>();
                services.AddScoped<ITrendyolEFaturaService, MockTrendyolEFaturaService>();
            }
            else
            {
                services.AddScoped<ITrendyolEFaturaApiClient, TrendyolEFaturaApiClient>();
                services.AddScoped<ITrendyolEFaturaService, TrendyolEFaturaService>();
            }
            services.AddScoped<TrendyolEFaturaInvoiceBuilder>();

            services.AddEventChannels();
            services.AddValidators();
            services.AddBusinessMapping();
            return services;
        }
        public static IServiceCollection AddClients(this IServiceCollection services)
        {

            services.AddHttpClient(StringConstants.TrendyolApi, x =>
            {
                x.BaseAddress = new Uri("https://apigw.trendyol.com/integration/");
            });
            services.AddHttpClient(StringConstants.HepsiburadaApi, x =>
            {
                x.BaseAddress = new Uri("https://mpop.hepsiburada.com/product/");
            });
            return services;
        }
        public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Main")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionString 'Main' is not configured. Check appsettings.json or environment variables.");

            services.AddDbContextFactory<IntegrationDbContext>(x =>
            {
                x.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
                x.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
#if DEBUG
                x.EnableSensitiveDataLogging();
                x.EnableDetailedErrors();
                x.LogTo(z => Debug.WriteLine(z));
#endif
            });
            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<CategoryImportBackgroundService>();
            services.AddHostedService<TrendyolProductPublishBackgroundService>();
            services.AddHostedService<TrendyolBatchStatusPollingService>();
            services.AddHostedService<TrendyolStockPriceSyncService>();
            services.AddHostedService<TrendyolProductStatusSyncService>();
            services.AddHostedService<TrendyolOrderPollingService>();
            services.AddHostedService<N11OrderPollingService>();
            services.AddHostedService<DashboardRefreshService>();
            services.AddHostedService<HepsiburadaStatusPollingService>();
            services.AddHostedService<HepsiburadaStockPriceSyncService>();
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
            services.AddHostedService<TrendyolEFaturaStatusPollingService>();
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
            services.AddScoped<ISignalRNotificationSender, SignalRSender>();
            services.AddScoped<IEmailSender, EmailSender>();
            return services;
        }
    }
}
