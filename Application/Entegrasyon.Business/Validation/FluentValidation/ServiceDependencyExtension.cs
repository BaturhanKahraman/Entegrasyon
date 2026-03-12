using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.DiscountVouchers;
using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Validation.FluentValidation;

public static class ServiceDependencyExtension
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<BranchOffice>,BranchValidator>();
        services.AddScoped<IValidator<AddRoleDto>,AddRoleDtoValidator>();
        services.AddScoped<IValidator<EditRoleDto>,EditRoleDtoValidator>();
        services.AddScoped<IValidator<AddUserDto>,AddUserDtoValidator>();
        services.AddScoped<IValidator<UserEditDto>,UserEditDtoValidator>();
        services.AddScoped<IValidator<Brand>,BrandValidator>();
        services.AddScoped<IValidator<CustomerAddDto>,AddCustomerDtoValidator>();
        services.AddScoped<IValidator<UpdateCustomerDto>,UpdateCustomerDtoValidator>();
        services.AddScoped<IValidator<AddProductDto>,AddProductValidator>();
        services.AddScoped<IValidator<AddProductVariantDto>,AddProductVariantValidator>();
        services.AddScoped<IValidator<AddBranchOfficeStockDto>,AddBranchOfficeStockValidator>();
        services.AddScoped<IValidator<CreateDiscountVoucherDto>,CreateDiscountVoucherDtoValidator>();
        services.AddScoped<IValidator<MakeSaleDto>,MakeSaleValidator>();
        services.AddScoped<IValidator<EditCategoryDto>,EditCategoryDtoValidator>();
        services.AddScoped<IValidator<AddCategoryDtoStepOne>,AddCategoryDtoStepOneValidator>();
        services.AddScoped<IValidator<AddCategoryAttributeDto>,AddCategoryAttributeDtoValidator>();
        services.AddScoped<IValidator<EditCategoryAttributeDto>,EditCategoryAttributeDtoValidator>();
        services.AddScoped<IValidator<AddBrandDto>, AddBrandDtoValidator>();
        services.AddScoped<IValidator<Notification>, SendNotificationValidator>();
        //services.AddScoped<IValidator<string>,PasswordValidator>();
        services.AddScoped<IFluentValidator,FluentValidator>();
        return services;
    }
}