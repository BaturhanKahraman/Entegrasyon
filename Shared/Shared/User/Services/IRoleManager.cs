using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Shared.User.Services;

public interface IRoleManager<TRole, TClaim>
        where TRole : RootRole
        where TClaim : RootClaim

{
    Task AddRole(TRole role,List<TClaim> claim);
    Task AddRole(TRole role);
    Task AddRole(TRole role,List<int> claims);
    Task DeleteRole(int id);
    Task<TRole> GetRoleByNameAsync(string roleName);
    Task<List<TClaim>> GetRoleClaims(Expression<Func<TClaim,bool>> func = null);
    Task<IEnumerable<TRole>> GetRoles();
    Task UpdateRole(TRole role,List<TClaim> claims);
    Task UpdateRole(int roleId,string name,List<int> claimIds);
}