using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Results;
using Shared.User.Services;

namespace Entegrasyon.Business.Concrete;

public class ApplicationUserManager
{
    private readonly IUserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationUserDal _applicationUserDal;
    public ApplicationUserManager(IUserManager<ApplicationUser> userManager,IMapper mapper,ApplicationLogManager applicationLogManager,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
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
        await _applicationLogManager.AddLog("Kullanıcı ekleniyor...",LogType.User,LogAction.Add,dto);
        var user = _mapper.Map<AddUserDto,ApplicationUser>(dto);
        user.NeedsTakeNewPassword = true;
        var result = await _userManager.CreateUserAsync(user);
        await _applicationLogManager.AddLog("Kullanıcı eklendi.",LogType.User,LogAction.Add);
        return result;
    }

    public async Task<IResult> EditUser(UserEditDto dto)
    {
        await _applicationLogManager.AddLog("Kullanıcı güncelleniyor...",LogType.User,LogAction.Update,dto);
        var user = _mapper.Map<ApplicationUser>(dto);
        var result =await _userManager.UpdateUser(user);
        await _applicationLogManager.AddLog("Kullanıcı güncellendi...",LogType.User,LogAction.Update,dto);
        return result;
    }

    public async Task<IDataResult<List<ApplicationUser>>> GetUsers()
    {
        var users = await _userManager.GetUsers().ConfigureAwait(false);
        return new SuccessDataResult<List<ApplicationUser>>(users);
    }
    public async Task<IDataResult<Pageable<UserDetailListDto>>> GetPaginatedUserDetails(int pageIndex=0,int itemCount=50)
    {
        Expression<Func<ApplicationUser,UserDetailListDto>> selector = x => new UserDetailListDto(x.Id, x.Name, x.Surname, x.UserName,
            x.IsActive, x.IsTwoFactorAuthActive, x.NeedsTakeNewPassword, x.CreatedAt, x.DefaultBranchOffice.Name);
        var orders = new List<(string, string)>(1) { ("CreatedAt", "desc") };
        var result =await _applicationUserDal.GetPaginatedTransformedEntities(pageIndex,itemCount,selector,orders);
        return new SuccessDataResult<Pageable<UserDetailListDto>>(result);
    }
    public async Task<IDataResult<UserDetailDto>> GetUserDetails(string id)
    {
        var convertable =Guid.TryParse(id, out var guidId);
        if (!convertable)
            return new ErrorDataResult<UserDetailDto>(null, Messages.ProcessFailed);
        Expression<Func<ApplicationUser, bool>> expr = u => u.Id == guidId;
        var result = await _applicationUserDal.GetTransformedEntity(u => 
            new UserDetailDto(u.Id, u.Name, u.Surname, u.UserName, u.IsActive, u.IsTwoFactorAuthActive,
                u.NeedsTakeNewPassword,u.CreatedAt,u.DefaultBranchOffice.Name,u.Role.Name),expr);
        return new SuccessDataResult<UserDetailDto>(result);
    }
    public string GetActiveUserId() =>
       this.GetActiveUserGuidId().ToString();

    public Guid GetActiveUserGuidId() => Guid.Parse(_httpContextAccessor.HttpContext!.User
        .FindFirst(x => x.Type == ClaimTypes.NameIdentifier)
        ?.Value!);

    public async Task<IDataResult<int>> GetUserCount() => new SuccessDataResult<int>(await _applicationUserDal.GetCount());

    public Task<ApplicationUser> GetUserById(Guid id) => _userManager.GetUserAsync(u => u.Id == id,false);

    public async Task<IResult> SetPassive(Guid userId)
    {
        var user = await GetUserById(userId);
        user.IsActive = false;
        await _userManager.UpdateUser(user);
        return new SuccessResult(Messages.ProcessSuccess);
    }

    public async Task<IResult> SoftDelete(Guid userId)
    {
        if(await _applicationUserDal.GetCount() == 1)
        {
            return new ErrorResult(Messages.NoUserLeft);
        }
        var user = await GetUserById(userId);
        await _applicationUserDal.SoftDeleteAsync(user);
        return new SuccessResult(Messages.UserDeletedSuccessfuly);
    }

    public async Task<IDataResult<List<UserDetailListDto>>> GetUserDetailList()
    {
        var data =await _applicationUserDal.GetTransformedEntitiesAsync(
            u=>new UserDetailListDto(u.Id,u.Name,u.Surname,u.UserName,u.IsActive,u.IsTwoFactorAuthActive,u.NeedsTakeNewPassword,u.CreatedAt,u.DefaultBranchOffice.Name),
            new []{("CreatedAt","desc")}
            );
        return new SuccessDataResult<List<UserDetailListDto>>(data);
    }
}