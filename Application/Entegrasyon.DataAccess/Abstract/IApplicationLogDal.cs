using Entegrasyon.Entity.Logs;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface IApplicationLogDal:IEntityRepository<ApplicationLog>
{
    
}