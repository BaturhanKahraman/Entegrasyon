using System.Security.Claims;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.User;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Extensions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Requests;
using MapsterMapper;

namespace Entegrasyon.Business.Concrete.Auth;

public class ApplicationUserManager(
    IMapper mapper,
    IApplicationLogManager applicationLogManager,
    Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
    IFluentValidator validator,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<ApplicationUserManager> logger) : IApplicationUserManager
{
    public async Task<IResult> AddUser(AddUserDto dto, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Kullanici ekleniyor...", LogType.User, LogAction.Add, dto, token);
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult();

        await using var context = await contextFactory.CreateDbContextAsync();
        var user = mapper.Map<AddUserDto, ApplicationUser>(dto);
        user.NeedsTakeNewPassword = true;
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.CreatedAt = DateTimeOffset.UtcNow;

        var logicResult = LogicRunner.Run(await CheckSameUserName(context, user.NormalizedUserName));
        if (logicResult != null)
            return logicResult;
        await context.Users.AddAsync(user, token);
        await context.SaveChangesAsync(token);

        await applicationLogManager.AddLog("Kullanici eklendi.", LogType.User, LogAction.Add, token: token);
        return new SuccessResult(Messages.UserAdded);
    }

    private static async Task<IResult> CheckSameUserName(IntegrationDbContext context, string normalizedUserName)
    {
        var exists = await context.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName);
        return exists ? new ErrorResult(Messages.UserSameUsername) : new SuccessResult();
    }

    public async Task<IResult> EditUser(UserEditDto dto, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Kullanici guncelleniyor...", LogType.User, LogAction.Update, dto, token);
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult();
        await using var context = await contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users.FindAsync(dto.Id);
        if (dbUser == null)
            return new ErrorResult(Messages.UserNotFound);
        dbUser.Email = dto.Email;
        dbUser.NormalizedEmail = dto.Email.ToUpperInvariant();
        dbUser.UserName = dto.UserName;
        dbUser.NormalizedUserName = dto.UserName.ToUpperInvariant();
        dbUser.DefaultBranchOfficeId = dto.BranchOfficeId;
        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Concurrency Exception");
            return new ErrorResult("Bu kayit guncellenmis ve sizdeki versiyonu eski olabilir. Lutfen tekrar deneyin");
        }
        await applicationLogManager.AddLog("Kullanici guncellendi...", LogType.User, LogAction.Update, dto, token);
        return new SuccessResult(Messages.UserUpdated);
    }

    public Task<IDataResult<Pageable<UserDetailListDto>>> GetPaginatedUserDetails(int pageIndex = 0, int itemCount = 50)
        => GetPaginatedUserDetails(new UserPaginatedRequest() { PageIndex = pageIndex, PageSize = itemCount });

    public async Task<IDataResult<Pageable<UserDetailListDto>>> GetPaginatedUserDetails(UserPaginatedRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var result = await context.Users.AsNoTracking().ApplyGlobalSearch(request.SearchTerm,
            nameof(ApplicationUser.FullName),
            nameof(ApplicationUser.NormalizedUserName),
            nameof(ApplicationUser.NormalizedEmail))
            .Select(u=>
                new UserDetailListDto(u.Id,
                    u.Name,
                    u.Surname,
                    u.UserName,
                    u.IsActive,
                    u.IsTwoFactorAuthActive,
                    u.NeedsTakeNewPassword,
                    u.CreatedAt,
                    u.DefaultBranchOffice != null ? u.DefaultBranchOffice.Name : ""
                    ))
            .ToPageableAsync(request);

        return new SuccessDataResult<Pageable<UserDetailListDto>>(result);
    }

    public async Task<IDataResult<UserDetailDto>> GetUserDetails(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var result = await context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDto(
                u.Id, u.Name, u.Surname, u.UserName,
                u.IsActive, u.IsTwoFactorAuthActive, u.NeedsTakeNewPassword, u.CreatedAt,
                u.DefaultBranchOffice != null ? u.DefaultBranchOffice.Name : "",
                u.Roles.Select(r => r.Name).FirstOrDefault() ?? ""))
            .FirstOrDefaultAsync();
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


    public async ValueTask<ApplicationUser> GetUserById(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Users.FindAsync(id);
    }

    public async Task<IResult> SetPassive(Guid userId, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user is null)
        {
            return new ErrorResult(Messages.UserNotFound);
        }
        user.IsActive = false;
        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Concurrency hatasi");
            return new ErrorResult("Bu kayit guncellenmis ve sizdeki versiyonu eski olabilir. Lutfen tekrar deneyin");
        }
        return new SuccessResult(Messages.ProcessSuccess);
    }

    public async Task<IResult> SoftDelete(Guid userId, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.UserDeletedSuccessfuly);
    }

}
