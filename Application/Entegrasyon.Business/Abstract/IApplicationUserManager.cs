using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;

namespace Entegrasyon.Business.Abstract;

public interface IApplicationUserManager
{
    Task<IResult> AddUser(AddUserDto dto, CancellationToken token = default);
    Task<IResult> EditUser(UserEditDto dto, CancellationToken token = default);
    Task<IDataResult<Pageable<UserDetailListDto>>> GetPaginatedUserDetails(int pageIndex = 0, int itemCount = 50);
    Task<IDataResult<UserDetailDto>> GetUserDetails(Guid id);
    Task<IDataResult<UserDetailDto>> GetUserDetails(string id);
    Task<IDataResult<UserEditDetailDto>> GetUserEditDetail(Guid id);
    string GetActiveUserId();
    Guid GetActiveUserGuidId();
    ValueTask<ApplicationUser> GetUserById(Guid id);
    Task<IResult> SetPassive(Guid userId, CancellationToken token = default);
    Task<IResult> ToggleActive(Guid userId, CancellationToken token = default);
    Task<IResult> SoftDelete(Guid userId, CancellationToken token = default);

    /// <summary>
    /// Yönetici, başka bir kullanıcının şifresini sıfırlar: geçici şifre atar, ilk girişte
    /// değiştirmeye zorlar ve güvenlik damgasını yeniler (kullanıcının aktif oturumu düşer).
    /// Üretilen geçici şifreyi döner.
    /// </summary>
    Task<IDataResult<string>> AdminResetPassword(Guid userId, CancellationToken token = default);

    Task<IResult> UpdateOwnProfile(Guid userId, UpdateProfileDto dto, CancellationToken token = default);

    /// <summary>
    /// Admin kullanıcı detay sayfası için canlı takip özeti: bağlantı durumu (online/offline),
    /// son görülme zamanı ve ürün ekleme/güncelleme/silme + satış sayıları.
    /// Salt-okuma; sayaçlar ApplicationLog (LogType.Product) ve Sales üzerinden türetilir.
    /// </summary>
    Task<IDataResult<UserActivitySummaryDto>> GetUserActivitySummary(Guid id, CancellationToken token = default);
}
