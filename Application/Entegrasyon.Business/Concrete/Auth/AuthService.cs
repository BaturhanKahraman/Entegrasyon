using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Results;
using Shared.Security;
using Shared.User.Dto;

namespace Entegrasyon.Business.Concrete.Auth;

public class AuthService(
    IntegrationDbContext context,
    IApplicationLogManager applicationLogger) : IAuthService
{

    public async Task<IResult> LoginAsync(string userName,string password)
    {
        string normalizedUsername = userName.ToUpperInvariant();
        var user = await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => string.Equals(u.NormalizedUserName,normalizedUsername));
        if(user is null)
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        if(user.IsActive == false)
            return new ErrorResult(Messages.UserIsInactive);
        if(user.NeedsTakeNewPassword)
        {
            bool isTempPassword = user.TemporaryPassword == password;
            if(isTempPassword)
            {
                return new SuccessDataResult<LoginNewPasswordDto>(new LoginNewPasswordDto(true,user.Id.ToString()));
            }
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        }

        bool isTruePass = HashingHelper.VerifyPasswordHash(password,user.PasswordHash,user.PasswordSalt);

        if(!isTruePass)
            return new ErrorResult(Messages.LoginFailedWrongPassword);

        return new SuccessDataResult<UserLoginSuccessDto>(new(user.Id,user.Name,user.Surname,user.UserName,user.Roles));
    }
    public async Task<IResult> AssignTempPassword(string password,string userId,CancellationToken token = default)
    {
        bool isParsable = Guid.TryParse(userId,out var guidId);
        if(!isParsable)
            return new ErrorResult(Messages.ProcessFailed);
        var user = await context.Users.FindAsync(guidId);
        if(user == null)
            return new ErrorResult(Messages.ProcessFailed);
        user.NeedsTakeNewPassword = true;
        user.TemporaryPassword = password;
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.TemporaryPasswordAssigned);
    }

    public async Task<IResult> CreatePassword(string password,Guid userId,CancellationToken token = default)
    {
        //password rules need to be applied here TODO
        await applicationLogger.AddLog("Şifre oluşturma isteği geldi.",LogType.Auth,LogAction.Update);
        if(string.IsNullOrEmpty(password))
            return new ErrorResult(Messages.ProcessFailed);
        var user = await context.Users.FindAsync([userId],cancellationToken: token);
        if(user is null)
            return new ErrorResult(Messages.UserNotFound);
        HashingHelper.CreatePasswordHash(password,out var userPasswordHash,out var userPasswordSalt);
        user.PasswordHash = userPasswordHash;
        user.PasswordSalt = userPasswordSalt;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.NeedsTakeNewPassword = false;
        context.Update(user);
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.FirstPasswordAssigned);
    }

    //public async Task<IResult> LogOut(string userId)
    //{
    //    var user = await _userManager.GetUserAsync(x => x.Id == Guid.Parse(userId), false);
    //    if (_httpContext.IsMobileDevice())
    //    {
    //        user.MobileJwtToken = string.Empty;
    //        user.MobileJwtTokenExpiresAt = DateTimeOffset.MinValue;
    //    }
    //    else
    //    {
    //        user.WebJwtToken = string.Empty;
    //        user.WebJwtTokenExpiresAt = DateTimeOffset.MinValue;
    //    }

    //    await _userManager.UpdateUser(user);
    //    return new SuccessResult(Messages.LogOut);
    //}
}
