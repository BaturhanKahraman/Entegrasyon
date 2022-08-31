using Entegrasyon.Entity.Users;
using Shared.Abstract;

namespace Entegrasyon.DataAccess.Abstract;

public interface IApplicationUserDal : IEntityRepository<ApplicationUser>
{
}