using System.Diagnostics;
using Entegrasyon.Business.BackgroundServices;
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
using Shared.Extensions;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Emails;
using Entegrasyon.Business.Notifications.SignalR;
using Microsoft.AspNetCore.SignalR;
using Shared.FileStorage;
using Shared.FileStorage.ImageProcessing;
using Shared.FileStorage.Options;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.BackgroundServices;

namespace Entegrasyon.ApplicationBootstrap
{
    public static class ApplicationBootstrapExtensions
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
        {
            services.AddScoped<ApplicationLifetimeManager>();
            //services.AddScoped<DbContext,IntegrationDbContext>();

            services.AddSharedSettings();
            //services.AddUserServices<ApplicationUser, RootLogin, RootRole, RootClaim, IntegrationDbContext>();

            services.AddScoped<IBranchOfficeManager,BranchOfficeManager>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<ICategoryService, CategoryManager>();
            services.AddScoped<ICategoryAttributeManager, CategoryAttributeManager>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IApplicationLogManager, ApplicationLogManager>();
            services.AddScoped<CategoryAttributeManager>();
            services.AddScoped<ApplicationUserManager>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<CargoCompaniesManager>();
            services.AddScoped<CustomerManager>();
            services.AddScoped<IProductService,ProductManager>();
            services.AddScoped<IOfficeStockManager,OfficeStockManager>();
            services.AddScoped<IProductVariantManager,ProductVariantManager>();
            services.AddScoped<IImageManager, ImageManager>();
            services.AddScoped<DiscountVoucherManager>();
            services.AddScoped<AttributeKeyValueManager>();
            services.AddScoped<ISaleManager,SaleManager>();
            services.AddScoped<ITrendyolCategoryImportService, TrendyolCategoryImporterService>();
            services.AddScoped<ITrendyolBrandImporterService, TrendyolBrandImporterService>();
            services.AddScoped<TempBarcodeManager>();
            services.AddScoped<BrandMatchService>();
            services.AddScoped<CategoryAttributeCategoryManager>();
            services.AddScoped<CategoryAttributeValueManager>();
            services.AddScoped<INotificationManager, NotificationManager>();

            // Yeni Import Servisleri
            services.AddScoped<TrendyolCategoryImporter>();



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
                ?? "Host=localhost;Port=5432;Database=IntegrationDb;Username=Baturhan;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;";

            services.AddDbContext<IntegrationDbContext>(x =>
            {
                x.UseNpgsql(connectionString);
                x.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                x.EnableSensitiveDataLogging();
                x.EnableDetailedErrors();
                x.LogTo(z => Debug.WriteLine(z));
            });
            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<TempBarcodeBackgroundService>();
            services.AddHostedService<TrendyolCategoryImportBackgroundService>();
            services.AddHostedService<TrendyolProductPublishBackgroundService>();
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
            services.AddSingleton<INotificationSender, SignalRSender>();
            services.AddSingleton<INotificationSender, EmailSender>();
            return services;
        }
    }
}
