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
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Dtos.Product.Discount;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.Entity.Dtos.Shipping;

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
        services.AddScoped<IValidator<CreateBrandMarketPlaceMatchDto>, CreateBrandMarketPlaceMatchDtoValidator>();
        services.AddScoped<IValidator<SendNotificationRequest>, SendNotificationValidator>();
        services.AddScoped<IValidator<EditProductDto>, EditProductValidator>();
        services.AddScoped<IValidator<ApplyDiscountDto>, ApplyDiscountValidator>();
        services.AddScoped<IValidator<SaveLabelTemplateDto>, SaveLabelTemplateDtoValidator>();
        services.AddScoped<IValidator<SaveMarketplaceOverridesDto>, SaveMarketplaceOverridesDtoValidator>();
        services.AddScoped<IValidator<UpdateApplicationSettingDto>, UpdateApplicationSettingValidator>();
        services.AddScoped<IValidator<SaveCommissionRateDto>, SaveCommissionRateDtoValidator>();
        services.AddScoped<IValidator<TrackShipmentDto>, TrackShipmentValidator>();
        services.AddScoped<IValidator<OpenSessionDto>, OpenSessionValidator>();
        services.AddScoped<IValidator<CloseSessionDto>, CloseSessionValidator>();
        services.AddScoped<IValidator<POSTransactionDto>, POSTransactionValidator>();
        services.AddScoped<IValidator<AddCashMovementDto>, AddCashMovementValidator>();
        services.AddScoped<IValidator<CreateEInvoiceDto>, CreateEInvoiceValidator>();
        //services.AddScoped<IValidator<string>,PasswordValidator>();
        services.AddScoped<IFluentValidator,FluentValidator>();
        return services;
    }
}
