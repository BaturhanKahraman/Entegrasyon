using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontAuthManager
{
    Task<IDataResult<StorefrontCustomerAuth>> RegisterAsync(StorefrontRegisterDto dto);
    Task<IDataResult<StorefrontCustomerAuth>> LoginAsync(int tenantId, string email, string password);
    Task<IResult> ConfirmEmailAsync(int tenantId, string token);
    Task<IDataResult<string>> RequestPasswordResetAsync(int tenantId, string email);
    Task<IResult> ResetPasswordAsync(int tenantId, string token, string newPassword);
    Task<IResult> ChangePasswordAsync(int authId, string currentPassword, string newPassword);
    Task<IDataResult<StorefrontCustomerAuth>> GetAuthByCustomerIdAsync(int tenantId, int customerId);
    Task<IResult> UpdateProfileAsync(int customerId, StorefrontProfileDto dto);

    // Login History
    Task RecordLoginAttemptAsync(int authId, string? ipAddress, string? userAgent, bool isSuccessful, string? failureReason = null);
    Task<IDataResult<List<StorefrontLoginHistory>>> GetLoginHistoryAsync(int authId, int count = 20);

    // External Login (Social Login)
    Task<IDataResult<StorefrontCustomerAuth>> ExternalLoginAsync(int tenantId, string provider, string externalId, string email, string name, string surname);

    // KVKK Data Export
    Task<IDataResult<string>> ExportCustomerDataAsync(int tenantId, int customerId);

    // Two-Factor Authentication
    Task<IDataResult<string>> Enable2FAAsync(int authId);
    Task<IResult> Verify2FAAsync(int authId, string code);
    Task<IResult> Disable2FAAsync(int authId, string code);
    Task<IDataResult<List<string>>> GenerateRecoveryCodesAsync(int authId);
    Task<IResult> VerifyRecoveryCodeAsync(int authId, string code);
    Task<IDataResult<bool>> IsTwoFactorEnabledAsync(int authId);
}
