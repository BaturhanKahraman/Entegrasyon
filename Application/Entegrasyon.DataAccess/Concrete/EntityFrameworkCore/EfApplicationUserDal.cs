using System.Linq.Expressions;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationUserDal:EfEntityRepository<ApplicationUser,IntegrationDbContext>,IApplicationUserDal
{
    private readonly IntegrationDbContext _context;
    public EfApplicationUserDal(IntegrationDbContext ctx) : base(ctx)
    {
        _context=ctx;
    }

    public async Task<List<UserDetailListDto>> GetPagedUserDetailList(Expression<Func<ApplicationUser,bool>> expr=null,int itemTakingNumber = 50,int page = 1)
    {
        var users = expr==null ? _context.Users : _context.Users.Where(expr);
            return await users
            .Include(x => x.DefaultBranchOffice)
            .Skip((page-1)*itemTakingNumber)
            .Take(itemTakingNumber)
            .OrderByDescending(x=>x.CreatedAt)
            .AsNoTracking()
            .Select(UserToUserDetail)
            .ToListAsync();
    }

    public Task<int> GetCount(Expression<Func<ApplicationUser,bool>> expr = null) => 
        expr == null ?_context.Users.CountAsync() : _context.Users.CountAsync(expr);

    public async Task<List<UserDetailListDto>> GetUserDetailList(Expression<Func<ApplicationUser,bool>> expr = null)
    {
        var users = expr == null ? _context.Users : _context.Users.Where(expr);
        return await users.OrderByDescending(x=>x.CreatedAt).AsNoTracking()
            .Select(UserToUserDetail)
            .ToListAsync();
    }

    private static Expression<Func<ApplicationUser,UserDetailListDto>> UserToUserDetail=>(user) => new UserDetailListDto
    {
        CreatedAt = user.CreatedAt,
        DefaultOfficeName = user.DefaultBranchOffice.Name,
        Id = user.Id,
        IsActive = user.IsActive,
        IsTwoFactorAuthActive = user.IsTwoFactorAuthActive,
        Name = user.Name,
        UserName = user.UserName,
        Surname = user.Surname,
        NeedsTakeNewPassword = user.NeedsTakeNewPassword
    };
}