using AutoMapper;
using Entegrasyon.Business.Utility;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Products;
using Shared.Entity;
using Brand = Entegrasyon.Entity.Products.Brand;

namespace Entegrasyon.Business.MapperProfiles;

public class MapProfiles:Profile
{
    public MapProfiles()
    {
        CreateMap(typeof(Pageable<>),typeof(Pageable<>));
        CreateMap<AddUserDto, ApplicationUser>()
            .ForMember(dest=>dest.DefaultBranchOfficeId,opt=>opt.MapFrom(src=>src.BranchOfficeId))
            .ReverseMap();
        CreateMap<AddBrandDto, Brand>()
            .ReverseMap();
        CreateMap<AddCargoCompanyDto,CargoCompany>()
            .ReverseMap();
        CreateMap<AddCustomerDto, RetailCustomer>()
            .ForMember(x => x.NationalIdentity, opt => opt.MapFrom(x => x.NationalIdentityOrTaxNumber));

        CreateMap<AddCustomerDto, CorporateCustomer>()
            .ForMember(x => x.TaxNumber, opt => opt.MapFrom(x => x.NationalIdentityOrTaxNumber));

        CreateMap<Customer,CustomerDetailDto>()
            .ForMember(dest => dest.SalesCount,opt => opt.MapFrom(src => src.Sales.Count))
            .ReverseMap();
        CreateMap<AddProductDto,MainProduct>().ReverseMap();
        CreateMap<AddProductVariantDto,ProductVariant>().ReverseMap();
        CreateMap<AddBranchOfficeStockDto, BranchOfficeStock>().ReverseMap();
        CreateMap<AddCategoryDto, Category>().ReverseMap();
        /*
          dest => dest.SomeDestinationProperty,
        opt => opt.MapFrom(src => src.SomeSourceProperty)*/
    }
}