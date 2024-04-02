using AutoMapper;
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
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.User;
using Shared.Entity;

namespace Entegrasyon.Business.MapperProfiles;

public class MapProfiles : Profile
{
    public MapProfiles()
    {
        CreateMap(typeof(Pageable<>),typeof(Pageable<>));
        CreateMap<AddUserDto,ApplicationUser>()
            .ForMember(dest => dest.DefaultBranchOfficeId,opt => opt.MapFrom(src => src.BranchOfficeId))
            .ReverseMap();
        CreateMap<AddBrandDto,Brand>()
            .ReverseMap();
        CreateMap<AddCargoCompanyDto,CargoCompany>()
            .ReverseMap();
        CreateMap<CustomerAddDto,RetailCustomer>()
            .ForMember(x => x.NationalIdentity,opt => opt.MapFrom(x => x.NationalIdentity));
        CreateMap<CustomerAddDto,CorporateCustomer>()
            .ForMember(x => x.TaxNumber,opt => opt.MapFrom(x => x.NationalIdentity));

        CreateMap<Customer,CustomerDetailDto>()
            .ForMember(dest => dest.SalesCount,opt => opt.MapFrom(src => src.Sales.Count()))
            .ReverseMap();
        CreateMap<RetailCustomer,CustomerDetailDto>()
            .ForMember(x => x.NameSurname,opt => opt.MapFrom(x => x.FullName))
            .ReverseMap();
        CreateMap<UserEditDto, ApplicationUser>();

        CreateMap<CorporateCustomer,CustomerDetailDto>()
            .ForMember(x => x.NameSurname,opt => opt.MapFrom(x => x.FullName))
            .ReverseMap();

        CreateMap<AddProductDto,Product>().ReverseMap();
        CreateMap<AddProductVariantDto,ProductVariant>().ReverseMap();
        CreateMap<AddBranchOfficeStockDto,BranchOfficeStock>().ReverseMap();
        CreateMap<AddCategoryDto,Category>()
            .ForMember(dest => dest.CategoryAttributes,opt => opt.Ignore())
            .ForMember(dest => dest.SuperCategoryId,opt => opt.Condition(prop => prop.SuperCategoryId != 0))
            .ReverseMap();
        CreateMap<ProductEditDetailDto,Product>();
        CreateMap<ProductVariantEditDetailDto,ProductVariant>();
        CreateMap<EditBranchOfficeStockDto,BranchOfficeStock>().ReverseMap();

        CreateMap<BranchOfficeAddDto, BranchOffice>();
        CreateMap<BranchOfficeEditDto, BranchOffice>();

        CreateMap<CategoryAttribute,AddCategoryAttributeDto>()
            .ReverseMap()
            .ForMember(d => d.Categories,opt => opt.Ignore());

        CreateMap<AddCategoryDtoStepOne,Category>()
                .ReverseMap();

        CreateMap<EditCategoryAttributeDto, CategoryAttributeCategory>()
            .ForMember(dest=>dest.CategoryAttributeId,opt=>opt.MapFrom(source=>source.Id))
            .ForPath(dest => dest.CategoryAttribute.Id,opt => opt.MapFrom(source => source.Id))
            .ForPath(d => d.CategoryAttribute.CategoryAttributeHumanized, opt => opt.MapFrom(s => s.CategoryAttributeHumanized))
            .ForPath(d => d.CategoryAttribute.CategoryAttributeValues, opt => opt.MapFrom(s => s.CategoryAttributeValues))
            .ForPath(d => d.CategoryAttribute.AllowCustom, opt => opt.MapFrom(s => s.AllowCustom))
            .ForPath(d => d.CategoryAttribute.CategoryAttributeKey, opt => opt.MapFrom(s => s.CategoryAttributeKey))
            ;
        CreateMap<CategoryEditDetailDto, Category>()
            .ReverseMap();
        CreateMap<EditCategoryDto,Category>()
            .ForMember(dest=>dest.CategoryAttributes,opt=>opt.Ignore())
            .ReverseMap();


        CreateMap<TrendyolCategoryAttribute,CategoryAttribute>()
            .ForMember(dest => dest.CategoryAttributeKey,opt => opt.MapFrom(s => s.Attribute.Name))
            .ForMember(dest => dest.CategoryAttributeHumanized,opt => opt.MapFrom(s => s.Attribute.Name));
        CreateMap<TrendyolBrand, Brand>().ForMember(dest=>dest.Id,opt=>opt.Ignore());
        CreateMap<TrendyolAttributeValue,CategoryAttributeValue>().ForMember(dest => dest.Id,opt => opt.Ignore());
        CreateMap<SaleItem,SaleItemDto>().ReverseMap()
            .ForMember(x => x.UsedDiscountVoucherCode,opt => opt.MapFrom(z => z.DiscountVoucherCode));
        CreateMap<MakeSaleDto,Sale>().ReverseMap();
        /*
          dest => dest.SomeDestinationProperty,
        opt => opt.MapFrom(src => src.SomeSourceProperty)*/
    }
}