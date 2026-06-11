namespace Entegrasyon.Entity.Dtos.Auth;

/// <summary>
/// İlk-giriş zorunlu şifre belirleme isteği. Knowledge-proof için kullanıcının
/// kendisine atanan geçici şifreyi yeniden girmesi gerekir (IDOR koruması).
/// </summary>
public sealed record SetInitialPasswordDto(
    Guid UserId,
    string TemporaryPassword,
    string NewPassword,
    string ConfirmPassword);
