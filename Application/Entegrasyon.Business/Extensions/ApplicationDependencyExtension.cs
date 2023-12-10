using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.MapperProfiles;
using Entegrasyon.Business.Utility;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;
using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Shared.User;
using System.Diagnostics;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.Utility.Auth;
using Entegrasyon.Business.Utility.MessageBroker;
using Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Shared.Security.Jwt;
using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.Business.Utility.Constants;
using static System.Net.WebRequestMethods;

namespace Entegrasyon.Business.Extensions;

public static class ApplicationDependencyExtension
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddScoped<ApplicationLifetimeManager>();
       //services.AddScoped<DbContext,IntegrationDbContext>();
       
        services.AddSharedSettings();
        services.AddUserServices<ApplicationUser,RootLogin,RootRole,RootClaim,IntegrationDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILogDal,EfLogDal>();
        services.AddScoped<IApplicationUserDal,EfApplicationUserDal>();
        services.AddScoped<IBranchOfficeDal,EfBranchOfficeDal>();
        services.AddScoped<ICategoryDal,EfCategoryDal>();
        services.AddScoped<ICategoryAttributeDal,EfCategoryAttributeDal>();
        services.AddScoped<IBrandDal,EfBrandDal>();
        services.AddScoped<ICargoCompanyDal,EfCargoCompanyDal>();
        services.AddScoped<ICustomerDal,EfApplicationCustomerDal>();
        services.AddScoped<IMainProductDal, EfMainProductDal>();
        services.AddScoped<IBranchOfficeStockDal, EfBranchOfficeStockDal>();
        services.AddScoped<IImageDal, EfImageDal>();
        services.AddScoped<IProductVariantDal,EfProductVariantDal>();
        services.AddScoped<IDiscountVoucherDal,EfDiscountVoucherDal>();
        services.AddScoped<IAttributeKeyValueDal, EfAttributeKeyValueDal>();
        services.AddScoped<ISaleDal, EfSaleDal>();
        services.AddScoped<ITempBarcodeDal, EfTempBarcodeDal>();
        services.AddScoped<IBrandMarketPlaceMatchDal,EfBrandMarketPlaceMatchDal>();
        services.AddScoped<ICategoryAttributeCategoryDal,EfCategoryAttributeCategoryDal>();

        services.AddScoped<BranchOfficeManager>();
        services.AddScoped<ApplicationRoleManager>();
        services.AddScoped<BrandManager>();
        services.AddScoped<CategoryManager>();
        services.AddScoped<ApplicationLogManager>();
        services.AddScoped<CategoryAttributeManager>();
        services.AddScoped<ApplicationUserManager>();
        services.AddScoped<AuthManager>();
        services.AddScoped<CargoCompaniesManager>();
        services.AddScoped<CustomerManager>();
        services.AddScoped<ProductManager>();
        services.AddScoped<OfficeStockManager>();
        services.AddScoped<ProductVariantManager>();
        services.AddScoped<ImageManager>();
        services.AddScoped<DiscountVoucherManager>();
        services.AddScoped<AttributeKeyValueManager>();
        services.AddScoped<SaleManager>();
        services.AddScoped<ApplicationLifetimeManager>();
        services.AddScoped<TrendyolCategoryImporterService>();
        services.AddScoped<TrendyolBrandImporterService>();
        services.AddScoped<TempBarcodeManager>();
        services.AddScoped<BrandMatchService>();
        services.AddScoped<CategoryAttributeCategoryManager>();
        services.AddScoped<CategoryAttributeValueManager>();

        services.AddScoped<ITokenHelper, ClaimHelper>();

        services.AddScoped<IMessageBrokerHelper, RabbitMQHelper>();

        services.AddValidators();
        services.AddAutoMapper(x =>
        {
            x.AddProfile<MapProfiles>();
        });
        return services;
    }
    public static IServiceCollection AddClients(this IServiceCollection services)
    {
        services.AddHttpClient(StringConstants.TrendyolApi, x =>
        {
            x.BaseAddress = new Uri("https://api.trendyol.com/sapigw/");
        });
        return services;
    }
    public static IServiceCollection AddCustomDbContext(this IServiceCollection services)
    {
        services.AddDbContext<IntegrationDbContext>(x =>
        {
            var cs = new NpgsqlConnectionStringBuilder()
            {
                Database = "IntegrationDb",
                Host = "db",
                Port = 5432,
                Password = "649471",
                Username = "Baturhan",
                IncludeErrorDetail = true,
                Pooling = true,
                Timeout = 120,
            };
            //"Server=db;Port=5432;Database=IntegrationDb;User Id=Baturhan;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;"
            x.UseNpgsql("Server=db;Port=5432;Database=IntegrationDb;User Id=Baturhan;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;");
            //x.UseInMemoryDatabase("InMemory");
            x.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            x.EnableSensitiveDataLogging();
            x.EnableDetailedErrors();
            x.LogTo(z => Debug.WriteLine(z));
        });
        return services;
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<TrendyolCategoryImportListener>();
        services.AddHostedService<TrendyolBrandImportListener>();
        services.AddHostedService<TempBarcodeBackgroundService>();
        return services;
    }
    public static IServiceCollection AddConfigurations(this IServiceCollection services,IConfiguration configuration = null)
    {
        services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);
        services.Configure<Shared.Security.Jwt.TokenOptions>(configuration.GetSection("JwtTokenOptions"));
        return services;
    }

}