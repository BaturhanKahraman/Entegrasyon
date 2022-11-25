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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Entegrasyon.Business.Extensions;

public static class ApplicationDependencyExtension
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddScoped<ApplicationLifetimeManager>();
       //services.AddScoped<DbContext,IntegrationDbContext>();
       
        services.AddSharedSettings();
        services.AddUserServices<ApplicationUser,RootLogin,RootRole,RootClaim,IntegrationDbContext>();

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

        services.AddScoped<TrendyolCategories>();
        services.AddScoped<TrendyolBrands>();
        services.AddScoped<TrendyolCargoCompanies>();

        services.AddValidators();
        services.AddAutoMapper(x =>
        {
            x.AddProfile<MapProfiles>();
        });
        return services;
    }
    public static IServiceCollection AddCustomDbContext(this IServiceCollection services)
    {
        services.AddDbContext<IntegrationDbContext>(x =>
        {
            //x.UseNpgsql("Server=db;Port=5432;Database=IntegrationDb111;User Id=Baturhan;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;",
            //    npg=>
            //    {
            //        npg.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            //        npg.EnableRetryOnFailure(5,TimeSpan.FromSeconds(2),null);
            //    });
            x.UseNpgsql("Server=localhost;Port=5432;Database=IntegrationDb1;User Id=postgres;Password=649471;Pooling=true;Maximum Pool Size=1024;ConnectionIdleLifetime=120;Include Error Detail=true;",
                npg =>
                {
                    npg.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null);
                });
            x.EnableSensitiveDataLogging();
            x.EnableDetailedErrors();
            x.LogTo(z => Debug.WriteLine(z));
        });
        return services;
    }
    
    public static IServiceCollection AddConfigurations(this IServiceCollection services,IConfiguration configuration = null)
    {
        services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);
        services.Configure<Shared.Security.Jwt.TokenOptions>(configuration.GetSection("JwtTokenOptions"));
        return services;
    }

}