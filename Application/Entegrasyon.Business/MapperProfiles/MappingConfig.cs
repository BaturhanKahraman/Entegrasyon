using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Brands.Import;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.User;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Business.MapperProfiles;

public static class MappingConfig
{
    public static void AddBusinessMapping(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        // Generic pageable mapping
        config.NewConfig(typeof(Pageable<>),typeof(Pageable<>));

        // User mappings
        config.NewConfig<AddUserDto,ApplicationUser>()
              .Map(dest => dest.DefaultBranchOfficeId,src => src.BranchOfficeId);

        config.NewConfig<ApplicationUser,AddUserDto>();
        config.NewConfig<UserEditDto,ApplicationUser>();

        // Brand
        config.NewConfig<AddBrandDto,Brand>();
        config.NewConfig<Brand,AddBrandDto>();

        // Brand Marketplace Matching
        config.NewConfig<BrandMarketPlaceMatch, BrandMarketPlaceMatchDto>()
              .Map(dest => dest.ApplicationBrandName, src => src.ApplicationBrand.Name)
              .Map(dest => dest.MarketPlaceBrandName, src => ""); // TODO: Get from external API or local cache

        config.NewConfig<CreateBrandMarketPlaceMatchDto, BrandMarketPlaceMatch>();

        // Cargo Company
        config.NewConfig<AddCargoCompanyDto,CargoCompany>();
        config.NewConfig<CargoCompany,AddCargoCompanyDto>();

        // Customer Add
        config.NewConfig<CustomerAddDto,RetailCustomer>()
              .Map(dest => dest.NationalIdentity,src => src.NationalIdentity);

        config.NewConfig<CustomerAddDto,CorporateCustomer>()
              .Map(dest => dest.TaxNumber,src => src.NationalIdentity);

        // Customer Detail
        config.NewConfig<Customer,CustomerDetailDto>()
              .Map(dest => dest.SalesCount,src => src.Sales.Count());

        config.NewConfig<RetailCustomer,CustomerDetailDto>()
              .Map(dest => dest.NameSurname,src => src.FullName);

        config.NewConfig<CorporateCustomer,CustomerDetailDto>()
              .Map(dest => dest.NameSurname,src => src.FullName);

        // Product
        config.NewConfig<AddProductDto,Product>();
        config.NewConfig<Product,AddProductDto>();
        config.NewConfig<ProductEditDetailDto,Product>();

        // Product Variant
        config.NewConfig<AddProductVariantDto,ProductVariant>()
              .Map(dest => dest.VatRate, src => src.VatRate ?? 0m);
        config.NewConfig<ProductVariant,AddProductVariantDto>();
        config.NewConfig<ProductVariantEditDetailDto,ProductVariant>();

        // Branch Office Stock
        config.NewConfig<AddBranchOfficeStockDto,BranchOfficeStock>();
        config.NewConfig<BranchOfficeStock,AddBranchOfficeStockDto>();
        config.NewConfig<EditBranchOfficeStockDto,BranchOfficeStock>();
        config.NewConfig<BranchOfficeStock,EditBranchOfficeStockDto>();

        // Branch Office
        config.NewConfig<BranchOfficeAddDto,BranchOffice>();
        config.NewConfig<BranchOfficeEditDto,BranchOffice>();

        // Category
        config.NewConfig<AddCategoryDto,Category>()
              .Ignore(dest => dest.CategoryAttributes)
              .Map(dest => dest.SuperCategoryId,src => src.SuperCategoryId == 0 ? (int?)null : src.SuperCategoryId);

        config.NewConfig<Category,AddCategoryDto>();

        config.NewConfig<CategoryEditDetailDto,Category>();
        config.NewConfig<Category,CategoryEditDetailDto>();

        config.NewConfig<EditCategoryDto,Category>()
              .Ignore(dest => dest.CategoryAttributes);

        config.NewConfig<Category,EditCategoryDto>();

        config.NewConfig<AddCategoryDtoStepOne,Category>();
        config.NewConfig<Category,AddCategoryDtoStepOne>();

        // Category Attribute
        config.NewConfig<AddCategoryAttributeDto,CategoryAttribute>()
             .Ignore(dest => dest.Categories);
        config.NewConfig<AddCategoryAttributeDto,CategoryAttribute>();

        config.NewConfig<EditCategoryAttributeDto,CategoryAttributeCategory>()
              .Map(dest => dest.CategoryAttributeId,src => src.Id)
              .Map(dest => dest.CategoryAttribute.Id,src => src.Id)
              .Map(dest => dest.CategoryAttribute.CategoryAttributeHumanized,src => src.CategoryAttributeHumanized)
              .Map(dest => dest.CategoryAttribute.CategoryAttributeValues,src => src.CategoryAttributeValues)
              .Map(dest => dest.CategoryAttribute.AllowCustom,src => src.AllowCustom)
              .Map(dest => dest.CategoryAttribute.CategoryAttributeKey,src => src.CategoryAttributeKey);

        // Trendyol
        config.NewConfig<TrendyolCategoryAttribute,CategoryAttribute>()
              .Map(dest => dest.CategoryAttributeKey,src => src.Attribute.Name)
              .Map(dest => dest.CategoryAttributeHumanized,src => src.Attribute.Name);

        config.NewConfig<TrendyolBrand,Brand>()
              .Ignore(dest => dest.Id);

        config.NewConfig<TrendyolAttributeValue,CategoryAttributeValue>()
              .Ignore(dest => dest.Id);

        // Sales
        config.NewConfig<SaleItem,SaleItemDto>()
              .Map(dest => dest.DiscountVoucherCode,src => src.UsedDiscountVoucherCode);

        config.NewConfig<SaleItemDto,SaleItem>()
              .Map(dest => dest.UsedDiscountVoucherCode,src => src.DiscountVoucherCode);

        config.NewConfig<MakeSaleDto,Sale>();
        config.NewConfig<Sale,MakeSaleDto>();

        services.AddSingleton(config);
        services.AddMapster();
    }
}
