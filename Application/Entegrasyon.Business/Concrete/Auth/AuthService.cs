using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Utilities;

namespace Entegrasyon.Business.Concrete.Auth;

public class AuthService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogger) : IAuthService
{

    public async Task<IResult> LoginAsync(string userName,string password)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        string normalizedUsername = userName.ToUpperInvariant();
        var user = await context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.RoleClaims)
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

        bool isTruePass = HashingHelper.VerifyPasswordHash(password,user.PasswordHash!,user.PasswordSalt!);

        if(!isTruePass)
            return new ErrorResult(Messages.LoginFailedWrongPassword);

        return new SuccessDataResult<UserLoginSuccessDto>(new(user.Id,user.Name!,user.Surname!,user.UserName!,user.Roles));
    }
    public async Task<IResult> AssignTempPassword(string password,string userId,CancellationToken token = default)
    {
        bool isParsable = Guid.TryParse(userId,out var guidId);
        if(!isParsable)
            return new ErrorResult(Messages.ProcessFailed);
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(guidId);
        if(user == null)
            return new ErrorResult(Messages.ProcessFailed);
        user.NeedsTakeNewPassword = true;
        user.TemporaryPassword = password;
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.TemporaryPasswordAssigned);
    }

    public async Task<IResult> ChangeOwnPassword(Guid userId, ChangePasswordDto dto, CancellationToken token = default)
    {
        // 1. Validation
        if (dto.NewPassword != dto.ConfirmPassword)
            return new ErrorResult(Messages.PasswordsDoNotMatch);

        // 2. Business Rules — kullanici ve mevcut sifre kontrolu
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);

        bool isCurrentPasswordValid = HashingHelper.VerifyPasswordHash(dto.CurrentPassword, user.PasswordHash!, user.PasswordSalt!);
        if (!isCurrentPasswordValid)
            return new ErrorResult(Messages.CurrentPasswordWrong);

        // 3. Execution
        HashingHelper.CreatePasswordHash(dto.NewPassword, out var newHash, out var newSalt);
        user.PasswordHash = newHash;
        user.PasswordSalt = newSalt;
        user.NeedsTakeNewPassword = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        context.Update(user);
        await context.SaveChangesAsync(token);

        await applicationLogger.AddLog("Kullanici kendi sifresini degistirdi.", LogType.Auth, LogAction.Update, token: token);
        return new SuccessResult(Messages.PasswordChanged);
    }

    public async Task<IResult> CreatePassword(string password,Guid userId,CancellationToken token = default)
    {
        //password rules need to be applied here TODO
        await applicationLogger.AddLog("Sifre olusturma istegi geldi.",LogType.Auth,LogAction.Update);
        if(string.IsNullOrEmpty(password))
            return new ErrorResult(Messages.ProcessFailed);
        await using var context = await contextFactory.CreateDbContextAsync();
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
}
