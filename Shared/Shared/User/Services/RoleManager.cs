using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Shared.User.Services;

public class RoleManager<TRole, TClaim, TContext> : IRoleManager<TRole,TClaim>
    where TRole : RootRole
    where TContext : DbContext
    where TClaim : RootClaim
{
    private readonly TContext _context;

    public RoleManager(TContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<TRole>> GetRoles()=>
        await _context.Set<TRole>().Include(x=>x.Claims).AsNoTracking().ToListAsync();
    
    public async Task<TRole> GetRoleByNameAsync(string roleName)=>
        await _context.Set<TRole>().FirstOrDefaultAsync(x => x.Name == roleName);
    
    public async Task<List<TClaim>> GetRoleClaims(Expression<Func<TClaim,bool>> func=null)
    {
        var claims = func == null ? _context.Set<TClaim>() : _context.Set<TClaim>().Where(func);
        return await claims.AsNoTracking().ToListAsync();
    }

    public async Task UpdateRole(TRole role,List<TClaim> claims)
    {
        if(role.Claims == null || !role.Claims.Any())
            role.Claims = claims as List<RootClaim>;
        _context.Set<TRole>().Update(role);
        await _context.SaveChangesAsync();
    }
    
    public async Task AddRole(TRole role,List<TClaim> claims)
    {
        if(role.Claims == null || !role.Claims.Any())
            role.Claims = claims as List<RootClaim>;
        await AddRole(role);
    }
    public async Task AddRole(TRole role)
    {
        await _context.Set<TRole>().AddAsync(role);
        await _context.SaveChangesAsync();
    }
}
