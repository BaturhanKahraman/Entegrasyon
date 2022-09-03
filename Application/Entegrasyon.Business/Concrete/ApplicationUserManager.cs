using AutoMapper;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;
using Shared.Results;
using Shared.User.Services;

namespace Entegrasyon.Business.Concrete;

public class ApplicationUserManager
{
    private readonly IUserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    public ApplicationUserManager(IUserManager<ApplicationUser> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<IResult> AddUser(AddUserDto dto)
    {
        var user =_mapper.Map<AddUserDto,ApplicationUser>(dto);
        user.NeedsTakeNewPassword = true;
        var result =await _userManager.CreateUserAsync(user);
        return result;
    }
}