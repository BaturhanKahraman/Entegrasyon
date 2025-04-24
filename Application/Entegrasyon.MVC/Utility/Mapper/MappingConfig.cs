using Mapster;
using Entegrasyon.Entity.Brands.Import;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.User;
using Entegrasyon.Entity;
using Entegrasyon.MVC.ViewModels.Brand;
using Entegrasyon.MVC.ViewModels.Category;
using Entegrasyon.MVC.ViewModels.CategoryAttribute;
using Entegrasyon.MVC.ViewModels.Customer;
using Shared.Entity;
using Entegrasyon.MVC.ViewModels.Products;


public static class MappingConfig
{
    public static void AddModelViewMapping(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        // Generic pageable mapping
        config.NewConfig(typeof(Pageable<>),typeof(Pageable<>))
    .ShallowCopyForSameType(true);

        // Categories
        config.NewConfig<CategoryDetailDto,CategoryDetailListViewModel>();
        config.NewConfig<Category,CategoryUpsertViewModel>();
        config.NewConfig<CategoryUpsertViewModel,EditCategoryDto>();
        config.NewConfig<CategoryUpsertViewModel,AddCategoryDto>();

        // CategoryAttributes
        config.NewConfig<CategoryAttributeValueViewModel,CategoryAttributeValue>();

        // Brand
        config.NewConfig<BrandListDetailDto,BrandListDetailViewModel>();

        // Customer
        config.NewConfig<CustomerDetailDto,CustomerDetailListViewModel>();
        config.NewConfig<CustomerDetailDto,CustomerDetailViewModel>();
        config.NewConfig<RetailCustomer,CustomerEditViewModel>();
        config.NewConfig<CorporateCustomer,CustomerEditViewModel>();

        // Önceki tanımlar
        config.NewConfig<AddUserDto,ApplicationUser>()
              .Map(dest => dest.DefaultBranchOfficeId,src => src.BranchOfficeId);
        config.NewConfig<ApplicationUser,AddUserDto>();
        config.NewConfig<UserEditDto,ApplicationUser>();

        config.NewConfig<AddBrandDto,Brand>();
        config.NewConfig<Brand,AddBrandDto>();

        config.NewConfig<AddCargoCompanyDto,CargoCompany>();
        config.NewConfig<CargoCompany,AddCargoCompanyDto>();

        config.NewConfig<CustomerAddDto,RetailCustomer>()
              .Map(dest => dest.NationalIdentity,src => src.NationalIdentity);
        config.NewConfig<CustomerAddDto,CorporateCustomer>()
              .Map(dest => dest.TaxNumber,src => src.NationalIdentity);

        config.NewConfig<Customer,CustomerDetailDto>()
              .Map(dest => dest.SalesCount,src => src.Sales.Count());
        config.NewConfig<RetailCustomer,CustomerDetailDto>()
              .Map(dest => dest.NameSurname,src => src.FullName);
        config.NewConfig<CorporateCustomer,CustomerDetailDto>()
              .Map(dest => dest.NameSurname,src => src.FullName);

        config.NewConfig<AddProductDto,Product>();
        config.NewConfig<Product,AddProductDto>();
        config.NewConfig<ProductEditDetailDto,Product>();
        config.NewConfig<ProductDetailDto,ProductDetailListViewModel>()
            .MapWith(src => new ProductDetailListViewModel(
                src.Id,
                src.Title,
                src.Description,
                src.StockCode,
                src.BrandName,
                src.CategoryName,
                src.TotalQuantity,
                src.TotalSoldQuantity,
                src.ProductVariantsDetails.Count()
            ));

        config.NewConfig<AddProductVariantDto,ProductVariant>();
        config.NewConfig<ProductVariant,AddProductVariantDto>();
        config.NewConfig<ProductVariantEditDetailDto,ProductVariant>();

        config.NewConfig<AddBranchOfficeStockDto,BranchOfficeStock>();
        config.NewConfig<BranchOfficeStock,AddBranchOfficeStockDto>();
        config.NewConfig<EditBranchOfficeStockDto,BranchOfficeStock>();
        config.NewConfig<BranchOfficeStock,EditBranchOfficeStockDto>();

        config.NewConfig<BranchOfficeAddDto,BranchOffice>();
        config.NewConfig<BranchOfficeEditDto,BranchOffice>();

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

        config.NewConfig<CategoryAttribute,AddCategoryAttributeDto>();
        config.NewConfig<AddCategoryAttributeDto,CategoryAttribute>()
              .Ignore(dest => dest.Categories);

        config.NewConfig<EditCategoryAttributeDto,CategoryAttributeCategory>()
              .Map(dest => dest.CategoryAttributeId,src => src.Id)
              .Map(dest => dest.CategoryAttribute.Id,src => src.Id)
              .Map(dest => dest.CategoryAttribute.CategoryAttributeHumanized,src => src.CategoryAttributeHumanized)
              .Map(dest => dest.CategoryAttribute.CategoryAttributeValues,src => src.CategoryAttributeValues)
              .Map(dest => dest.CategoryAttribute.AllowCustom,src => src.AllowCustom)
              .Map(dest => dest.CategoryAttribute.CategoryAttributeKey,src => src.CategoryAttributeKey);

        config.NewConfig<TrendyolCategoryAttribute,CategoryAttribute>()
              .Map(dest => dest.CategoryAttributeKey,src => src.Attribute.Name)
              .Map(dest => dest.CategoryAttributeHumanized,src => src.Attribute.Name);
        config.NewConfig<TrendyolBrand,Brand>()
              .Ignore(dest => dest.Id);
        config.NewConfig<TrendyolAttributeValue,CategoryAttributeValue>()
              .Ignore(dest => dest.Id);

        config.NewConfig<SaleItem,SaleItemDto>()
              .Map(dest => dest.DiscountVoucherCode,src => src.UsedDiscountVoucherCode);
        config.NewConfig<SaleItemDto,SaleItem>()
              .Map(dest => dest.UsedDiscountVoucherCode,src => src.DiscountVoucherCode);
        config.NewConfig<MakeSaleDto,Sale>();
        config.NewConfig<Sale,MakeSaleDto>();

        services.AddMapster();
    }
}