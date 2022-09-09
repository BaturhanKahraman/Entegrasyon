using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Http;
using Shared.Extensions;
using Shared.Results;
using Shared.User.Services;

namespace Entegrasyon.Business.Concrete;

public class ApplicationUserManager
{
    private readonly IUserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationUserDal _applicationUserDal;
    public ApplicationUserManager(IUserManager<ApplicationUser> userManager,IMapper mapper,ApplicationLogManager applicationLogManager,
        IHttpContextAccessor httpContextAccessor,
        IApplicationUserDal applicationUserDal)
    {
        _userManager = userManager;
        _mapper = mapper;
        _applicationLogManager = applicationLogManager;
        _httpContextAccessor = httpContextAccessor;
        _applicationUserDal = applicationUserDal;
    }

    public async Task<IResult> AddUser(AddUserDto dto)
    {
        await _applicationLogManager.AddLog("Kullanıcı ekleniyor...",LogType.User,LogAction.Add);
        var user = _mapper.Map<AddUserDto,ApplicationUser>(dto);
        user.NeedsTakeNewPassword = true;
        var result = await _userManager.CreateUserAsync(user);
        await _applicationLogManager.AddLog("Kullanıcı eklendi.",LogType.User,LogAction.Add);
        return result;
    }

    public async Task<IDataResult<List<ApplicationUser>>> GetUsers()
    {
        var users = await _userManager.GetUsers();
        return new SuccessDataResult<List<ApplicationUser>>(users);
    }
    public async Task<IDataResult<List<UserDetailListDto>>> GetPaginatedUserDetails(Expression<Func<ApplicationUser,bool>> expression=null,int page=1,int itemCount=50)
    {
        var result =await _applicationUserDal.GetPagedUserDetailList(expression, itemCount, page);
        return new SuccessDataResult<List<UserDetailListDto>>(result);
    }
    public async Task<IDataResult<List<UserDetailListDto>>> GetUserDetails(Expression<Func<ApplicationUser,bool>> expression = null)
    {
        var result = await _applicationUserDal.GetUserDetailList(expression);
        return new SuccessDataResult<List<UserDetailListDto>>(result);
    }
    public string GetActiveUserId() =>
        _httpContextAccessor.HttpContext.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

    public Guid GetActiveUserGuidId() => Guid.Parse(_httpContextAccessor.HttpContext.User
        .FindFirst(x => x.Type == ClaimTypes.NameIdentifier)
        ?.Value!);

    public async Task<IDataResult<int>> GetUserCount() => new SuccessDataResult<int>(await _applicationUserDal.GetCount());
}