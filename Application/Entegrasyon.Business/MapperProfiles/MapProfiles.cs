using AutoMapper;
using Entegrasyon.Entity;
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
        CreateMap<AddCustomerDto,ApplicationCustomer>()
            .ReverseMap();
        CreateMap<ApplicationCustomer,CustomerDetailDto>()
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