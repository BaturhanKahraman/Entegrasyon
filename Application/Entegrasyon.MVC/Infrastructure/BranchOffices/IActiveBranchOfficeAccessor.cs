// MVC projesinde Microsoft.AspNetCore.Http.IResult ile çakıştığı için tam nitelikli isim kullanılır
using IResult = Entegrasyon.Entity.Results.IResult;

namespace Entegrasyon.MVC.Infrastructure.BranchOffices;

/// <summary>
/// HTTP isteği boyunca kullanıcının "aktif şube ofisini" yöneten servis.
///
/// Aktif şube üç katmanlı bir kaynakta tutulur:
///   1. HttpContext.Session (primary, per-browser per-login)
///   2. User.LastSelectedBranchOfficeId + RememberLastBranchOffice (persistent "beni hatırla")
///   3. User.DefaultBranchOfficeId (birincil/atanmış ofis)
///   4. HQ (IsHeadquarters=true) — final fallback
///
/// Login sırasında ResolveAndStoreAsync çağrılır: öncelik sırasına göre bir ofis seçilip Session'a yazılır.
/// Kullanıcı ofis değiştirdiğinde SwitchAsync çağrılır: erişim kontrolü + Session + opsiyonel persist.
/// Her authenticated request'te ValidateAndResetIfStaleAsync çağrılır: stale ise sessizce HQ'ya reset.
/// </summary>
public interface IActiveBranchOfficeAccessor
{
    /// <summary>
    /// Current HTTP request için Session'da tutulan aktif şube id'sini döner.
    /// Session'da yoksa null. Login middleware pipeline sonrası çağrılır.
    /// </summary>
    int? GetActive();

    /// <summary>
    /// Kullanıcı için aktif şube ofisini öncelik sırasına göre bul ve Session'a yaz.
    /// Login sırasında AuthController tarafından çağrılır.
    /// Döndürülen değer: seçilen şube ofisinin Id'si (asla null değil — en kötü HQ döner).
    /// </summary>
    Task<int> ResolveAndStoreAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Kullanıcı aktif ofisini değiştirmek istiyor. Hedefe erişim doğrulanır
    /// (junction membership VEYA HQ VEYA DefaultBranchOffice).
    /// remember=true ise kalıcı tercih DB'ye yazılır (LastSelectedBranchOfficeId + RememberLastBranchOffice=true).
    /// </summary>
    Task<IResult> SwitchAsync(Guid userId, int branchOfficeId, bool remember, CancellationToken ct = default);

    /// <summary>
    /// Her authenticated request'te middleware tarafından çağrılır.
    /// Session'daki aktif şube hâlâ geçerli mi kontrol eder; değilse HQ'ya reset eder.
    /// Geçerlilik kriteri (kesin sözleşme):
    ///   (a) BranchOffice exists AND !IsDeleted, VE
    ///   (b) (IsHeadquarters=true) VEYA (junction membership) VEYA (DefaultBranchOfficeId == session value)
    /// Döndürülen: true = geçerli (değişiklik yok), false = stale → HQ'ya resetlendi.
    /// </summary>
    Task<bool> ValidateAndResetIfStaleAsync(Guid userId, CancellationToken ct = default);
}
