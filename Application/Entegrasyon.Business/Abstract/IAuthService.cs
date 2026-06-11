using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract
{
    public interface IAuthService
    {
        Task<IResult> AssignTempPassword(string password, string userId, CancellationToken token = default);
        Task<IResult> ChangeOwnPassword(Guid userId, ChangePasswordDto dto, CancellationToken token = default);
        Task<IResult> CreatePassword(string password, Guid userId, CancellationToken token = default);

        /// <summary>
        /// İlk-giriş zorunlu şifre belirleme. Knowledge-proof: kullanıcı geçici şifresini
        /// yeniden girer; sunucu NeedsTakeNewPassword==true VE TemporaryPassword eşleşmesini
        /// doğrular (IDOR koruması), ancak o zaman yeni şifreyi kalıcılaştırır.
        /// </summary>
        Task<IResult> SetInitialPasswordAsync(SetInitialPasswordDto dto, CancellationToken token = default);

        Task<IResult> LoginAsync(string userName, string password);

        // Password Reset
        Task<IResult> RequestPasswordResetAsync(string email, CancellationToken token = default);
        Task<IResult> ResetPasswordAsync(string resetToken, string newPassword, CancellationToken token = default);

        // 2FA TOTP
        Task<IDataResult<TwoFactorSetupDto>> SetupTwoFactorAsync(Guid userId, CancellationToken token = default);
        Task<IResult> EnableTwoFactorAsync(Guid userId, string verificationCode, CancellationToken token = default);
        Task<IResult> DisableTwoFactorAsync(Guid userId, string verificationCode, CancellationToken token = default);
        Task<IDataResult<List<string>>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken token = default);
        Task<IResult> VerifyTwoFactorAsync(Guid userId, string code, CancellationToken token = default);
    }
}