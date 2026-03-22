namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Amazon SP-API OAuth 2.0 / LWA token yöneticisi.
/// Refresh token ile access token alır, cache'ler ve auto-refresh yapar.
/// Singleton olarak register edilmeli — token cache tüm scope'lar arasında paylaşılır.
/// </summary>
public interface IAmazonTokenManager
{
    /// <summary>
    /// Geçerli access token döner. Cache'te varsa cache'ten, yoksa refresh yapar.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Cache'teki token'ı geçersiz kılar. 401 aldığında çağrılmalı.
    /// </summary>
    void InvalidateToken();
}
