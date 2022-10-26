using AutoMapper;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Product;
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
        CreateMap<AddBrandDto, Brand>()
            .ReverseMap();
        CreateMap<AddCargoCompanyDto,CargoCompany>()
            .ReverseMap();
        CreateMap<AddCustomerDto, RetailCustomer>()
            .ForMember(x => x.Name, opt => opt.MapFrom(x => x.NameOrCorporateName))
            .ForMember(x => x.NationalIdentity, opt => opt.MapFrom(x => x.NationalIdentityOrTaxNumber))
            .ForMember(x => x.Discriminator, opt => opt.MapFrom(x => x.Type));

        CreateMap<AddCustomerDto, CorporateCustomer>()
            .ForMember(x => x.CorporateName, opt => opt.MapFrom(x => x.NameOrCorporateName))
            .ForMember(x => x.TaxNumber, opt => opt.MapFrom(x => x.NationalIdentityOrTaxNumber))
            .ForMember(x => x.Discriminator, opt => opt.MapFrom(x => x.Type));

        CreateMap<Customer,CustomerDetailDto>()
            .ForMember(dest => dest.SalesCount,opt => opt.MapFrom(src => src.Sales.Count))
            .ReverseMap();
        CreateMap<AddProductDto,MainProduct>().ReverseMap();
        CreateMap<AddProductVariantDto,ProductVariant>().ReverseMap();
        CreateMap<AddBranchOfficeStockDto, BranchOfficeStock>().ReverseMap();

        /*
          dest => dest.SomeDestinationProperty,
        opt => opt.MapFrom(src => src.SomeSourceProperty)*/
    }
}