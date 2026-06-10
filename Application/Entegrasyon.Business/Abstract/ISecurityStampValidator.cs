namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Oturum geçersizleştirme doğrulayıcısı. Cookie'deki SecurityStamp + kullanıcı durumunun
/// DB ile tutarlı olup olmadığını kontrol eder. Pasif/silinmiş/şifresi-sıfırlanmış kullanıcının
/// aktif oturumu bir sonraki istekte düşürülür (auto-logout).
/// </summary>
public interface ISecurityStampValidator
{
    /// <summary>
    /// Kullanıcı hâlâ aktif (IsActive &amp;&amp; !IsDeleted) ve cookie'deki damga DB'deki ile eşleşiyorsa true.
    /// Aksi halde (kullanıcı yok, pasif, silinmiş, damga değişmiş) false.
    /// </summary>
    Task<bool> IsValidAsync(Guid userId, string? securityStamp, CancellationToken token = default);
}
