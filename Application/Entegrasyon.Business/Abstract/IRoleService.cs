using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Shared.Results;

namespace Entegrasyon.Business.Concrete.Auth
{
    public interface IRoleService
    {
        Task<IResult> AddRole(AddRoleDto dto,CancellationToken token = default);
        Task<IResult> DeleteRole(int id,CancellationToken token = default);
        ValueTask<IEnumerable<Role>> GetRolesSelectList(CancellationToken token = default);
        Task<IResult> UpdateRole(EditRoleDto dto,CancellationToken token = default);
    }
}