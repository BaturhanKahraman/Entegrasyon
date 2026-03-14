using System.Diagnostics;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.Concrete.Trendyol;
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
using Npgsql;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Emails;
using Entegrasyon.Business.Notifications.SignalR;
using Microsoft.AspNetCore.SignalR;
using Entegrasyon.ApplicationBootstrap.FileStorage;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Labels;
using Entegrasyon.Business.BackgroundServices;

namespace Entegrasyon.ApplicationBootstrap
{
    public static class ApplicationBootstrapExtensions
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services, IConfiguration configuration)
        {
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
            services.AddScoped<CategoryAttributeManager>();
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
            services.AddScoped<ICategoryAttributeCategoryManager,CategoryAttributeCategoryManager>();
            services.AddScoped<ICategoryAttributeValueManager,CategoryAttributeValueManager>();
            services.AddScoped<INotificationManager, NotificationManager>();
            services.AddScoped<IProductSyncManager, ProductSyncManager>();
            services.AddScoped<IDiscountManager, DiscountManager>();
            services.AddScoped<IMarketPlaceManager, MarketPlaceManager>();
            services.AddScoped<IProductActivityLogger, ProductActivityLogger>();

            // Etiket & Fiş servisleri
            services.AddSingleton<ILabelGenerator, ZplLabelGenerator>();
            services.AddSingleton<IReceiptGenerator, EscPosReceiptGenerator>();
            services.AddScoped<ILabelService, LabelManager>();
            services.AddScoped<ILabelTemplateService, LabelTemplateManager>();

            // Trendyol servisleri
            services.AddScoped<TrendyolCategoryImporter>();
            services.AddScoped<TrendyolMappingValidator>();
            services.AddScoped<ITrendyolApiClient, TrendyolApiClient>();
            services.AddScoped<ITrendyolProductMapper, TrendyolProductMapper>();

            services.AddScoped<IOrderManager, OrderManager>();

            var useMock = configuration.GetValue<bool>("Trendyol:UseMock", true);
            services.AddScoped<TrendyolSupplierAddressCache>();

            if (useMock)
            {
                services.AddScoped<ITrendyolProductService, MockTrendyolProductService>();
                services.AddScoped<ITrendyolStockPriceService, MockTrendyolStockPriceService>();
                services.AddScoped<ITrendyolOrderService, MockTrendyolOrderService>();
                services.AddScoped<ITrendyolInvoiceService, MockTrendyolInvoiceService>();
            }
            else
            {
                services.AddScoped<ITrendyolProductService, TrendyolProductService>();
                services.AddScoped<ITrendyolStockPriceService, TrendyolStockPriceService>();
                services.AddScoped<ITrendyolOrderService, TrendyolOrderService>();
                services.AddScoped<ITrendyolInvoiceService, TrendyolInvoiceService>();
            }



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
            return services;
        }
        public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Main")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=IntegrationDb;Username=Baturhan;Password=649471;Pooling=true;Maximum Pool Size=30;ConnectionIdleLifetime=120;Include Error Detail=true;";

            services.AddDbContext<IntegrationDbContext>(x =>
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
            }, ServiceLifetime.Scoped);
            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<TrendyolCategoryImportBackgroundService>();
            services.AddHostedService<TrendyolProductPublishBackgroundService>();
            services.AddHostedService<TrendyolBatchStatusPollingService>();
            services.AddHostedService<TrendyolStockPriceSyncService>();
            services.AddHostedService<TrendyolProductStatusSyncService>();
            services.AddHostedService<TrendyolOrderPollingService>();
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
            // TODO: SignalRSender ve EmailSender henüz implemente edilmedi.
            // Implemente edildiklerinde buraya kayıt eklenecek:
            // services.AddSingleton<INotificationSender, SignalRSender>();
            // services.AddSingleton<INotificationSender, EmailSender>();
            return services;
        }
    }
}
