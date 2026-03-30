using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public async Task<IResult> LoginAsync(string userName, string password)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        string normalizedUsername = userName.ToUpperInvariant();
        var user = await context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.RoleClaims)
            .FirstOrDefaultAsync(u => string.Equals(u.NormalizedUserName, normalizedUsername));

        if (user is null)
            return new ErrorResult(Messages.LoginFailedWrongPassword);

        if (!user.IsActive)
            return new ErrorResult(Messages.UserIsInactive);

        // Check lockout
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            var remaining = (int)Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes);
            return new ErrorResult($"Hesabınız {remaining} dakika kilitli.");
        }

        if (user.NeedsTakeNewPassword)
        {
            bool isTempPassword = user.TemporaryPassword == password;
            if (isTempPassword)
                return new SuccessDataResult<LoginNewPasswordDto>(new LoginNewPasswordDto(true, user.Id.ToString()));
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        }

        // Verify password (version-aware)
        bool isValidPassword = user.PasswordHashVersion == 1
            ? HashingHelper.VerifyBcryptHash(password, user.BcryptPasswordHash!)
            : HashingHelper.VerifyPasswordHash(password, user.PasswordHash!, user.PasswordSalt!);

        if (!isValidPassword)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes);
            await context.SaveChangesAsync();
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        }

        // Auto-migrate legacy HMACSHA512 → bcrypt
        if (user.PasswordHashVersion == 0)
        {
            user.BcryptPasswordHash = HashingHelper.CreateBcryptHash(password);
            user.PasswordHashVersion = 1;
            user.PasswordHash = null;
            user.PasswordSalt = null;
        }

        // Reset lockout on success
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        await context.SaveChangesAsync();

        // If 2FA is active, require second factor
        if (user.IsTwoFactorAuthActive)
            return new SuccessDataResult<TwoFactorRequiredDto>(new TwoFactorRequiredDto(user.Id),
                Messages.TwoFactorRequired);

        return new SuccessDataResult<UserLoginSuccessDto>(new(user.Id, user.Name!, user.Surname!, user.UserName!, user.Roles));
    }

    public async Task<IResult> AssignTempPassword(string password, string userId, CancellationToken token = default)
    {
        bool isParsable = Guid.TryParse(userId, out var guidId);
        if (!isParsable)
            return new ErrorResult(Messages.ProcessFailed);
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(guidId);
        if (user == null)
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

        bool isCurrentPasswordValid = user.PasswordHashVersion == 1
            ? HashingHelper.VerifyBcryptHash(dto.CurrentPassword, user.BcryptPasswordHash!)
            : HashingHelper.VerifyPasswordHash(dto.CurrentPassword, user.PasswordHash!, user.PasswordSalt!);

        if (!isCurrentPasswordValid)
            return new ErrorResult(Messages.CurrentPasswordWrong);

        // 3. Execution — always upgrade to bcrypt
        user.BcryptPasswordHash = HashingHelper.CreateBcryptHash(dto.NewPassword);
        user.PasswordHashVersion = 1;
        user.PasswordHash = null;
        user.PasswordSalt = null;
        user.NeedsTakeNewPassword = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        context.Update(user);
        await context.SaveChangesAsync(token);

        await applicationLogger.AddLog("Kullanici kendi sifresini degistirdi.", LogType.Auth, LogAction.Update, token: token);
        return new SuccessResult(Messages.PasswordChanged);
    }

    public async Task<IResult> CreatePassword(string password, Guid userId, CancellationToken token = default)
    {
        await applicationLogger.AddLog("Sifre olusturma istegi geldi.", LogType.Auth, LogAction.Update);
        if (string.IsNullOrEmpty(password))
            return new ErrorResult(Messages.ProcessFailed);
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);

        // Always use bcrypt for new passwords
        user.BcryptPasswordHash = HashingHelper.CreateBcryptHash(password);
        user.PasswordHashVersion = 1;
        user.PasswordHash = null;
        user.PasswordSalt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.NeedsTakeNewPassword = false;
        context.Update(user);
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.FirstPasswordAssigned);
    }

    // ──────────────────────────────────────────────────────────────
    // Password Reset
    // ──────────────────────────────────────────────────────────────

    public async Task<IResult> RequestPasswordResetAsync(string email, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, token);

        // Always return success — do not leak whether email exists
        if (user is null)
            return new SuccessResult(Messages.PasswordResetRequested);

        user.PasswordResetToken = GenerateToken();
        user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        await context.SaveChangesAsync(token);

        await applicationLogger.AddLog("Sifre sifirlama talep edildi.", LogType.Auth, LogAction.Update, token: token);
        return new SuccessResult(Messages.PasswordResetRequested);
    }

    public async Task<IResult> ResetPasswordAsync(string resetToken, string newPassword, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == resetToken, token);

        if (user is null)
            return new ErrorResult(Messages.PasswordResetTokenInvalid);

        if (user.PasswordResetTokenExpiresAt.HasValue &&
            user.PasswordResetTokenExpiresAt.Value < DateTimeOffset.UtcNow)
            return new ErrorResult(Messages.PasswordResetTokenExpired);

        // Hash with bcrypt and clear lockout
        user.BcryptPasswordHash = HashingHelper.CreateBcryptHash(newPassword);
        user.PasswordHashVersion = 1;
        user.PasswordHash = null;
        user.PasswordSalt = null;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(token);
        await applicationLogger.AddLog("Sifre sifirlama tamamlandi.", LogType.Auth, LogAction.Update, token: token);
        return new SuccessResult(Messages.PasswordResetSuccess);
    }

    // ──────────────────────────────────────────────────────────────
    // 2FA TOTP
    // ──────────────────────────────────────────────────────────────

    public async Task<IDataResult<TwoFactorSetupDto>> SetupTwoFactorAsync(Guid userId, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorDataResult<TwoFactorSetupDto>(null!, Messages.UserNotFound);

        var secret = OtpNet.Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        user.TwoFactorSecret = secret;
        await context.SaveChangesAsync(token);

        var qrUri = $"otpauth://totp/Entegrasyon:{Uri.EscapeDataString(user.Email ?? user.UserName ?? userId.ToString())}?secret={secret}&issuer=Entegrasyon&digits=6&period=30";
        var dto = new TwoFactorSetupDto(secret, qrUri, secret);
        return new SuccessDataResult<TwoFactorSetupDto>(dto, Messages.TwoFactorSetupSuccess);
    }

    public async Task<IResult> EnableTwoFactorAsync(Guid userId, string verificationCode, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return new ErrorResult(Messages.TwoFactorNotConfigured);

        if (!VerifyTotp(user.TwoFactorSecret, verificationCode))
            return new ErrorResult(Messages.TwoFactorInvalidCode);

        user.IsTwoFactorAuthActive = true;
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.TwoFactorEnabled);
    }

    public async Task<IResult> DisableTwoFactorAsync(Guid userId, string verificationCode, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return new ErrorResult(Messages.TwoFactorNotConfigured);

        if (!VerifyTotp(user.TwoFactorSecret, verificationCode))
            return new ErrorResult(Messages.TwoFactorInvalidCode);

        user.IsTwoFactorAuthActive = false;
        user.TwoFactorSecret = null;
        user.TwoFactorRecoveryCodes = null;
        await context.SaveChangesAsync(token);
        return new SuccessResult(Messages.TwoFactorDisabled);
    }

    public async Task<IDataResult<List<string>>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorDataResult<List<string>>(null!, Messages.UserNotFound);

        var plainCodes = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var code = $"{RandomNumberGenerator.GetInt32(100000, 999999)}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
            plainCodes.Add(code);
        }

        // Store hashed codes as JSON
        var hashedCodes = plainCodes
            .Select(c => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(c))))
            .ToList();
        user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashedCodes);
        await context.SaveChangesAsync(token);

        return new SuccessDataResult<List<string>>(plainCodes, Messages.RecoveryCodesGenerated);
    }

    public async Task<IResult> VerifyTwoFactorAsync(Guid userId, string code, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorResult(Messages.TwoFactorInvalidCode);

        await using var context = await contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);

        if (!user.IsTwoFactorAuthActive)
            return new ErrorResult(Messages.TwoFactorNotConfigured);

        // Try TOTP first
        if (!string.IsNullOrEmpty(user.TwoFactorSecret) && VerifyTotp(user.TwoFactorSecret, code))
            return new SuccessResult(Messages.ProcessSuccess);

        // Try recovery codes
        if (!string.IsNullOrEmpty(user.TwoFactorRecoveryCodes))
        {
            var hashedCodes = JsonSerializer.Deserialize<List<string>>(user.TwoFactorRecoveryCodes) ?? [];
            var hashedInput = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));
            if (hashedCodes.Contains(hashedInput))
            {
                // Remove used code
                hashedCodes.Remove(hashedInput);
                user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashedCodes);
                await context.SaveChangesAsync(token);
                return new SuccessResult(Messages.RecoveryCodeUsed);
            }
        }

        return new ErrorResult(Messages.TwoFactorInvalidCode);
    }

    // ──────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────

    private static bool VerifyTotp(string secret, string code)
    {
        var secretBytes = OtpNet.Base32Encoding.ToBytes(secret);
        var totp = new OtpNet.Totp(secretBytes, step: 30, totpSize: 6);
        return totp.VerifyTotp(code.Trim(), out _, new OtpNet.VerificationWindow(previous: 1, future: 1));
    }

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
