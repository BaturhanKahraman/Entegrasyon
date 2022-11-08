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


namespace Entegrasyon.Business.Extensions;

public static class ApplicationDependencyExtension
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {

       services.AddScoped<DbContext,IntegrationDbContext>();

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
    
}