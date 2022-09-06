using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;
using Shared;
using System.Linq.Expressions;

namespace Entegrasyon.DataAccess.Abstract;

public interface IApplicationUserDal:IEntityRepository<ApplicationUser>
{
    Task<List<UserDetailListDto>> GetPagedUserDetailList(Expression<Func<ApplicationUser,bool>> expr,int itemTakingNumber = 50,int page = 1);
    Task<int> GetCount(Expression<Func<ApplicationUser,bool>> expr = null);
}