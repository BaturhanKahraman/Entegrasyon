using System.Linq.Expressions;
using System.Security.Claims;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.User;
using Shared.Entity;
using Shared.Results;
using Entegrasyon.Business.Extensions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Logic;
using Shared.Extensions;
using Entegrasyon.Business.Abstract;
using MapsterMapper;

namespace Entegrasyon.Business.Concrete.Auth;

public class ApplicationUserManager(
    IMapper mapper,
    IApplicationLogManager applicationLogManager,
    Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
    IFluentValidator validator,
    IntegrationDbContext context,
    ILogger<ApplicationUserManager> logger) : IApplicationUserManager
{
    public async Task<IResult> AddUser(AddUserDto dto, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Kullanıcı ekleniyor...", LogType.User, LogAction.Add, dto);
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult();

        var user = mapper.Map<AddUserDto, ApplicationUser>(dto);
        user.NeedsTakeNewPassword = true;
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.CreatedAt = DateTimeOffset.UtcNow;

        var logicResult = LogicRunner.Run(await CheckSameUserName(user.NormalizedUserName));
        if (logicResult != null)
            return logicResult;
        await context.Users.AddAsync(user, token);
        await context.SaveChangesAsync(token);

        await applicationLogManager.AddLog("Kullanıcı eklendi.", LogType.User, LogAction.Add);
        return new SuccessResult(Messages.UserAdded);
    }

    private async Task<IResult> CheckSameUserName(string normalizedUserName)
    {
        var exists = await context.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName);
        return exists ? new ErrorResult(Messages.UserSameUsername) : new SuccessResult();
    }

    public async Task<IResult> EditUser(UserEditDto dto, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Kullanıcı güncelleniyor...", LogType.User, LogAction.Update, dto);
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult();
        var dbUser = await context.Users.FindAsync(dto.Id);
        if (dbUser == null)
            return new ErrorResult(Messages.UserNotFound);
        dbUser.Email = dto.Email;
        dbUser.NormalizedEmail = dto.Email.ToUpperInvariant();
        dbUser.UserName = dto.UserName;
        dbUser.NormalizedUserName = dto.UserName.ToUpperInvariant();
        dbUser.DefaultBranchOfficeId = dto.BranchOfficeId;
        //if (dto.RoleIds.Count > 0)
        //    dbUser.Roles = dto.RoleIds.Select(r => new Role { Id = r }).ToList();
        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Concurrency Exception");
            return new ErrorResult("Bu kayıt güncellenmiş ve sizdeki versiyonu eski olabilir. Lütfen tekrar deneyin");
        }
        await applicationLogManager.AddLog("Kullanıcı güncellendi...", LogType.User, LogAction.Update, dto);
        return new SuccessResult(Messages.UserUpdated);
    }

    public async Task<IDataResult<Pageable<UserDetailListDto>>> GetPaginatedUserDetails(int pageIndex = 0, int itemCount = 50)
    {
        Expression<Func<ApplicationUser, UserDetailListDto>> selector = x => new UserDetailListDto(x.Id, x.Name, x.Surname, x.UserName,
            x.IsActive, x.IsTwoFactorAuthActive, x.NeedsTakeNewPassword, x.CreatedAt, x.DefaultBranchOffice.Name);
        var orders = new List<(string, string)>(1) { ("CreatedAt", "desc") };
        var result =
            await context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(selector)
                .ToPage(pageIndex, itemCount);
        return new SuccessDataResult<Pageable<UserDetailListDto>>(result);
    }

    public async Task<IDataResult<UserDetailDto>> GetUserDetails(Guid id)
    {
        var result = await context.Users.AsNoTracking().Select(u => new UserDetailDto(u.Id, u.Name, u.Surname, u.UserName, u.IsActive, u.IsTwoFactorAuthActive, u.NeedsTakeNewPassword, u.CreatedAt, u.DefaultBranchOffice.Name, u.Roles.FirstOrDefault().Name))
            .FirstOrDefaultAsync(u => u.Id
                                      == id);
        return new SuccessDataResult<UserDetailDto>(result);
    }

    public async Task<IDataResult<UserDetailDto>> GetUserDetails(string id)
    {
        var convertable = Guid.TryParse(id, out var guidId);
        if (!convertable)
            return new ErrorDataResult<UserDetailDto>(null, Messages.ProcessFailed);
        return await GetUserDetails(guidId);
    }
    public string GetActiveUserId() =>
       GetActiveUserGuidId().ToString();

    public Guid GetActiveUserGuidId() => Guid.Parse(httpContextAccessor.HttpContext!.User
        .FindFirst(x => x.Type == ClaimTypes.NameIdentifier)
        ?.Value!);


    public ValueTask<ApplicationUser> GetUserById(Guid id) => context.Users.FindAsync(id);

    public async Task<IResult> SetPassive(Guid userId, CancellationToken token = default)
    {
        var user = await GetUserById(userId);
        if (user is null)
        {
            return new ErrorResult(Messages.UserNotFound);
        }
        user.IsActive = false;
        //geçerli oturum sonlandırılacak.
        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Concurrency hatasi");
            return new ErrorResult("Bu kayıt güncellenmiş ve sizdeki versiyonu eski olabilir. Lütfen tekrar deneyin");
        }
        return new SuccessResult(Messages.ProcessSuccess);
    }

    public async Task<IResult> SoftDelete(Guid userId, CancellationToken token = default)
    {
        var user = await GetUserById(userId);
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.UserDeletedSuccessfuly);
    }

}
