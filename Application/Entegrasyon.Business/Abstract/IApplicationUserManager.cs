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
    Task<IResult> UpdateOwnProfile(Guid userId, UpdateProfileDto dto, CancellationToken token = default);
}
