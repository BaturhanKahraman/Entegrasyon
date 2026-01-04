using System.Diagnostics;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.MapperProfiles;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;
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

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ILogDal, EfLogDal>();
            services.AddScoped<IApplicationUserDal, EfApplicationUserDal>();
            services.AddScoped<IBranchOfficeDal, EfBranchOfficeDal>();
            services.AddScoped<ICategoryDal, EfCategoryDal>();
            services.AddScoped<ICategoryAttributeDal, EfCategoryAttributeDal>();
            services.AddScoped<IBrandDal, EfBrandDal>();
            services.AddScoped<ICargoCompanyDal, EfCargoCompanyDal>();
            services.AddScoped<ICustomerDal, EfApplicationCustomerDal>();
            services.AddScoped<IMainProductDal, EfMainProductDal>();
            services.AddScoped<IBranchOfficeStockDal, EfBranchOfficeStockDal>();
            services.AddScoped<IImageDal, EfImageDal>();
            services.AddScoped<IProductVariantDal, EfProductVariantDal>();
            services.AddScoped<IDiscountVoucherDal, EfDiscountVoucherDal>();
            services.AddScoped<IAttributeKeyValueDal, EfAttributeKeyValueDal>();
            services.AddScoped<ISaleDal, EfSaleDal>();
            services.AddScoped<ITempBarcodeDal, EfTempBarcodeDal>();
            services.AddScoped<IBrandMarketPlaceMatchDal, EfBrandMarketPlaceMatchDal>();
            services.AddScoped<ICategoryAttributeCategoryDal, EfCategoryAttributeCategoryDal>();

            services.AddScoped<BranchOfficeManager>();
            services.AddScoped<IRoleService,RoleService>();
            services.AddScoped<BrandManager>();
            services.AddScoped<CategoryManager>();
            services.AddScoped<IApplicationLogManager,ApplicationLogManager>();
            services.AddScoped<CategoryAttributeManager>();
            services.AddScoped<ApplicationUserManager>();
            services.AddScoped<IAuthService,AuthService>();
            services.AddScoped<CargoCompaniesManager>();
            services.AddScoped<CustomerManager>();
            services.AddScoped<ProductManager>();
            services.AddScoped<OfficeStockManager>();
            services.AddScoped<ProductVariantManager>();
            //services.AddScoped<ImageManager>();
            services.AddScoped<DiscountVoucherManager>();
            services.AddScoped<AttributeKeyValueManager>();
            services.AddScoped<SaleManager>();
            services.AddScoped<ITrendyolCategoryImportService,TrendyolCategoryImporterService>();
            services.AddScoped<ITrendyolBrandImporterService,TrendyolBrandImporterService>();
            services.AddScoped<TempBarcodeManager>();
            services.AddScoped<BrandMatchService>();
            services.AddScoped<CategoryAttributeCategoryManager>();
            services.AddScoped<CategoryAttributeValueManager>();



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
            //services.AddHostedService<TrendyolCategoryImportListener>();
            //services.AddHostedService<TrendyolBrandImportListener>();
            
            services.AddHostedService<TempBarcodeBackgroundService>();
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
