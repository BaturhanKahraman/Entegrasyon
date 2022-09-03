using AutoMapper;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;

namespace Entegrasyon.Business.MapperProfiles;

public class UserProfile:Profile
{
    public UserProfile()
    {
        CreateMap<AddUserDto, ApplicationUser>();
    }
}