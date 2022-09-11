using AutoMapper;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;

namespace Entegrasyon.Business.MapperProfiles;

public class UserProfile:Profile
{
    public UserProfile()
    {
        CreateMap<AddUserDto, ApplicationUser>().ForMember(dest=>dest.DefaultBranchOfficeId,opt=>opt.MapFrom(src=>src.BranchOfficeId)).ReverseMap();
        /*
          dest => dest.SomeDestinationProperty,
        opt => opt.MapFrom(src => src.SomeSourceProperty)*/
    }
}