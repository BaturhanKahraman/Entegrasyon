using AutoMapper;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.MapperProfiles;

public class MapProfiles:Profile
{
    public MapProfiles()
    {
        CreateMap<AddUserDto, ApplicationUser>()
            .ForMember(dest=>dest.DefaultBranchOfficeId,opt=>opt.MapFrom(src=>src.BranchOfficeId))
            .ReverseMap();
        CreateMap<AddBrandDto, Brand>()
            .ReverseMap();
        CreateMap<AddCargoCompanyDto,CargoCompany>()
            .ReverseMap();
        CreateMap<AddCustomerDto,ApplicationCustomer>()
            .ReverseMap();
        /*
          dest => dest.SomeDestinationProperty,
        opt => opt.MapFrom(src => src.SomeSourceProperty)*/
    }
}