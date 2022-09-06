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
            .AsNoTracking()
            .OrderByDescending(x=>x.CreatedAt)
            .Select(u => 
                new UserDetailListDto()
                {
                    CreatedAt = u.CreatedAt,
                    DefaultOfficeName = u.DefaultBranchOffice.Name,
                    Id = u.Id,IsActive = u.IsActive,
                    IsTwoFactorAuthActive = u.IsTwoFactorAuthActive,
                    Name = u.Name,
                    UserName = u.UserName,
                    Surname = u.Surname,
                    NeedsTakeNewPassword = u.NeedsTakeNewPassword
                })
            .ToListAsync();
    }

    public Task<int> GetCount(Expression<Func<ApplicationUser,bool>> expr = null) => 
        expr == null ?_context.Users.CountAsync() : _context.Users.CountAsync(expr);

}