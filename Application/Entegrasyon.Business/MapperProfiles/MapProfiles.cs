using AutoMapper;
using Entegrasyon.Business.Utility;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Business.MapperProfiles;

public class MapProfiles:Profile
{
    public MapProfiles()
    {
        CreateMap(typeof(Pageable<>),typeof(Pageable<>));
        CreateMap<AddUserDto, ApplicationUser>()
            .ForMember(dest=>dest.DefaultBranchOfficeId,opt=>opt.MapFrom(src=>src.BranchOfficeId))
            .ReverseMap();
        CreateMap<AddBrandDto, Entity.Products.Brand>()
            .ReverseMap();
        CreateMap<AddCargoCompanyDto,CargoCompany>()
            .ReverseMap();
        CreateMap<AddCustomerDto, RetailCustomer>()
            .ForMember(x => x.NationalIdentity, opt => opt.MapFrom(x => x.NationalIdentityOrTaxNumber));
        CreateMap<AddCustomerDto, CorporateCustomer>()
            .ForMember(x => x.TaxNumber, opt => opt.MapFrom(x => x.NationalIdentityOrTaxNumber));

        CreateMap<Customer,CustomerDetailDto>()
            .ForMember(dest => dest.SalesCount,opt => opt.MapFrom(src => src.Sales.Count()))
            .ReverseMap();
        CreateMap<RetailCustomer, CustomerDetailDto>()
            .ForMember(x => x.NameSurname,opt => opt.MapFrom(x => x.FullName))
            .ForMember(x => x.NationalIdentityOrTaxNumber,opt => opt.MapFrom(x => x.NationalIdentity))
            .ReverseMap();

        CreateMap<CorporateCustomer, CustomerDetailDto>()
            .ForMember(x => x.NameSurname,opt => opt.MapFrom(x => x.FullName))
            .ForMember(x => x.NationalIdentityOrTaxNumber,opt => opt.MapFrom(x => x.TaxNumber))
            .ReverseMap();

        CreateMap<AddProductDto,Product>().ReverseMap();
        CreateMap<AddProductVariantDto,ProductVariant>().ReverseMap();
        CreateMap<AddBranchOfficeStockDto, BranchOfficeStock>().ReverseMap();
        CreateMap<AddCategoryDto, Entity.Categories.Category>()
            .ForMember(dest=>dest.CategoryAttributes,opt=>opt.Ignore())
            .ForMember(dest=>dest.SuperCategoryId,opt=>opt.Condition(prop=>prop.SuperCategoryId!=0))
            .ReverseMap();
        CreateMap<EditProductDto, Product>();
        CreateMap<EditProductVariantDto, ProductVariant>();
        CreateMap<EditBranchOfficeStockDto, BranchOfficeStock>().ReverseMap();
        CreateMap<AddCategoryAttributeDto, CategoryAttribute>().ReverseMap();

        CreateMap<TrendyolCategoryAttribute, CategoryAttribute>()
            .ForMember(dest => dest.CategoryAttributeKey, opt => opt.MapFrom(s => s.Attribute.Name))
            .ForMember(dest => dest.CategoriyAttributeHumanized, opt => opt.MapFrom(s => s.Attribute.Name));
        CreateMap<TrendyolAttributeValue, CategoryAttributeValue>().ForMember(dest=>dest.Id,opt=>opt.Ignore());

        /*
          dest => dest.SomeDestinationProperty,
        opt => opt.MapFrom(src => src.SomeSourceProperty)*/
    }
}